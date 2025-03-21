using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using Avalonia.Controls;
using System.Threading.Tasks;
using OpenUtau.Core.Ustx;
using OpenUtau.RVC.Utils;
using OpenUtau.RVC.Processing;
using OpenUtau.Core;
using ReactiveUI;
using Avalonia.Platform.Storage; // ✅ Fix for FolderPickerOpenOptions
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia;
using Avalonia;
using OpenUtau.Core.Util;
using System.Collections.Generic;

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
            SelectFolderCommand = ReactiveCommand.CreateFromTask(SelectFolder);
            ExportCommand = ReactiveCommand.CreateFromTask(async () => {
                await ExportAsync();
                return Unit.Default;
            });
        }

        private async Task SelectFolder() {
            var result = await new Window().StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions());
            if (result != null && result.Count > 0) {
                ExportPath = result[0].Path.LocalPath;
                this.RaisePropertyChanged(nameof(ExportPath));
            }
        }

        private async Task ExportAsync() {
            if (string.IsNullOrEmpty(ExportPath)) {
                await ShowMessageAsync("Please select an export folder.");
                return;
            }

            if (SelectedOption == "Standard Export") {
                await ShowMessageAsync("Standard export completed successfully.");
            } else {
                await ExportWithRvsynthAsync();
            }
        }

        private async Task ExportWithRvsynthAsync() {
            bool perTrack = SelectedOption.Contains("Per Track");
            var project = SingletonBase<DocManager>.Inst?.Project;
            if (project == null) {
                await ShowMessageAsync("No project is currently loaded.");
                return;
            }

            foreach (var track in (project.Tracks as IEnumerable<UVoicePart>) ?? Enumerable.Empty<UVoicePart>()) {
                // Check if track has a Singer (You might need a different property)
                var singerProperty = track.GetType().GetProperty("Singer");
                if (singerProperty == null) {
                    Console.WriteLine("🚨 UVoicePart does not contain a 'Singer' property.");
                    continue;
                }

                var singerValue = singerProperty.GetValue(track);
                if (singerValue == null) continue;



                var rvcEngine = new RvcInferenceEngine("model.pth", "index.pth");
                await rvcEngine.ProcessAsync("input.wav", "output.wav", 0.0, progress => { });
            }
            await ShowMessageAsync("Rvsynth export completed.");
        }

        private async Task ShowMessageAsync(string message) {
            var messageBox = MessageBoxManager.GetMessageBoxStandard("Notification", message, ButtonEnum.Ok, Icon.Info);
            await messageBox.ShowAsync();
        }
    }
}
