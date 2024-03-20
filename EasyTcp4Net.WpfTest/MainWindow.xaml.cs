using System.Windows;

namespace EasyTcp4Net.WpfTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModelFixHeader();
            //DataContext = new MainWindowViewModel();
        }
    }
}