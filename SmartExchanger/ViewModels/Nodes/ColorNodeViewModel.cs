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
            CurrentTexture = new SKBitmap(256, 256);
            ProcessNode();
        }

        partial void OnRChanged(byte value) => NotifyColorUpdate();
        partial void OnGChanged(byte value) => NotifyColorUpdate();
        partial void OnBChanged(byte value) => NotifyColorUpdate();

        private void NotifyColorUpdate()
        {
            OnPropertyChanged(nameof(ColorBrush));
            PropsChanged?.Invoke();
            ProcessNode();
        }

        public override void ProcessNode()
        {
            using var canvas = new SKCanvas(CurrentTexture);
            canvas.Clear(new SKColor(R, G, B));
        }

        public override void ClearNode()
        {
            return;
        }
    }
}
