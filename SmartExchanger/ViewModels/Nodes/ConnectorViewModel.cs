using CommunityToolkit.Mvvm.ComponentModel;
using System.Reflection.Metadata;

namespace SmartExchanger.ViewModels.Nodes
{
    public partial class ConnectorViewModel : ObservableObject
    {
        public BaseNodeViewModel Node { get; }
        public string Id { get; }
        public string Title { get; }

        [ObservableProperty]
        private Point _anchor;

        [ObservableProperty]
        private bool _isConnected;

        public ConnectorViewModel(BaseNodeViewModel node, string title, string id)
        {
            Node = node;
            Title = title;
            Id = id;
        }
    }
}
