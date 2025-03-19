using Avalonia.Controls;
using Avalonia.Interactivity;
using OpenUtau.App.ViewModels;

namespace OpenUtau.App.Views {
    public partial class ExportDialogView : Window {
        public ExportDialogView() {
            InitializeComponent();
            DataContext = new ExportDialogViewModel();
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
