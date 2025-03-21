using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using OpenUtau.Core;
using OpenUtau.RVC.Processing;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia.Dto;
using System.Collections.Generic;
using Avalonia;
using MsBox.Avalonia.Models;

namespace OpenUtau.App.Views {
    public partial class RvsynthView : Window {
        private ComboBox modelComboBox = new();
        private RvcInferenceEngine engine = null!;

        private double pitch = 0.0;
        private ComboBox indexComboBox = new();
        private Slider pitchSlider = new();
        private ProgressBar trackProgressBar = new();
        private ProgressBar aiProgressBar = new();
        private Button processButton = new();
        private Button backButton = new();
        private string selectedExportFolder = string.Empty;

        public RvsynthView() {
            InitializeComponent();
            InitializeControls();

            // This avoids the CS8618 error by giving 'engine' a non-null value
            try {
                string modelPath = Path.Combine(AppContext.BaseDirectory, "rvc", "model", "dummy_model.pth");
                string indexPath = Path.Combine(AppContext.BaseDirectory, "rvc", "index", "dummy_index.index");

                if (File.Exists(modelPath) && File.Exists(indexPath)) {
                    engine = new RvcInferenceEngine(modelPath, indexPath);
                } else {
                    engine = null!;
                }
            } catch {
                engine = null!;
            }
        }

        private void InitializeControls() {
            modelComboBox = this.FindControl<ComboBox>("ModelComboBox") ?? new ComboBox();
            indexComboBox = this.FindControl<ComboBox>("IndexComboBox") ?? new ComboBox();
            pitchSlider = this.FindControl<Slider>("PitchSlider") ?? new Slider();
            trackProgressBar = this.FindControl<ProgressBar>("TrackProgressBar") ?? new ProgressBar();
            aiProgressBar = this.FindControl<ProgressBar>("AIProgressBar") ?? new ProgressBar();
            processButton = this.FindControl<Button>("ProcessButton") ?? new Button();
            backButton = this.FindControl<Button>("BackButton") ?? new Button();

            processButton.Click += async (s, e) => await ProcessButton_Click();
            backButton.Click += (s, e) => Close();

            LoadModelList();
            LoadIndexList();
        }

        private void LoadModelList() {
            string modelDir = Path.Combine(AppContext.BaseDirectory, "rvc", "model");
            if (Directory.Exists(modelDir)) {
                var models = Directory.GetFiles(modelDir, "*.pth")
                                      .Select(Path.GetFileNameWithoutExtension)
                                      .ToList();
                modelComboBox.ItemsSource = models;
            }
        }

        private void LoadIndexList() {
            string indexDir = Path.Combine(AppContext.BaseDirectory, "rvc", "index");
            if (Directory.Exists(indexDir)) {
                var indices = Directory.GetFiles(indexDir, "*.index")
                                       .Select(Path.GetFileNameWithoutExtension)
                                       .ToList();
                indexComboBox.ItemsSource = indices;
            }
        }
        private async Task<string?> SelectExportFolder() {
            var options = new FolderPickerOpenOptions {
                Title = "Select Export Folder",
                AllowMultiple = false
            };

            var folders = await this.StorageProvider.OpenFolderPickerAsync(options);
            return folders?.FirstOrDefault()?.Path.LocalPath;
        }

        private async Task ProcessButton_Click() {
            if (modelComboBox.SelectedItem == null || indexComboBox.SelectedItem == null) {
                await ShowMessageBox("Please select a model and an index file.");
                return;
            }

            string selectedModel = modelComboBox.SelectedItem.ToString() + ".pth";
            string selectedIndex = indexComboBox.SelectedItem.ToString() + ".index";
            string modelPath = Path.Combine(AppContext.BaseDirectory, "rvc", "model", selectedModel);
            string indexPath = Path.Combine(AppContext.BaseDirectory, "rvc", "index", selectedIndex);

            selectedExportFolder = await SelectExportFolder() ?? string.Empty;
            if (string.IsNullOrEmpty(selectedExportFolder)) return;


            await RunRVCInference(modelPath, indexPath);
        }

        private async Task RunRVCInference(string modelPath, string indexPath) {
            trackProgressBar.IsIndeterminate = false;
            aiProgressBar.IsIndeterminate = false;

            var tracks = new List<string> { "Track1", "Track2" };
            foreach (var track in tracks) {
                string inputWav = Path.Combine(AppContext.BaseDirectory, "renders", track + ".wav");
                string outputWav = Path.Combine(selectedExportFolder, track + ".wav");

                await engine.ProcessAsync(inputWav, outputWav, pitch, progress => {
                    aiProgressBar.Value = progress;
                });
            }

            await ShowMessageBox("Processing complete!");
        }

        private async Task ShowMessageBox(string message) {
            var messageBox = MessageBoxManager
    .GetMessageBoxStandard("Notification", message, ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Info);
            await messageBox.ShowAsync();
        }

    }
}
