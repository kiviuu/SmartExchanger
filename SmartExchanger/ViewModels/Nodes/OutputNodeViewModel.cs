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


        public SKImage? InputTexture { get; set; }
        public Action? RequestRender { get; set; }
        public OutputNodeViewModel()
        {
            Title = "Output Node";
            Inputs.Add(new ConnectorViewModel(this, "In"));
            //ProcessNode();
        }

        public override void ProcessNode(GRContext context, int size)
        {
            if (context is null) return;
            if (InputTexture is null)
            {
                CurrentTexture = null;
                OnPropertyChanged(nameof(CurrentTexture));
                return;
            }
            CurrentTexture = InputTexture;
            //ResultTexture = CurrentTexture.ToWriteableBitmap();
            OnPropertyChanged(nameof(CurrentTexture));
        }

        public override void ClearNode()
        {
            //ResultTexture = null;
            CurrentTexture = null;
            InputTexture = null;
        }
    }
}
