using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SmartExchanger.Views
{
    public partial class SplashWindow : Window
    {
        public string CopyrightText { get;  }
        public string VersionText { get; }
        public SplashWindow()
        {
            this.CopyrightText = $"© {DateTime.Now.Year} Bartosz Zięba. All rights reserved.";
            Version? version = Assembly.GetExecutingAssembly().GetName().Version;
            VersionText = version is null ? string.Empty : $"Version {version.Major}.{version.Minor}.{version.Build}";

            InitializeComponent();

            DataContext = this;
        }
    }
}
