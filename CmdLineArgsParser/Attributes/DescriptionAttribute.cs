using CmdLineArgsParser.Interfaces;

namespace CmdLineArgsParser.Attributes
{
    public class DescriptionAttribute : System.Attribute, IDescription
    {
        public DescriptionAttribute(string description)
        {
            Description = description;
        }

        public string Description { get; set; }
    }
}
