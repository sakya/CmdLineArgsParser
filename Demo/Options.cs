using CmdLineArgsParser;
using CmdLineArgsParser.Attributes;

namespace Demo;

public class Options : IOptions
{
    [Option("command", ShortName = 'c',
        Description = "The command to execute on the file",
        Required = true, ValidValues = "copy;delete;backup")]
    public string? Command { get; set; }

    [Option("input", ShortName = 'i',
        Description = "The input file full path",
        Required = true)]
    public string? InputFile { get; set; }

    [Option("output", ShortName = 'o',
        Description = "The output file full path",
        Required = true)]
    public string? OutputFile { get; set; }

    [Option("retry", ShortName = 'r',
        Description = "The number of time the command is retried in case of error")]
    public int? Retry { get; set; }

    [Option("yes", ShortName = 'y',
        Description = "Assume yes as a reply to all the questions")]
    public bool AlwaysYes { get; set; }

    [Option("overwrite", ShortName = 'w',
        Description = "Overwrite output without warning")]
    public bool OverwriteOutput { get; set; }
}