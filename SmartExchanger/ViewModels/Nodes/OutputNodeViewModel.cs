using CommunityToolkit.Mvvm.ComponentModel;
using SkiaSharp;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SmartExchanger.ViewModels.Nodes
{
    /// <summary>
    /// Last output node with preview
    /// - it does not contain SKGElement nor GrContext, SkImage
    /// - preview is small CPU WPF buffor (WritableBitmap)
    /// </summary>
    public partial class OutputNodeViewModel : BaseNodeViewModel
    {
        [ObservableProperty]
        private bool _hasSignal;

        [ObservableProperty]
        private WriteableBitmap? _previewImage;

        public ConnectorViewModel InputConnector { get; }

        public override bool ProducesTexture => false;

        public OutputNodeViewModel()
        {
            Title = "Output Node";
            InputConnector = new ConnectorViewModel(this, "In");
            Inputs.Add(InputConnector);
        }

        public override SKImage? Render(GRContext context, int size, NodeRenderInputs inputs)
        {
            return null;
        }

        public void UpdatePreview(SKBitmap bitmap)
        {
            ArgumentNullException.ThrowIfNull(bitmap);

            int width = bitmap.Width;
            int height = bitmap.Height;

            if (PreviewImage is null ||
                PreviewImage.PixelWidth != width ||
                PreviewImage.PixelHeight != height)
            {
                PreviewImage = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
            }

            int bufferSize = checked(bitmap.RowBytes * bitmap.Height);
            PreviewImage.WritePixels(new Int32Rect(0, 0, width, height), bitmap.GetPixels(), bufferSize, bitmap.RowBytes);

            HasSignal = true;
        }

        public void ClearPreview()
        {
            HasSignal = false;
            PreviewImage = null;
        }

        protected override bool IsRenderAffectingProperty(string? propertyName)
        {
            return propertyName != nameof(HasSignal) &&
                   propertyName != nameof(PreviewImage) &&
                   base.IsRenderAffectingProperty(propertyName);
        }
    }
}
