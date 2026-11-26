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

public class Lcd2004DisplayTests
{
    [Theory]
    [InlineData("one\ntwo\nthree\nfour", "one", "two", "three", "four")]
    [InlineData("a\r\nb\rc\nd", "a", "b", "c", "d")]
    [InlineData("a\n\nc", "a", "", "c", "")]
    [InlineData("12345678901234567890123456789012345678901234567890123456789012345678901234567890", "12345678901234567890", "12345678901234567890", "12345678901234567890", "12345678901234567890")]
    [InlineData("1234567890123456789e\u0301😀", "1234567890123456789e\u0301", "😀", "", "")]
    [InlineData("", "", "", "", "")]
    public async Task RendersFourRowsOfTwentyCharacters(string value, string first, string second, string third, string fourth)
    {
        XElement svg = await Render(value);
        XNamespace ns = "http://www.w3.org/2000/svg";
        var characters = svg.Descendants(ns + "text").Where(element => (string?)element.Attribute("class") == "lcd2004-character").ToArray();
        string[] expected = [first, second, third, fourth];
        for (int row = 0; row < 4; row++)
        {
            Assert.Equal(expected[row], string.Concat(characters.Where(character => (string?)character.Attribute("y") == (106 + row * 61).ToString()).Select(character => character.Value)));
        }
        Assert.Equal(80, svg.Elements(ns + "rect").Count(rect => (string?)rect.Attribute("fill") == "#1c57bd"));
        Assert.Equal("0 0 704 372", (string?)svg.Attribute("viewBox"));
    }

    [Theory]
    [InlineData("123456789012345678901\nx")]
    [InlineData("a\nb\nc\nd\ne")]
    [InlineData("x\ty")]
    [InlineData("x\0y")]
    public async Task RejectsInvalidValues(string value)
    {
        Assert.False(Lcd2004Display.IsValidValue(value));
        Assert.Equal("Value", (await Assert.ThrowsAsync<ArgumentException>(() => Render(value))).ParamName);
    }

    [Fact]
    public async Task RejectsOversizedAndInvalidDimensions()
    {
        Assert.False(Lcd2004Display.IsValidValue(new string('x', 81)));
        Assert.Equal("Value", (await Assert.ThrowsAsync<ArgumentException>(() => Render(new string('x', 81)))).ParamName);
        Assert.Equal("Width", (await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => Render("x", width: 0))).ParamName);
        Assert.Equal("Height", (await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => Render("x", height: 0))).ParamName);
    }

    [Fact]
    public async Task ASelectedDimensionPreservesProportions()
    {
        XElement widthOnly = await Render("x", width: 400);
        Assert.Equal("width: 400px; max-width: 100%; height: auto;", (string?)widthOnly.Attribute("style"));
        Assert.Equal("xMidYMid meet", (string?)widthOnly.Attribute("preserveAspectRatio"));
        XElement heightOnly = await Render("x", height: 372);
        Assert.Equal("width: 704px; max-width: 100%; height: auto;", (string?)heightOnly.Attribute("style"));
        Assert.Equal("xMidYMid meet", (string?)heightOnly.Attribute("preserveAspectRatio"));
    }

    [Fact]
    public async Task UpdatesTextLabelConnectionsAndSize()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        await using var provider = services.BuildServiceProvider();
        await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            Lcd2004Display? display = null;
            var output = await renderer.RenderComponentAsync<DisplayHost>(ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                [nameof(DisplayHost.Capture)] = (Action<Lcd2004Display>)(component => display = component)
            }));
            Assert.NotNull(display);
            XNamespace ns = "http://www.w3.org/2000/svg";
            XElement initial = XElement.Parse(output.ToHtmlString());
            Assert.Equal("LCD2004", initial.Element(ns + "text")?.Value);
            Assert.Equal(new[] { "GND", "VCC", "SDA", "SCL" }, initial.Elements(ns + "g").Single(group => (string?)group.Attribute("class") == "lcd2004-connections").Descendants(ns + "text").Select(text => text.Value));
            await display.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                [nameof(Lcd2004Display.Value)] = "a\nb\nc\nd",
                [nameof(Lcd2004Display.Label)] = "Room & <sensor>",
                [nameof(Lcd2004Display.ShowConnections)] = false,
                [nameof(Lcd2004Display.Width)] = 400,
                [nameof(Lcd2004Display.Height)] = 150
            }));
            XElement updated = XElement.Parse(output.ToHtmlString());
            Assert.Equal("1.5 0 634 372", (string?)updated.Attribute("viewBox"));
            Assert.Equal("width: 400px; max-width: 100%; height: auto; aspect-ratio: 400 / 150;", (string?)updated.Attribute("style"));
            Assert.Equal("none", (string?)updated.Attribute("preserveAspectRatio"));
            Assert.Equal("Room & <sensor>", updated.Element(ns + "text")?.Value);
            Assert.Empty(updated.Element(ns + "text")!.Elements());
            Assert.DoesNotContain(updated.Elements(ns + "g"), group => (string?)group.Attribute("class") == "lcd2004-connections");
            await display.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?> { [nameof(Lcd2004Display.Value)] = "X", [nameof(Lcd2004Display.Label)] = "" }));
            XElement cleared = XElement.Parse(output.ToHtmlString());
            Assert.Equal("", cleared.Element(ns + "text")?.Value);
            Assert.Equal("X", string.Concat(cleared.Descendants(ns + "text").Where(element => (string?)element.Attribute("class") == "lcd2004-character").Select(element => element.Value)));
        });
    }

    private static async Task<XElement> Render(string value, int? width = null, int? height = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        await using var provider = services.BuildServiceProvider();
        await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
        string html = await renderer.Dispatcher.InvokeAsync(async () => (await renderer.RenderComponentAsync<Lcd2004Display>(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(Lcd2004Display.Value)] = value,
            [nameof(Lcd2004Display.Width)] = width,
            [nameof(Lcd2004Display.Height)] = height
        }))).ToHtmlString());
        return XElement.Parse(html);
    }

    public sealed class DisplayHost : ComponentBase
    {
        [Parameter]
        public Action<Lcd2004Display> Capture { get; set; } = null!;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<Lcd2004Display>(0);
            builder.AddComponentReferenceCapture(1, component => Capture((Lcd2004Display)component));
            builder.CloseComponent();
        }
    }
}
