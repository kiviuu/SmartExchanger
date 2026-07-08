using CommunityToolkit.Mvvm.ComponentModel;
using SmartExchanger.ViewModels.Nodes;

namespace SmartExchanger.ViewModels
{
    public partial class ConnectionViewModel : ObservableObject
    {
        [ObservableProperty]
        private ConnectorViewModel _source;

        [ObservableProperty]
        private ConnectorViewModel _target;

        public ConnectionViewModel(ConnectorViewModel source, ConnectorViewModel target)
        {
            _source = source;
            _target = target;
        }
    }
}
