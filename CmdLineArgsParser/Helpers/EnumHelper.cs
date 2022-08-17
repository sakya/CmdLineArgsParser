using System;

namespace CmdLineArgsParser.Helpers
{
    /// <summary>
    /// Helper for enums
    /// </summary>
    public static class EnumHelper
    {
        /// <summary>
        /// Get the enum value from a string, case insensitive
        /// </summary>
        /// <param name="type">The num type</param>
        /// <param name="value">The value</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
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