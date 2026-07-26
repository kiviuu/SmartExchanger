using Microsoft.Extensions.Options;
using SmartExchanger.Services;

namespace SmartExchanger
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly IHost _host;
        public App()
        {
            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // Add other services as needed
                    services.Configure<Models.AppSettingsModel>(context.Configuration.GetSection("Default"));
                    //services.AddHttpClient<Repositories.CurrencyRepository>((sp, client) =>
                    //{
                    //    var config = sp.GetRequiredService<IOptions<Models.ApiSettings>>().Value;
                    //    client.BaseAddress = new Uri(config.BaseUrl);
                    //    client.Timeout = TimeSpan.FromSeconds(config.Timeout);
                    //    client.DefaultRequestHeaders.Add("Accept", "application/json");
                    //});

                    //views
                    services.AddTransient<MainView>();

                    // view models
                    services.AddTransient<EditorViewModel>();
                    services.AddTransient<MainViewModel>();
                    services.AddTransient<MaterialPreviewViewModel>();
                    services.AddTransient<TexturePreviewViewModel>();

                    //services
                    services.AddSingleton<IShaderService, ShaderService>();
                })
                .Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            await _host.StartAsync();
            base.OnStartup(e);

            var splashWindow = new SplashWindow();
            splashWindow.Show();

            await Dispatcher.InvokeAsync(static () => { }, System.Windows.Threading.DispatcherPriority.Loaded);

            Task minimumSplashTime = Task.Delay(2000);
            

            var mainView = _host.Services.GetRequiredService<MainView>();

            await minimumSplashTime;
            MainWindow = mainView;
            ShutdownMode = ShutdownMode.OnMainWindowClose;
            mainView.Show();
            splashWindow.Close();
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            using (_host)
            {
                await _host.StopAsync(TimeSpan.FromSeconds(5));
            }
            base.OnExit(e);
        }
    }
}
