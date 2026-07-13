using CommunityToolkit.Mvvm.ComponentModel;

namespace SmartExchanger.ViewModels
{
    public partial class MainViewModel : ObservableObject, IDisposable
    {
        public EditorViewModel Editor { get; } = new();

        public void Dispose()
        {
            Editor.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
