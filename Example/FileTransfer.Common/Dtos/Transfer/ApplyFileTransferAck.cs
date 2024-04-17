﻿using FileTransfer.Common.Dtos.Messages;

namespace FileTransfer.Common.Dtos.Transfer
{
    public class ApplyFileTransferAck : Message
    {
        public string FileSendId { get; set; }
        public bool Approve { get; set; }
        public string Message { get; set; }
    }
}