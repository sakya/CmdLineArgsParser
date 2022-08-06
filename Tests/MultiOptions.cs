using CmdLineArgsParser;
using NUnit.Framework;
using Tests.TestOptions;

namespace Tests;

public class MultiOptions : BaseTest
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Boolean()
    {
        var res = Parser.Parse<Options>(
            new []
            {
                "-bc",
            },
            out Errors);
        CheckPropertyValue("Boolean1", res, true);
        CheckPropertyValue("Boolean2", res, true);

        res = Parser.Parse<Options>(
            new []
            {
                "--boolean",
                "--boolean2"
            },
            out Errors);
        CheckPropertyValue("Boolean1", res, true);
        CheckPropertyValue("Boolean2", res, true);

        res = Parser.Parse<Options>(
            new []
            {
                "--boolean",
                "--mt2"
            },
            out Errors);
        CheckPropertyValue("Boolean1", res, true);
        CheckPropertyValue("MutuallyExclusive2", res, true);

        Assert.Pass();
    }

    [Test]
    public void String()
    {
        var res = Parser.Parse<Options>(
            new []
            {
                "-s", "value1",
                "--string2", "value2"
            },
            out Errors);
        CheckPropertyValue("String", res, "value1");
        CheckPropertyValue("String2", res, "value2");

        res = Parser.Parse<Options>(
            new []
            {
                "--string", "value1",
                "--string2", "value2"
            },
            out Errors);
        CheckPropertyValue("String", res, "value1");
        CheckPropertyValue("String2", res, "value2");

        res = Parser.Parse<Options>(
            new []
            {
                "--mt1", "value1",
                "--string2", "value2"
            },
            out Errors);
        CheckPropertyValue("MutuallyExclusive1", res, "value1");
        CheckPropertyValue("String2", res, "value2");

        Assert.Pass();
    }
}
