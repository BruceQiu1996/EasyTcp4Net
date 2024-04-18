﻿namespace FileTransfer.Common.Dtos.Messages
{
    public enum MessageType : int
    {
        ConnectionAck = 1, //连接后的回复

        ApplyTrasnfer = 2,
        ApplyTrasnferAck = 3,

        FileSend = 4,
        CancelSend = 5,
        FileSendComplete = 6,
    }
}
