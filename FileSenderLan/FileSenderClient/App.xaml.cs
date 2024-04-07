using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;
using System.Windows;

namespace FileSenderClient
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        internal static IServiceProvider? ServiceProvider;
        internal static IHost host;
        protected async override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
            var builder = Host.CreateDefaultBuilder(e.Args);
            builder.ConfigureServices((context, service) =>
            {
                service.AddSingleton<MainWindow>();
                service.AddSingleton<MainWindowViewModel>();
            });

            builder.ConfigureHostConfiguration(options =>
            {
                options.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            });

            host = builder.Build();
            ServiceProvider = host.Services;
            await host.StartAsync();
            host.Services.GetRequiredService<MainWindow>().Show();
        }
    }
}
