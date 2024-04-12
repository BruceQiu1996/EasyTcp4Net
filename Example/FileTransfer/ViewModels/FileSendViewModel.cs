using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using EasyTcp4Net;
using FileTransfer.Common.Dtos;
using FileTransfer.Common.Dtos.Transfer;
using FileTransfer.Helpers;
using FileTransfer.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Buffers;
using System.IO;
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
            set
            {
                SetProperty(ref _progress, value);
            }
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
        public async Task SendAsync(EasyTcpClient easyTcpClient, string transferToken)
        {
            TransferToken = transferToken;
            FileInfo fileInfo = new FileInfo(FileLocation);
            if (TransferBytes >= fileInfo.Length)
            {
                //文件传输完成
            }

            int bufferSize = 1024 * 400;
            int segement = 1; //总共的段数
            var fileHelper = App.ServiceProvider.GetRequiredService<FileHelper>();
            long needTransfer = fileInfo.Length - TransferBytes;
            await Task.Run(async () =>
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
                        fileStream.Seek(TransferBytes, SeekOrigin.Begin);
                        int segementIndex = 1;
                        while (TransferBytes < totalLength)
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
                            }.Serialize());
                            segementIndex++;
                            TransferBytes += length;
                        }
                    }
                    catch (Exception ex)
                    {
                        //TODO异常结束发送
                        //结束发送并且暂停发送

                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(buffer, true);
                    }
                }
            });

            await NormolCompletedAsync();
        }

        public async Task NormolCompletedAsync()
        {
            var dbHelper = App.ServiceProvider!.GetRequiredService<DBHelper>();
            //更新数据库
            await dbHelper.UpdateFileSendRecordCompleteAsync(Id, RemoteId, true);
            WeakReferenceMessenger.Default.Send(Id, "SendFinish");
        }
    }
}
