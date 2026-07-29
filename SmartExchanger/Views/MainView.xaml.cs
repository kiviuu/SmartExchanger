using SmartExchanger.ViewModels;
using System.Windows;

namespace SmartExchanger.Views
{
    public partial class MainView : Window
    {
        public MainView(MainViewModel mainViewModel)
        {
            InitializeComponent();
            DataContext = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));
            Closed += OnClosed;
        }

        private void OnClosed(object? sender, EventArgs e)
        {
            Closed -= OnClosed;

            if (DataContext is IDisposable disposable)
            {
                disposable.Dispose();
            }

            DataContext = null;
        }

        private void OnGraphContextMenuOpened(object sender, RoutedEventArgs e)
        {
            if (DataContext is not MainViewModel mainViewModel)
            {
                return;
            }
            mainViewModel.Editor.CaptureNodeCreationLocation(GraphEditor.MouseLocation);
        }
    }
}
