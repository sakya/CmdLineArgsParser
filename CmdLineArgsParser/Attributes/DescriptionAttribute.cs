namespace CmdLineArgsParser.Attributes
{
    public class DescriptionAttribute : System.Attribute
    {
        public DescriptionAttribute(string description)
        {
            Description = description;
        }

        public string Description { get; set; }
    }
}