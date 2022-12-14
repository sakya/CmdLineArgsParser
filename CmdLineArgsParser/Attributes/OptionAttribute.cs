using System.Collections.Generic;
using System.Linq;
using CmdLineArgsParser.Interfaces;

namespace CmdLineArgsParser.Attributes
{
    /// <summary>
    /// Attribute to define an option
    /// </summary>
    public class OptionAttribute : System.Attribute, IDescription
    {
        /// <summary>
        /// Create a new <see cref="OptionAttribute"/> with the given name
        /// </summary>
        /// <param name="name">The option name</param>
        public OptionAttribute(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Create a new <see cref="OptionAttribute"/> with the given name ans short name
        /// </summary>
        /// <param name="name">The option name</param>
        /// <param name="shortName">The option short name</param>
        public OptionAttribute(string name, char shortName)
        {
            Name = name;
            ShortName = shortName;
        }

        /// <summary>
        /// Section name, used to show usage
        /// </summary>
        public string Section { get; set; }

        /// <summary>
        /// The option short name
        /// </summary>
        public char ShortName { get; set; }

        /// <summary>
        /// The option name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The option description, used to show usage
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Define if this option is required
        /// </summary>
        public bool Required { get; set; }

        /// <summary>
        /// Define if this option is verb
        /// </summary>
        /// <remarks>You can have only one verb option and it must appear as the first option in the arguments</remarks>
        public bool Verb { get; set; }

        /// <summary>
        /// Set this option valid only for verbs (separated by a semicolon).
        /// </summary>
        public string OnlyForVerbs { get; set; }

        /// <summary>
        /// The option valid values, separated by a semicolon
        /// </summary>
        public string ValidValues { get; set; }

        /// <summary>
        /// The option default value
        /// </summary>
        public string DefaultValue { get; set; }

        /// <summary>
        /// This option cannot be set with other options with the same value of <see cref="MutuallyExclusive"/>
        /// </summary>
        public string MutuallyExclusive { get; set; }

        /// <summary>
        /// Get the option valid values
        /// </summary>
        /// <returns></returns>
        public string[] GetValidValues()
        {
            if (!string.IsNullOrEmpty(ValidValues))
                return ValidValues.Split(';');
            return null;
        }

        /// <summary>
        /// Get the option "only for verbs" values
        /// </summary>
        /// <returns></returns>
        public string[] GetOnlyForVerbs()
        {
            if (!string.IsNullOrEmpty(OnlyForVerbs))
                return OnlyForVerbs.Split(';');
            return null;
        }
    }
}
