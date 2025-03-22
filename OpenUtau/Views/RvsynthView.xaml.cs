using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using OpenUtau.App.ViewModels;

namespace OpenUtau.App.Views {
    public partial class RvsynthView : Window {
        public RvsynthView() {
            InitializeComponent();
            DataContext = new RvsynthViewModel();
        }

        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
        }

        // Handles mode selection from the dropdown
        private void OnModeSelected(object? sender, SelectionChangedEventArgs e) {
            if (DataContext is RvsynthViewModel vm) {
                vm.IsModeSelectionVisible = false;
                vm.IsMainContentVisible = true;
                vm.IsBackButtonVisible = true;
            }
        }
    }
}
