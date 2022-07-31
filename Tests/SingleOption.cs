using System.Collections.Generic;
using CmdLineArgsParser;
using NUnit.Framework;
using Tests.TestOptions;

namespace Tests;

public class SingleOption : BaseTest
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Boolean()
    {
        var res = Parser.Parse<Options>(
            new ParserSettings(),
            new []
            {
                "-b",
            },
            out Errors);
        CheckPropertyValue("Boolean1", res, true);

        res = Parser.Parse<Options>(
            new ParserSettings(),
            new []
            {
                "--boolean",
            },
            out Errors);
        CheckPropertyValue("Boolean1", res, true);

        Assert.Pass();
    }

    [Test]
    public void String()
    {
        var res = Parser.Parse<Options>(
            new ParserSettings(),
            new []
            {
                "-s", "value"
            },
            out Errors);
        CheckPropertyValue("String", res, "value");

        res = Parser.Parse<Options>(
            new ParserSettings(),
            new []
            {
                "--string", "value"
            },
            out Errors);

        CheckPropertyValue("String", res, "value");

        Assert.Pass();
    }

    [Test]
    public void StringWithValues()
    {
        var res = Parser.Parse<Options>(
            new ParserSettings(),
            new []
            {
                "-v", "First"
            },
            out Errors);
        CheckPropertyValue("StringWithValues", res, "First");

        res = Parser.Parse<Options>(
            new ParserSettings(),
            new []
            {
                "--stringwithvalues", "First"
            },
            out Errors);
        CheckPropertyValue("StringWithValues", res, "First");

        Assert.Pass();
    }

    [Test]
    public void Int()
    {
        var res = Parser.Parse<Options>(
            new ParserSettings(),
            new []
            {
                "-i", "5"
            },
            out Errors);
        CheckPropertyValue("IntNumber", res, 5);

        res = Parser.Parse<Options>(
            new ParserSettings(),
            new []
            {
                "--int", "5"
            },
            out Errors);
        CheckPropertyValue("IntNumber", res, 5);

        Assert.Pass();
    }

    [Test]
    public void Double()
    {
        var res = Parser.Parse<Options>(
            new ParserSettings(),
            new []
            {
                "-d", "5.3"
            },
            out Errors);
        CheckPropertyValue("DoubleNumber", res, 5.3);

        res = Parser.Parse<Options>(
            new ParserSettings(),
            new []
            {
                "--double", "5.3"
            },
            out Errors);
        CheckPropertyValue("DoubleNumber", res, 5.3);

        Assert.Pass();
    }

    [Test]
    public void StringArray()
    {
        var res = Parser.Parse<Options>(
            new ParserSettings(),
            new []
            {
                "-a", "test1",
                "-a", "test2"
            },
            out Errors);
        CheckPropertyValue("StringArray", res, new [] { "test1", "test2" });

        res = Parser.Parse<Options>(
            new ParserSettings(),
            new []
            {
                "--stringarray", "test1",
                "--stringarray", "test2"
            },
            out Errors);
        CheckPropertyValue("StringArray", res, new [] { "test1", "test2" });

        Assert.Pass();
    }

    [Test]
    public void StringList()
    {
        var res = Parser.Parse<Options>(
            new ParserSettings(),
            new []
            {
                "-l", "test1",
                "-l", "test2"
            },
            out Errors);
        CheckPropertyValue("StringList", res, new List<string>() { "test1", "test2" });

        res = Parser.Parse<Options>(
            new ParserSettings(),
            new []
            {
                "--stringlist", "test1",
                "--stringlist", "test2"
            },
            out Errors);
        CheckPropertyValue("StringList", res, new List<string>() { "test1", "test2" });

        Assert.Pass();
    }

    [Test]
    public void Enum()
    {
        var res = Parser.Parse<Options>(
            new ParserSettings(),
            new []
            {
                "-e", "One",
            },
            out Errors);
        CheckPropertyValue("Enum", res, Options.EnumValues.One);

        res = Parser.Parse<Options>(
            new ParserSettings(),
            new []
            {
                "--enum", "Two",
            },
            out Errors);
        CheckPropertyValue("Enum", res, Options.EnumValues.Two);
    }

    [Test]
    public void Verb()
    {
        var res = Parser.Parse<Options>(
            new ParserSettings(),
            new[]
            {
                "verb1"
            },
            out Errors);
        CheckPropertyValue("Verb", res, Options.Verbs.Verb1);
    }

    [Test]
    public void OptionForVerb()
    {
        var res = Parser.Parse<Options>(
            new ParserSettings(),
            new[]
            {
                "verb1",
                "--forverb1",
            },
            out Errors);
        CheckPropertyValue("OptionForVerb1", res, true);
    }
}
