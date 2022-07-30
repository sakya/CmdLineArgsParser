using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using CmdLineArgsParser.Attributes;
using CmdLineArgsParser.Extensions;

namespace CmdLineArgsParser
{
    public partial class Parser
    {
        /// <summary>
        /// Writes options to the <see cref="Console"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void ShowUsage<T>(int columnsForName = 30) where T : IOptions, new()
        {
            if (columnsForName <= 0)
                throw new ArgumentException($"{nameof(columnsForName)} must be greater than zero",
                    nameof(columnsForName));

            ValidateOptionsType<T>();
            var properties = GetProperties<T>();
            var verb = properties.FirstOrDefault(p => p.Option.Verb);

            // Usage line
            Console.WriteLine("Usage:");
            var usage = $"{Path.GetFileName(Assembly.GetCallingAssembly().Location)}";
            if (verb != null) {
                if (string.IsNullOrEmpty(verb.Option.Name))
                    usage = $"{usage} {(verb.Option.Required ? "VERB" : "[VERB]")}";
                else
                    usage = $"{usage} {(verb.Option.Required ? verb.Option.Name : $"[{ verb.Option.Name }]")}";
            }

            foreach (var req in properties.Where(p => !p.Option.Verb && p.Option.Required).OrderBy(p => p.Option.Name)) {
                usage = $"{usage} --{req.Option.Name} VALUE";
            }
            if (properties.Any(p => !p.Option.Verb && !p.Option.Required))
                usage = $"{usage} [OPTIONS]";
            Console.WriteLine(usage);

            Console.WriteLine();
            // Options
            // Verb first
            if (verb != null && verb.HasValuesList) {
                ShowVerbUsage(verb, columnsForName);
            }

            var sections = properties.GroupBy(p => p.Option.Section);
            foreach (var section in sections) {
                Console.WriteLine($"{ (string.IsNullOrEmpty(section.Key) ? "General" : section.Key) }:");
                foreach (var property in section.Where(p => !p.Option.Verb).OrderBy(p => p.Option.Name)) {
                    ShowOptionUsage(property, columnsForName);
                }
            }
        }

        private void ShowVerbUsage(OptionProperty verb, int columnsForName)
        {
            Console.WriteLine(string.IsNullOrEmpty(verb.Option.Name) ? "Verb:" : $"{verb.Option.Name.Capitalize()}:");
            if (verb.Option.ValidValues?.Length > 0) {
                foreach (var v in verb.Option.ValidValues) {
                    Console.WriteLine($"  {v}");
                }
            } else {
                var propertyType = GetOptionBaseType(verb.Property.PropertyType);
                if (propertyType != null) {
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

        private void ShowOptionUsage(OptionProperty property, int columnsForName)
        {
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
