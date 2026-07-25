using CommunityToolkit.Mvvm.ComponentModel;
using SmartExchanger.Models;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SmartExchanger.ViewModels
{
    public partial class TexturePreviewViewModel
        : ObservableObject
    {
        [ObservableProperty]
        private WriteableBitmap? _previewImage;

        [ObservableProperty]
        private bool _hasSignal;

        [ObservableProperty]
        private int _textureWidth;

        [ObservableProperty]
        private int _textureHeight;

        public void ApplyPreview(TexturePreviewFrame frame)
        {
            var dispatcher = Application.Current?.Dispatcher;

            if (dispatcher is not null && !dispatcher.CheckAccess())
            {
                dispatcher.BeginInvoke(
                    new Action(
                        () => ApplyPreview(frame)));
                return;
            }

            if (!frame.HasSignal || frame.Pixels is not byte[] pixels)
            {
                ClearPreview();
                return;
            }

            WriteableBitmap bitmap;

            if (PreviewImage is null || PreviewImage.PixelWidth != frame.Width ||  PreviewImage.PixelHeight != frame.Height)
            {
                bitmap =
                    new WriteableBitmap(frame.Width, frame.Height, dpiX: 96.0, dpiY: 96.0, PixelFormats.Pbgra32, palette: null);
            }
            else
            {
                bitmap = PreviewImage;
            }

            bitmap.WritePixels(
                new Int32Rect(0, 0, frame.Width, frame.Height),
                pixels,
                frame.RowBytes,
                offset: 0);


            if (!ReferenceEquals(PreviewImage, bitmap))
            {
                PreviewImage = bitmap;
            }

            TextureWidth = frame.Width;
            TextureHeight = frame.Height;
            HasSignal = true;
        }

        private void ClearPreview()
        {
            PreviewImage = null;
            TextureWidth = 0;
            TextureHeight = 0;
            HasSignal = false;
        }
    }
}