using CommunityToolkit.Mvvm.ComponentModel;

namespace SmartExchanger.ViewModels
{
    public partial class MainViewModel : ObservableObject, IDisposable
    {
        private bool _isDisposed;
        public EditorViewModel Editor { get; }

        public MainViewModel(EditorViewModel editorViewModel)
        {
            this.Editor = editorViewModel ?? throw new ArgumentNullException(nameof(editorViewModel));
        }
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }
            _isDisposed = true;
            Editor.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
