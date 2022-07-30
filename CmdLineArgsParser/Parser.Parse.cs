using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CmdLineArgsParser
{
    public partial class Parser
    {
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
            var splitArgs = new List<string>();
            var match = Regex.Match(args, "(\"[^\"]+\"|[^\\s\"]+)");
            while (match.Success) {
                var arg = match.Value;
                if (arg.StartsWith("\"") && arg.EndsWith("\"")) {
                    arg = arg.Substring(1);
                    arg = arg.Substring(0, arg.Length - 1);
                }

                splitArgs.Add(arg);
                match = match.NextMatch();
            }

            return Parse<T>(settings, splitArgs.ToArray(), out errors);
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
             _verbValue = null;
            errors = new List<ParserError>();

            OptionProperty[] properties = GetProperties<T>();
            _verbOption = properties.FirstOrDefault(p => p.Option.Verb);

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
                        if (first && _verbOption != null) {
                            SetOptionValue(_verbOption,  res,arg, errors);
                        } else {
                            errors.Add(new ParserError(null, $"Value without option: '{arg}'"));
                        }
                    }
                }

                first = false;
            }

            CheckRequiredOptions(properties, errors);

            return res;
        }

        #region private operations
        /// <summary>
        /// Check for required options not set
        /// </summary>
        /// <param name="properties"></param>
        /// <param name="errors"></param>
        private void CheckRequiredOptions(OptionProperty[] properties, List<ParserError> errors)
        {
            foreach (var p in properties.Where(p => p.Option.Required & !p.Set)) {
                if (p.Option.Verb)
                    errors.Add(new ParserError(p.Option.Name, $"Required verb option not set"));
                else {
                    if (p.Option.OnlyForVerbs?.Length > 0 && _verbOption != null) {
                        foreach (var ofv in p.Option.GetOnlyForVerbs()) {
                            var v = GetValueFromString(_verbOption.Property.PropertyType, ofv, out _);
                            if (v.Equals(_verbValue)) {
                                errors.Add(new ParserError(p.Option.Name, $"Required option '{p.Option.Name}' not set"));
                                break;
                            }
                        }
                    } else {
                        errors.Add(new ParserError(p.Option.Name, $"Required option '{p.Option.Name}' not set"));
                    }
                }
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
                    errors.Add(new ParserError(option.Option.Name, $"Invalid value for verb option: {stringValue}"));
                else
                    errors.Add(new ParserError(option.Option.Name, $"Invalid value for option '{option.Option.Name}' (expected {expectedType}): {stringValue}"));
                return;
            }

            if (option.Option.Verb)
                _verbValue = value;

            if (option.Property.PropertyType.IsArray) {
                SetArrayOptionValue(option, obj, value, errors);
            } else if (option.Property.PropertyType.IsGenericType && typeof(List<>).IsAssignableFrom(option.Property.PropertyType.GetGenericTypeDefinition())) {
                SetListOptionValue(option, obj, value, errors);
            } else {
                SetSimpleOptionValue(option, obj, value, errors);
            }

            CheckValidValues(option, obj, value, errors);
            CheckOnlyForVerbs(option, obj, value, errors);

            option.Set = true;
        }

        private void SetArrayOptionValue(OptionProperty option, object obj, object value, List<ParserError> errors)
        {
            Array array = option.Property.GetValue(obj) as Array;
            if (array == null) {
                array = Array.CreateInstance(option.Property.PropertyType.GetElementType(), 1);
            } else {
                Type elementType = array.GetType().GetElementType();
                if (elementType != null) {
                    Array newArray = Array.CreateInstance(elementType, array.Length + 1);
                    Array.Copy(array, newArray, Math.Min(array.Length, newArray.Length));
                    array = newArray;
                }
            }

            array.SetValue(value, array.Length - 1);
            option.Property.SetValue(obj, array);
        }

        private void SetListOptionValue(OptionProperty option, object obj, object value, List<ParserError> errors)
        {
            IList list = option.Property.GetValue(obj) as IList;
            if (list == null) {
                Type genericListType = typeof(List<>).MakeGenericType(option.Property.PropertyType.GetGenericArguments().FirstOrDefault());
                list = (IList)Activator.CreateInstance(genericListType);
                option.Property.SetValue(obj, list);
            }

            list.Add(value);
        }

        private void SetSimpleOptionValue(OptionProperty option, object obj, object value, List<ParserError> errors)
        {
            if (option.Set)
                errors.Add(new ParserError(option.Option.Name, $"Option '{option.Option.Name}' set multiple times"));
            else
                option.Property.SetValue(obj, value);
        }

        private void CheckValidValues(OptionProperty option, object obj, object value, List<ParserError> errors)
        {
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
                    errors.Add(new ParserError(option.Option.Name, $"Invalid value for option '{option.Option.Name}': {value}"));
                }
            }
        }

        private void CheckOnlyForVerbs(OptionProperty option, object obj, object value, List<ParserError> errors)
        {
            if (option.Option.OnlyForVerbs?.Length > 0) {
                if (_verbValue == null) {
                    errors.Add(new ParserError(option.Option.Name, $"Option '{ option.Option.Name }' can be set only for specific verbs but no verb has been specified"));
                } else {
                    bool valueOk = false;
                    foreach (var ofvs in option.Option.GetOnlyForVerbs()) {
                        var ofv = GetValueFromString(_verbOption.Property.PropertyType, ofvs, out _);
                        if (_verbValue.Equals(ofv)) {
                            valueOk = true;
                            break;
                        }
                    }

                    if (!valueOk) {
                        option.Property.SetValue(obj, GetDefaultValue(option.Property.PropertyType));
                        errors.Add(new ParserError(option.Option.Name, $"Option '{option.Option.Name}' is not valid for verb { _verbValue }"));
                    }
                }
            }
        }

        #endregion
    }
}
