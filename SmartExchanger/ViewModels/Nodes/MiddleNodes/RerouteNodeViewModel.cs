using SkiaSharp;

namespace SmartExchanger.ViewModels.Nodes
{
    public class RerouteNodeViewModel : BaseNodeViewModel
    {
        public ConnectorViewModel Input { get; }
        public ConnectorViewModel Output { get; }
        public RerouteNodeViewModel()
        {
            Title = "";
            Input = new ConnectorViewModel(this, "In");
            Output = new ConnectorViewModel(this, "Out");
            Inputs.Add(Input);
            Outputs.Add(Output);
        }
        public override SKImage? Render(GRContext context, int size, NodeRenderInputs inputs)
        {
            using var surface = CreateGpuSurface(context, size);
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Transparent);

            var destination = new SKRect(0, 0, size, size);
            var input = inputs.Get(Input);

            if (input is not null)
            {
                canvas.DrawImage(input, destination, new SKSamplingOptions());
            }

            return surface.Snapshot();
        }
    }
}
