using System;
using System.Collections.ObjectModel;
using ReactiveUI;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using OpenUtau.Core;
using System.Reactive;
using System.Diagnostics;
using OpenUtau.RVC.Utils;
using OpenUtau.RVC.Processing;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.OnnxRuntime;
using TorchSharp;

namespace OpenUtau.App.ViewModels {
    public class TrackModelSelection : ViewModelBase {
        public string TrackName { get; set; } = string.Empty;
        public string SelectedModel { get; set; } = string.Empty;
        public string SelectedIndexFile { get; set; } = string.Empty;
        public ObservableCollection<string> AvailableModels { get; set; } = new();
        public ObservableCollection<string> AvailableIndexFiles { get; set; } = new();
    }

    public class RvsynthViewModel : ViewModelBase {
        // Mode selection
        public ObservableCollection<string> ModeOptions { get; } = new() { "Single Model", "Multi Model" };
        private string _selectedMode = "Single Model";
        public string SelectedMode {
            get => _selectedMode;
            set {
                this.RaiseAndSetIfChanged(ref _selectedMode, value);
                this.RaisePropertyChanged(nameof(IsSingleModel));
                this.RaisePropertyChanged(nameof(IsMultiModel));
            }
        }

        public bool IsSingleModel => SelectedMode == "Single Model";
        public bool IsMultiModel => SelectedMode == "Multi Model";

        // Model selections
        public ObservableCollection<string> AvailableModels { get; } = new();
        public string SelectedModel { get; set; } = string.Empty;

        public ObservableCollection<string> AvailableIndexFiles { get; } = new();
        public string SelectedIndexFile { get; set; } = string.Empty;

        // Track selections for Multi-model option
        public ObservableCollection<TrackModelSelection> TrackModelSelections { get; } = new();

        // Sliders
        private double _pitch = 0.0;
        public double Pitch {
            get => _pitch;
            set => this.RaiseAndSetIfChanged(ref _pitch, value);
        }

        private double _indexRatio = 0.75;
        public double IndexRatio {
            get => _indexRatio;
            set => this.RaiseAndSetIfChanged(ref _indexRatio, value);
        }

        private int _filterRadius = 3;
        public int FilterRadius {
            get => _filterRadius;
            set => this.RaiseAndSetIfChanged(ref _filterRadius, value);
        }

        private int _resampleRate = 0;
        public int ResampleRate {
            get => _resampleRate;
            set => this.RaiseAndSetIfChanged(ref _resampleRate, value);
        }

        private double _rmsMixRate = 0.25;
        public double RmsMixRate {
            get => _rmsMixRate;
            set => this.RaiseAndSetIfChanged(ref _rmsMixRate, value);
        }

        private double _protectVoiceless = 0.33;
        public double ProtectVoiceless {
            get => _protectVoiceless;
            set => this.RaiseAndSetIfChanged(ref _protectVoiceless, value);
        }

        private double _featureRetrievalRatio = 0.83;
        public double FeatureRetrievalRatio {
            get => _featureRetrievalRatio;
            set => this.RaiseAndSetIfChanged(ref _featureRetrievalRatio, value);
        }

        private double _voiceEnvelopeMix = 1.00;
        public double VoiceEnvelopeMix {
            get => _voiceEnvelopeMix;
            set => this.RaiseAndSetIfChanged(ref _voiceEnvelopeMix, value);
        }

        // Commands (✅ FIXED: Added Default Values)
        public ReactiveCommand<Unit, Unit> ProcessCommand { get; }
        public ReactiveCommand<Unit, Unit> BackCommand { get; }

        public RvsynthViewModel() {
            // ✅ Initialize Commands to Prevent Null Errors
            ProcessCommand = ReactiveCommand.Create(() => { Debug.WriteLine("Processing..."); });
            BackCommand = ReactiveCommand.Create(() => { Debug.WriteLine("Going Back..."); });

            LoadAvailableModels();
            LoadAvailableIndexFiles();
            LoadTrackList();
        }

        private float[] RunInference(float[] inputAudio, string modelPath, string indexPath) {
            bool isOnnx = modelPath.EndsWith(".onnx");

            if (isOnnx) {
                var inputTensor = new DenseTensor<float>(inputAudio, new[] { 1, inputAudio.Length });
                var inputs = new NamedOnnxValue[] {
            NamedOnnxValue.CreateFromTensor("input", inputTensor)
        };

                using var onnxSession = new InferenceSession(modelPath);
                using var results = onnxSession.Run(inputs);
                var outputTensor = results.First().AsTensor<float>();
                return outputTensor.ToArray();
            } else {
                var torchModel = torch.jit.load(modelPath);
                using var inputTensor = torch.tensor(inputAudio, new long[] { 1, inputAudio.Length });

                torchModel.eval();
                var result = torchModel.call(inputTensor);

                if (result is torch.Tensor outputTensor) {
                    return outputTensor.data<float>().ToArray();
                } else {
                    Console.WriteLine("❌ Torch model returned unexpected output.");
                    return new float[0];
                }
            }
        }

        private void LoadAvailableModels() {
            var modelPath = Path.Combine(AppContext.BaseDirectory, "rvc", "model");
            if (Directory.Exists(modelPath)) {
                var models = Directory.GetFiles(modelPath, "*.*")
                    .Where(file => file.EndsWith(".pth") || file.EndsWith(".onnx"));
                AvailableModels.Clear();
                foreach (var model in models)
                    AvailableModels.Add(Path.GetFileName(model));
            }
        }

        private void LoadAvailableIndexFiles() {
            var indexPath = Path.Combine(AppContext.BaseDirectory, "rvc", "index");
            if (Directory.Exists(indexPath)) {
                var indexes = Directory.GetFiles(indexPath, "*.index");
                AvailableIndexFiles.Clear();
                foreach (var index in indexes)
                    AvailableIndexFiles.Add(Path.GetFileName(index));
            }
        }

        private void LoadTrackList() {
            TrackModelSelections.Clear();
            foreach (var track in DocManager.Inst.Project.tracks) {
                TrackModelSelections.Add(new TrackModelSelection {
                    TrackName = $"{track.TrackNo} - {track.Singer?.Name ?? "Unnamed Track"}",
                    AvailableModels = AvailableModels,
                    AvailableIndexFiles = AvailableIndexFiles
                });
            }
        }
    } // Closing class
} // Closing namespace
