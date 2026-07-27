using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Media;

namespace SmartExchanger.ViewModels.Nodes
{
    public partial class GradientStopViewModel
        : ObservableObject
    {
        [ObservableProperty]
        private float _position;

        [ObservableProperty]
        private byte _red;

        [ObservableProperty]
        private byte _green;

        [ObservableProperty]
        private byte _blue;

        [ObservableProperty]
        private byte _alpha = 255;

        private SolidColorBrush _previewBrush =
            Brushes.Transparent;

        public SolidColorBrush PreviewBrush
        {
            get => _previewBrush;

            private set =>
                SetProperty(
                    ref _previewBrush,
                    value);
        }

        public string HexColor => $"#{Alpha:X2}{Red:X2}{Green:X2}{Blue:X2}";

        public GradientStopViewModel(float position, byte red, byte green, byte blue, byte alpha = 255)
        {
            _position = position;
            _red = red;
            _green = green;
            _blue = blue;
            _alpha = alpha;

            UpdateColorPresentation();
        }

        partial void OnPositionChanged(float value)
        {
            
        }

        partial void OnRedChanged(byte value)
        {
            UpdateColorPresentation();
        }

        partial void OnGreenChanged( byte value)
        {
            UpdateColorPresentation();
        }

        partial void OnBlueChanged(byte value)
        {
            UpdateColorPresentation();
        }

        partial void OnAlphaChanged(byte value)
        {
            UpdateColorPresentation();
        }

        private void UpdateColorPresentation()
        {
            var brush = new SolidColorBrush(Color.FromArgb(Alpha, Red, Green, Blue));

            brush.Freeze();
            PreviewBrush = brush;
            OnPropertyChanged(nameof(HexColor));
        }
    }
}