using Microsoft.Extensions.Options;
using SmartExchanger.Services;
using SmartExchanger.Configuration;
using SmartExchanger.Options;
using SmartExchanger.Persistence;

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
                .UseContentRoot(AppContext.BaseDirectory)
                .ConfigureServices((context, services) =>
                {
                    // Add other services as needed
                    services.AddAppOptions(context.Configuration);
                    //services.AddHttpClient<Repositories.CurrencyRepository>((sp, client) =>
                    //{
                    //    var config = sp.GetRequiredService<IOptions<Models.ApiSettings>>().Value;
                    //    client.BaseAddress = new Uri(config.BaseUrl);
                    //    client.Timeout = TimeSpan.FromSeconds(config.Timeout);
                    //    client.DefaultRequestHeaders.Add("Accept", "application/json");
                    //});

                    //views
                    services.AddTransient<MainView>();
                    services.AddTransient<SplashWindow>();

                    // view models
                    services.AddTransient<EditorViewModel>();
                    services.AddTransient<MainViewModel>();
                    services.AddTransient<MaterialPreviewViewModel>();
                    services.AddTransient<TexturePreviewViewModel>();

                    //services
                    services.AddSingleton<IShaderService, ShaderService>();
                    services.AddSingleton<INodeFactory, NodeFactory>();
                    services.AddSingleton<INodeStateSerializer,NodeStateSerializer>();
                    services.AddSingleton<IGraphPersistenceService, GraphPersistenceService>();
                })
                .Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            try
            {
                await _host.StartAsync();
                //ValidateConfiguration();
                var applicationOptions = _host.Services.GetRequiredService<IOptions<ApplicationOptions>>().Value;

                var splashWindow = _host.Services.GetRequiredService<SplashWindow>();
                splashWindow.Show();

                await Dispatcher.InvokeAsync(static () => { }, System.Windows.Threading.DispatcherPriority.Loaded);

                Task minimumSplashTime = Task.Delay(applicationOptions.SplashMinimumDisplayMilliseconds);


                var mainView = _host.Services.GetRequiredService<MainView>();

                await minimumSplashTime;
                MainWindow = mainView;
                ShutdownMode = ShutdownMode.OnMainWindowClose;
                mainView.Show();
                splashWindow.Close();
            }
            catch (OptionsValidationException exception)
            {
                string failures = string.Join(Environment.NewLine,exception.Failures);

                MessageBox.Show($"Invalid application configuration:\n\n{failures}","Smart Exchanger",
                    MessageBoxButton.OK, MessageBoxImage.Error);

                Shutdown(-1);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Application startup failed. \n\n {ex.Message}", "Something went wrong", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(-1);
            }
            
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            using (_host)
            {
                await _host.StopAsync(TimeSpan.FromSeconds(5));
            }
            base.OnExit(e);
        }

        //private void ValidateConfiguration()
        //{
        //    ArgumentNullException.ThrowIfNull(_host);

        //    IServiceProvider services =_host.Services;
        //    _ = services
        //        .GetRequiredService<IOptions<ApplicationOptions>>()
        //        .Value;
        //    _ = services
        //        .GetRequiredService<IOptions<RenderingOptions>>()
        //        .Value;
        //    _ = services
        //        .GetRequiredService<IOptions<MaterialPreviewOptions>>()
        //        .Value;
        //    _ = services
        //        .GetRequiredService<IOptions<ShaderOptions>>()
        //        .Value;
        //    _ = services
        //        .GetRequiredService<IOptions<ExportOptions>>()
        //        .Value;
        //}
    }
}
