using CmdLineArgsParser.Interfaces;

namespace CmdLineArgsParser.Attributes
{
    /// <summary>
    /// Attribute to add description to a member
    /// </summary>
    public class DescriptionAttribute : System.Attribute, IDescription
    {
        /// <summary>
        /// Create a new <see cref="DescriptionAttribute"/> with the given description
        /// </summary>
        /// <param name="description"></param>
        public DescriptionAttribute(string description)
        {
            Description = description;
        }

        /// <summary>
        /// The member description
        /// </summary>
        public string Description { get; set; }
    }
}
