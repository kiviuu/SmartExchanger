using CommunityToolkit.Mvvm.ComponentModel;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using System.Drawing;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SmartExchanger.ViewModels.Nodes
{
    // contain final render resul - ending Node
    public partial class OutputNodeViewModel : BaseNodeViewModel
    {
        //[ObservableProperty]
        //private SolidColorBrush _inputBrush = new SolidColorBrush(Colors.Transparent);

        [ObservableProperty]
        private WriteableBitmap? _resultTexture;
        public OutputNodeViewModel()
        {
            Title = "Output Node";
            Inputs.Add(new ConnectorViewModel(this, "In"));
            //ProcessNode();
        }

        public override void ProcessNode(int size)
        {
            if (CurrentTexture is null || CurrentTexture.Width != size || CurrentTexture.Height != size)
            {
                CurrentTexture?.Dispose();
                CurrentTexture = new SKBitmap(size, size);
            }
            ResultTexture = CurrentTexture?.ToWriteableBitmap();
            OnPropertyChanged(nameof(CurrentTexture));
        }

        public override void ClearNode()
        {
            ResultTexture = null;
            CurrentTexture = null;
        }
    }
}
