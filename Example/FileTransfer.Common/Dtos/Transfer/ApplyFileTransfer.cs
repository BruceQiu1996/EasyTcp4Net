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
        /// <param name="totalSize">文件总大小</param>
        /// <param name="segmentCount">分段数量</param>
        /// <param name="startIndex">起始下标</param>
        /// <param name="code">文件sha256</param>
        /// <param name="sessionToken">会话的token</param>
        public ApplyFileTransfer(string fileName, long totalSize, int segmentCount, int startIndex,
            string code, string sessionToken)
        {
            FileName = fileName;
            TotalSize = totalSize;
            SegmentCount = segmentCount;
            StartIndex = startIndex;
            Code = code;
            SessionToken = sessionToken;
        }

        /// <summary>
        /// 断点续传需要多一个传输token
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <param name="totalSize">文件总大小</param>
        /// <param name="segmentCount">分段数量</param>
        /// <param name="startIndex">起始下标</param>
        /// <param name="code">文件sha256</param>
        /// <param name="sessionToken">会话的token</param>
        /// <param name="transferToken">传输的token</param>
        public ApplyFileTransfer(string fileName, long totalSize, int segmentCount, int startIndex,
            string code, string sessionToken, string transferToken) : this(fileName, totalSize, segmentCount, startIndex, code, sessionToken)
        {
            TransferToken = transferToken;
        }

        /// <summary>
        /// 反序列化
        /// </summary>
        public ApplyFileTransfer() { }

        public string FileName { get; set; }
        public long TotalSize { get; set; } //剩余传输的字节数
        public int SegmentCount { get; set; } //段数
        public int StartIndex { get; set; } //起始字节，不为0则是断点续传
        public string Code { get; set; } //文件md5
        public string SessionToken { get; set; } //会话连接的token
        public string TransferToken { get; set; } ////本次传输的Token,如果是断点续传则需要发送端携带过来，目的是方便确定文件的唯一性
    }
}
