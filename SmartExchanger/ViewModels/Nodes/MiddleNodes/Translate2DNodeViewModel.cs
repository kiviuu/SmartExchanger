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
        private float _rotationDegrees;

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

            float rotationDegrees = NormalizeRotation(RotationDegrees);
            int canvasSaveCount = canvas.Save();
            try
            {
                canvas.ClipRect(new SKRect(0, 0, size, size));
                if (Wrap)
                {
                    DrawWrapped(canvas, input, size, OffsetX, OffsetY, rotationDegrees, sampling);
                }
                else
                {
                    DrawWithoutWrap(canvas, input, size, OffsetX, OffsetY, rotationDegrees, sampling);
                }
            }
            finally
            {
                canvas.RestoreToCount(canvasSaveCount);
            }

            
            return surface.Snapshot();
        }

        private static void DrawWrapped(SKCanvas canvas, SKImage input, int size, float offsetX, 
            float offsetY, float rotationDegrees, SKSamplingOptions sampling)
        {
            float wrappedX = WrapOffset(offsetX) * size;
            float wrappedY = WrapOffset(offsetY) * size;

            float pivotX = wrappedX + size * 0.5f;
            float pivotY = wrappedY + size * 0.5f;

            canvas.RotateDegrees(rotationDegrees, pivotX, pivotY);

            // needs three copy on every row -> left, right, up, down, and corners
            for (int xIdx = -1; xIdx <= 1; xIdx++)
            {
                for (int yIdx = -1; yIdx <=1; yIdx++)
                {
                    float left = wrappedX + xIdx * size;
                    float top = wrappedY + yIdx * size;

                    var destination = new SKRect(left, top, left + size, top + size);

                    canvas.DrawImage(input, destination, sampling);
                }
            }
        }

        private static void DrawWithoutWrap(SKCanvas canvas, SKImage input, int size, float offsetX, 
            float offsetY, float rotationDegrees, SKSamplingOptions sampling)
        {
            float offsetXPixels = SanitizeOffset(offsetX) * size;
            float offsetYPixels = SanitizeOffset(offsetY) * size;

            float pivotX = offsetXPixels + size * 0.5f;
            float pivotY = offsetYPixels + size * 0.5f;

            var destination = new SKRect(offsetXPixels, offsetYPixels, offsetXPixels + size, offsetYPixels + size);

            canvas.RotateDegrees(rotationDegrees, pivotX, pivotY);

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
        private static float NormalizeRotation(float value)
        {
            if (!float.IsFinite(value))
            {
                return 0f;
            }
            value %= 360f;
            if (value > 180f)
            {
                value -= 360f;
            }
            else if (value < -180f)
            {
                value += 360f;
            }
            return value;
        }
    }
}

