# CmdLineArgsParser
[![CodeFactor](https://www.codefactor.io/repository/github/sakya/cmdlineargsparser/badge)](https://www.codefactor.io/repository/github/sakya/cmdlineargsparser)
[![NuGet](https://img.shields.io/nuget/v/cmdlineargsparser.svg)](https://www.nuget.org/packages/CmdLineArgsParser/)
[![License](https://img.shields.io/github/license/sakya/cmdlineargsparser)](https://github.com/sakya/cmdlineargsparser/blob/master/LICENSE)

CmdLineArgsParser helps you parse command line arguments.

Define a class containing the options you want to parse and then call the `Parse` method.

When defining the options you can specify:
- **Name (required)**: the long name of the option (e.g. `--option`)
- **ShortName**: the short name of the option (e.g. `-o`)
- **Verb**: boolean indicating it the option is a verb. You can only have one option set as verb and must be passed as the first argument.
- **OnlyForVerbs**: a list of verbs this option is valid for separated by a semicolon 
- **Required**: boolean indicating if the option is required
- **DefaultValue**: the option default value. A verb, bool, array or list option cannot have a default value
- **ValidValues**: a list of valid values separated by a semicolon (e.g. "value1;value2")
- **MutuallyExclusive**: a string to set the option group name. More than one Option with the same MutuallyExclusive value cannot be set.
- **Description**: the description of the option
- **Section**: the section this option belongs to. This is only used by the `ShowUsage` method. If no section name is set the option belongs to the 'General' section.

Supported option type:
- **bool**: set to true if the argument is passed (e.g. `--option` or `-o`). Multiple boolean options can be set using the short name (e.g. `-op`)
- **enum**: set to the value after the argument (e.g. `--enumOpt value`)
- **string**: set to the value after the argument (e.g. `--option "string value"`)
- **int**: set to the value after the argument (e.g. `--intOpt 5`)
- **double/float**: set to the value after the argument (e.g. `--doubleOpt 5.2`)
- **DateTime**: set to the value after the argument (e.g. `--dateTimeOpt "12/01/2022 3:54:23 PM"`)
- **Uri**: set to the value after the argument (e.g. `--uriOpt https://host/address`)
- **Array of one of the supported built-in types**: one value added for each argument value (e.g. `--input /home/user --input /home/user2`)
- **List of one of the supported built-in types**: one value added for each argument value

## Parse
The `Parse` method parses the given arguments and returns an instance of the options class. 
It also checks for errors in the passed arguments (missing required arguments, wrong argument names...) and returns them in `errors`

## ShowInfo
The `ShowInfo` method prints to the console the assembly title, version and description

Example output:
```
CmdLineArgsParser Demo v0.1.0.0
Copyright ?? 2022, Paolo Iommarini

Some words about the program
```
## ShowUsage
The `ShowUsage` method prints to the console the options and the help text

Example output:
```
Usage:
Demo.dll action --input=VALUE [OPTIONS]

Action:
  Backup                      Description of backup verb
  Copy                        Description of copy verb
  Delete                      Description of delete verb

General:
  --enum=VALUE                An option with enum values
                              Valid values: EnumValue1, EnumValue2, EnumValue3, EnumValue4
  -i, --input=VALUE           The input file full path
  --list=VALUE                An option you can set multiple times
                              This option can be set multiple times
  -r, --retry=VALUE           The number of time the command is retried in case of error
  --withdefaultvalue=VALUE    An option with a default value
                              Default value: 5
  --withValues=VALUE          An option with a list of valid values
                              Valid values: value1, value2
  -y, --yes                   Assume yes as a reply to all the questions

Backup:
  -o, --output=VALUE          The output file full path
                              Valid for action: Backup, Copy
  -w, --overwrite             Overwrite output without warning
                              Valid for action: Copy, Backup

Copy:
  -o, --output=VALUE          The output file full path
                              Valid for action: Backup, Copy
  -w, --overwrite             Overwrite output without warning
                              Valid for action: Copy, Backup
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

    [Option("withdefaultvalue",
        Description = "An option with a default value",
        DefaultValue = "5")]
    public int? WithDefaultValue { get; set; }
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
  "OverwriteOutput": true,
  "WithValues": null,
  "SomeEumValue": 0,
  "SomeListValue": null,
  "WithDefaultValue": 5
}
```
