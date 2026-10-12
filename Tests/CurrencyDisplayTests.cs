using System.Globalization;
using DoughDoodle.Services.Currency;
using Xunit;

namespace DoughDoodle.Tests;

public class CurrencyDisplayTests
{
    [Theory]
    [InlineData(null, "USD")]
    [InlineData("", "USD")]
    [InlineData("usd", "USD")]
    [InlineData("ZAR", "ZAR")]
    [InlineData("US", "USD")]
    [InlineData("USDT", "USD")]
    [InlineData("12A", "USD")]
    public void NormalizeCode_DefaultsInvalidToUsd(string? input, string expected)
    {
        Assert.Equal(expected, CurrencyDisplay.NormalizeCode(input));
    }

    [Fact]
    public void Symbol_UsesNumberFormatInfoWithoutConverting()
    {
        Assert.Equal(CultureInfo.GetCultureInfo("en-US").NumberFormat.CurrencySymbol, CurrencyDisplay.Symbol("USD"));
        Assert.Equal(CultureInfo.GetCultureInfo("de-DE").NumberFormat.CurrencySymbol, CurrencyDisplay.Symbol("EUR"));
        Assert.Equal(CultureInfo.GetCultureInfo("en-GB").NumberFormat.CurrencySymbol, CurrencyDisplay.Symbol("GBP"));
        Assert.Equal(CultureInfo.GetCultureInfo("en-ZA").NumberFormat.CurrencySymbol, CurrencyDisplay.Symbol("ZAR"));
        Assert.Equal("$", CultureInfo.GetCultureInfo("en-AU").NumberFormat.CurrencySymbol);
        Assert.Equal("A$", CurrencyDisplay.Symbol("AUD"));
        Assert.Equal("C$", CurrencyDisplay.Symbol("CAD"));
        Assert.Equal("JPY ", CurrencyDisplay.Symbol("JPY"));
    }

    [Fact]
    public void Format_UsesSymbolWithoutConvertingAmount()
    {
        var amount = 1234.50m;
        Assert.Equal("$1,234.50", CurrencyDisplay.Format(amount, "USD"));
        Assert.Equal("R1,234.50", CurrencyDisplay.Format(amount, "ZAR"));
        Assert.Equal("\u20AC1,234.50", CurrencyDisplay.Format(amount, "EUR"));
        Assert.Equal("\u00A31,234.50", CurrencyDisplay.Format(amount, "GBP"));
        Assert.Equal("A$1,234.50", CurrencyDisplay.Format(amount, "AUD"));
        Assert.Equal("C$1,234.50", CurrencyDisplay.Format(amount, "CAD"));
        Assert.Equal("$1,234.50", CurrencyDisplay.Format(amount, null));
        Assert.Equal(1234.50m, amount);
    }

    [Fact]
    public void Format_N0_DoesNotChangeDecimalScaleOfSource()
    {
        var leftover = 200.00m;
        var formatted = CurrencyDisplay.Format(leftover, "USD", "N0");
        Assert.Equal("$200", formatted);
        Assert.Equal(200.00m, leftover);
    }
}
