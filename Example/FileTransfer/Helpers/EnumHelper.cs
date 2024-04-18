using System.ComponentModel;
using System.Reflection;

namespace FileTransfer.Helpers
{
    public static class EnumHelper
    {
        public static string GetDescription(this Enum value)
        {
            var type = value.GetType();
            var name = Enum.GetName(type, value);
            if (string.IsNullOrWhiteSpace(name))
                return value.ToString();

            var field = type.GetField(name);
            var des = field?.GetCustomAttribute<DescriptionAttribute>();
            if (des == null)
                return value.ToString();

            return des.Description;
        }
    }
}
