using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FileTransfer.Helpers;
using FileTransfer.Models;
using FileTransfer.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Windows;

namespace FileTransfer.ViewModels
{
    /// <summary>
    /// 连接远端的连接对象
    /// </summary>
    public class RemoteChannelViewModel : ObservableObject
    {
        public string Id { get; set; }
        private string remark;
        public string Remark
        {
            get => remark;
            set => SetProperty(ref remark, value);
        }

        public string IPAddress { get; set; }
        public ushort Port { get; set; }

        private bool connected;
        public bool Connected
        {
            get => connected;
            set => SetProperty(ref connected, value);
        }

        private string status;
        public string Status
        {
            get => status;
            set => SetProperty(ref status, value);
        }

        public RemoteChannelViewModel(string id, string ip, ushort port, string remark = null)
        {
            Id = id;
            IPAddress = ip;
            Port = port;
            Remark = remark;
            DropFilesCommandAsync = new AsyncRelayCommand<DragEventArgs>(DropFilesAsync);
        }

        public AsyncRelayCommand ConnectCommandAsync { get; set; }
        public AsyncRelayCommand<DragEventArgs> DropFilesCommandAsync { get; set; }

        /// <summary>
        /// 拖拽发送文件
        /// </summary>
        /// <returns></returns>
        private async Task DropFilesAsync(DragEventArgs e)
        {
            var files = (Array)e.Data.GetData(DataFormats.FileDrop);
            foreach (var file in files)
            {
                if (File.Exists(file.ToString()))
                {
                    await SendFileAsync(file.ToString()!);
                }
            }
        }


        /// <summary>
        /// 发送新文件
        /// </summary>
        /// <param name="file">文件路径</param>
        /// <returns></returns>
        private async Task SendFileAsync(string file)
        {
            if (!File.Exists(file))
            {
                HandyControl.Controls.MessageBox
                    .Show("文件不存在", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            //创建请求到对端
            try
            {
                FileInfo fileInfo = new FileInfo(file);
                string code = null; //sha265
                using (var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read))
                {
                    code = App.ServiceProvider!.GetRequiredService<FileHelper>().ToSHA256(fileStream);

                }
                //创建到数据库，并且添加到传输列表
                var record = new FileSendRecordModel(file, fileInfo.Name, code, fileInfo.Length, Id);
                var result = await App.ServiceProvider!.GetRequiredService<DBHelper>().AddFileSendRecordAsync(record);
                if (!result)
                    throw new Exception("写入到数据库错误");

                //发送到界面
                var channel = await App.ServiceProvider!.GetRequiredService<FileTransferDbContext>()
                    .RemoteChannels.FirstOrDefaultAsync(x => x.Id == Id);

                //根据channel和发送记录生成发送任务viewmodel
                var fileSendViewModel = new FileSendViewModel(record, channel);
                WeakReferenceMessenger.Default.Send(fileSendViewModel, "AddSendFileRecord");
                await fileSendViewModel.StartSendFileAsync();
            }
            catch (Exception ex)
            {
                HandyControl.Controls.MessageBox
                    .Show("文件异常,发送失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 从模型映射到VM
        /// </summary>
        /// <param name="channelModel">数据模型</param>
        /// <returns></returns>
        public static RemoteChannelViewModel FromModel(RemoteChannelModel channelModel)
        {
            return new RemoteChannelViewModel(channelModel.Id, channelModel.IPAddress, channelModel.Port, channelModel.Remark);
        }
    }
}
