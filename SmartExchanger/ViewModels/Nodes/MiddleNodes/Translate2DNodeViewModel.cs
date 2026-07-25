using CommunityToolkit.Mvvm.ComponentModel;
using SkiaSharp;

namespace SmartExchanger.ViewModels.Nodes
{
    public partial class Translate2DNodeViewModel : BaseNodeViewModel
    {
        [ObservableProperty]
        private float _offsetX;

        [ObservableProperty]
        private float _offsetY;

        [ObservableProperty]
        private bool _wrap = true;

        public ConnectorViewModel InputConnector { get; }
        public ConnectorViewModel OutputConnector { get; }

        public Translate2DNodeViewModel()
        {
            Title = "Translate 2D";
            InputConnector = new ConnectorViewModel(this, "In");
            OutputConnector = new ConnectorViewModel(this, "Out");
            Inputs.Add(InputConnector);
            Outputs.Add(OutputConnector);
        }

        public override SKImage? Render(GRContext context, int size, NodeRenderInputs inputs)
        {
            SKImage? input = inputs.Get(InputConnector);
            if (input is null)
            {
                return null;
            }

            using var surface = CreateGpuSurface(context, size);
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Transparent);

            var sampling = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None);

            if (Wrap)
            {
                DrawWrap(canvas, input, size, OffsetX, OffsetY, sampling);
            }
            else
            {
                DrawTranslated(canvas, input, size, OffsetX, OffsetY, sampling);
            }
            return surface.Snapshot();
        }

        private static void DrawWrap(SKCanvas canvas, SKImage input, int size, float offsetX, float offsetY, SKSamplingOptions sampling)
        {
            float wrappedX = WrapOffset(offsetX) * size;
            float wrappedY = WrapOffset(offsetY) * size;

            // needs two copy on every axis -> left, right, up, down
            for (int xIdx = -1; xIdx <= 0; xIdx++)
            {
                for (int yIdx = -1; yIdx <=0; yIdx++)
                {
                    float left = wrappedX + xIdx * size;
                    float top = wrappedY + yIdx * size;

                    var destination = new SKRect(left, top, left + size, top + size);

                    canvas.DrawImage(input, destination, sampling);
                }
            }
        }

        private static void DrawTranslated(SKCanvas canvas, SKImage input, int size, float offsetX, float offsetY, SKSamplingOptions sampling)
        {
            float offsetXPixels = SanitizeOffset(offsetX) * size;
            float offsetYPixels = SanitizeOffset(offsetY) * size;
            var destination = new SKRect(offsetXPixels, offsetYPixels, offsetXPixels + size, offsetYPixels + size);
            canvas.DrawImage(input, destination, sampling);
        }

        private static float SanitizeOffset(float value)
        {
            return float.IsFinite(value) ? value : 0f;
        }
        private static float WrapOffset(float value)
        {
            value = SanitizeOffset(value);
            return value - MathF.Floor(value);
        }
    }
}
