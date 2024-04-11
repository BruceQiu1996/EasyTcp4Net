using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileTransfer.Common.Core
{
    public static class ByteArrayExtension
    {
        private static object _lock = new object();

        /// <summary>
        /// 字节数添加int8数字，默认大端
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="startIndex"></param>
        /// <param name="v"></param>
        public static void AddInt8(this byte[] buffer, int startIndex, byte v)
        {
            lock (_lock)
            {
                buffer[startIndex++] = v;
            }
        }

        /// <summary>
        /// 字节数添加int32数字，默认大端
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="startIndex"></param>
        /// <param name="v"></param>
        public static void AddInt32(this byte[] buffer, int startIndex, int v)
        {
            lock (_lock)
            {
                buffer[startIndex++] = (byte)(v >> 24);
                buffer[startIndex++] = (byte)(v >> 16);
                buffer[startIndex++] = (byte)(v >> 8);
                buffer[startIndex++] = (byte)v;
            }
        }

        /// <summary>
        /// 字节数添加int64数字，默认大端
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="startIndex"></param>
        /// <param name="v"></param>
        public static void AddInt64(this byte[] buffer, int startIndex, long v)
        {
            lock (_lock)
            {
                buffer[startIndex++] = (byte)(v >> 56);
                buffer[startIndex++] = (byte)(v >> 48);
                buffer[startIndex++] = (byte)(v >> 40);
                buffer[startIndex++] = (byte)(v >> 32);
                buffer[startIndex++] = (byte)(v >> 24);
                buffer[startIndex++] = (byte)(v >> 16);
                buffer[startIndex++] = (byte)(v >> 8);
                buffer[startIndex++] = (byte)v;
            }
        }
    }
}
