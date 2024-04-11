using CommunityToolkit.Mvvm.ComponentModel;
using EasyTcp4Net;
using FileTransfer.Common.Dtos;
using FileTransfer.Common.Dtos.Transfer;
using FileTransfer.Helpers;
using FileTransfer.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Buffers;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace FileTransfer.ViewModels
{
    public class FileSendViewModel : ObservableObject
    {
        public string Id { get; set; }
        public string RemoteId { get; set; }
        public string TransferToken { get; private set; }
        public string FileName { get; set; }
        public string FileLocation { get; set; }
        public FileSendStatus Status { get; set; }
        public BitmapImage Icon => App.ServiceProvider!.GetRequiredService<FileHelper>().GetIconByFileExtension(FileName).Item2;
        public long Size { get; set; } //发送文件的大小
        private long _transferBytes;
        //已经传输的字节
        public long TransferBytes
        {
            get => _transferBytes;
            set
            {
                _transferBytes = value;
                Progress = TransferBytes / (double)Size * 100;
            }
        }
        public string SizeText => App.ServiceProvider!.GetRequiredService<FileHelper>().ToSizeText(Size);
        private double _progress;
        //传输的进度
        public double Progress
        {
            get => _progress;
            set => SetProperty(ref _progress, value);
        }

        private string _speed;
        public string Speed
        {
            get => _speed;
            set => SetProperty(ref _speed, value);
        }

        public string Remote { get; set; }

        public FileSendViewModel() { }

        public static FileSendViewModel FromModel(FileSendRecordModel model, RemoteChannelModel remoteChannelModel)
        {
            FileSendViewModel fileSendViewModel = new FileSendViewModel();
            fileSendViewModel.Id = model.Id;
            fileSendViewModel.RemoteId = remoteChannelModel.Id;
            fileSendViewModel.FileName = model.FileName;
            fileSendViewModel.FileLocation = model.FileLocation;
            fileSendViewModel.Status = model.Status;
            fileSendViewModel.Size = model.TotalSize;
            fileSendViewModel.TransferBytes = model.TransferedSize;
            fileSendViewModel.Progress = fileSendViewModel.Size == 0 ? 100 : fileSendViewModel.TransferBytes * 100 / fileSendViewModel.Size;
            fileSendViewModel.Remote = $"目标主机：{remoteChannelModel.IPAddress}" + (string.IsNullOrEmpty(remoteChannelModel.Remark) ? "" : $"（{remoteChannelModel.Remark}）");

            return fileSendViewModel;
        }

        /// <summary>
        /// 发送文件数据
        /// </summary>
        /// <param name="startIndex"></param>
        /// <param name="easyTcpClient"></param>
        /// <returns></returns>
        public async Task SendAsync(int startIndex, EasyTcpClient easyTcpClient, string transferToken)
        {
            TransferToken = transferToken;
            FileInfo fileInfo = new FileInfo(FileLocation);
            if (startIndex >= fileInfo.Length)
            {
                //文件传输完成
            }

            var sended = startIndex;
            int bufferSize = 1024 * 100;
            DateTime _lastRecordSend = DateTime.Now; //区间测速的时间
            long recordTransferedBytes = 0;//区间测速的字节数
            int segement = 1; //总共的段数
            var fileHelper = App.ServiceProvider.GetRequiredService<FileHelper>();
            long needTransfer = fileInfo.Length - startIndex;
            var _ = Task.Run(async () =>
            {
                using (var fileStream = File.OpenRead(FileLocation))
                {
                    byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
                    //分段
                    if (needTransfer > buffer.Length)
                    {
                        segement = (int)(needTransfer / buffer.Length);
                        if (needTransfer % buffer.Length != 0)
                        {
                            segement++;
                        }
                    }
                    try
                    {
                        var totalLength = fileInfo.Length;
                        fileStream.Seek(startIndex, SeekOrigin.Begin);
                        int segementIndex = 1;
                        while (sended < totalLength)
                        {

                            var length = await fileStream.ReadAsync(buffer);
                            await easyTcpClient.SendAsync(new Packet<FileSegement>()
                            {
                                MessageType = Common.Dtos.Messages.MessageType.FileSend,
                                Body = new FileSegement()
                                {
                                    Data = buffer[..length],
                                    SegementIndex = segementIndex,
                                    TotalSegement = segement,
                                    FileSendId = Id,
                                    TransferToken = TransferToken
                                }
                            }.Serialize()).ConfigureAwait(true);
                            segementIndex++;
                            sended += length;
                            recordTransferedBytes += length;
                            //记录速度以及更新传输百分比
                            if (DateTime.Now.AddSeconds(-1) >= _lastRecordSend)
                            {
                                var speed = $"{fileHelper.ToSizeText(recordTransferedBytes)} / s";
                                var progress = sended * 100 / totalLength;

                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    Speed = speed;
                                    Progress = progress;
                                });
                                _lastRecordSend = DateTime.Now;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        //TODO异常结束发送
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(buffer, true);
                    }
                }
            });
        }
    }
}
