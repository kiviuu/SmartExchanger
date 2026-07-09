using CommunityToolkit.Mvvm.ComponentModel;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace SmartExchanger.ViewModels.Nodes
{
    public abstract partial class BaseNodeViewModel : ObservableObject
    {
        [ObservableProperty]
        private Point _location;

        [ObservableProperty]
        private string _title = "Node";

        public ObservableCollection<ConnectorViewModel> Inputs { get; set; } = new();
        public ObservableCollection<ConnectorViewModel> Outputs { get; set; } = new();

        public SKBitmap? CurrentTexture { get; set; }
        public abstract void ProcessNode(int size);

        public event Action? PropsChanged;
        public abstract void ClearNode();

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.PropertyName != nameof(Location) &&
                e.PropertyName != nameof(Title) &&
                e.PropertyName != "CurrentTexture" &&
                e.PropertyName != "ResultTexture" &&
                e.PropertyName != "InputTexture" &&
                e.PropertyName != "InputTextureA" &&
                e.PropertyName != "InputTextureB")
            {
                PropsChanged?.Invoke();
            }
        }
    }
}
