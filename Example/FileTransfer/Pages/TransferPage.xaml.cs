using FileTransfer.ViewModels;
using System.Windows.Controls;

namespace FileTransfer.Pages
{
    /// <summary>
    /// Interaction logic for Transfer.xaml
    /// </summary>
    public partial class TransferPage : Page
    {
        public TransferPage(TransferPageViewModel transferPageViewModel)
        {
            InitializeComponent();
            DataContext = transferPageViewModel;
        }
    }
}
