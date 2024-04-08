using System.Windows;

namespace FileTransfer
{
    /// <summary>
    /// Interaction logic for AgreeConnectWindow.xaml
    /// </summary>
    public partial class AgreeConnectWindow : Window
    {
        CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();
        public AgreeConnectWindow(string remoteEndPoint)
        {
            InitializeComponent();
            title.Text = $"主机：{remoteEndPoint} 申请连接您的主机";

            var _ = Task.Run(() =>
            {
                int i = 10;
                while (i > 0)
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        time.Text = $"{i}s";
                    });

                    i--;
                    Thread.Sleep(1000);
                }

                if (!CancellationTokenSource.IsCancellationRequested)
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        DialogResult = false;
                    });
                }
            }, CancellationTokenSource.Token);
        }

        private void Border_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
                DragMove();
        }

        private async void Label_MouseLeftButtonDown_1(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            CancellationTokenSource.Cancel();
            DialogResult = false;
        }

        /// <summary>
        /// 拒绝连接
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            CancellationTokenSource.Cancel();
            DialogResult = false;
        }

        /// <summary>
        /// 同意连接
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            CancellationTokenSource.Cancel();
            DialogResult = true;
        }
    }
}
