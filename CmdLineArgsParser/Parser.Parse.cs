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
        /// Parse <see cref="args"/>
        /// </summary>
        /// <param name="args">The arguments to parse</param>
        /// <param name="errors">A list of <see cref="ParserError"/></param>
        /// <typeparam name="T">The options type</typeparam>
        /// <returns>An instance of <see cref="T"/></returns>
        public T Parse<T>(string args, out List<ParserError> errors) where T : IOptions, new()
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

            return Parse<T>(splitArgs.ToArray(), out errors);
        }

        /// <summary>
        /// Parse <see cref="args"/>
        /// </summary>
        /// <param name="args">The arguments to parse</param>
        /// <param name="errors">A list of <see cref="ParserError"/></param>
        /// <typeparam name="T">The options type</typeparam>
        /// <returns>An instance of <see cref="T"/></returns>
        public T Parse<T>(string[] args, out List<ParserError> errors) where T : IOptions, new()
        {
            ValidateOptionsType<T>();

            if (Settings.EnableEqualSyntax)
                args = ParseArgumentsForEqualSyntax(args);

            var res = new T();
             _verbValue = null;
            errors = new List<ParserError>();

            OptionProperty[] properties = GetProperties<T>();
            SetDefaultValues(res, properties);
            _verbOption = properties.FirstOrDefault(p => p.Option.Verb);

            OptionProperty lastOption = null;
            bool first = true;
            foreach (var arg in args) {
                if (arg.StartsWith("--")) {
                    CheckLastOption(lastOption, errors);
                    lastOption = ParseNameOption(res, properties, arg, errors);
                } else if (arg.StartsWith(("-"))) {
                    CheckLastOption(lastOption, errors);
                    lastOption = ParseShortNameOption(res, properties, arg, errors);
                } else {
                    if (lastOption != null) {
                        SetOptionValue(lastOption, res, arg, errors);
                        lastOption = null;
                    } else {
                        if (first && _verbOption != null)
                            SetOptionValue(_verbOption,  res,arg, errors);
                        else
                            errors.Add(new ParserError(null, $"Value without option: '{arg}'"));
                    }
                }

                first = false;
            }

            CheckLastOption(lastOption, errors);
            CheckRequiredOptions(properties, errors);
            CheckMutuallyExclusiveOptions(properties, errors);

            return res;
        }

        #region private operations

        private void SetDefaultValues<T>(T obj, OptionProperty[] properties) where T : IOptions, new()
        {
            foreach (var opt in properties) {
                if (!string.IsNullOrEmpty(opt.Option.DefaultValue)) {
                    var v = GetValueFromString(opt.Property.PropertyType, opt.Option.DefaultValue, out _);
                    opt.Property.SetValue(obj, v);
                } else if (GetOptionBaseType(opt.Property.PropertyType) == typeof(bool)) {
                    opt.Property.SetValue(obj, false);
                }
            }
        }

        private OptionProperty ParseNameOption<T>(T obj, OptionProperty[] properties, string arg, List<ParserError> errors) where T : IOptions, new()
        {
            var optName = arg.Substring(2);
            var res = properties.FirstOrDefault(p =>
                string.Compare(p.Option.Name, optName, StringComparison.InvariantCultureIgnoreCase) == 0);

            if (res == null) {
                errors.Add(new ParserError(null, $"Unknown option '{optName}'"));
            } else if (res.Property.PropertyType.IsAssignableFrom(typeof(bool))) {
                SetOptionValue(res,  obj,"true", errors);
                res = null;
            }

            return res;
        }

        private OptionProperty ParseShortNameOption<T>(T obj, OptionProperty[] properties, string arg, List<ParserError> errors) where T : IOptions, new()
        {
            OptionProperty res = null;
            var optName = arg.Substring(1);
            foreach (char c in optName) {
                res = properties.FirstOrDefault(p =>
                    char.ToLower(p.Option.ShortName) == char.ToLower(c));
                if (res == null) {
                    errors.Add(new ParserError(null, $"Unknown option '{optName}'"));
                } else {
                    if (optName.Length > 1) {
                        if (!res.Property.PropertyType.IsAssignableFrom(typeof(bool))) {
                            errors.Add(new ParserError(res.Option.Name, $"Cannot set option '{res.Option.Name}' with multiple switch (only boolean options supported)"));
                        } else {
                            SetOptionValue(res,  obj,"true", errors);
                        }
                    } else if (res.Property.PropertyType.IsAssignableFrom(typeof(bool))) {
                        SetOptionValue(res,  obj,"true", errors);
                    }
                }
            }

            return res;
        }

        private void CheckLastOption(OptionProperty option, List<ParserError> errors)
        {
            if (option != null && !option.Set)
                errors.Add(new ParserError(option.Option.Name, $"Missing value for option '{ option.Option.Name }'"));
        }

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
        /// Check mutually exclusive options
        /// </summary>
        /// <param name="properties"></param>
        /// <param name="errors"></param>
        private void CheckMutuallyExclusiveOptions(OptionProperty[] properties, List<ParserError> errors)
        {
            HashSet<string> eAdded = new HashSet<string>();
            foreach (var p in properties.Where(p => !string.IsNullOrEmpty(p.Option.MutuallyExclusive) && p.Set)) {
                var otherOption = properties.FirstOrDefault(op => op != p && op.Set && string.Compare(op.Option.MutuallyExclusive,
                    p.Option.MutuallyExclusive, StringComparison.InvariantCultureIgnoreCase) == 0);
                if (otherOption != null && !eAdded.Contains($"{p.Option.Name};{otherOption.Option.Name}") && !eAdded.Contains($"{otherOption.Option.Name};{p.Option.Name}")) {
                    errors.Add(new ParserError(p.Option.Name,
                        $"Option '{p.Option.Name}' cannot be used with option '{otherOption.Option.Name}'"));
                    eAdded.Add($"{p.Option.Name};{otherOption.Option.Name}");
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
                SetArrayOptionValue(option, obj, value);
            } else if (option.Property.PropertyType.IsGenericType && typeof(List<>).IsAssignableFrom(option.Property.PropertyType.GetGenericTypeDefinition())) {
                SetListOptionValue(option, obj, value);
            } else {
                SetSimpleOptionValue(option, obj, value, errors);
            }

            CheckValidValues(option, obj, value, errors);
            CheckOnlyForVerbs(option, obj, errors);
            option.Set = true;
        }

        private void SetArrayOptionValue(OptionProperty option, object obj, object value)
        {
            var array = option.Property.GetValue(obj) as Array;
            if (array == null) {
                array = Array.CreateInstance(option.Property.PropertyType.GetElementType(), 1);
            } else {
                var elementType = array.GetType().GetElementType();
                if (elementType != null) {
                    Array newArray = Array.CreateInstance(elementType, array.Length + 1);
                    Array.Copy(array, newArray, Math.Min(array.Length, newArray.Length));
                    array = newArray;
                }
            }

            array.SetValue(value, array.Length - 1);
            option.Property.SetValue(obj, array);
        }

        private void SetListOptionValue(OptionProperty option, object obj, object value)
        {
            var list = option.Property.GetValue(obj) as IList;
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
                var valueOk = false;
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

        private void CheckOnlyForVerbs(OptionProperty option, object obj, List<ParserError> errors)
        {
            if (option.Option.OnlyForVerbs?.Length > 0) {
                if (_verbValue == null) {
                    errors.Add(new ParserError(option.Option.Name, $"Option '{ option.Option.Name }' can be set only for specific verbs but no verb has been specified"));
                } else {
                    var valueOk = false;
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

        /// <summary>
        /// Parse arguments for alternative syntax "--name=value"
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private string[] ParseArgumentsForEqualSyntax(string[] args)
        {
            var res = new List<string>();
            if (args?.Length > 0) {
                foreach (var arg in args) {
                    if (arg.StartsWith("-")) {
                        int idx = arg.IndexOf('=');
                        if (idx >= 0) {
                            res.Add(arg.Substring(0, idx));
                            if (arg.Length > idx + 1)
                                res.Add(arg.Substring(idx + 1));
                        } else {
                            res.Add(arg);
                        }
                    } else {
                        res.Add(arg);
                    }
                }
            }
            return res.ToArray();
        }
        #endregion
    }
}
