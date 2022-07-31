using System;

namespace CmdLineArgsParser.Helpers
{
    public static class EnumHelper
    {
        public static object GetValue(Type type, string value)
        {
            if (!type.IsEnum)
                throw new ArgumentException("Type must be an Enum", nameof(type));

            var enumValues = Enum.GetNames(type);
            foreach (var enumValue in enumValues) {
                if (string.Compare(enumValue, value, StringComparison.InvariantCultureIgnoreCase) == 0) {
                    return Enum.Parse(type, enumValue);
                }
            }

            return null;
        }
    }
}