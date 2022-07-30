using System.Collections.Generic;
using System.Linq;
using CmdLineArgsParser.Interfaces;

namespace CmdLineArgsParser.Attributes
{
    public class OptionAttribute : System.Attribute, IDescription
    {
        public OptionAttribute(string name)
        {
            Name = name;
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

        public string[] GetValidValues()
        {
            if (!string.IsNullOrEmpty(ValidValues))
                return ValidValues.Split(';');
            return null;
        }

        public string[] GetOnlyForVerbs()
        {
            if (!string.IsNullOrEmpty(OnlyForVerbs))
                return OnlyForVerbs.Split(';');
            return null;
        }
    }
}