using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using CmdLineArgsParser.Attributes;
using CmdLineArgsParser.Extensions;

namespace CmdLineArgsParser
{
    /// <summary>
    /// Arguments parser
    /// </summary>
    public class Parser
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
        }
        #endregion

        /// <summary>
        /// Parse <see cref="args"/> using the default <see cref="ParserSettings"/>
        /// </summary>
        /// <param name="args">The arguments to parse</param>
        /// <param name="errors">A list of <see cref="ParserError"/></param>
        /// <typeparam name="T">The options type</typeparam>
        /// <returns>An instance of <see cref="T"/></returns>
        public T Parse<T>(string args, out List<ParserError> errors) where T : IOptions, new()
        {
            return Parse<T>(new ParserSettings(), args, out errors);
        }

        /// <summary>
        /// Parse <see cref="args"/> using the given <see cref="settings"/>
        /// </summary>
        /// <param name="settings">The <see cref="ParserSettings"/></param>
        /// <param name="args">The arguments to parse</param>
        /// <param name="errors">A list of <see cref="ParserError"/></param>
        /// <typeparam name="T">The options type</typeparam>
        /// <returns>An instance of <see cref="T"/></returns>
        public T Parse<T>(ParserSettings settings, string args, out List<ParserError> errors) where T : IOptions, new()
        {
            List<string> splittedArgs = new List<string>();
            var match = Regex.Match(args, "(\"[^\"]+\"|[^\\s\"]+)");
            while (match.Success) {
                var arg = match.Value;
                if (arg.StartsWith("\"") && arg.EndsWith("\"")) {
                    arg = arg.Substring(1);
                    arg = arg.Substring(0, arg.Length - 1);
                }

                splittedArgs.Add(arg);
                match = match.NextMatch();
            }

            return Parse<T>(settings, splittedArgs.ToArray(), out errors);
        }

        /// <summary>
        /// Parse <see cref="args"/> using the default <see cref="ParserSettings"/>
        /// </summary>
        /// <param name="args">The arguments to parse</param>
        /// <param name="errors">A list of <see cref="ParserError"/></param>
        /// <typeparam name="T">The options type</typeparam>
        /// <returns>An instance of <see cref="T"/></returns>
        public T Parse<T>(string[] args, out List<ParserError> errors) where T : IOptions, new()
        {
            return Parse<T>(new ParserSettings(), args, out errors);
        }

        /// <summary>
        /// Parse <see cref="args"/> using the given <see cref="settings"/>
        /// </summary>
        /// <param name="settings">The <see cref="ParserSettings"/></param>
        /// <param name="args">The arguments to parse</param>
        /// <param name="errors">A list of <see cref="ParserError"/></param>
        /// <typeparam name="T">The options type</typeparam>
        /// <returns>An instance of <see cref="T"/></returns>
        public T Parse<T>(ParserSettings settings, string[] args, out List<ParserError> errors) where T : IOptions, new()
        {
            ValidateOptionsType<T>();

            var res = new T();
            errors = new List<ParserError>();
            OptionProperty[] properties = GetProperties<T>();
            OptionProperty verb = properties.FirstOrDefault(p => p.Option.Verb);

            OptionProperty lastOption = null;
            bool first = true;
            foreach (var arg in args) {
                if (arg.StartsWith("--")) {
                    // Argument name
                    var optName = arg.Substring(2);
                    lastOption = properties.FirstOrDefault(p =>
                            string.Compare(p.Option.Name, optName, StringComparison.InvariantCultureIgnoreCase) == 0);

                    if (lastOption == null) {
                        errors.Add(new ParserError(null, $"Unknown option '{optName}'"));
                    } else if (lastOption.Property.PropertyType.IsAssignableFrom(typeof(bool))) {
                        SetOptionValue(lastOption,  res,"true", errors);
                        lastOption = null;
                    }
                } else if (arg.StartsWith(("-"))) {
                    // Short argument name(s)
                    var optName = arg.Substring(1);
                    foreach (char c in optName) {
                        lastOption = properties.FirstOrDefault(p =>
                            char.ToLower(p.Option.ShortName) == char.ToLower(c));
                        if (lastOption == null) {
                            errors.Add(new ParserError(null, $"Unknown option '{optName}'"));
                        } else {
                            if (optName.Length > 1) {
                                if (!lastOption.Property.PropertyType.IsAssignableFrom(typeof(bool))) {
                                    errors.Add(new ParserError(lastOption.Option.Name, $"Cannot set option '{lastOption.Option.Name}' with multiple switch (only boolean options supported)"));
                                } else {
                                    SetOptionValue(lastOption,  res,"true", errors);
                                }
                            } else if (lastOption.Property.PropertyType.IsAssignableFrom(typeof(bool))) {
                                SetOptionValue(lastOption,  res,"true", errors);
                            }
                        }
                    }
                } else {
                    if (lastOption != null) {
                        SetOptionValue(lastOption, res, arg, errors);
                        lastOption = null;
                    } else {
                        if (first && verb != null) {
                            SetOptionValue(verb,  res,arg, errors);
                        } else {
                            errors.Add(new ParserError(null, $"Value without option: '{arg}'"));
                        }
                    }
                }

                first = false;
            }

            // Check required options
            foreach (var p in properties) {
                if (p.Option.Required && !p.Set) {
                    errors.Add(new ParserError(p.Option.Name, $"Required option '{p.Option.Name}' not set"));
                }
            }

            return res;
        }

        /// <summary>
        /// Writes options to the <see cref="Console"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void ShowUsage<T>(int columnsForName = 30) where T : IOptions, new()
        {
            if (columnsForName <= 0)
                throw new ArgumentException($"{nameof(columnsForName)} must be greater than zero",
                    nameof(columnsForName));

            var sections = GetProperties<T>()
                .GroupBy(p => p.Option.Section);
            foreach (var section in sections) {
                Console.WriteLine($"{ (string.IsNullOrEmpty(section.Key) ? "General" : section.Key) }:");
                foreach (var property in section.OrderBy(p => p.Option.Name)) {
                    var opt = property.Option;

                    StringBuilder sb = new StringBuilder();
                    sb.Append("  ");
                    if (opt.ShortName != '\0') {
                        sb.Append($"-{opt.ShortName}, ");
                    }

                    sb.Append($"--{opt.Name}");
                    if (property.Property.PropertyType != typeof(bool))
                        sb.Append("=VALUE");
                    if (sb.Length < columnsForName) {
                        sb.Append(new string(' ', columnsForName - sb.Length));
                    } else {
                        sb.AppendLine();
                        sb.Append(new string(' ', columnsForName));
                    }

                    if (!string.IsNullOrEmpty(opt.Description)) {
                        var wrappedText = opt.Description.WordWrap(Console.WindowWidth - columnsForName).TrimEnd();
                        var lines = wrappedText.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                        for (int i = 0; i < lines.Length; i++) {
                            var line = lines[i];
                            if (i == 0)
                                sb.Append(line);
                            else
                                sb.Append($"{Environment.NewLine}{new string(' ', columnsForName)}{line}");
                        }
                    }

                    Console.WriteLine(sb.ToString());
                }
            }
        }

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

            bool verbFound = false;
            foreach (var property in properties) {
                var opt = property.Option;
                if (!property.Property.CanWrite)
                    throw new Exception($"Readonly property '{property.Property.Name}'");

                if (opt.Verb) {
                    if (verbFound)
                        throw new Exception("Only one option can be defined as verb");
                    verbFound = true;
                }

                if (string.IsNullOrEmpty(opt.Name) && !opt.Verb)
                    throw new Exception($"Empty option name for property '{property.Property.Name}'");
                if (opt.Name.Contains(" "))
                    throw new Exception($"Invalid option name '{opt.Name}'");

                if (names.Contains(opt.Name.ToLower())) {
                    throw new Exception($"Duplicated option name '{opt.Name}'");
                }
                if (shortNames.Contains(opt.ShortName)) {
                    throw new Exception($"Duplicated option short name '{opt.ShortName}'");
                }

                var validValues = opt.GetValidValues();
                if (validValues?.Length > 0) {
                    // Check valid values
                    foreach (var validValue in validValues) {
                        var v = GetValueFromString(property.Property.PropertyType, validValue, out _);
                        if (v == null)
                            throw new Exception($"Invalid value for option '{opt.Name}': {validValue}");
                    }
                }

                names.Add(opt.Name.ToLower());
                if (opt.ShortName != '\0')
                    shortNames.Add(opt.ShortName);
            }
        }

        /// <summary>
        /// Set a property value
        /// </summary>
        /// <param name="option"></param>
        /// <param name="obj"></param>
        /// <param name="stringValue"></param>
        /// <param name="errors"></param>
        private void SetOptionValue(OptionProperty option, object obj, string stringValue, List<ParserError> errors)
        {
            object value = GetValueFromString(option.Property.PropertyType, stringValue, out var expectedType);
            if (value == null) {
                if (option.Option.Verb)
                    errors.Add(new ParserError(option.Option.Name, $"Invalid value for verb option (expected {expectedType}): {stringValue}"));
                else
                    errors.Add(new ParserError(option.Option.Name, $"Invalid value for option '{option.Option.Name}' (expected {expectedType}): {stringValue}"));
                return;
            }

            if (option.Property.PropertyType.IsArray) {
                // Array
                Array array = option.Property.GetValue(obj) as Array;
                if (array == null) {
                    array = Array.CreateInstance(option.Property.PropertyType.GetElementType(), 1);
                } else {
                    Type elementType = array.GetType().GetElementType();
                    Array newArray = Array.CreateInstance(elementType, array.Length + 1);
                    Array.Copy(array, newArray, Math.Min(array.Length, newArray.Length));
                    array = newArray;
                }

                array.SetValue(value, array.Length - 1);
                option.Property.SetValue(obj, array);
            } else if (option.Property.PropertyType.IsGenericType && typeof(List<>).IsAssignableFrom(option.Property.PropertyType.GetGenericTypeDefinition())) {
                // List
                IList list = option.Property.GetValue(obj) as IList;
                if (list == null) {
                    Type genericListType = typeof(List<>).MakeGenericType(option.Property.PropertyType.GetGenericArguments().FirstOrDefault());
                    list = (IList)Activator.CreateInstance(genericListType);
                    option.Property.SetValue(obj, list);
                }

                list.Add(value);
            } else {
                // Simple value
                if (option.Set)
                    errors.Add(new ParserError(option.Option.Name, $"Option '{option.Option.Name}' set multiple times"));
                else
                    option.Property.SetValue(obj, value);
            }

            // Check valid values:
            if (option.Option.ValidValues?.Length > 0) {
                bool valueOk = false;
                foreach (var vvs in option.Option.GetValidValues()) {
                    var vv = GetValueFromString(option.Property.PropertyType, vvs, out _);
                    if (value.Equals(vv)) {
                        valueOk = true;
                        break;
                    }
                }

                if (!valueOk) {
                    option.Property.SetValue(obj, GetDefaultValue(option.Property.PropertyType));
                    errors.Add(new ParserError(option.Option.Name, $"Invalid value for option '{option.Option.Name}': {stringValue}"));
                }
            }
            option.Set = true;
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
            if (propertyType.IsArray) {
                propertyType = propertyType.GetElementType();
            } else if (propertyType.IsGenericType && typeof(List<>).IsAssignableFrom(propertyType.GetGenericTypeDefinition())) {
                propertyType = propertyType.GetGenericArguments().FirstOrDefault();
            } else if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>)) {
                propertyType = propertyType.GetGenericArguments().FirstOrDefault();
            }

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

            return properties.ToArray();
        }
        #endregion
    }
}