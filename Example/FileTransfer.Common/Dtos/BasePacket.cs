using FileTransfer.Common.Core;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace FileTransfer.Common.Dtos
{
    [StructLayout(LayoutKind.Sequential)]
    public class Packet<TBody>
    {
        //序列号 8字节
        public long Sequence { get; set; }
        //数据包包体长度 4字节
        public int Length { get; set; }
        //消息类型 4字节
        public MessageType MessageType { get; set; }
        public TBody? Body { get; set; }
        public Packet<TBody> Deserialize(byte[] bodyData)
        {
            var bodyStr = System.Text.Encoding.Default.GetString(bodyData);
            Body = JsonSerializer.Deserialize<TBody>(bodyStr);

            return this;
        }

        public byte[] Serialize()
        {
            Length = 8 + 4 + 4;
            var bodyArray = System.Text.Encoding.Default.GetBytes(JsonSerializer.Serialize(Body));
            Length += bodyArray.Length;
            byte[] result = new byte[Length];
            result.AddInt64(0, Sequence);
            result.AddInt32(8, bodyArray.Length);
            result.AddInt32(12, (int)MessageType);
            Buffer.BlockCopy(bodyArray, 0, result, 16, bodyArray.Length);

            return result;
        }
    }
}
