using CmdLineArgsParser.Attributes;

namespace Tests.TestOptions;

public class OptionsDefaultValue : Options
{
    [Option("booleandefault")]
    public bool? BoolDefault { get; set; }

    [Option("stringdefault",
        DefaultValue = "value1",
        ValidValues = "value1;value2")]
    public string? StringDefault { get; set; }

    [Option("intdefault",
        DefaultValue = "5")]
    public int? IntDefault { get; set; }

    [Option("enumdefault",
        DefaultValue = "One")]
    public EnumValues? EnumDefault1 { get; set; }

    [Option("enumdefault2",
        DefaultValue = "TWO")]
    public EnumValues? EnumDefault2 { get; set; }
}