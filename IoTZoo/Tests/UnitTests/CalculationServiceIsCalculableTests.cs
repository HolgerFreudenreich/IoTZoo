using Domain.Services.RuleEngine;

namespace UnitTests;

public class CalculationServiceIsCalculableTests
{
    [Theory]
    [InlineData("1 + 1")]
    [InlineData("1 + 2 + 3;")]
    [InlineData("sin(90) - 1")]
    [InlineData("'IoTZoo' == 'IoTZoo'")]
    [InlineData("'it''s' = 'it''s'")]
    [InlineData("5 > 3 and 2 < 4")]
    [InlineData("NOT (1 = 2)")]
    [InlineData("round(3.14159, 2)")]
    [InlineData("0x1F + 1")]
    [InlineData("1e3 * 2")]
    [InlineData("case when 1 > 0 then 'a' else 'b' end")]
    [InlineData("42")]
    public void CalculableExpressionsAreAccepted(string expression)
    {
        Assert.True(CalculationService.IsCalculable(expression));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(";")]
    [InlineData("hello world")]
    [InlineData("Temperature")]
    [InlineData("iotzoo/topic/1")]
    [InlineData("12:30")]
    [InlineData("1; drop table setting")]
    [InlineData("1 -- comment")]
    [InlineData("1 /* comment */")]
    [InlineData("(select count(*) from setting)")]
    [InlineData("'unterminated")]
    [InlineData("load_extension('x')")]
    [InlineData("a.b + 1")]
    [InlineData("+")]
    public void NonCalculableExpressionsAreRejected(string? expression)
    {
        Assert.False(CalculationService.IsCalculable(expression));
    }
}
