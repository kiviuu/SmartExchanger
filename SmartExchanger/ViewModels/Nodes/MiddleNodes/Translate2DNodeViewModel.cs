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

            InputConnector = new ConnectorViewModel(this, "In", "textureIn");
            OutputConnector = new ConnectorViewModel(this, "Out", "out");
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
            SKCanvas canvas = surface.Canvas;
            canvas.Clear(SKColors.Transparent);

            float offsetXPixels = SanitizeOffset(OffsetX) * size;
            float offsetYPixels = SanitizeOffset(OffsetY) * size;

            float rotationDegrees = NormalizeRotation(RotationDegrees);

            float center = size * 0.5f;

            // rotate texture
            SKMatrix rotationMatrix = SKMatrix.CreateRotationDegrees(rotationDegrees, center, center);

            // move rotated texture
            SKMatrix translationMatrix = SKMatrix.CreateTranslation(offsetXPixels, offsetYPixels);

            SKShaderTileMode tileMode = Wrap ? SKShaderTileMode.Repeat : SKShaderTileMode.Decal;

            var sampling = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None);

            using SKShader rotatedShader = SKShader.CreateImage(input, tileMode, tileMode, sampling, rotationMatrix)
                ?? throw new InvalidOperationException(
                    "Could not create the rotated image shader.");

            // rotate -> translate
            using SKShader transformedShader = SKShader.CreateLocalMatrix(rotatedShader, translationMatrix)
                ?? throw new InvalidOperationException(
                    "Could not create the translated image shader.");

            using var paint = new SKPaint
                {
                    Shader = transformedShader,
                    BlendMode = SKBlendMode.Src,
                    IsAntialias = false
                };

            canvas.DrawPaint(paint);
            return surface.Snapshot();
        }

        private static float SanitizeOffset(float value)
        {
            return float.IsFinite(value) ? value : 0f;
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