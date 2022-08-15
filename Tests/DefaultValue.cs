using NUnit.Framework;
using Tests.TestOptions;

namespace Tests;

public class DefaultValue : BaseTest
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Boolean()
    {
        var res = Parser.Parse<OptionsDefaultValue>(
            new string[] {},
            out Errors);
        CheckPropertyValue("BoolDefault", res, false);
        CheckPropertyValue("StringDefault", res, "value1");
        CheckPropertyValue("IntDefault", res, 5);
        CheckPropertyValue("EnumDefault1", res, Options.EnumValues.One);
        CheckPropertyValue("EnumDefault2", res, Options.EnumValues.Two);

        Assert.Pass();
    }
}