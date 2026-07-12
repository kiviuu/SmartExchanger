using CommunityToolkit.Mvvm.ComponentModel;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace SmartExchanger.ViewModels.Nodes
{
    public partial class PerlinNoiseTurbulenceNodeViewModel : BaseNodeViewModel
    {
        [ObservableProperty]
        private float _seed;
        [ObservableProperty]
        private float _frequencyX = 0.1f;
        [ObservableProperty]
        private float _frequencyY = 0.1f;
        [ObservableProperty]
        private int _octaves = 3;

        public SKImage? InputTexture { get; set; }

        public event Action? PropsChanged;

        public PerlinNoiseTurbulenceNodeViewModel()
        {
            Title = "Perlin Noise Turbulence Node";
            Outputs.Add(new ConnectorViewModel(this, "Out"));
            Inputs.Add(new ConnectorViewModel(this, "In"));
        }
        public override void ProcessNode(GRContext context, int size)
        {
            if (context is null) return;
            var info = new SKImageInfo(size, size);
            using var surface = SKSurface.Create(context, true, info);
            if (surface is null) return;
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Transparent);
            if (InputTexture is not null)
            {
                canvas.DrawImage(InputTexture, new SKPoint(0, 0), new SKSamplingOptions());
            }


            using var paint = new SKPaint();
            using var shader = SKShader.CreatePerlinNoiseTurbulence(FrequencyX, FrequencyY, Octaves, Seed);
            paint.Shader = shader;
            paint.BlendMode = SKBlendMode.Multiply;
            canvas.DrawRect(0, 0, size, size, paint);
            CurrentTexture?.Dispose();
            CurrentTexture = surface.Snapshot();
            OnPropertyChanged(nameof(CurrentTexture));
        }

        partial void OnFrequencyXChanged(float value) => NotifyInputChanged();
        partial void OnSeedChanged(float value) => NotifyInputChanged();
        partial void OnOctavesChanged(int value) => NotifyInputChanged();
        partial void OnFrequencyYChanged(float value) => NotifyInputChanged();

        private void NotifyInputChanged()
        {
            PropsChanged?.Invoke();
            //ProcessNode();
        }

        public override void ClearNode()
        {
            InputTexture = null;
        }
    }
}
