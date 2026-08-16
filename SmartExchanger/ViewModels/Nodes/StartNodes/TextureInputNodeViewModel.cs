using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using SkiaSharp;
using System.IO;

namespace SmartExchanger.ViewModels.Nodes
{
    public partial class TextureInputNodeViewModel : BaseNodeViewModel, IDisposable
    {
        private bool _isDisposed;
        private SKBitmap? _sourceBitmap;

        [ObservableProperty]
        private string? _filePath;

        [ObservableProperty]
        private string _textureInfo = "PNG / JPG";

        [ObservableProperty]
        private bool _hasTexture;

        [ObservableProperty]
        private bool _flipVertically;

        public string FileName => FilePath is null ? "No texture selected" : Path.GetFileName(FilePath);

        public ConnectorViewModel OutputConnector { get; }

        public TextureInputNodeViewModel()
        {
            Title = "Texture Input";

            OutputConnector = new ConnectorViewModel(this, "Out", "out");

            Outputs.Add(OutputConnector);
        }

        [RelayCommand]
        private void LoadTexture()
        {
            if (_isDisposed)
            {
                return;
            }

            var fileDialog = new OpenFileDialog
            {
                Title = "Load Texture",
                CheckFileExists = true,
                CheckPathExists = true,
                Multiselect = false,
                Filter =
                    "Supported images (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|" +
                    "PNG image (*.png)|*.png|" +
                    "JPEG image (*.jpg;*.jpeg)|*.jpg;*.jpeg"
            };

            bool? result = Application.Current?.MainWindow is Window owner ? fileDialog.ShowDialog(owner) : fileDialog.ShowDialog();

            if (result != true)
            {
                return;
            }

            if (!TryLoadTextureFromPath(fileDialog.FileName, out string? errorMessage, invalidateRender: true))
            {
                MessageBox.Show($"Could not load the selected texture.\n\n{errorMessage}",
                    "Texture loading failure",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        public bool TryLoadTextureFromPath(string filePath, out string? errorMessage, bool invalidateRender = true)
        {
            errorMessage = null;
            if (_isDisposed)
            {
                errorMessage = "The Texture Input node has already been disposed.";

                return false;
            }

            if (string.IsNullOrWhiteSpace(filePath))
            {
                errorMessage = "The texture path is empty.";

                return false;
            }

            SKBitmap? nextBitmap = null;

            try
            {
                string fullFilePath = Path.GetFullPath(filePath);

                using var fileStream = new FileStream(fullFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);

                nextBitmap = SKBitmap.Decode(fileStream)
                    ?? throw new InvalidOperationException(
                        "SkiaSharp could not decode the selected image.");

                if (nextBitmap.Width <= 0 || nextBitmap.Height <= 0)
                {
                    throw new InvalidOperationException("The selected image has invalid dimensions.");
                }

                SKBitmap? previousBitmap = _sourceBitmap;
                _sourceBitmap = nextBitmap;

                nextBitmap = null;
                previousBitmap?.Dispose();
                FilePath = fullFilePath;
                TextureInfo = $"{_sourceBitmap.Width} x {_sourceBitmap.Height}";
                HasTexture = true;

                if (invalidateRender)
                {
                    InvalidateRender();
                }

                return true;
            }
            catch (Exception exception)
            {
                nextBitmap?.Dispose();
                errorMessage = exception.Message;

                return false;
            }
        }

        [RelayCommand]
        private void ClearTexture()
        {
            if (_isDisposed || _sourceBitmap is null)
            {
                return;
            }

            ClearLoadedTexture(invalidateRender: true);
        }

        private void ClearLoadedTexture(bool invalidateRender)
        {
            _sourceBitmap?.Dispose();
            _sourceBitmap = null;

            FilePath = null;
            TextureInfo = "PNG / JPG";
            HasTexture = false;

            if (invalidateRender)
            {
                InvalidateRender();
            }
        }

        public override SKImage? Render(GRContext context, int size, NodeRenderInputs inputs)
        {
            if (_isDisposed || _sourceBitmap is null)
            {
                return null;
            }

            using var surface =CreateGpuSurface(context, size);
            SKCanvas canvas =surface.Canvas;
            canvas.Clear(SKColors.Transparent);

            using var sourceImage = SKImage.FromBitmap(_sourceBitmap);

            float scale =MathF.Min(size / (float)_sourceBitmap.Width, size / (float)_sourceBitmap.Height);

            float destinationWidth = _sourceBitmap.Width * scale;

            float destinationHeight = _sourceBitmap.Height * scale;

            float destinationX = (size - destinationWidth) * 0.5f;

            float destinationY = (size - destinationHeight) * 0.5f;

            var destination = SKRect.Create(destinationX, destinationY, destinationWidth, destinationHeight);

            var sourceRect = SKRect.Create(0, 0, _sourceBitmap.Width, _sourceBitmap.Height);
            var sampling = new SKSamplingOptions(SKFilterMode.Linear,SKMipmapMode.None);

            int canvasSaveCount = canvas.Save();

            try
            {
                if (FlipVertically)
                {
                    canvas.Scale(
                    sx: 1.0f,
                    sy: -1.0f,
                    px: size * 0.5f,
                    py: size * 0.5f);
                }
                

                canvas.DrawImage(sourceImage, sourceRect, destination, sampling);
            }
            finally
            {
                canvas.RestoreToCount(canvasSaveCount);
            }

            return surface.Snapshot();
        }

        partial void OnFilePathChanged(string? value)
        {
            OnPropertyChanged(nameof(FileName));
        }

        protected override bool IsRenderAffectingProperty(string? propertyName)
        {
            return propertyName == nameof(FlipVertically);
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }
            _isDisposed =true;
            ClearLoadedTexture( invalidateRender: false);
            GC.SuppressFinalize(this);
        }
    }
}