using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using Windows.Media.Audio;
using Microsoft.Win32;

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

        public string FileName => FilePath is null ? "No texture selected" : Path.GetFileName(FilePath);

        public ConnectorViewModel OutputConnector { get;  }
        public TextureInputNodeViewModel()
        {
            Title = "Texture Input";
            OutputConnector = new ConnectorViewModel(this, "Out");
            Outputs.Add(OutputConnector);
        }

        [RelayCommand]
        private void LoadTexture()
        {
            if (this._isDisposed)
            {
                return;
            }

            var fileDialog = new OpenFileDialog
            {
                Title = "Load Texture",
                CheckFileExists = true,
                CheckPathExists = true,
                Multiselect = false,
                Filter = "Supported images (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|" +
                    "PNG image (*.png)|*.png|" +
                    "JPEG image (*.jpg;*.jpeg)|*.jpg;*.jpeg"
            };

            bool? result = System.Windows.Application.Current?.MainWindow is Window owner ?
                fileDialog.ShowDialog(owner) : fileDialog.ShowDialog();
            if (result != true)
            {
                return;
            }
            TryLoadTexture(fileDialog.FileName);
        }

        private void TryLoadTexture(string filePath)
        {
            SKBitmap? nextBitmap = null;
            try
            {
                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                nextBitmap = SKBitmap.Decode(fileStream) ?? throw new InvalidOperationException("SkiaSharp could not decode the selected image.");
                if (nextBitmap.Width <= 0 || nextBitmap.Height <= 0)
                {
                    throw new InvalidOperationException("The selected image has invalid dimensions.");
                }
                SKBitmap? previousBitmap = _sourceBitmap;
                _sourceBitmap = nextBitmap;
                previousBitmap?.Dispose();

                FilePath = filePath;
                TextureInfo = $"{_sourceBitmap.Width} x {_sourceBitmap.Height}";
                HasTexture = true;
                InvalidateRender();
            }
            catch(System.Exception ex)
            {
                nextBitmap?.Dispose();
                MessageBox.Show($"Could not load the selected texture.\n\n {ex.Message}",
                    "Texture loading failure", MessageBoxButton.OK, MessageBoxImage.Error
                );
            }
        }

        [RelayCommand]
        private void ClearTexture()
        {
            if (this._isDisposed || this._sourceBitmap is null)
            {
                return;
            }

            _sourceBitmap.Dispose();
            _sourceBitmap = null;
            FilePath = null;
            TextureInfo = "PNG / JPG";
            HasTexture = false;
            InvalidateRender();
        }

        public override SKImage? Render(GRContext context, int size, NodeRenderInputs inputs)
        {
            if (this._isDisposed || this._sourceBitmap is null)
            {
                return null;
            }
            using var surface = CreateGpuSurface(context, size);
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Transparent);

            using var sourceImage = SKImage.FromBitmap(this._sourceBitmap);
            var destination = SKRect.Create(0, 0, size, size);
            var sourceRect = SKRect.Create(0, 0, _sourceBitmap.Width, _sourceBitmap.Height);
            var sampling = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None);
            canvas.DrawImage(sourceImage, sourceRect, destination, sampling);
            return surface.Snapshot();

        }

        partial void OnFilePathChanged(string? value)
        {
            OnPropertyChanged(nameof(FileName));
        }

        protected override bool IsRenderAffectingProperty(string? propertyName)
        {
            return false;
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            this._isDisposed = true;
            this._sourceBitmap?.Dispose();
            this._sourceBitmap = null;

            GC.SuppressFinalize(this);
        }
    }
}
