using FileTransfer.Helpers;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace FileTransfer
{
    /// <summary>
    /// Interaction logic for AgreeConnectWindow.xaml
    /// </summary>
    public partial class AgreeTransferWindow : Window
    {
        CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();
        public AgreeTransferWindow(string remoteEndPoint, string fileName, long size)
        {
            InitializeComponent();
            var sizeText = App.ServiceProvider.GetRequiredService<FileHelper>().ToSizeText(size);
            title.Text = $"主机：{remoteEndPoint} 申请发送文件 [{fileName}],文件大小：{sizeText}";

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
