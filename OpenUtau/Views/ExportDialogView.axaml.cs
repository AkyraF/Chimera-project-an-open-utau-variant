using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using OpenUtau.App.ViewModels;
using System;

namespace OpenUtau.App.Views {
    public partial class ExportDialogView : Window {
        public ExportDialogView() {
            InitializeComponent();  // Ensure this exists
            DataContext = new ExportDialogViewModel();
        }

        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnExportClick(object sender, RoutedEventArgs e) {
            if (DataContext is ExportDialogViewModel viewModel) {
                viewModel.ExportCommand.Execute().Subscribe();
            }
        }

        private void OnSelectFolderClick(object sender, RoutedEventArgs e) {
            if (DataContext is ExportDialogViewModel viewModel) {
                viewModel.SelectFolderCommand.Execute().Subscribe();
            }
        }
    }
}
