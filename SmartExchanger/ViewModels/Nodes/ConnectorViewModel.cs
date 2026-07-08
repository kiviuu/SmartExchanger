using CommunityToolkit.Mvvm.ComponentModel;

namespace SmartExchanger.ViewModels.Nodes
{
    public partial class ConnectorViewModel : ObservableObject
    {
        public BaseNodeViewModel Node { get; }
        public string Title { get; }

        [ObservableProperty]
        private Point _anchor;

        [ObservableProperty]
        private bool _isConnected;

        public ConnectorViewModel(BaseNodeViewModel node, string title)
        {
            Node = node;
            Title = title;
        }
    }
}
