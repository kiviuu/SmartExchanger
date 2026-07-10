using CommunityToolkit.Mvvm.ComponentModel;
using SkiaSharp;

namespace SmartExchanger.ViewModels.Nodes
{
    public partial class BlendNodeViewModel : BaseNodeViewModel
    {
        [ObservableProperty]
        private float _factor = 0.5f;

        [ObservableProperty]
        private SKImage? _inputTextureA;

        [ObservableProperty]
        private SKImage? _inputTextureB;

        public event Action? PropsChanged;

        [ObservableProperty]
        private SKBlendMode _selectedBlendMode = SKBlendMode.SrcOver;

        public IReadOnlyList<SKBlendMode> AvailableBlendModes { get; } = Enum.GetValues<SKBlendMode>();

        public BlendNodeViewModel()
        {
            Title = "Blend Node";
            Inputs.Add(new ConnectorViewModel(this, "A"));
            Inputs.Add(new ConnectorViewModel(this, "B"));
            Outputs.Add(new ConnectorViewModel(this, "Out"));
            //ProcessNode();
        }
        public override void ClearNode()
        {
            InputTextureA = null;
            InputTextureB = null;
        }

        public override void ProcessNode(GRContext context, int size)
        {
            if (context is null) return;
            var refTexture = InputTextureA ?? InputTextureB;
            if (refTexture is null)
            {
                CurrentTexture?.Dispose();
                CurrentTexture = null;
                return;
            }

            var info = new SKImageInfo(size, size);
            using var surface = SKSurface.Create(context, true, info);
            if (surface is null) return;
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Transparent);


            if (InputTextureA is not null)
            {
                canvas.DrawImage(InputTextureA, new SKPoint(0,0), new SKSamplingOptions());
            }

            if (InputTextureB is not null)
            {
                using var paint = new SKPaint();
                byte alpha = (byte)(Factor * 255);
                paint.Color = SKColors.White.WithAlpha(alpha);
                paint.IsAntialias = true;
                paint.BlendMode = SelectedBlendMode;
                canvas.DrawImage(InputTextureB, new SKPoint(0,0), new SKSamplingOptions(), paint);
            }
            CurrentTexture?.Dispose();
            CurrentTexture = surface.Snapshot();
            OnPropertyChanged(nameof(CurrentTexture));
        }

        partial void OnFactorChanged(float value) => NotifyInputChanged();
        partial void OnSelectedBlendModeChanged(SKBlendMode value) => NotifyInputChanged();
        private void NotifyInputChanged()
        {
            PropsChanged?.Invoke();
            //ProcessNode();
        }

    }
}
