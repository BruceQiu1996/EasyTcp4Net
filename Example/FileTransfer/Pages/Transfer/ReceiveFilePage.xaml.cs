using FileTransfer.ViewModels.Transfer;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FileTransfer.Pages.Transfer
{
    /// <summary>
    /// Interaction logic for ReceiveFilePage.xaml
    /// </summary>
    public partial class ReceiveFilePage : Page
    {
        public ReceiveFilePage(ReceiveFilePageViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void ListView_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!e.Handled)
            {
                e.Handled = true;
                var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
                eventArg.RoutedEvent = MouseWheelEvent;
                eventArg.Source = sender;
                var parent = ((Control)sender).Parent as UIElement;
                parent.RaiseEvent(eventArg);
            }
        }

        private void ScrollViewer_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            e.Handled = true;
        }
    }
}
