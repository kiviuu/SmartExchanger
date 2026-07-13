using CommunityToolkit.Mvvm.ComponentModel;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace SmartExchanger.ViewModels.Nodes
{
    /// <summary>
    /// Based model for Nodes
    /// </summary>
    public abstract partial class BaseNodeViewModel : ObservableObject
    {
        [ObservableProperty]
        private Point _location;

        [ObservableProperty]
        private string _title = "Node";

        public ObservableCollection<ConnectorViewModel> Inputs { get; } = new();
        public ObservableCollection<ConnectorViewModel> Outputs { get; } = new();

        public event Action? PropsChanged;

        /// <summary>
        /// Creates node results in provided GRContext
        /// Contract:
        ///  - returned SKImage belongs to EditorVM
        ///  - null means lack of output signal
        ///  - node can not return inputted image 
        ///  - node can not Dispose images from inputs
        /// </summary>
        public abstract SKImage? Render(GRContext context, int size, NodeRenderInputs inputs);

        public virtual bool ProducesTexture => true;

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (IsRenderAffectingProperty(e.PropertyName))
            {
                PropsChanged?.Invoke();
            }
        }

        /// <summary>
        /// Which class's affect graph
        /// </summary>
        protected virtual bool IsRenderAffectingProperty(string? propertyName)
        {
            return propertyName is not null &&
                   propertyName != nameof(Location) &&
                   propertyName != nameof(Title);
        }

        protected static SKSurface CreateGpuSurface(GRContext context, int size)
        {
            ArgumentNullException.ThrowIfNull(context);

            if (size <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

            var info = new SKImageInfo(size, size, SKColorType.Rgba8888, SKAlphaType.Premul);

            return SKSurface.Create(context, true, info)
                   ?? throw new InvalidOperationException(
                       $"SkiaSharp could not create a {size}x{size} GPU surface.");
        }
    }
}
