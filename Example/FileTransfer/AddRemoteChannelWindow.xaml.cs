using CommunityToolkit.Mvvm.Messaging;
using EasyTcp4Net;
using FileTransfer.Helpers;
using Microsoft.Extensions.DependencyInjection;
using System.Linq.Expressions;
using System.Windows;

namespace FileTransfer
{
    /// <summary>
    /// Interaction logic for AddRemoteChannelWindow.xaml
    /// </summary>
    public partial class AddRemoteChannelWindow : Window
    {
        public AddRemoteChannelWindow()
        {
            InitializeComponent();
        }

        private void Border_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
                DragMove();
        }

        private async void Label_MouseLeftButtonDown_1(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// 测试连接
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(port.Text) || string.IsNullOrEmpty(ip.Text))
                return;

            var result = App.ServiceProvider.GetRequiredService<NetHelper>().MatchIP(ip.Text.Trim());
            if (!result)
            {
                HandyControl.Controls.MessageBox
                    .Show("非法的IP地址", "错误", MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }

            var portValue = ushort.TryParse(port.Text.Trim(), out var temp) ? temp : default;
            if (portValue == default || portValue < 0 || portValue > 65535)
            {
                HandyControl.Controls.MessageBox
                        .Show("非法的端口号", "错误", MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }

            var pintResult = App.ServiceProvider.GetRequiredService<NetHelper>().CheckIPCanPing(ip.Text.Trim());
            if (pintResult)
            {
                HandyControl.Controls.MessageBox
                        .Show("网络连通", "消息", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                HandyControl.Controls.MessageBox
                           .Show("网络不通", "消息", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 保存连接
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(port.Text) || string.IsNullOrEmpty(ip.Text))
                return;

            var result = App.ServiceProvider.GetRequiredService<NetHelper>().MatchIP(ip.Text.Trim());
            if (!result)
            {
                HandyControl.Controls.MessageBox
                    .Show("非法的IP地址", "错误", MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }

            var portValue = ushort.TryParse(port.Text.Trim(), out var temp) ? temp : default;
            if (portValue == default || portValue < 0 || portValue > 65535)
            {
                HandyControl.Controls.MessageBox
                        .Show("非法的端口号", "错误", MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }

            WeakReferenceMessenger.Default
                .Send(new Tuple<string, string, ushort>(remark.Text.Trim(), ip.Text.Trim(), portValue), "AddRemoteChannel");
        }
    }
}
