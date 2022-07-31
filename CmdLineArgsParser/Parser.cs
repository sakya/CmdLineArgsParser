using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using CmdLineArgsParser.Attributes;

namespace CmdLineArgsParser
{
    /// <summary>
    /// Arguments parser
    /// </summary>
    public partial class Parser
    {
        #region classes
        private class OptionProperty
        {
            public OptionProperty(PropertyInfo property, OptionAttribute option)
            {
                Property = property;
                Option = option;
                Set = false;
            }

            public PropertyInfo Property { get; private set; }
            public OptionAttribute Option { get; private set; }
            public bool Set { get; set; }

            public bool HasValuesList
            {
                get
                {
                    if (Option?.ValidValues?.Length > 0)
                        return true;
                    var propertyType = Property.PropertyType;
                    if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                        propertyType = propertyType.GetGenericArguments().FirstOrDefault();

                    return propertyType?.IsEnum == true;
                }
            }
        }
        #endregion

        private OptionProperty _verbOption = null;
        private object _verbValue = null;

        #region private operations
        /// <summary>
        /// Validate the options type
        /// </summary>
        private void ValidateOptionsType<T>() where T : IOptions, new()
        {
            // Check names
            HashSet<string> names = new HashSet<string>();
            HashSet<char> shortNames = new HashSet<char>();

            var properties = GetProperties<T>();
            if (properties.Length == 0)
                throw new Exception("No public option properties defined");

            OptionProperty verb = null;
            foreach (var property in properties) {
                var opt = property.Option;
                if (!property.Property.CanWrite)
                    throw new Exception($"Readonly property '{property.Property.Name}'");

                if (opt.Verb) {
                    if (verb != null)
                        throw new Exception("Only one option can be defined as verb");

                    if (GetOptionBaseType(property.Property.PropertyType) == typeof(bool))
                        throw new Exception("Verb option cannot be a bool");
                    verb = property;
                }

                if (string.IsNullOrEmpty(opt.Name) && !opt.Verb)
                    throw new Exception($"Empty option name for property '{property.Property.Name}'");
                if (opt.Name.Contains(" ") || opt.Name.StartsWith("-"))
                    throw new Exception($"Invalid option name '{opt.Name}'");

                if (names.Contains(opt.Name.ToLower())) {
                    throw new Exception($"Duplicated option name '{opt.Name}'");
                }
                if (shortNames.Contains(opt.ShortName)) {
                    throw new Exception($"Duplicated option short name '{opt.ShortName}'");
                }

                // Bool options cannot be required
                if (property.Property.PropertyType.IsAssignableFrom(typeof(bool)) && property.Option.Required) {
                    throw new Exception($"Bool option '{opt.Name}' cannot be required");
                }

                var validValues = opt.GetValidValues();
                var onlyForVerbs = opt.GetOnlyForVerbs();
                if (validValues?.Length > 0 && onlyForVerbs?.Length > 0) {
                    throw new Exception($"Cannot set both ValidValues and OnlyForVerbs for option '{opt.Name}'");
                }

                // Check valid values
                if (validValues?.Length > 0) {
                    foreach (var validValue in validValues) {
                        var v = GetValueFromString(property.Property.PropertyType, validValue, out _);
                        if (v == null)
                            throw new Exception($"Invalid value for option '{opt.Name}': {validValue}");
                    }
                }

                // Check OnlyForVerbs
                if (onlyForVerbs?.Length > 0) {
                    if (opt.Verb)
                        throw new Exception($"Verb option cannot have OnlyForVerbs set");
                    if (verb == null)
                        throw new Exception($"OnlyForVerbs set for option '{ opt.Name }' but no verb defined");

                    foreach (var ofv in onlyForVerbs) {
                        var v = GetValueFromString(verb.Property.PropertyType, ofv, out _);
                        if (v == null)
                            throw new Exception($"Invalid OnlyForVerbs for option '{opt.Name}': {ofv}");
                    }
                }

                names.Add(opt.Name.ToLower());
                if (opt.ShortName != '\0')
                    shortNames.Add(opt.ShortName);
            }
        }

        /// <summary>
        /// Get a value from its string representation
        /// </summary>
        /// <param name="propertyType">The <see cref="Type"/> of the option</param>
        /// <param name="value">The string value representation</param>
        /// <param name="expectedType">The expected type</param>
        /// <returns>The value or null if <see cref="value"/> could not be converted</returns>
        private object GetValueFromString(Type propertyType, string value, out string expectedType)
        {
            expectedType = null;
            propertyType = GetOptionBaseType(propertyType);

            if (propertyType == typeof(string)) {
                expectedType = "string";
                return value;
            }

            if (propertyType == typeof(bool)) {
                value = value?.Trim().ToLower();
                expectedType = "bool";
                return value == "true" || value == "1";
            }

            if (propertyType?.IsEnum == true) {
                expectedType = propertyType.Name;
                var enumValues = Enum.GetNames(propertyType);
                foreach (var enumValue in enumValues) {
                    if (string.Compare(enumValue, value, StringComparison.InvariantCultureIgnoreCase) == 0) {
                        return Enum.Parse(propertyType, enumValue);
                    }
                }
                return null;
            }

            if (propertyType == typeof(int)) {
                expectedType = "int";
                if (int.TryParse(value, out int intValue))
                    return intValue;
                return null;
            }

            if (propertyType == typeof(double)) {
                expectedType = "double";
                if (double.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double doubleValue))
                    return doubleValue;
                return null;
            }

            if (propertyType == typeof(float)) {
                expectedType = "float";
                if (float.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out float floatValue))
                    return floatValue;
                return null;
            }
            return null;
        }

        /// <summary>
        /// Get the default value for a <see cref="Type"/>
        /// </summary>
        /// <param name="type">The <see cref="Type"/></param>
        /// <returns>Te default value for <see cref="type"/></returns>
        private object GetDefaultValue(Type type)
        {
            if (type.IsValueType)
                return Activator.CreateInstance(type);

            return null;
        }

        /// <summary>
        /// Get properties with the <see cref="OptionAttribute"/>
        /// </summary>
        /// <typeparam name="T">The Type</typeparam>
        /// <returns>A List of <see cref="OptionProperty"/></returns>
        private OptionProperty[] GetProperties<T>() where T : IOptions, new()
        {
            // Collect properties with the OptionAttribute
            List<OptionProperty> properties = new List<OptionProperty>();
            var type = new T().GetType();
            foreach (var p in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
                if (p.CanWrite) {
                    var optAttribute = p.GetCustomAttributes(typeof(OptionAttribute), false);
                    if (optAttribute.Any()) {
                        properties.Add(new OptionProperty(p, optAttribute.First() as OptionAttribute));
                    }
                }
            }

            properties = properties.OrderBy(p => !p.Option.Verb)
                .ToList();
            return properties.ToArray();
        }

        /// <summary>
        /// Get an option base type
        /// </summary>
        /// <param name="propertyType">The type of the option</param>
        /// <returns></returns>
        private Type GetOptionBaseType(Type propertyType)
        {
            if (propertyType.IsArray)
                return propertyType.GetElementType();

            if (propertyType.IsGenericType && typeof(List<>).IsAssignableFrom(propertyType.GetGenericTypeDefinition()))
                return propertyType.GetGenericArguments().FirstOrDefault();

            if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                return propertyType.GetGenericArguments().FirstOrDefault();

            return propertyType;
        }
        #endregion
    }
}
