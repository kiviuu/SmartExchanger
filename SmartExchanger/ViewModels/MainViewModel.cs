using CommunityToolkit.Mvvm.ComponentModel;

namespace SmartExchanger.ViewModels
{
    public partial class MainViewModel : ObservableObject, IDisposable
    {
        private bool _isDisposed;
        public EditorViewModel Editor { get; }
        public MaterialPreviewViewModel MaterialPreview { get; }

        public MainViewModel(EditorViewModel editorViewModel, MaterialPreviewViewModel materialPreviewViewModel)
        {
            this.Editor = editorViewModel ?? throw new ArgumentNullException(nameof(editorViewModel));
            this.MaterialPreview = materialPreviewViewModel ?? throw new ArgumentNullException(nameof(materialPreviewViewModel));
        }
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }
            _isDisposed = true;
            Editor.Dispose();
            MaterialPreview.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
