using FileTransfer.Helpers;
using FileTransfer.Pages;
using FileTransfer.Pages.Transfer;
using FileTransfer.Resources;
using FileTransfer.ViewModels;
using FileTransfer.ViewModels.Transfer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;
using System.Windows;

namespace FileTransfer
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
                service.AddSingleton<MainPageViewModel>();
                service.AddSingleton<MainPage>();

                service.AddSingleton<TransferPage>();
                service.AddSingleton<TransferPageViewModel>();
                service.AddSingleton<SendFilePage>();
                service.AddSingleton<SendFilePageViewModel>();
                service.AddSingleton<ReceiveFilePage>();
                service.AddSingleton<ReceiveFilePageViewModel>();
                service.AddSingleton<CompleteTransferPage>();
                service.AddSingleton<CompleteTransferPageViewModel>();

                service.AddSingleton<IniHelper>();
                service.AddSingleton<IniSettings>();
                service.AddSingleton<NetHelper>();
                service.AddSingleton<DBHelper>();
                service.AddSingleton<FileHelper>();
                service.AddSingleton<GrowlHelper>();

                var connString = $"Data Source = {Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "storage.db")}";
                service.AddDbContext<FileTransferDbContext>(option =>
                {
                    option.UseSqlite(connString, options =>
                    {
                        options.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                        options.MaxBatchSize(100);
                        options.CommandTimeout(10000);
                    });
                }, ServiceLifetime.Singleton);
            });

            host = builder.Build();
            ServiceProvider = host.Services;
            await host.StartAsync();
            await ServiceProvider.GetRequiredService<IniSettings>().InitializeAsync();
            host.Services.GetRequiredService<MainWindow>().Show();
        }
    }
}
