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

        public SKBitmap? InputTexture { get; set; }

        public event Action? PropsChanged;
        public PerlinNoiseFractalNodeViewModel()
        {
            Title = "Perlin Noise Fractal Node";
            Outputs.Add(new ConnectorViewModel(this, "Out"));
            Inputs.Add(new ConnectorViewModel(this, "In"));

            CurrentTexture = new SKBitmap(256, 256);
            ProcessNode();
        }

        public override void ProcessNode()
        {
            if (CurrentTexture is null) return;
            using var canvas = new SKCanvas(CurrentTexture);
            canvas.Clear(SKColors.Transparent);
            if (InputTexture is not null)
            {
                canvas.DrawBitmap(InputTexture, new SKPoint(0, 0), new SKSamplingOptions());
                //canvas.DrawBitmap(InputTexture, 0, 0);
            }


            using var paint = new SKPaint();
            using var shader = SKShader.CreatePerlinNoiseFractalNoise(FrequencyX, FrequencyY, Octaves, Seed);
            paint.Shader = shader;
            paint.BlendMode = SKBlendMode.Multiply;
            canvas.DrawRect(0,0, CurrentTexture.Width, CurrentTexture.Height, paint);
        }

        partial void OnFrequencyXChanged(float value) => NotifyInputChanged();
        partial void OnSeedChanged(float value) => NotifyInputChanged();
        partial void OnOctavesChanged(int value) => NotifyInputChanged();
        partial void OnFrequencyYChanged(float value) => NotifyInputChanged();

        private void NotifyInputChanged()
        {
            PropsChanged?.Invoke();
            ProcessNode();
        }

        public override void ClearNode()
        {
            InputTexture = null;
        }
    }
}
