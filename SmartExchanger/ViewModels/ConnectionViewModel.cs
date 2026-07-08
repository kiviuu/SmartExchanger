using CommunityToolkit.Mvvm.ComponentModel;

namespace SmartExchanger.ViewModels
{
    // Reprsents connection between two nodes in the Nodify graph
    public partial class ConnectionViewModel : ObservableObject
    {
        [ObservableProperty]
        private BaseNodeViewModel? _source;

        [ObservableProperty]
        private BaseNodeViewModel? _target;
    }
}
