using CommunityToolkit.Mvvm.ComponentModel;
using SkiaSharp;

namespace SmartExchanger.ViewModels.Nodes
{
    public partial class BlendNodeViewModel : BaseNodeViewModel
    {
        [ObservableProperty]
        private float _factor;

        [ObservableProperty]
        private SKBitmap? _inputTextureA;

        [ObservableProperty]
        private SKBitmap? _inputTextureB;

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

        public override void ProcessNode(int size)
        {
            if (CurrentTexture is null || CurrentTexture.Width != size || CurrentTexture.Height != size)
            {
                CurrentTexture?.Dispose();
                CurrentTexture = new SKBitmap(size, size);
            }

            var referenceTexture = InputTextureA ?? InputTextureB;

            if (referenceTexture == null)
            {
                CurrentTexture = null;
                return;
            }

            if (CurrentTexture == null || CurrentTexture.Width != referenceTexture.Width || CurrentTexture.Height != referenceTexture.Height)
            {
                CurrentTexture?.Dispose();
                CurrentTexture = new SKBitmap(referenceTexture.Width, referenceTexture.Height);
            }

            using var canvas = new SKCanvas(CurrentTexture);
            canvas.Clear(SKColors.Transparent);

            if (InputTextureA is not null)
            {
                canvas.DrawBitmap(InputTextureA, new SKPoint(0,0), new SKSamplingOptions());
            }

            if (InputTextureB is not null)
            {
                using var paint = new SKPaint();
                byte alpha = (byte)(Factor * 255);
                paint.Color = SKColors.White.WithAlpha(alpha);
                paint.IsAntialias = true;
                paint.BlendMode = SelectedBlendMode;
                canvas.DrawBitmap(InputTextureB, new SKPoint(0,0), new SKSamplingOptions(), paint);
            }
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
