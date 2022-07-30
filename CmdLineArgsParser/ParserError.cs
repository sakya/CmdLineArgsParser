namespace CmdLineArgsParser
{
    public class ParserError
    {
        public ParserError(string optionName, string message)
        {
            OptionName = optionName;
            Message = message;
        }

        public string OptionName { get; set; }
        public string Message { get; set; }
    }
}
