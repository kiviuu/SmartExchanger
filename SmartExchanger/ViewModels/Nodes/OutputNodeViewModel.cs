using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Media.Imaging;
using SkiaSharp.Views.WPF;

namespace SmartExchanger.ViewModels.Nodes
{
    public partial class OutputNodeViewModel : BaseNodeViewModel
    {
        [ObservableProperty]
        private BitmapSource? _renderedImage;

        public BaseNodeViewModel? InputNode { get; set; }
        public OutputNodeViewModel()
        {
            Title = "Texture Output";
        }
        public override void Process()
        {
            if (InputNode?.OutputTexture is not null)
            {
                RenderedImage = InputNode.OutputTexture.ToWriteableBitmap();
            }
        }
    }
}
