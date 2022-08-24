using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CmdLineArgsParser;
using CmdLineArgsParser.Attributes;

namespace Tests.TestOptions;

public class Options : IOptions
{
    public enum Verbs
    {
        [Display(Description = "Description of Verb1")]
        Verb1,
        [Display(Description = "Description of Verb2")]
        Verb2,
        [Display(Description = "Description of Verb3")]
        Verb3
    }
    public enum EnumValues
    {
        One,
        Two,
        Three
    }

    [Option("action", Verb = true)]
    public Verbs? Verb { get; set; }

    [Option("boolean", 'b')]
    public bool? Boolean1 { get; set; }
    [Option("boolean2", 'c')]
    public bool? Boolean2 { get; set; }

    [Option("string", 's')]
    public string? String { get; set; }
    [Option("string2")]
    public string? String2 { get; set; }

    [Option("stringwithvalues", 'v',
        ValidValues = "First;Second")]
    public string? StringWithValues { get; set; }

    [Option("int", 'i')]
    public int? IntNumber { get; set; }

    [Option("double", 'd')]
    public double? DoubleNumber { get; set; }

    [Option("stringarray", 'a')]
    public string[]? StringArray { get; set; }
    [Option("stringlist", 'l')]
    public List<string>? StringList { get; set; }

    [Option("enum", 'e')]
    public EnumValues? Enum { get; set; }

    [Option("forverb1", OnlyForVerbs = "Verb1;Verb3")]
    public bool? OptionForVerb1 { get; set; }

    [Option("datetime")]
    public DateTime? DateTime { get; set; }

    [Option("mt1",
        MutuallyExclusive = "group")]
    public string? MutuallyExclusive1 { get; set; }
    [Option("mt2",
        MutuallyExclusive = "group")]
    public bool MutuallyExclusive2 { get; set; }

    [Option("uri")]
    public Uri? Uri { get; set; }
}
