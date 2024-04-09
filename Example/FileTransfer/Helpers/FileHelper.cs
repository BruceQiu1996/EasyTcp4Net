using System.IO;
using System.Security.Cryptography;

namespace FileTransfer.Helpers
{
    public class FileHelper
    {
        public string ToSHA256(Stream stream)
        {
            string code = string.Empty;
            using (SHA256 mySHA256 = SHA256.Create())
            {
                var sha256Bytes = mySHA256.ComputeHash(stream);
                code = BitConverter.ToString(sha256Bytes).Replace("-", "").ToUpper();
            }

            return code;
        }

        public string ToSizeText(long size)
        {
            if (size == 0)
                return "0KB";

            if (size < 1024)
            {
                return $"{size}B";
            }
            else if (size >= 1024 && size < 1024 * 1024)
            {
                return $"{size * 1.0 / 1024:0.0}KB";
            }
            else if (size >= 1024 * 1024 && size < 1024 * 1024 * 1024)
            {
                return $"{size * 1.0 / (1024 * 1024):0.0}MB";
            }
            else
            {
                return $"{size * 1.0 / (1024 * 1024 * 1024):0.0}GB";
            }
        }
    }
}
