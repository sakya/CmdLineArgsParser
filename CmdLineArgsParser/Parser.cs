using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using CmdLineArgsParser.Attributes;
using CmdLineArgsParser.Helpers;

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

            public bool IsEnumerable =>
                Property.PropertyType.IsArray || (Property.PropertyType.IsGenericType &&
                typeof(List<>).IsAssignableFrom(Property.PropertyType.GetGenericTypeDefinition()));
        }
        #endregion

        private OptionProperty _verbOption;
        private object _verbValue;

        public Parser(ParserSettings settings = null)
        {
            if (settings == null)
                settings = new ParserSettings();
            Settings = settings;
        }

        public ParserSettings Settings { get; private set; }

        #region private operations
        /// <summary>
        /// Validate the options type
        /// </summary>
        private void ValidateOptionsType<T>() where T : IOptions, new()
        {
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

                ValidateName(property, names, shortNames);

                if (opt.Verb) {
                    if (verb != null)
                        throw new Exception("Only one option can be defined as verb");

                    if (GetOptionBaseType(property.Property.PropertyType) == typeof(bool))
                        throw new Exception("Verb option cannot be a bool");
                    verb = property;
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

                ValidateValidValues(property, validValues);
                ValidateDefaultValue(property, validValues);
                ValidateOnlyForVerbs(property, verb, onlyForVerbs);

                names.Add(opt.Name.ToLower());
                if (opt.ShortName != '\0')
                    shortNames.Add(char.ToLower(opt.ShortName));
            }
        }

        private void ValidateName(OptionProperty property, HashSet<string> usedNames, HashSet<char> usedShortNames)
        {
            if (string.IsNullOrEmpty(property.Option.Name))
                throw new Exception($"Empty option name for property '{property.Property.Name}'");
            if (property.Option.Name.Contains(" ") || property.Option.Name.StartsWith("-"))
                throw new Exception($"Invalid option name '{property.Option.Name}'");
            if (property.Option.Name.Length == 1)
                throw new Exception($"Option name '{property.Property.Name}' must contain at least two characters");

            if (usedNames.Contains(property.Option.Name.ToLower()))
                throw new Exception($"Duplicated option name '{property.Option.Name}'");
            if (usedShortNames.Contains(char.ToLower(property.Option.ShortName)))
                throw new Exception($"Duplicated option short name '{property.Option.ShortName}'");
        }

        private void ValidateDefaultValue(OptionProperty property, string[] validValues)
        {
            if (!string.IsNullOrEmpty(property.Option.DefaultValue)) {
                if (property.Option.Verb)
                    throw new Exception("Verb option cannot have a default value");
                if (GetOptionBaseType(property.Property.PropertyType) == typeof(bool))
                    throw new Exception($"Bool option '{property.Option.Name}' cannot have a default value");
                if (property.IsEnumerable)
                    throw new Exception($"Enumerable option '{property.Option.Name}' cannot have a default value");

                List<object> vv = new List<object>();
                if (validValues != null) {
                    foreach (var validValue in validValues) {
                        vv.Add(GetValueFromString(property.Property.PropertyType, validValue, out _));
                    }
                }

                var v = GetValueFromString(property.Property.PropertyType, property.Option.DefaultValue, out _);
                if (v == null)
                    throw new Exception($"Invalid default value for option '{property.Option.Name}': {property.Option.DefaultValue}");
                if (vv.Count > 0 && !vv.Contains(v))
                    throw new Exception($"Invalid default value for option '{property.Option.Name}': {property.Option.DefaultValue}");
            }
        }

        private void ValidateValidValues(OptionProperty property, string[] validValues)
        {
            if (validValues?.Length > 0) {
                foreach (var validValue in validValues) {
                    var v = GetValueFromString(property.Property.PropertyType, validValue, out _);
                    if (v == null)
                        throw new Exception($"Invalid value for option '{property.Option.Name}': {validValue}");
                }
            }
        }

        private void ValidateOnlyForVerbs(OptionProperty property, OptionProperty verb, string[] onlyForVerbs)
        {
            if (onlyForVerbs?.Length > 0) {
                if (property.Option.Verb)
                    throw new Exception($"Verb option cannot have OnlyForVerbs set");
                if (verb == null)
                    throw new Exception($"OnlyForVerbs set for option '{ property.Option.Name }' but no verb defined");

                foreach (var ofv in onlyForVerbs) {
                    var v = GetValueFromString(verb.Property.PropertyType, ofv, out _);
                    if (v == null)
                        throw new Exception($"Invalid OnlyForVerbs for option '{property.Option.Name}': {ofv}");
                }
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
                return string.Compare(value, "true", StringComparison.InvariantCultureIgnoreCase) == 0 || value == "1";
            }

            if (propertyType?.IsEnum == true) {
                expectedType = propertyType.Name;
                return EnumHelper.GetValue(propertyType, value);
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

            if (propertyType == typeof(DateTime)) {
                expectedType = "DateTime";
                if (!string.IsNullOrEmpty(Settings.DateTimeFormat)) {
                    if (DateTime.TryParseExact(value, Settings.DateTimeFormat, null, DateTimeStyles.None, out var dtValue))
                        return dtValue;
                } else {
                    if (DateTime.TryParse(value, out var dtValue))
                        return dtValue;
                }
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
