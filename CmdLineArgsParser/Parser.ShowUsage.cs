using System;
using System.Collections.Generic;
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
        private const string DefaultSection = "General";

        /// <summary>
        /// Writes information lines<br/>
        /// <br/>
        /// AssemblyTitle AssemblyVersion<br/>
        /// AssemblyCopyright<br/>
        /// AssemblyDescription<br/>
        /// </summary>
        /// <param name="includeDescription">Define if the AssemblyDescription should be printed</param>
        public void ShowInfo(bool includeDescription = true)
        {
            ShowAssemblyInformation(Assembly.GetCallingAssembly(), includeDescription);
        }

        /// <summary>
        /// Writes usage information tow the <see cref="Console"/>
        /// </summary>
        /// <param name="assemblyName">The assembly name to use for the usage line (defaults to the calling assembly name)</param>
        /// <param name="columnsForName">The number of columns to reserve for option names (default: 0)</param>
        /// <param name="useEqualSyntax">If set to true the usage uses the equal syntax (e.g. --option=VALUE)</param>
        /// <typeparam name="T"></typeparam>
        public void ShowUsage<T>(string assemblyName = null, int columnsForName = 30, bool useEqualSyntax = true) where T : IOptions, new()
        {
            if (columnsForName <= 0)
                throw new ArgumentException($"{nameof(columnsForName)} must be greater than zero", nameof(columnsForName));
            ValidateOptionsType<T>();

            var cAssembly = Assembly.GetCallingAssembly();
            var properties = GetProperties<T>();
            _verbOption = properties.FirstOrDefault(p => p.Option.Verb);

            if (string.IsNullOrEmpty(assemblyName))
                assemblyName = Path.GetFileName(cAssembly.Location);
            ShowUsageLine(assemblyName, properties, _verbOption, useEqualSyntax);

            // Options
            // Verb first
            if (_verbOption != null && _verbOption.HasValuesList) {
                ShowVerbUsage(_verbOption, columnsForName);
            }

            var sections = GetSections(properties);
            if (sections.TryGetValue(DefaultSection, out var general)) {
                ShowSectionUsage(DefaultSection, general);
            }

            foreach (var sectionName in sections.Keys.Where(k => k != DefaultSection).OrderBy(k => k)) {
                ShowSectionUsage(sectionName, sections[sectionName]);
            }

            _verbOption = null;
        }

        #region private operations

        private void ShowAssemblyInformation(Assembly assembly, bool includeDescription = false)
        {
            bool newline = false;
            if (assembly.GetCustomAttribute(typeof(AssemblyTitleAttribute)) is AssemblyTitleAttribute ta) {
                Console.WriteLine($"{ta.Title} v{assembly.GetName().Version}");
                newline = true;
            }

            if (assembly.GetCustomAttribute(typeof(AssemblyCopyrightAttribute)) is AssemblyCopyrightAttribute ca) {
                Console.WriteLine(ca.Copyright);
                newline = true;
            }

            if (newline) {
                Console.WriteLine();
                newline = false;
            }

            if (includeDescription && assembly.GetCustomAttribute(typeof(AssemblyDescriptionAttribute)) is AssemblyDescriptionAttribute da) {
                Console.WriteLine(da.Description);
                newline = true;
            }

            if (newline)
                Console.WriteLine();
        }

        private void ShowUsageLine(string callingAssembly, OptionProperty[] properties, OptionProperty verb, bool useEqualSyntax)
        {
            // Usage line
            Console.WriteLine("Usage:");
            var usage = $"{callingAssembly}";
            if (verb != null)
                usage = $"{usage} {(verb.Option.Required ? verb.Option.Name : $"[{ verb.Option.Name }]")}";

            foreach (var req in properties.Where(p => !p.Option.Verb && string.IsNullOrEmpty(p.Option.OnlyForVerbs?.Trim()) && p.Option.Required).OrderBy(p => p.Option.Name)) {
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
        }

        private void ShowSectionUsage(string section, IEnumerable<OptionProperty> properties, int columnsForName = 30, bool useEqualSyntax = true)
        {
            Console.WriteLine();
            Console.WriteLine($"{ (string.IsNullOrEmpty(section) ? DefaultSection : section) }:");
            foreach (var property in properties.OrderBy(p => p.Option.Name)) {
                ShowOptionUsage(property, columnsForName, useEqualSyntax);
            }
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
            var onlyForVerbs = GetShowOnlyForVerbs(property);
            if (!string.IsNullOrEmpty(onlyForVerbs))
                description = $"{description}{ (string.IsNullOrEmpty(description) ? string.Empty : Environment.NewLine) }{onlyForVerbs}";

            if (property.IsEnumerable) {
                description = $"{description}{Environment.NewLine}This option can be set multiple times";
            }

            if (!string.IsNullOrEmpty(property.Option.DefaultValue)) {
                var v = GetValueFromString(property.Property.PropertyType, property.Option.DefaultValue, out _);
                description = $"{description}{Environment.NewLine}Default value: {v}";
            }

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

        private Dictionary<string, List<OptionProperty>> GetSections(IEnumerable<OptionProperty> properties)
        {
            var res = new Dictionary<string, List<OptionProperty>>();
            foreach (var p in properties) {
                if (p.Option.Verb)
                    continue;

                if (!string.IsNullOrEmpty(p.Option.Section)) {
                    if (res.TryGetValue(p.Option.Section, out var list)) {
                        list.Add(p);
                    } else {
                        res[p.Option.Section] = new List<OptionProperty>() { p };
                    }
                } else if (!string.IsNullOrEmpty(p.Option.OnlyForVerbs)) {
                    foreach (var v in p.Option.GetOnlyForVerbs()) {
                        var verb = GetValueFromString(_verbOption.Property.PropertyType, v, out _);
                        if (res.TryGetValue(verb.ToString(), out var list)) {
                            list.Add(p);
                        } else {
                            res[verb.ToString()] = new List<OptionProperty>() { p };
                        }
                    }
                } else {
                    if (res.TryGetValue(DefaultSection, out var list)) {
                        list.Add(p);
                    } else {
                        res[DefaultSection] = new List<OptionProperty>() { p };
                    }
                }
            }

            return res;
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

        private string GetShowOnlyForVerbs(OptionProperty property)
        {
            string res = null;

            var onlyForVerbs = property.Option.GetOnlyForVerbs();
            if (onlyForVerbs?.Length > 0) {
                res = $"Valid for {_verbOption.Option.Name}:";
                for (int i = 0; i < onlyForVerbs.Length; i++) {
                    var v = GetValueFromString(_verbOption.Property.PropertyType, onlyForVerbs[i], out _);
                    if (i != 0)
                        res = $"{res},";
                    res = $"{res} {v}";
                }
            }

            return res;
        }
        #endregion
    }
}
