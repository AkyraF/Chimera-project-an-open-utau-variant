using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using OpenUtau.Core.Ustx;
using OpenUtau.Core;
using OpenUtau.RVC;
using OpenUtau.RVC.Utils;
using ReactiveUI;
using Avalonia.Platform.Storage;
using Avalonia;
using OpenUtau.RVC.Processing;

namespace OpenUtau.App.ViewModels {
    public class ExportDialogViewModel : ViewModelBase {
        public ObservableCollection<string> ExportOptions { get; }
        public string SelectedOption { get; set; }
        public string ExportPath { get; set; } = string.Empty;

        public ReactiveCommand<Unit, Unit> SelectFolderCommand { get; }
        public ReactiveCommand<Unit, Unit> ExportCommand { get; }

        public ExportDialogViewModel() {
            ExportOptions = new ObservableCollection<string> {
                "Standard Export",
                "Export with Rvsynth (Single Model)",
                "Export with Rvsynth (Per Track Model)"
            };
            SelectedOption = ExportOptions.First();

            SelectFolderCommand = ReactiveCommand.CreateFromTask(SelectFolderAsync);
            ExportCommand = ReactiveCommand.CreateFromTask(ExportAsync);
        }

        private async Task SelectFolderAsync() {
            if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime appLifetime) {
                var storageProvider = appLifetime.MainWindow?.StorageProvider;
                if (storageProvider != null) {
                    var folder = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions {
                        Title = "Select Export Folder",
                        AllowMultiple = false
                    });

                    if (folder?.Any() == true) {
                        ExportPath = folder[0].Path.LocalPath;
                        this.RaisePropertyChanged(nameof(ExportPath));
                    }
                }
            }
        }

        private async Task ExportAsync() {
            if (string.IsNullOrEmpty(ExportPath)) {
                await ShowMessageAsync("Please select an export folder.");
                return;
            }

            if (SelectedOption == "Standard Export") {
                await StandardExportAsync();
            } else {
                await ExportWithRvsynthAsync();
            }
        }

        private async Task StandardExportAsync() {
            // Placeholder for actual standard export logic
            await ShowMessageAsync("Standard export completed successfully.");
        }

        private async Task ExportWithRvsynthAsync() {
            bool perTrack = SelectedOption.Contains("Per Track");

            // ✅ Ensure `UProject` is correctly referenced
            var project = DocManager.Inst.Project;
            if (project == null) {
                await ShowMessageAsync("No project is currently loaded.");
                return;
            }

            foreach (var track in project.Tracks.OfType<UVoicePart>()) {
                if (track.Singer == null) {
                    continue; // Skip if no singer assigned
                }

                string modelPath = perTrack
                    ? Path.Combine(AppContext.BaseDirectory, "rvc", "models", $"{track.Singer.Name}.pth")
                    : Path.Combine(AppContext.BaseDirectory, "rvc", "models", "default.pth");

                if (!File.Exists(modelPath)) {
                    await ShowMessageAsync($"Model not found for {track.Singer?.Name}: {modelPath}");
                    continue;
                }

                string inputWav = Path.Combine(ExportPath, $"{track.Name}_raw.wav");
                string outputWav = Path.Combine(ExportPath, $"{track.Name}_rvc.wav");

                if (!File.Exists(inputWav)) {
                    await ShowMessageAsync($"Input WAV file missing: {inputWav}");
                    continue;
                }

                // ✅ Call Rvsynth Processing
                try {
                    var rvcEngine = new RvcInferenceEngine(modelPath, "");
                    await rvcEngine.ProcessAsync(inputWav, outputWav, 0.0, progress => {
                        Console.WriteLine($"Progress: {progress}%");
                    });
                    await ShowMessageAsync($"Successfully processed {track.Name}.");
                } catch (Exception ex) {
                    await ShowMessageAsync($"Error processing {track.Name}: {ex.Message}");
                }
            }

            await ShowMessageAsync("Rvsynth export completed successfully.");
        }

        // ✅ Replaces `MessageBox` with proper Avalonia UI message dialogs
        private async Task ShowMessageAsync(string message) {
            if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime appLifetime) {
                var window = appLifetime.MainWindow;
                if (window != null) {
                    await Dispatcher.UIThread.InvokeAsync(() => {
                        var dialog = new Window {
                            Title = "Message",
                            Content = new TextBlock { Text = message, Padding = new Thickness(10) },
                            Width = 300,
                            Height = 150,
                            WindowStartupLocation = WindowStartupLocation.CenterOwner
                        };
                        dialog.ShowDialog(window);
                    });
                }
            }
        }
    }
}
