using CommunityToolkit.Mvvm.ComponentModel;
using SkiaSharp;

namespace SmartExchanger.ViewModels.Nodes
{
    public partial class PerlinNoiseFractalNodeViewModel : BaseNodeViewModel
    {
        [ObservableProperty]
        private float _seed;

        [ObservableProperty]
        private float _frequencyX = 0.1f;

        [ObservableProperty]
        private float _frequencyY = 0.1f;

        [ObservableProperty]
        private int _octaves = 3;

        public ConnectorViewModel InputConnector { get; }
        public ConnectorViewModel OutputConnector { get; }

        public PerlinNoiseFractalNodeViewModel()
        {
            Title = "Perlin Noise Fractal Node";

            InputConnector = new ConnectorViewModel(this, "In");
            OutputConnector = new ConnectorViewModel(this, "Out");

            Inputs.Add(InputConnector);
            Outputs.Add(OutputConnector);
        }

        public override SKImage Render(GRContext context, int size, NodeRenderInputs inputs)
        {
            using var surface = CreateGpuSurface(context, size);
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Transparent);

            var destination = new SKRect(0, 0, size, size);
            var input = inputs.Get(InputConnector);

            if (input is not null)
            {
                canvas.DrawImage(input, destination, new SKSamplingOptions());
            }

            using var shader = SKShader.CreatePerlinNoiseFractalNoise(FrequencyX, FrequencyY, Math.Max(0, Octaves), Seed);

            using var paint = new SKPaint
            {
                Shader = shader,
                BlendMode = SKBlendMode.Multiply
            };

            canvas.DrawRect(destination, paint);
            return surface.Snapshot();
        }
    }
}
