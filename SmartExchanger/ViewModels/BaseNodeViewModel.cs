using CommunityToolkit.Mvvm.ComponentModel;
using SkiaSharp;
using System.Diagnostics;
using System.Security.Policy;

namespace SmartExchanger.ViewModels
{
    public abstract partial class BaseNodeViewModel : ObservableObject
    {
        public string Id { get; } = Guid.NewGuid().ToString();

        [ObservableProperty]
        private string _title = "Base Node";

        [ObservableProperty]
        private Point _location;    // Node possition for Nodify canvas

        public SKBitmap? OutputTexture { get; protected set; }

        public abstract void Process(); // Node functionality, to be implemented in derived classes
    }
}
