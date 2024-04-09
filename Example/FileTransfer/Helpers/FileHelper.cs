using System.IO;
using System.Security.Cryptography;
using System.Windows.Media.Imaging;

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

        public (int, BitmapImage) GetIconByFileExtension(string fileName)
        {
            var extensionName = Path.GetExtension(fileName)?.ToLower();
            var icon = string.Empty;
            int key = int.MaxValue;
            switch (extensionName)
            {
                case ".zip":
                case ".7z":
                case ".rar":
                    icon = "zip.png";
                    key = 1;
                    break;
                case ".dll":
                    key = 2;
                    icon = "dll.png";
                    break;
                case ".doc":
                case ".docx":
                    key = 3;
                    icon = "word.png";
                    break;
                case ".mp4":
                case ".mov":
                case ".avi":
                case ".flv":
                case ".wmv":
                case ".mpeg":
                case ".mkv":
                case ".asf":
                case ".rmvb":
                    key = 4;
                    icon = "vedio.png";
                    break;
                case ".exe":
                case ".msi":
                    key = 5;
                    icon = "exe.png";
                    break;
                case ".sql":
                    key = 6;
                    icon = "sql.png";
                    break;
                case ".xbm":
                case ".bmp":
                case ".jpg":
                case ".jpeg":
                case ".png":
                case ".gif":
                case ".tif":
                case ".tiff":
                case ".ico":
                    key = 7;
                    icon = "image.png";
                    break;
                case ".pdf":
                    key = 8;
                    icon = "pdf.png";
                    break;
                case ".html":
                    key = 9;
                    icon = "html.png";
                    break;
                case ".txt":
                    key = 10;
                    icon = "txt.png";
                    break;
                case ".xml":
                    key = 11;
                    icon = "xml.png";
                    break;
                case ".json":
                    key = 12;
                    icon = "json.png";
                    break;
                case ".ppt":
                case ".pptx":
                case ".potx":
                case ".pot":
                    key = 13;
                    icon = "ppt.png";
                    break;
                default:
                    icon = "unknown.png";
                    break;
            }

            return (key, GetBitmapImageByFileExtension(icon));
        }

        public BitmapImage GetBitmapImageByFileExtension(string imageName)
        {
            var source = new BitmapImage();
            try
            {
                string imgUrl = $"pack://application:,,,/FileTransfer;component/Images/{imageName}";
                source.BeginInit();
                source.UriSource = new Uri(imgUrl, UriKind.RelativeOrAbsolute);
                source.EndInit();

                return source;
            }
            finally
            {
                source.Freeze();
            }
        }
    }
}
