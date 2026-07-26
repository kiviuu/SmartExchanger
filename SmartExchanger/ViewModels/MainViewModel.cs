using CommunityToolkit.Mvvm.ComponentModel;
using SmartExchanger.Models;

namespace SmartExchanger.ViewModels
{
    public partial class MainViewModel : ObservableObject, IDisposable
    {
        private bool _isDisposed;
        public EditorViewModel Editor { get; }
        public MaterialPreviewViewModel MaterialPreview { get; }
        public TexturePreviewViewModel TexturePreview { get; }

        public MainViewModel(EditorViewModel editorViewModel, MaterialPreviewViewModel materialPreviewViewModel,
            TexturePreviewViewModel texturePreviewViewModel)
        {
            this.Editor = editorViewModel ?? throw new ArgumentNullException(nameof(editorViewModel));
            this.MaterialPreview = materialPreviewViewModel ?? throw new ArgumentNullException(nameof(materialPreviewViewModel));
            this.TexturePreview = texturePreviewViewModel ?? throw new ArgumentNullException(nameof(texturePreviewViewModel));
            this.Editor.MaterialPreviewFrameReady += OnMaterialPreviewReady;
            this.Editor.TexturePreviewFrameReady += OnTexturePreviewReady;
        }
        private void OnMaterialPreviewReady(MaterialPreviewFrame frame)
        {
            MaterialPreview.ApplyPreview(frame);
        }
        private void OnTexturePreviewReady(TexturePreviewFrame frame)
        {
            TexturePreview.ApplyPreview(frame);
        }
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }
            _isDisposed = true;

            Editor.MaterialPreviewFrameReady -= OnMaterialPreviewReady;
            Editor.TexturePreviewFrameReady -= OnTexturePreviewReady;

            Editor.Dispose();
            MaterialPreview.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
