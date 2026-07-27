using SkiaSharp;

namespace SmartExchanger.ViewModels.Nodes
{
    public partial class TexturePreviewNodeViewModel : BaseNodeViewModel
    {
        public ConnectorViewModel InputConnector { get; }

        public TexturePreviewNodeViewModel()
        {
            Title = "Texture Preview";
            InputConnector = new ConnectorViewModel(this, "In", "textureIn");
            Inputs.Add(InputConnector);
        }

        public override bool ProducesTexture => false;
        public override SKImage? Render(GRContext context, int size, NodeRenderInputs inputs)
        {
            return null;
        }
    }
}
