using System.Windows;
using SmartExchanger.ViewModels;

namespace SmartExchanger.Views
{
    public partial class MainView : Window
    {
        public MainView()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }
    }
}