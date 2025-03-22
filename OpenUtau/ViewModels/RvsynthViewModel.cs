using System;
using System.Collections.ObjectModel;
using ReactiveUI;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Diagnostics;
using OpenUtau.Core;
using OpenUtau.RVC.Processing;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.OnnxRuntime;
using TorchSharp;
using Avalonia.Controls.ApplicationLifetimes;
using System.Windows.Forms;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

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
        public ObservableCollection<string> ModeOptions { get; } = new() { "Select Mode", "Single Model", "Multi Model" };

        private string _selectedMode = "Select Mode";
        public string SelectedMode {
            get => _selectedMode;
            set {
                if (value != _selectedMode) {
                    try {
                        _selectedMode = value;
                        this.RaisePropertyChanged(nameof(SelectedMode));
                        this.RaisePropertyChanged(nameof(IsSingleModel));
                        this.RaisePropertyChanged(nameof(IsMultiModel));

                        // Ensure the mode selection is visible until the user picks a mode
                        IsModeSelectionVisible = _selectedMode == "Select Mode";
                        IsMainContentVisible = _selectedMode != "Select Mode";
                        IsBackButtonVisible = _selectedMode != "Select Mode";

                        // Debugging log
                        Debug.WriteLine($"🔄 Mode changed to: {_selectedMode}");

                        // Ensure UI elements are refreshed properly
                        LoadAvailableModels();
                        LoadAvailableIndexFiles();
                        LoadTrackList();
                    } catch (Exception ex) {
                        Debug.WriteLine($"❌ Error changing mode: {ex.Message}");
                    }
                }
            }
        }
        public bool IsSingleModel => SelectedMode == "Single Model";
        public bool IsMultiModel => SelectedMode == "Multi Model";

        private bool _isModeSelectionVisible = true;
        public bool IsModeSelectionVisible {
            get => _isModeSelectionVisible;
            set => this.RaiseAndSetIfChanged(ref _isModeSelectionVisible, value);
        }

        private bool _isMainContentVisible = false;
        public bool IsMainContentVisible {
            get => _isMainContentVisible;
            set => this.RaiseAndSetIfChanged(ref _isMainContentVisible, value);
        }

        private bool _isBackButtonVisible = false;
        public bool IsBackButtonVisible {
            get => _isBackButtonVisible;
            set => this.RaiseAndSetIfChanged(ref _isBackButtonVisible, value);
        }

       

        // Model selections
        public ObservableCollection<string> AvailableModels { get; } = new();
        private string _selectedModel = string.Empty;
        public string SelectedModel {
            get => _selectedModel;
            set => this.RaiseAndSetIfChanged(ref _selectedModel, value);
        }

        public ObservableCollection<string> AvailableIndexFiles { get; } = new();
        private string _selectedIndexFile = string.Empty;
        public string SelectedIndexFile {
            get => _selectedIndexFile;
            set => this.RaiseAndSetIfChanged(ref _selectedIndexFile, value);
        }

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
            ProcessCommand = ReactiveCommand.CreateFromTask(async () => {
                Debug.WriteLine("🟡 Process button clicked.");

                if (App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
                    desktop.MainWindow is Window mainWindow) {

                    // 📁 Use StorageProvider to open folder picker
                    var folders = await mainWindow.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions {
                        Title = "Select Export Folder",
                        AllowMultiple = false
                    });

                    var exportFolder = folders?.FirstOrDefault();
                    if (exportFolder == null) {
                        Debug.WriteLine("⚠️ Export folder selection cancelled.");
                        return;
                    }

                    string exportPath = exportFolder.Path.LocalPath;
                    Debug.WriteLine($"📁 Export path selected: {exportPath}");
                    Debug.WriteLine("✅ Processing started...");

                    // ✅ Close Rvsynth window
                    foreach (var window in desktop.Windows) {
                        if (window.DataContext == this) {
                            window.Close(); // ⬅️ This ensures only the active RvsynthView closes
                            break;
                        }
                    }

                    // ✅ Now begin your inference process — replace this with your actual call
                    // await RvcInferenceEngine.RunAsync(...); or whatever method you use

                } else {
                    Debug.WriteLine("❌ Could not access main window.");
                }
            });

            BackCommand = ReactiveCommand.Create(() => {
                Debug.WriteLine("🔙 Back button pressed, returning to mode selection...");

                // Reset mode selection
                SelectedMode = "Select Mode";

                // Restore visibility settings
                IsModeSelectionVisible = true;
                IsMainContentVisible = false;
                IsBackButtonVisible = false;
            });


            // ✅ Ensure everything is loaded properly
            LoadAvailableModels();
            LoadAvailableIndexFiles();
            LoadTrackList();
        }

        private void LoadAvailableModels() {
            try {
                AvailableModels.Clear();
                var modelPath = Path.Combine(AppContext.BaseDirectory, "rvc", "model");

                Debug.WriteLine($"🔍 Looking for models in: {modelPath}");

                if (Directory.Exists(modelPath)) {
                    var models = Directory.GetFiles(modelPath, "*.*")
                        .Where(file => file.EndsWith(".pth") || file.EndsWith(".onnx"));

                    foreach (var model in models) {
                        var fileName = Path.GetFileName(model);
                        Debug.WriteLine($"📦 Found model: {fileName}");
                        AvailableModels.Add(fileName);
                    }

                    Debug.WriteLine($"✅ Total models loaded: {AvailableModels.Count}");
                } else {
                    Debug.WriteLine("❌ Model directory does not exist.");
                }
            } catch (Exception ex) {
                Debug.WriteLine($"❌ Error loading models: {ex.Message}");
            }
        }


        private void LoadAvailableIndexFiles() {
            try {
                AvailableIndexFiles.Clear();
                var indexRoot = Path.Combine(AppContext.BaseDirectory, "rvc", "index");

                Debug.WriteLine($"🔍 Looking for index folders in: {indexRoot}");

                if (Directory.Exists(indexRoot)) {
                    var indexDirs = Directory.GetDirectories(indexRoot);

                    foreach (var dir in indexDirs) {
                        var folderName = Path.GetFileName(dir);
                        Debug.WriteLine($"📁 Found index folder: {folderName}");
                        AvailableIndexFiles.Add(folderName);
                    }

                    Debug.WriteLine($"✅ Total index folders loaded: {AvailableIndexFiles.Count}");
                } else {
                    Debug.WriteLine("❌ Index directory does not exist.");
                }
            } catch (Exception ex) {
                Debug.WriteLine($"❌ Error loading index folders: {ex.Message}");
            }
        }

        private void LoadTrackList() {
            try {
                TrackModelSelections.Clear();
                if (DocManager.Inst?.Project?.tracks != null) {
                    foreach (var track in DocManager.Inst.Project.tracks) {
                        TrackModelSelections.Add(new TrackModelSelection {
                            TrackName = $"{track.TrackNo} - {track.Singer?.Name ?? "Unnamed Track"}",
                            AvailableModels = AvailableModels,
                            AvailableIndexFiles = AvailableIndexFiles
                        });
                    }
                }
            } catch (Exception ex) {
                Debug.WriteLine($"❌ Error loading track list: {ex.Message}");
            }
        }
    } // Closing class
} // Closing namespace
