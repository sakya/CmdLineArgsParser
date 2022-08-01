using System.Collections.Generic;
using CmdLineArgsParser;
using NUnit.Framework;
using Tests.TestOptions;

namespace Tests;

public class EqualSyntax: BaseTest
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void String()
    {
        var res = Parser.Parse<Options>(
            new[]
            {
                "--string=value"
            },
            out Errors);
        CheckPropertyValue("String", res, "value");

        res = Parser.Parse<Options>(
            new[]
            {
                "--string=", "value"
            },
            out Errors);
        CheckPropertyValue("String", res, "value");

        res = Parser.Parse<Options>(
            new[]
            {
                "--string=value=test"
            },
            out Errors);
        CheckPropertyValue("String", res, "value=test");
    }

    [Test]
    public void StringList()
    {
        var res = Parser.Parse<Options>(
            new []
            {
                "-l=test1",
                "-l=test2"
            },
            out Errors);
        CheckPropertyValue("StringList", res, new List<string>() { "test1", "test2" });

        res = Parser.Parse<Options>(
            new []
            {
                "--stringlist=test1",
                "--stringlist=test2"
            },
            out Errors);
        CheckPropertyValue("StringList", res, new List<string>() { "test1", "test2" });

        Assert.Pass();
    }

    [Test]
    public void Int()
    {
        var res = Parser.Parse<Options>(
            new []
            {
                "-i=5"
            },
            out Errors);
        CheckPropertyValue("IntNumber", res, 5);

        res = Parser.Parse<Options>(
            new []
            {
                "--int=5"
            },
            out Errors);
        CheckPropertyValue("IntNumber", res, 5);

        Assert.Pass();
    }

    [Test]
    public void Enum()
    {
        var res = Parser.Parse<Options>(
            new []
            {
                "-e=One",
            },
            out Errors);
        CheckPropertyValue("Enum", res, Options.EnumValues.One);

        res = Parser.Parse<Options>(
            new []
            {
                "--enum=Two",
            },
            out Errors);
        CheckPropertyValue("Enum", res, Options.EnumValues.Two);
    }
}