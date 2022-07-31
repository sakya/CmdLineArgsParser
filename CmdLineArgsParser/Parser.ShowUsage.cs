using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using CmdLineArgsParser.Extensions;
using DescriptionAttribute = CmdLineArgsParser.Attributes.DescriptionAttribute;

namespace CmdLineArgsParser
{
    public partial class Parser
    {
        /// <summary>
        /// Writes options to the <see cref="Console"/>
        /// </summary>
        /// <param name="columnsForName">The number of columns to reserve for option names (default: 0)</param>
        /// <param name="useEqualSyntax">If set to true the usage uses the equal syntax (e.g. --option=VALUE)</param>
        /// <typeparam name="T"></typeparam>
        public void ShowUsage<T>(int columnsForName = 30, bool useEqualSyntax = true) where T : IOptions, new()
        {
            if (columnsForName <= 0)
                throw new ArgumentException($"{nameof(columnsForName)} must be greater than zero",
                    nameof(columnsForName));

            ValidateOptionsType<T>();
            var properties = GetProperties<T>();
            var verb = properties.FirstOrDefault(p => p.Option.Verb);

            ShowUsageLine(Path.GetFileName(Assembly.GetCallingAssembly().Location), properties, verb, useEqualSyntax);

            // Options
            // Verb first
            if (verb != null && verb.HasValuesList) {
                ShowVerbUsage(verb, columnsForName);
            }

            var sections = properties.GroupBy(p => p.Option.Section);
            foreach (var section in sections) {
                Console.WriteLine($"{ (string.IsNullOrEmpty(section.Key) ? "General" : section.Key) }:");
                foreach (var property in section.Where(p => !p.Option.Verb).OrderBy(p => p.Option.Name)) {
                    ShowOptionUsage(property, columnsForName, useEqualSyntax);
                }
            }
        }

        #region private operations
        private void ShowUsageLine(string callingAssembly, OptionProperty[] properties, OptionProperty verb, bool useEqualSyntax)
        {
            // Usage line
            Console.WriteLine("Usage:");
            var usage = $"{callingAssembly}";
            if (verb != null)
                usage = $"{usage} {(verb.Option.Required ? verb.Option.Name : $"[{ verb.Option.Name }]")}";

            foreach (var req in properties.Where(p => !p.Option.Verb && p.Option.Required).OrderBy(p => p.Option.Name)) {
                usage = $"{usage} --{req.Option.Name}{ (useEqualSyntax ? "=" : string.Empty) }VALUE";
            }
            if (properties.Any(p => !p.Option.Verb && !p.Option.Required))
                usage = $"{usage} [OPTIONS]";
            Console.WriteLine(usage);

            Console.WriteLine();
        }

        private void ShowVerbUsage(OptionProperty verb, int columnsForName)
        {
            Console.WriteLine($"{verb.Option.Name.Capitalize()}:");
            if (verb.Option.ValidValues?.Length > 0) {
                foreach (var v in verb.Option.ValidValues) {
                    Console.WriteLine($"  {v}");
                }
            } else {
                var propertyType = GetOptionBaseType(verb.Property.PropertyType);
                if (propertyType != null && propertyType.IsEnum) {
                    var enumValues = Enum.GetNames(propertyType);
                    foreach (var v in enumValues) {
                        Console.Write($"  {v}{ new string(' ', columnsForName - v.Length - 2) }");

                        var enumVal = Enum.Parse(propertyType, v);
                        var type = enumVal.GetType();
                        var memInfo = type.GetMember(enumVal.ToString());
                        var attribute = memInfo[0].GetCustomAttribute(typeof(DescriptionAttribute), false) as DescriptionAttribute;
                        if (attribute != null) {
                            var desc = attribute.Description;
                            var wrappedText = desc.WordWrap(Console.WindowWidth - columnsForName).TrimEnd();
                            var lines = wrappedText.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                            for (int i = 0; i < lines.Length; i++) {
                                var line = lines[i];
                                if (i == 0)
                                    Console.Write(line);
                                else {
                                    Console.WriteLine();
                                    Console.Write($"{new string(' ', columnsForName)}{line}");
                                }
                            }
                        }
                        Console.WriteLine();
                    }
                }
            }

            Console.WriteLine();
        }

        private void ShowOptionUsage(OptionProperty property, int columnsForName, bool useEqualSyntax)
        {
            var opt = property.Option;

            StringBuilder sb = new StringBuilder();
            sb.Append("  ");
            if (opt.ShortName != '\0') {
                sb.Append($"-{opt.ShortName}, ");
            }

            sb.Append($"--{opt.Name}");
            if (property.Property.PropertyType != typeof(bool)) {
                if (useEqualSyntax)
                    sb.Append("=");
                sb.Append("VALUE");
            }

            if (sb.Length < columnsForName) {
                sb.Append(new string(' ', columnsForName - sb.Length));
            } else {
                sb.AppendLine();
                sb.Append(new string(' ', columnsForName));
            }

            var description = opt.Description?.Trim();
            var validValues = GetShowValidValues(property);
            if (!string.IsNullOrEmpty(validValues))
                description = $"{description}{ (string.IsNullOrEmpty(description) ? string.Empty : Environment.NewLine) }{validValues}";

            if (!string.IsNullOrEmpty(description)) {
                var wrappedText = description.WordWrap(Console.WindowWidth - columnsForName).TrimEnd();
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

        private string GetShowValidValues(OptionProperty property)
        {
            string res = null;

            var propertyType = GetOptionBaseType(property.Property.PropertyType);
            var validValues = property.Option.GetValidValues();
            if (validValues?.Length > 0) {
                res = "Valid values:";
                for (int i = 0; i < validValues.Length; i++) {
                    if (i != 0)
                        res = $"{res},";
                    res = $"{res} {validValues[i]}";
                }
            } else if (propertyType.IsEnum) {
                res = "Valid values:";
                var enumValues = Enum.GetNames(propertyType);
                for (int i = 0; i < enumValues.Length; i++) {
                    if (i != 0)
                        res = $"{res},";
                    res = $"{res} {enumValues[i]}";
                }
            }

            return res;
        }
        #endregion
    }
}
