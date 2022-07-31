using CmdLineArgsParser.Attributes;

namespace Tests.TestOptions;

public class OptionsRequired : Options
{
    [Option("requiredstring", 'r',
        Required = true)]
    public string? RequiredString { get; set; }

    [Option("requiredforverb1", Required = true, OnlyForVerbs = "Verb1")]
    public string? OptionRequiredForVerb1 { get; set; }
}
