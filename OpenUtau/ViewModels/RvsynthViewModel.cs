using System;
using System.Collections.ObjectModel;
using ReactiveUI;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using OpenUtau.Core;
using System.Reactive;
using System.Diagnostics;

namespace OpenUtau.App.ViewModels {
    public class TrackModelSelection : ViewModelBase {
        public string TrackName { get; set; }
        public string SelectedModel { get; set; }
        public string SelectedIndexFile { get; set; }
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
        public string SelectedModel { get; set; }

        public ObservableCollection<string> AvailableIndexFiles { get; } = new();
        public string SelectedIndexFile { get; set; }

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

        // Commands
        public ReactiveCommand<Unit, Unit> ProcessCommand { get; }
        public ReactiveCommand<Unit, Unit> BackCommand { get; }

        public RvsynthViewModel() {
            LoadAvailableModels();
            LoadAvailableIndexFiles();
            LoadTrackList();
        }
public string ProcessAudio(string inputFilePath, string outputFolder) {
            try {
                if (!File.Exists(inputFilePath)) {
                    throw new FileNotFoundException($"Input file not found: {inputFilePath}");
                }

                string tempFile = Path.Combine(Path.GetTempPath(), "temp_resampled.wav");

                // 🔹 Step 1: Ensure input WAV is resampled to 44100 Hz
                WavUtils.ResampleTo44100Hz(inputFilePath, tempFile);

                // 🔹 Step 2: Prepare output file path
                string outputFilePath = Path.Combine(outputFolder, "rvc_output.wav");

                // 🔹 Step 3: Run RVC inference process
                RunInference(tempFile, outputFilePath);

                return outputFilePath;
            } catch (Exception ex) {
                Debug.WriteLine($"[RvcInferenceEngine] Error: {ex.Message}");
                return string.Empty;
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

