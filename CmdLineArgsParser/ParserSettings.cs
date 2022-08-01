namespace CmdLineArgsParser
{
    /// <summary>
    /// The <see cref="Parser"/> settings
    /// </summary>
    public class ParserSettings
    {
        public ParserSettings()
        {
            DateTimeFormat = null;
            EnableEqualSyntax = true;
        }

        /// <summary>
        /// Enable syntax with equal sign between option name and value (e.g. --option=value)
        /// </summary>
        public bool EnableEqualSyntax { get; set; }

        /// <summary>
        /// The date time format to use for date time options.
        /// If set to null <see cref="Parser"/> tries to parse date times using default settings
        /// </summary>
        public string DateTimeFormat { get; set; }
    }
}
