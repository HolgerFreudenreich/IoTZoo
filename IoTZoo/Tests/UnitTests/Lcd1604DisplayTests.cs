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

public class Lcd1604DisplayTests
{
    [Theory]
    [InlineData("one\ntwo\nthree\nfour", "one", "two", "three", "four")]
    [InlineData("a\r\nb\rc\nd", "a", "b", "c", "d")]
    [InlineData("a\n\nc", "a", "", "c", "")]
    [InlineData("1234567890123456123456789012345612345678901234561234567890123456", "1234567890123456", "1234567890123456", "1234567890123456", "1234567890123456")]
    [InlineData("123456789012345e\u0301😀", "123456789012345e\u0301", "😀", "", "")]
    [InlineData("", "", "", "", "")]
    public async Task RendersFourRows(string value, string first, string second, string third, string fourth)
    {
        XElement svg = await Render(value);
        XNamespace ns = "http://www.w3.org/2000/svg";
        var characters = svg.Descendants(ns + "text").Where(element => (string?)element.Attribute("class") == "lcd1604-character").ToArray();
        string[] expected = [first, second, third, fourth];
        for (int row = 0; row < 4; row++)
        {
            Assert.Equal(expected[row], string.Concat(characters.Where(character => (string?)character.Attribute("y") == (106 + row * 61).ToString()).Select(character => character.Value)));
        }
        Assert.Equal(64, svg.Elements(ns + "rect").Count(rect => (string?)rect.Attribute("fill") == "#1c57bd"));
        Assert.Equal("0 0 600 372", (string?)svg.Attribute("viewBox"));
    }

    [Theory]
    [InlineData("12345678901234567\nx")]
    [InlineData("a\nb\nc\nd\ne")]
    [InlineData("x\ty")]
    [InlineData("x\0y")]
    public async Task RejectsInvalidValues(string value)
    {
        Assert.False(Lcd1604Display.IsValidValue(value));
        Assert.Equal("Value", (await Assert.ThrowsAsync<ArgumentException>(() => Render(value))).ParamName);
    }

    [Fact]
    public async Task RejectsMoreThan64CharactersAndInvalidDimensions()
    {
        Assert.False(Lcd1604Display.IsValidValue(new string('x', 65)));
        Assert.Equal("Value", (await Assert.ThrowsAsync<ArgumentException>(() => Render(new string('x', 65)))).ParamName);
        Assert.Equal("Width", (await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => Render("x", width: 0))).ParamName);
        Assert.Equal("Height", (await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => Render("x", height: 0))).ParamName);
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
            Lcd1604Display? display = null;
            var output = await renderer.RenderComponentAsync<DisplayHost>(ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                [nameof(DisplayHost.Capture)] = (Action<Lcd1604Display>)(component => display = component)
            }));
            Assert.NotNull(display);
            XNamespace ns = "http://www.w3.org/2000/svg";
            XElement initial = XElement.Parse(output.ToHtmlString());
            Assert.Equal("LCD1604", initial.Element(ns + "text")?.Value);
            Assert.Equal(new[] { "GND", "VCC", "SDA", "SCL" }, initial.Elements(ns + "g").Single(group => (string?)group.Attribute("class") == "lcd1604-connections").Descendants(ns + "text").Select(text => text.Value));
            await display.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                [nameof(Lcd1604Display.Value)] = "a\nb\nc\nd",
                [nameof(Lcd1604Display.Label)] = "Room & <sensor>",
                [nameof(Lcd1604Display.ShowConnections)] = false,
                [nameof(Lcd1604Display.Width)] = 400,
                [nameof(Lcd1604Display.Height)] = 150
            }));
            XElement updated = XElement.Parse(output.ToHtmlString());
            Assert.Equal("1.5 0 530 372", (string?)updated.Attribute("viewBox"));
            Assert.Equal("width: 400px; max-width: 100%; height: auto; aspect-ratio: 400 / 150;", (string?)updated.Attribute("style"));
            Assert.Equal("none", (string?)updated.Attribute("preserveAspectRatio"));
            Assert.Equal("Room & <sensor>", updated.Element(ns + "text")?.Value);
            Assert.Empty(updated.Element(ns + "text")!.Elements());
            Assert.DoesNotContain(updated.Elements(ns + "g"), group => (string?)group.Attribute("class") == "lcd1604-connections");
            await display.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?> { [nameof(Lcd1604Display.Value)] = "X", [nameof(Lcd1604Display.Label)] = "" }));
            XElement cleared = XElement.Parse(output.ToHtmlString());
            Assert.Equal("", cleared.Element(ns + "text")?.Value);
            Assert.Equal("X", string.Concat(cleared.Descendants(ns + "text").Where(element => (string?)element.Attribute("class") == "lcd1604-character").Select(element => element.Value)));
        });
    }

    private static async Task<XElement> Render(string value, int? width = null, int? height = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        await using var provider = services.BuildServiceProvider();
        await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
        string html = await renderer.Dispatcher.InvokeAsync(async () => (await renderer.RenderComponentAsync<Lcd1604Display>(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(Lcd1604Display.Value)] = value,
            [nameof(Lcd1604Display.Width)] = width,
            [nameof(Lcd1604Display.Height)] = height
        }))).ToHtmlString());
        return XElement.Parse(html);
    }

    public sealed class DisplayHost : ComponentBase
    {
        [Parameter]
        public Action<Lcd1604Display> Capture { get; set; } = null!;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<Lcd1604Display>(0);
            builder.AddComponentReferenceCapture(1, component => Capture((Lcd1604Display)component));
            builder.CloseComponent();
        }
    }
}
