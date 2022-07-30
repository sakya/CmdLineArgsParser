using CmdLineArgsParser;
using NUnit.Framework;

namespace Tests;

public class Errors : BaseTest
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void InvalidVerb()
    {
        Parser.Parse<Options>(
            new ParserSettings(),
            new[]
            {
                "testVerb",
            },
            out Errors);
        CheckErrors(new[] { "Invalid value for verb option: testVerb" });
    }


    [Test]
    public void InvalidOption()
    {
        Parser.Parse<Options>(
            new ParserSettings(),
            new []
            {
                "--int", "5",
                "testvalue",
            },
            out Errors);
        CheckErrors(new [] { "Value without option: 'testvalue'" });

        Parser.Parse<Options>(
            new ParserSettings(),
            new []
            {
                "--invalid",
            },
            out Errors);
        CheckErrors(new [] { "Unknown option 'invalid'" });

        Parser.Parse<Options>(
            new ParserSettings(),
            new []
            {
                "-x",
            },
            out Errors);
        CheckErrors(new [] { "Unknown option 'x'" });

        Assert.Pass();
    }

    [Test]
    public void InvalidValue()
    {
        Parser.Parse<Options>(
            new ParserSettings(),
            new []
            {
                "--int", "xyz"
            },
            out Errors);
        CheckErrors(new [] { "Invalid value for option 'int' (expected int): xyz" });

        Parser.Parse<Options>(
            new ParserSettings(),
            new []
            {
                "--double", "xyz"
            },
            out Errors);
        CheckErrors(new [] { "Invalid value for option 'double' (expected double): xyz" });

        Assert.Pass();
    }

    [Test]
    public void InvalidOptionValue()
    {
        Parser.Parse<Options>(
            new ParserSettings(),
            new []
            {
                "--stringwithvalues", "First2"
            },
            out Errors);
        CheckErrors(new [] { "Invalid value for option 'stringwithvalues': First2" });

        Parser.Parse<Options>(
            new ParserSettings(),
            new []
            {
                "-v", "First2"
            },
            out Errors);
        CheckErrors(new [] { "Invalid value for option 'stringwithvalues': First2" });

        Assert.Pass();
    }

    [Test]
    public void OptionSetMultipleTimes()
    {
        Parser.Parse<Options>(
            new ParserSettings(),
            new []
            {
                "--int", "5",
                "--int", "4"
            },
            out Errors);
        CheckErrors(new [] { "Option 'int' set multiple times" });

        Parser.Parse<Options>(
            new ParserSettings(),
            new []
            {
                "--stringwithvalues", "First",
                "--stringwithvalues", "Second"
            },
            out Errors);
        CheckErrors(new [] { "Option 'stringwithvalues' set multiple times" });
    }

    [Test]
    public void RequiredOptionNotSet()
    {
        Parser.Parse<OptionsRequired>(
            new ParserSettings(),
            new []
            {
                "--int", "5",
            },
            out Errors);
        CheckErrors(new [] { "Required option 'requiredstring' not set" });
    }

    [Test]
    public void OptionForVerb()
    {
        Parser.Parse<Options>(
            new ParserSettings(),
            new []
            {
                "verb2",
                "--forverb1",
            },
            out Errors);
        CheckErrors(new [] { "Option 'forverb1' is not valid for verb Verb2" });
    }
}