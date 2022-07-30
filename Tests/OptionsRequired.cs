using CmdLineArgsParser.Attributes;

namespace Tests;

public class OptionsRequired : Options
{
    [Option("requiredstring", ShortName = 'r', Required = true)]
    public string? RequiredString { get; set; }
}