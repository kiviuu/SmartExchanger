using SkiaSharp;
using SmartExchanger.Helpers;

namespace SmartExchanger.ViewModels.Nodes
{
    public partial class ColorNodeViewModel : BaseNodeViewModel
    {
        public ColorNodeViewModel()
        {
            Title = "Solid Color Generator";
            Process();
        }
        public override void Process()
        {
            var bitmap = TextureHelper.CreateEmptyBitmap();

            using var canvas = new SKCanvas(bitmap);

            canvas.Clear(new SKColor(138, 43, 226));

            OutputTexture = bitmap;
        }
    }
}
