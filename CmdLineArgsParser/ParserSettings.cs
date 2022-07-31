namespace CmdLineArgsParser
{
    /// <summary>
    /// The <see cref="Parser"/> settings
    /// </summary>
    public class ParserSettings
    {
        public ParserSettings()
        {
            EnableEqualSyntax = true;
        }

        /// <summary>
        /// Enable syntax with equal sign between option name and value (e.g. --option=value)
        /// </summary>
        public bool EnableEqualSyntax { get; set; }
    }
}
