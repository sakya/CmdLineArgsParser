using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.SymbolStore;
using CmdLineArgsParser;
using CmdLineArgsParser.Attributes;
using Newtonsoft.Json;

namespace Tests;

public class Options : IOptions
{
    public enum Verbs
    {
        [Display(Description = "Description of Verb1")]
        Verb1,
        [Display(Description = "Description of Verb2")]
        Verb2
    }
    public enum EnumValues
    {
        One,
        Two,
        Three
    }

    [Option("", Verb = true)]
    public Verbs? Verb { get; set; }

    [Option("boolean", ShortName = 'b')]
    public bool? Boolean1 { get; set; }
    [Option("boolean2", ShortName = 'c')]
    public bool? Boolean2 { get; set; }

    [Option("string", ShortName = 's')]
    public string? String { get; set; }
    [Option("string2")]
    public string? String2 { get; set; }

    [Option("stringwithvalues", ShortName = 'v', ValidValues = "First;Second")]
    public string? StringWithValues { get; set; }

    [Option("int", ShortName = 'i')]
    public int? IntNumber { get; set; }

    [Option("double", ShortName = 'd')]
    public double? DoubleNumber { get; set; }

    [Option("stringarray", ShortName = 'a')]
    public string[]? StringArray { get; set; }
    [Option("stringlist", ShortName = 'l')]
    public List<string>? StringList { get; set; }

    [Option("enum", ShortName = 'e')]
    public EnumValues? Enum { get; set; }
}

public class OptionsRequired : Options
{
    [Option("requiredstring", ShortName = 'r', Required = true)]
    public string? RequiredString { get; set; }
}