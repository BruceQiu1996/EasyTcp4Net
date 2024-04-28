using FileTransfer.Common.Dtos.Messages;

namespace FileTransfer.Common.Dtos.Transfer
{
    /// <summary>
    /// 一个文件传输请求
    /// </summary>
    public class ApplyFileTransfer : Message
    {
        /// <summary>
        /// 文件传输
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <param name="fileSendId">发送记录编号</param>
        /// <param name="totalSize">文件总大小</param>
        /// <param name="startIndex">起始下标</param>
        /// <param name="code">文件sha256</param>
        public ApplyFileTransfer(string fileName, string fileSendId, long totalSize, int startIndex,
            string code)
        {
            FileName = fileName;
            FileSendId = fileSendId;
            TotalSize = totalSize;
            StartIndex = startIndex;
            Code = code;
        }

        /// <summary>
        /// 反序列化
        /// </summary>
        public ApplyFileTransfer() { }

        public string FileName { get; set; }
        public string FileSendId { get; set; }
        public long TotalSize { get; set; } //剩余传输的字节数
        public int StartIndex { get; set; } //起始字节，不为0则是断点续传
        public string Code { get; set; } //文件md5
    }
}
