using CmdLineArgsParser;
using CmdLineArgsParser.Attributes;

namespace Demo;

public class Options : IOptions
{
    public enum Verbs
    {
        [Description("Description of backup verb")]
        Backup,
        [Description("Description of copy verb")]
        Copy,
        [Description("Description of delete verb")]
        Delete,
    }

    public enum SomeEnumValues
    {
        EnumValue1,
        EnumValue2,
        EnumValue3,
        EnumValue4,
    }

    [Option("action",
        Description = "The command to execute on the file",
        Verb = true, Required = true)]
    public Verbs Command { get; set; }

    [Option("input", 'i',
        Description = "The input file full path",
        Required = true)]
    public string? InputFile { get; set; }

    [Option("output", 'o',
        Description = "The output file full path",
        Required = true,
        OnlyForVerbs = "Backup;Copy")]
    public string? OutputFile { get; set; }

    [Option("retry", 'r',
        Description = "The number of time the command is retried in case of error")]
    public int? Retry { get; set; }

    [Option("yes", 'y',
        Description = "Assume yes as a reply to all the questions")]
    public bool AlwaysYes { get; set; }

    [Option("overwrite", 'w',
        OnlyForVerbs = "Copy;Backup",
        Description = "Overwrite output without warning")]
    public bool OverwriteOutput { get; set; }

    [Option("withValues",
        ValidValues = "value1;value2",
        Description = "An option with a list of valid values")]
    public string? WithValues { get; set; }

    [Option("enum",
        Description = "An option with enum values")]
    public SomeEnumValues SomeEumValue { get; set; }

    [Option("list",
        Description = "An option you can set multiple times")]
    public List<string>? SomeListValue { get; set; }
}
