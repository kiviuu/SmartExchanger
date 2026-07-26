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
    }
}
