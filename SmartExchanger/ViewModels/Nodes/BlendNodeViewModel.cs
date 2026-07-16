using CommunityToolkit.Mvvm.ComponentModel;
using SkiaSharp;

namespace SmartExchanger.ViewModels.Nodes
{
    public partial class BlendNodeViewModel : BaseNodeViewModel
    {
        [ObservableProperty]
        private float _factor = 0.5f;

        [ObservableProperty]
        private SKBlendMode _selectedBlendMode = SKBlendMode.SrcOver;

        public ConnectorViewModel InputAConnector { get; }
        public ConnectorViewModel InputBConnector { get; }
        public ConnectorViewModel InputMaskConnector { get; }
        public ConnectorViewModel OutputConnector { get; }

        public IReadOnlyList<SKBlendMode> AvailableBlendModes { get; } =
            Enum.GetValues<SKBlendMode>();

        public BlendNodeViewModel()
        {
            Title = "Blend Node";

            InputAConnector = new ConnectorViewModel(this, "A");
            InputBConnector = new ConnectorViewModel(this, "B");
            InputMaskConnector = new ConnectorViewModel(this, "Mask");
            OutputConnector = new ConnectorViewModel(this, "Out");

            Inputs.Add(InputAConnector);
            Inputs.Add(InputBConnector);
            Inputs.Add(InputMaskConnector);
            Outputs.Add(OutputConnector);
        }

        public override SKImage? Render(GRContext context, int size, NodeRenderInputs inputs)
        {
            var inputA = inputs.Get(InputAConnector);
            var inputB = inputs.Get(InputBConnector);
            var inputMask = inputs.Get(InputMaskConnector);

            if (inputA is null && inputB is null)
            {
                return null;
            }

            using var surface = CreateGpuSurface(context, size);
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Transparent);

            var destination = new SKRect(0, 0, size, size);
            var sampling = new SKSamplingOptions();

            if (inputA is not null)
            {
                canvas.DrawImage(inputA, destination, sampling);
            }

            if (inputB is not null)
            {
                byte factorAlpha = (byte)Math.Round(Math.Clamp(Factor, 0f, 1f) * 255f);
                if (inputMask is not null)
                {
                    DrawWithMask(canvas, inputB, inputMask, destination, sampling, factorAlpha);
                }
                else
                {
                    DrawWithoutMask(canvas, inputB, destination, sampling, factorAlpha);
                }
            }

            return surface.Snapshot();
        }

        private void DrawWithMask(SKCanvas canvas, SKImage inputB, SKImage mask, SKRect destination, SKSamplingOptions sampling, byte factorAlpha)
        {
            using var layerPaint = new SKPaint
            {
                BlendMode = SelectedBlendMode
            };
            canvas.SaveLayer();
            try
            {
                canvas.DrawImage(inputB, destination, sampling);
                using var lumaFilter = SKColorFilter.CreateLumaColor();
                using var maskPaint = new SKPaint
                {
                    BlendMode = SKBlendMode.DstIn,
                    ColorFilter = lumaFilter,
                    Color = SKColors.White.WithAlpha(factorAlpha)
                };
                canvas.DrawImage(mask, destination, sampling, maskPaint);
            }
            finally
            {
                canvas.Restore();
            }
        }

        private void DrawWithoutMask(SKCanvas canvas, SKImage inputB, SKRect destination, SKSamplingOptions sampling, byte factorAlpha)
        {
            using var paint = new SKPaint()
            {
                BlendMode = SelectedBlendMode,
                Color = SKColors.White.WithAlpha(factorAlpha)
            };
            canvas.DrawImage(inputB, destination, sampling, paint);
        }
    }
}
