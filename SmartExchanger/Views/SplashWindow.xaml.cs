using Microsoft.Extensions.Options;
using SmartExchanger.Options;
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
        public string ApplicationName { get; }
        public string Subtitle { get; }
        public string AuthorText { get; }
        public string CopyrightText { get;  }
        public string VersionText { get; }
        public SplashWindow(IOptions<ApplicationOptions> options)
        {
            ArgumentNullException.ThrowIfNull(options);
            ApplicationOptions settings = options.Value;

            ApplicationName = settings.Name;
            Subtitle = settings.Subtitle;

            AuthorText = $"Created by {settings.Author}";

            CopyrightText =$"© {DateTime.Now.Year} {settings.CopyrightOwner}. All rights reserved.";

            Version? version = Assembly.GetExecutingAssembly().GetName().Version;

            VersionText = version is null ? string.Empty
                    : $"Version {version.Major}.{version.Minor}.{version.Build}";

            InitializeComponent();
            DataContext = this;
        }
    }
}
