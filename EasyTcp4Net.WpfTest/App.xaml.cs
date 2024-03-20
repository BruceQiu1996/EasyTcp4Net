using System.Windows;

namespace EasyTcp4Net.WpfTest
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            Startup += (e,a) =>
            {
                var mainWindow = new MainWindow();
                mainWindow.Show();
            };   
        }
    }

}
