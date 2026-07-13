using CommunityToolkit.Mvvm.ComponentModel;
using SkiaSharp;

namespace SmartExchanger.ViewModels.Nodes
{
    public partial class TextureSizeNodeViewModel : BaseNodeViewModel
    {
        [ObservableProperty]
        private int _selectedSize = 512;

        public IReadOnlyList<int> AvailableSizes { get; } =
            new[] { 128, 256, 512, 1024, 2048, 4096 };

        public override bool ProducesTexture => false;

        public TextureSizeNodeViewModel()
        {
            Title = "Texture Size";
        }

        public override SKImage? Render(GRContext context, int size, NodeRenderInputs inputs)
        {
            return null;
        }
    }
}
