using CommunityToolkit.Mvvm.ComponentModel;
using SkiaSharp;
using System.Windows.Media;

namespace SmartExchanger.ViewModels.Nodes
{
    public partial class ColorNodeViewModel : BaseNodeViewModel
    {
        [ObservableProperty]
        private byte _r = 128;

        [ObservableProperty]
        private byte _g;

        [ObservableProperty]
        private byte _b = 128;

        public ConnectorViewModel OutputConnector { get; }

        public SolidColorBrush ColorBrush
        {
            get
            {
                var brush = new SolidColorBrush(Color.FromRgb(R, G, B));
                brush.Freeze();
                return brush;
            }
        }

        public ColorNodeViewModel()
        {
            Title = "Color";
            OutputConnector = new ConnectorViewModel(this, "Out");
            Outputs.Add(OutputConnector);
        }

        public override SKImage Render(
            GRContext context,
            int size,
            NodeRenderInputs inputs)
        {
            using var surface = CreateGpuSurface(context, size);
            surface.Canvas.Clear(new SKColor(R, G, B, 255));
            return surface.Snapshot();
        }

        partial void OnRChanged(byte value) => OnPropertyChanged(nameof(ColorBrush));
        partial void OnGChanged(byte value) => OnPropertyChanged(nameof(ColorBrush));
        partial void OnBChanged(byte value) => OnPropertyChanged(nameof(ColorBrush));

        protected override bool IsRenderAffectingProperty(string? propertyName)
        {
            return propertyName != nameof(ColorBrush) &&
                   base.IsRenderAffectingProperty(propertyName);
        }
    }
}
