using SmartExchanger.ViewModels;
using System.Windows;
using System.Windows.Input;

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


        private void OnGraphEditorMouseMove(object sender, MouseEventArgs e)
        {
            UpdateGraphPointerLocation(e);
        }


        private void OnGraphEditorPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            /*
             * Capture the location before the context menu opens.
             * Clicking a menu item moves the Windows cursor outside
             * the graph, but this graph-space position remains valid.
             */
            UpdateGraphPointerLocation(e);
        }


        private void OnGraphContextMenuOpened(object sender, RoutedEventArgs e)
        {
            if (DataContext is not MainViewModel mainViewModel)
            {
                return;
            }

            mainViewModel.Editor.UpdateGraphPointerLocation(GraphEditor.MouseLocation);
        }


        private void UpdateGraphPointerLocation(MouseEventArgs e)
        {
            if (DataContext is not MainViewModel mainViewModel)
            {
                return;
            }

            Point graphLocation = GraphEditor.GetLocationInsideEditor(e);

            mainViewModel.Editor.UpdateGraphPointerLocation(graphLocation);
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