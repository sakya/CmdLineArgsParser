using CmdLineArgsParser;
using NUnit.Framework;

namespace Tests;

public class FromString : BaseTest
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void OptionsFromString()
    {
        var res = Parser.Parse<Options>(
            new ParserSettings(),
            "verb1 -bc --stringwithvalues First --stringarray \"test1\" --stringarray \"string with spaces\"",
            out Errors);
        CheckPropertyValue("Verb", res, Options.Verbs.Verb1);
        CheckPropertyValue("Boolean1", res, true);
        CheckPropertyValue("Boolean2", res, true);
        CheckPropertyValue("StringArray", res, new [] { "test1", "string with spaces" });
        CheckPropertyValue("StringWithValues", res, "First");
    }
}