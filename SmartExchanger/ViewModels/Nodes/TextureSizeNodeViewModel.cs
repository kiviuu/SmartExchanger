using CommunityToolkit.Mvvm.ComponentModel;

namespace SmartExchanger.ViewModels.Nodes
{
    public partial class TextureSizeNodeViewModel : BaseNodeViewModel
    {
        [ObservableProperty]
        private int _selectedSize = 512;
        public IReadOnlyList<int> AvailableSizes { get; } = new List<int>() { 128, 256, 512, 1024, 2048, 4096 };
        public event Action? PropsChanged;
        public TextureSizeNodeViewModel()
        {
            Title = "Texture Size";
        }
        public override void ClearNode()
        {
            return;
        }

        public override void ProcessNode(int size)
        {
            return;
        }

        partial void OnSelectedSizeChanged(int value)
        {
            PropsChanged?.Invoke();
        }
    }
}
