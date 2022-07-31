# CmdLineArgsParser
[![CodeFactor](https://www.codefactor.io/repository/github/sakya/cmdlineargsparser/badge)](https://www.codefactor.io/repository/github/sakya/cmdlineargsparser)
[![NuGet](https://img.shields.io/nuget/v/cmdlineargsparser.svg)](https://www.nuget.org/packages/CmdLineArgsParser/)

CmdLineArgsParser helps you parse command line arguments.

Define a class containing the options you want to parse and then call the `Parse` method.

When defining the options you can specify:
- **Name (required)**: the long name of the option (e.g. `--option`)
- **ShortName**: the short name of the option (e.g. `-o`)
- **Verb**: boolean indicating it the option is a verb. You can only have one option set as verb and must be passed as the first argument.
- **OnlyForVerbs**: a list of verbs this option is valid for separated by a semicolon 
- **Required**: boolean indicating if the option is required
- **ValidValues**: a list of valid values separated by a semicolon (e.g. "value1;value2")
- **Description**: the description of the option
- **Section**: the section this option belongs to. This is only used by the `ShowUsage` method. If no section name is set the option belongs to the 'General' section.

Supported option type:
- **bool**: set to true if the argument is passed (e.g. `--option` or `-o`). Multiple boolean options can be set using the short name (e.g. `-op`)
- **enum**: set to the value after the argument (e.g. `--enumOpt value`)
- **string**: set to the value after the argument (e.g. `--option "string value"`)
- **int**: set to the value after the argument (e.g. `--intOpt 5`)
- **double/float**: set to the value after the argument (e.g. `--doubleOpt 5.2`)
- **Array of one of the supported built-in types**: one value added for each argument value (e.g. `--input /home/user --input /home/user2`)
- **List of one of the supported built-in types**: one value added for each argument value

## Parse
The `Parse` method parses the given arguments and returns an instance of the options class. 
It also checks for errors in the passed arguments (missing required arguments, wrong argument names...) and returns them in `errors`

## ShowUsage
The `ShowUsage` method prints to the console the options and the help text

Example output:
```
Usage:
Demo.dll action --input VALUE --output VALUE [OPTIONS]

Action:
  Backup                      Description of backup verb
  Copy                        Description of copy verb
  Delete                      Description of delete verb

General:
  -i, --input=VALUE           The input file full path
  -o, --output=VALUE          The output file full path
  -w, --overwrite             Overwrite output without warning
  -r, --retry=VALUE           The number of time the command is retried in case of error
  -y, --yes                   Assume yes as a reply to all the questions
```

## Example
Options class:
```csharp
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

    [Option("action",
        Description = "The command to execute on the file",
        Verb = true, Required = true)]
    public Verbs Command { get; set; }

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
        OnlyForVerbs = "Copy;Backup",
        Description = "Overwrite output without warning")]
    public bool OverwriteOutput { get; set; }
}
```

Usage example:
```csharp
string[] testArgs =
{
    "backup",
    "--input", "/home/user/file",
    "-yw",
    "-r", "5"
};

var options = new CmdLineArgsParser.Parser().Parse<Options>(testArgs, out var errors);
if (errors.Count > 0) {
    Console.WriteLine("Errors:");
    foreach (var error in errors) {
        Console.WriteLine(error.Message);
    }
}
```

The output of the above code is
```
Errors:
Required option 'output' not set
```
The returned `options` object
```json
{
  "Command": 0,
  "InputFile": "/home/user/file",
  "OutputFile": null,
  "Retry": 5,
  "AlwaysYes": true,
  "OverwriteOutput": true
}
```
