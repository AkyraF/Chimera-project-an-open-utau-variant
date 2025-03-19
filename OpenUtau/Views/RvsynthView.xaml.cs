using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using OpenUtau.Core.Util;
using OpenUtau.App.ViewModels;

namespace OpenUtau.App.Views {
    public partial class RvsynthView : Window {
        private ComboBox modelComboBox;
        private ComboBox indexComboBox;
        private Slider pitchSlider;
        private ProgressBar trackProgressBar;
        private ProgressBar aiProgressBar;
        private Button processButton;
        private Button backButton;
        private string selectedExportFolder;

        public RvsynthView() {
            InitializeComponent();
            InitializeControls();
        }

        private void InitializeControls() {
            modelComboBox = this.FindControl<ComboBox>("ModelComboBox");
            indexComboBox = this.FindControl<ComboBox>("IndexComboBox");
            pitchSlider = this.FindControl<Slider>("PitchSlider");
            trackProgressBar = this.FindControl<ProgressBar>("TrackProgressBar");
            aiProgressBar = this.FindControl<ProgressBar>("AIProgressBar");
            processButton = this.FindControl<Button>("ProcessButton");
            backButton = this.FindControl<Button>("BackButton");

            LoadModelList();
            LoadIndexList();
        }

        private void LoadModelList() {
            string modelDir = Path.Combine(AppContext.BaseDirectory, "rvc", "model");
            if (Directory.Exists(modelDir)) {
                var models = Directory.GetFiles(modelDir, "*.pth")
                                      .Select(Path.GetFileNameWithoutExtension)
                                      .ToList();
                modelComboBox.Items = models;
            }
        }

        private void LoadIndexList() {
            string indexDir = Path.Combine(AppContext.BaseDirectory, "rvc", "index");
            if (Directory.Exists(indexDir)) {
                var indices = Directory.GetFiles(indexDir, "*.index")
                                       .Select(Path.GetFileNameWithoutExtension)
                                       .ToList();
                indexComboBox.Items = indices;
            }
        }

        private async void ProcessButton_Click(object sender, RoutedEventArgs e) {
            if (modelComboBox.SelectedItem == null || indexComboBox.SelectedItem == null) {
                MessageBox.Show("Please select a model and an index file.");
                return;
            }

            string selectedModel = modelComboBox.SelectedItem.ToString() + ".pth";
            string selectedIndex = indexComboBox.SelectedItem.ToString() + ".index";
            string modelPath = Path.Combine(AppContext.BaseDirectory, "rvc", "model", selectedModel);
            string indexPath = Path.Combine(AppContext.BaseDirectory, "rvc", "index", selectedIndex);

            // Choose export folder
            selectedExportFolder = await SelectExportFolder();
            if (string.IsNullOrEmpty(selectedExportFolder)) return;

            // Run AI processing
            await RunRVCInference(modelPath, indexPath);
        }

        private async Task<string> SelectExportFolder() {
            var dialog = new OpenFolderDialog { Title = "Select Export Folder" };
            return await dialog.ShowAsync(this);
        }

        private async Task RunRVCInference(string modelPath, string indexPath) {
            trackProgressBar.IsIndeterminate = false;
            aiProgressBar.IsIndeterminate = false;

            var tracks = GetAllTracks();
            int totalTracks = tracks.Length;
            int currentTrack = 0;

            foreach (var track in tracks) {
                trackProgressBar.Value = ((double)currentTrack / totalTracks) * 100;

                string inputWav = GetWavFileForTrack(track);
                string outputWav = Path.Combine(selectedExportFolder, track + ".wav");

                aiProgressBar.Value = 0;
                await RvcInferenceEngine.Process(modelPath, indexPath, inputWav, outputWav, pitchSlider.Value, (progress) => {
                    aiProgressBar.Value = progress;
                });

                currentTrack++;
            }

            trackProgressBar.Value = 100;
            aiProgressBar.Value = 100;
            MessageBox.Show("Processing complete!");
        }

        private string[] GetAllTracks() {
            // TODO: Fetch all track names from OpenUtau project
            return new string[] { "Track1", "Track2" };
        }

        private string GetWavFileForTrack(string trackName) {
            // TODO: Implement logic to fetch WAV file path for given track
            return Path.Combine(AppContext.BaseDirectory, "renders", trackName + ".wav");
        }
    }
}
