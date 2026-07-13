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
        public ConnectorViewModel OutputConnector { get; }

        public IReadOnlyList<SKBlendMode> AvailableBlendModes { get; } =
            Enum.GetValues<SKBlendMode>();

        public BlendNodeViewModel()
        {
            Title = "Blend Node";

            InputAConnector = new ConnectorViewModel(this, "A");
            InputBConnector = new ConnectorViewModel(this, "B");
            OutputConnector = new ConnectorViewModel(this, "Out");

            Inputs.Add(InputAConnector);
            Inputs.Add(InputBConnector);
            Outputs.Add(OutputConnector);
        }

        public override SKImage? Render(GRContext context, int size, NodeRenderInputs inputs)
        {
            var inputA = inputs.Get(InputAConnector);
            var inputB = inputs.Get(InputBConnector);

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
                using var paint = new SKPaint
                {
                    BlendMode = SelectedBlendMode,
                    Color = SKColors.White.WithAlpha((byte)Math.Round(Math.Clamp(Factor, 0f, 1f) * 255f))
                };

                canvas.DrawImage(inputB, destination, sampling, paint);
            }

            return surface.Snapshot();
        }
    }
}
