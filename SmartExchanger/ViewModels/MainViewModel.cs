using CommunityToolkit.Mvvm.ComponentModel;
using SmartExchanger.Models;

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
            this.Editor.MaterialPreviewFrameReady += OnMaterialPreviewReady;
        }
        private void OnMaterialPreviewReady(MaterialPreviewFrame frame)
        {
            MaterialPreview.ApplyPreview(frame);
        }
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }
            _isDisposed = true;

            Editor.MaterialPreviewFrameReady -= OnMaterialPreviewReady;

            Editor.Dispose();
            MaterialPreview.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
