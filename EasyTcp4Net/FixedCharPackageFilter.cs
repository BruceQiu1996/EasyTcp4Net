using System.Buffers;

namespace EasyTcp4Net
{
    /// <summary>
    /// 固定字符解析器,不建议使用，除非保证发送数据不会出现和字符相同的字节数据，否则会出现断包的情况
    /// 如果需要在项目中使用根据某个字符串解析数据包，建议自行实现ResolvePackage
    /// </summary>
    public class FixedCharPackageFilter : AbstractPackageFilter
    {
        private readonly char _splitChar;
        public FixedCharPackageFilter(char splitChar)
        {
            _splitChar = splitChar;
            try 
            {
                byte chatByte = (byte)_splitChar;
            }
            catch (Exception) 
            {
                throw new ArgumentException("The char dose not support.You can customize your own parser.");
            }
        }

        public override ReadOnlySequence<byte> ResolvePackage(ref ReadOnlySequence<byte> sequence)
        {
            var position = sequence.PositionOf((byte)_splitChar);
            
            if (position == null)
            {
                return default;
            }
            else 
            {
                var index = sequence.GetPosition(1, position.Value);
                var data = sequence.Slice(0, index);
                sequence = sequence.Slice(index);

                return data;
            }
        }
    }
}
