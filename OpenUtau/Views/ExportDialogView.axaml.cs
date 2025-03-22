using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using OpenUtau.App.ViewModels;
using System;

namespace OpenUtau.App.Views {
    public partial class ExportDialogView : Window {
        public ExportDialogView() {
            InitializeComponent();
            DataContext = new ExportDialogViewModel();

            var modeComboBox = this.FindControl<ComboBox>("ModeComboBox");
            var backButton = this.FindControl<Button>("BackButton");
            var processButton = this.FindControl<Button>("ProcessButton");

            if (modeComboBox != null) {
                modeComboBox.SelectionChanged += ModeComboBox_SelectionChanged;
            }

            if (backButton != null) {
                backButton.Click += BackButton_Click;
            }

            if (processButton != null) {
                processButton.Click += OnExportClick;
            }
        }

        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
        }

        private void ModeComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e) {
            var optionsPanel = this.FindControl<StackPanel>("OptionsPanel");
            var modePanel = this.FindControl<StackPanel>("ModeSelectionPanel");
            if (optionsPanel != null && modePanel != null) {
                optionsPanel.IsVisible = true;
                modePanel.IsVisible = false;
            }
        }

        private void BackButton_Click(object? sender, RoutedEventArgs e) {
            var optionsPanel = this.FindControl<StackPanel>("OptionsPanel");
            var modePanel = this.FindControl<StackPanel>("ModeSelectionPanel");
            if (optionsPanel != null && modePanel != null) {
                optionsPanel.IsVisible = false;
                modePanel.IsVisible = true;
            }
        }

        private void OnExportClick(object? sender, RoutedEventArgs e) {
            if (DataContext is ExportDialogViewModel viewModel && viewModel.ExportCommand != null) {
                viewModel.ExportCommand.Execute().Subscribe();
            }
        }

        private void OnSelectFolderClick(object? sender, RoutedEventArgs e) {
            if (DataContext is ExportDialogViewModel viewModel && viewModel.SelectFolderCommand != null) {
                viewModel.SelectFolderCommand.Execute().Subscribe();
            }
        }
    }
}
