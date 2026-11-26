// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/   P L A Y G R O U N D
// --------------------------------------------------------------------------------------------------------------------
// Connect «Things» with microcontrollers in a simple way.
// --------------------------------------------------------------------------------------------------------------------
// (c) 2025 - 2026 Holger Freudenreich under the MIT license
// --------------------------------------------------------------------------------------------------------------------

using System.Xml.Linq;
using IotZoo.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace UnitTests;

public class Lcd1602DisplayTests
{
    [Theory]
    [InlineData("Hello", "Hello", "")]
    [InlineData("12345678901234567890", "1234567890123456", "7890")]
    [InlineData("first\nsecond", "first", "second")]
    [InlineData("first\r\nsecond", "first", "second")]
    [InlineData("first\rsecond", "first", "second")]
    [InlineData("  A & B  ", "  A & B  ", "")]
    [InlineData("ä°C", "ä°C", "")]
    [InlineData("123456789012345e\u0301😀", "123456789012345e\u0301", "😀")]
    [InlineData("", "", "")]
    public async Task RendersTwoRowsWithoutLosingCharacters(string value, string first, string second)
    {
        XElement svg = await Render(value);
        XNamespace ns = "http://www.w3.org/2000/svg";
        var characters = svg.Descendants(ns + "text").Where(element => (string?)element.Attribute("class") == "lcd1602-character").ToArray();
        Assert.Equal(first + second, string.Concat(characters.Select(character => character.Value)));
        Assert.All(characters.Take(StringInfoLength(first)), character => Assert.Equal("106", (string?)character.Attribute("y")));
        Assert.All(characters.Skip(StringInfoLength(first)), character => Assert.Equal("167", (string?)character.Attribute("y")));
        Assert.Equal("0 0 600 250", (string?)svg.Attribute("viewBox"));
    }

    [Theory]
    [InlineData("123456789012345678901234567890123")]
    [InlineData("12345678901234567\nx")]
    [InlineData("x\n12345678901234567")]
    [InlineData("x\ny\nz")]
    [InlineData("hello\tworld")]
    [InlineData("hello\0world")]
    public async Task RejectsInvalidValues(string value)
    {
        Assert.False(Lcd1602Display.IsValidValue(value));
        var error = await Assert.ThrowsAsync<ArgumentException>(() => Render(value));
        Assert.Equal("Value", error.ParamName);
    }

    [Fact]
    public async Task RejectsUnpairedSurrogate()
    {
        string value = new string((char)0xd800, 1);
        Assert.False(Lcd1602Display.IsValidValue(value));
        var error = await Assert.ThrowsAsync<ArgumentException>(() => Render(value));
        Assert.Equal("Value", error.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ConnectionVisibilityAndHeightAreConfigurable(bool showConnections)
    {
        XElement svg = await Render("Test", showConnections, 250);
        XNamespace ns = "http://www.w3.org/2000/svg";
        var connection = svg.Elements(ns + "g").SingleOrDefault(group => (string?)group.Attribute("class") == "lcd1602-connections");
        Assert.Equal(showConnections, connection is not null);
        if (connection is not null)
        {
            Assert.Equal(new[] { "GND", "VCC", "SDA", "SCL" }, connection.Descendants(ns + "text").Select(label => label.Value));
        }
        Assert.Equal(showConnections ? "0 0 600 250" : "1.5 0 530 250", (string?)svg.Attribute("viewBox"));
        Assert.Equal(showConnections ? "width: 600px; max-width: 100%; height: auto;" : "width: 530px; max-width: 100%; height: auto;", (string?)svg.Attribute("style"));
        var error = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => Render("Test", height: 0));
        Assert.Equal("Height", error.ParamName);
    }

    [Fact]
    public async Task LabelCanBeChangedClearedAndReset()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        await using var provider = services.BuildServiceProvider();
        await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            Lcd1602Display? display = null;
            var output = await renderer.RenderComponentAsync<DisplayHost>(ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                [nameof(DisplayHost.Capture)] = (Action<Lcd1602Display>)(component => display = component)
            }));
            Assert.NotNull(display);
            XNamespace ns = "http://www.w3.org/2000/svg";
            Assert.Equal("LCD1602", XElement.Parse(output.ToHtmlString()).Element(ns + "text")?.Value);
            foreach (string? label in new[] { "Temperature & <humidity>", "", null })
            {
                await display.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?> { [nameof(Lcd1602Display.Label)] = label }));
                XElement svg = XElement.Parse(output.ToHtmlString());
                Assert.Equal(label ?? "LCD1602", svg.Element(ns + "text")?.Value);
                Assert.Empty(svg.Element(ns + "text")!.Elements());
            }
        });
    }

    [Fact]
    public async Task ChangingValueRefreshesBothRows()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        await using var provider = services.BuildServiceProvider();
        await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            Lcd1602Display? display = null;
            var output = await renderer.RenderComponentAsync<DisplayHost>(ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                [nameof(DisplayHost.Capture)] = (Action<Lcd1602Display>)(component => display = component)
            }));
            Assert.NotNull(display);
            foreach (string value in new[] { "12345678901234567890", "New" })
            {
                await display.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?> { [nameof(Lcd1602Display.Value)] = value }));
                XElement svg = XElement.Parse(output.ToHtmlString());
                XNamespace ns = "http://www.w3.org/2000/svg";
                Assert.Equal(value, string.Concat(svg.Descendants(ns + "text").Where(element => (string?)element.Attribute("class") == "lcd1602-character").Select(element => element.Value)));
            }
        });
    }

    private static int StringInfoLength(string value) => System.Globalization.StringInfo.ParseCombiningCharacters(value).Length;

    private static async Task<XElement> Render(string value, bool showConnections = true, int? height = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        await using var provider = services.BuildServiceProvider();
        await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
        string html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                [nameof(Lcd1602Display.Value)] = value,
                [nameof(Lcd1602Display.ShowConnections)] = showConnections,
                [nameof(Lcd1602Display.Height)] = height
            });
            return (await renderer.RenderComponentAsync<Lcd1602Display>(parameters)).ToHtmlString();
        });
        return XElement.Parse(html);
    }

    public sealed class DisplayHost : ComponentBase
    {
        [Parameter]
        public Action<Lcd1602Display> Capture { get; set; } = null!;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<Lcd1602Display>(0);
            builder.AddComponentReferenceCapture(1, component => Capture((Lcd1602Display)component));
            builder.CloseComponent();
        }
    }
}
