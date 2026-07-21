using CommunityToolkit.Mvvm.ComponentModel;
using SkiaSharp;

namespace SmartExchanger.ViewModels.Nodes
{
    public partial class ValueNodeViewModel : BaseNodeViewModel
    {
        [ObservableProperty]
        private float _value = 0.5f;

        public ConnectorViewModel OutputConnector { get; }
        public ValueNodeViewModel()
        {
            Title = "Value";
            OutputConnector = new ConnectorViewModel(this, "Out");
            Outputs.Add(OutputConnector);
        }
        public override SKImage? Render(GRContext context, int size, NodeRenderInputs inputs)
        {
            float clambedValue = Math.Clamp(Value, 0f, 1f);
            byte channel = (byte)Math.Round(clambedValue * 255f);
            using var surface = CreateGpuSurface(context, size);
            surface.Canvas.Clear(new SKColor(channel, channel, channel, 255));
            return surface.Snapshot();
        }
    }
}
