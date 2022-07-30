using CmdLineArgsParser.Attributes;

namespace Tests;

public class OptionsRequired : Options
{
    [Option("requiredstring", ShortName = 'r', Required = true)]
    public string? RequiredString { get; set; }

    [Option("requiredforverb1", Required = true, OnlyForVerbs = "Verb1")]
    public string? OptionRequiredForVerb1 { get; set; }
}