using CommunityToolkit.Mvvm.ComponentModel;
using SkiaSharp;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SmartExchanger.ViewModels.Nodes
{
    // starting node
    public partial class ColorNodeViewModel : BaseNodeViewModel
    {
        [ObservableProperty]
        private byte _r = 128;

        [ObservableProperty]
        private byte _g = 0;

        [ObservableProperty]
        private byte _b = 128;

        public SolidColorBrush ColorBrush => new SolidColorBrush(Color.FromRgb(R, G, B));

        public event Action? PropsChanged;
        public ColorNodeViewModel()
        {
            Title = "Color Node";
            Outputs.Add(new ConnectorViewModel(this, "Out"));
            //ProcessNode();
        }

        partial void OnRChanged(byte value) => NotifyColorUpdate();
        partial void OnGChanged(byte value) => NotifyColorUpdate();
        partial void OnBChanged(byte value) => NotifyColorUpdate();

        private void NotifyColorUpdate()
        {
            OnPropertyChanged(nameof(ColorBrush));
            PropsChanged?.Invoke();
            //ProcessNode();
        }

        public override void ProcessNode(GRContext context, int size)
        {
            if (context is null) return;
            var info = new SKImageInfo(size, size);
            using var surface = SKSurface.Create(context, true, info);
            if (surface is null) return;
            surface.Canvas.Clear(new SKColor(R, G, B));
            CurrentTexture?.Dispose();
            CurrentTexture = surface.Snapshot();
            OnPropertyChanged(nameof(CurrentTexture));
        }

        public override void ClearNode()
        {
            return;
        }
    }
}
