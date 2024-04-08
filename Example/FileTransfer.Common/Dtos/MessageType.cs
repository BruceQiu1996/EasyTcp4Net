namespace FileTransfer.Common.Dtos
{
    public enum MessageType : int
    {
        ApplyToTransfer = 1, //申请发送文件的请求到服务端

        MembersPush = 10, //服务端主动给客户端下发客户端列表
        MembersPull = 11, //客户端主动从服务端获取客户端列表
    }
}
