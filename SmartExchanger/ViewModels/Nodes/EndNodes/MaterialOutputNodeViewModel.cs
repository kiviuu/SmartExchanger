using SkiaSharp;

namespace SmartExchanger.ViewModels.Nodes
{
    public partial class MaterialOutputNodeViewModel : BaseNodeViewModel
    {
        public ConnectorViewModel BaseColorConnector { get; }
        public ConnectorViewModel NormalConnector { get; }
        public ConnectorViewModel RoughnessConnector { get; }
        public ConnectorViewModel MetallicConnector { get; }
        public override bool ProducesTexture =>  false;

        public MaterialOutputNodeViewModel()
        {
            Title = "Material Output";
            BaseColorConnector = new ConnectorViewModel(this, "Base Color");
            NormalConnector = new ConnectorViewModel(this, "Normal");
            RoughnessConnector = new ConnectorViewModel(this, "Roughness");
            MetallicConnector = new ConnectorViewModel(this, "Metalic");
            Inputs.Add(BaseColorConnector);
            Inputs.Add(NormalConnector);
            Inputs.Add(RoughnessConnector);
            Inputs.Add(MetallicConnector);
        }

        public override SKImage? Render(GRContext context, int size, NodeRenderInputs inputs)
        {
            return null;
        }
    }
}
