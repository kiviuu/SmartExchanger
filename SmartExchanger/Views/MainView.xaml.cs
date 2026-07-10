using System.Windows;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;
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