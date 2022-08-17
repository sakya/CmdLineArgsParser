namespace CmdLineArgsParser
{
    /// <summary>
    /// A parser error
    /// </summary>
    public class ParserError
    {
        /// <summary>
        /// Create a new <see cref="ParserError"/>
        /// </summary>
        /// <param name="optionName">The option name</param>
        /// <param name="message">The error message</param>
        public ParserError(string optionName, string message)
        {
            OptionName = optionName;
            Message = message;
        }

        /// <summary>
        /// The name of the option this error refers to
        /// </summary>
        public string OptionName { get; set; }

        /// <summary>
        /// The error message
        /// </summary>
        public string Message { get; set; }
    }
}
