using CommunityToolkit.Mvvm.ComponentModel;

namespace SmartExchanger.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private EditorViewModel _editor = new();
    }
}