using FileTransfer.Common.Core;
using FileTransfer.Common.Dtos.Messages;
using FileTransfer.Common.Dtos.Transfer;
using System.Buffers.Binary;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace FileTransfer.Common.Dtos
{
    [StructLayout(LayoutKind.Sequential)]
    public class Packet<TBody> where TBody : Message
    {
        //序列号 8字节
        public long Sequence { get; private set; }
        //数据包包体长度 4字节
        public int BodyLength { get; private set; }
        //消息类型 4字节
        public MessageType MessageType { get; set; }
        public TBody? Body { get; set; }
        private Packet<TBody> Deserialize(byte[] bodyData)
        {
            var bodyStr = System.Text.Encoding.Default.GetString(bodyData);
            Body = JsonSerializer.Deserialize<TBody>(bodyStr);

            return this;
        }

        public Packet()
        {
            Sequence = SequenceIncreaseHelper.Next();
        }

        public static Packet<TBody> FromBytes(ReadOnlyMemory<byte> data)
        {
            Packet<TBody> packet = new Packet<TBody>();
            packet.Sequence = BinaryPrimitives.ReadInt64BigEndian(data.Slice(0, 8).Span);
            packet.BodyLength = BinaryPrimitives.ReadInt32BigEndian(data.Slice(8, 4).Span);
            packet.MessageType = (MessageType)BinaryPrimitives.ReadInt32BigEndian(data.Slice(12, 4).Span);
            packet.Deserialize(data.Slice(16, packet.BodyLength).Span.ToArray());

            return packet;
        }

        public byte[] Serialize()
        {
            var Length = 8 + 4 + 4;
            var bodyArray = System.Text.Encoding.Default.GetBytes(JsonSerializer.Serialize(Body));
            BodyLength = bodyArray.Length;
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
