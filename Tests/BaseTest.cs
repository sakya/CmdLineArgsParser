using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CmdLineArgsParser;
using NUnit.Framework;

namespace Tests;

public abstract class BaseTest
{
    protected readonly Parser Parser = new Parser();
    protected List<ParserError>? Errors;

    protected void CheckPropertyValue(string propertyName, object res, object value)
    {
        var property = res.GetType().GetProperty(propertyName);
        if (property == null)
            Assert.Fail($"Property {propertyName} not found");

        var objValue = property?.GetValue(res);
        if (property?.PropertyType.IsArray == true) {
            Array array = objValue as Array;
            Array valueArray = value as Array;
            if (valueArray == null)
                Assert.Fail($"Wrong value for comparison (not an array)");

            if (valueArray?.Length != array?.Length)
                Assert.Fail($"Property {propertyName} expected value was {value}, got {objValue}");
            for (int i = 0; i < array.Length; i++) {
                if (!array.GetValue(i).Equals(valueArray.GetValue(i)))
                    Assert.Fail($"Property {propertyName} expected value was {value}, got {objValue}");
            }
        } else if (property.PropertyType.IsGenericType && typeof(List<>).IsAssignableFrom(property.PropertyType.GetGenericTypeDefinition())) {
            IList list = objValue as IList;
            IList valueList = value as IList;
            if (valueList == null)
                Assert.Fail($"Wrong value for comparison (not an array)");

            if (valueList?.Count != list?.Count)
                Assert.Fail($"Property {propertyName} expected value was {value}, got {objValue}");
            for (int i = 0; i < list.Count; i++) {
                if (!list[i].Equals(valueList[i]))
                    Assert.Fail($"Property {propertyName} expected value was {value}, got {objValue}");
            }

        } else {
            if (!value.Equals(objValue)) {
                Assert.Fail($"Property {propertyName} expected value was {value}, got {objValue}");
            }
        }
    }

    protected void CheckErrors(string[] errors)
    {
        if (Errors!.Count != errors.Length)
            Assert.Fail();

        foreach (var error in errors) {
            if (Errors.FirstOrDefault(e => e.Message == error) == null) {
                if (Errors.Count == 1 && errors.Length == 1)
                    Assert.Fail($"Expected error was '{errors[0]}', got '{Errors[0].Message}'");
                Assert.Fail();
            }
        }
    }
}