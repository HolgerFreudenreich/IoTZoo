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

public class TM1637_6DisplayTests
{
    [Theory]
    [InlineData("123456", false, "123456", -1, false, false)]
    [InlineData("-99999", false, "-99999", -1, false, false)]
    [InlineData("1.2345", false, " 12345", 1, false, false)]
    [InlineData("12,3456", false, "123456", 1, false, false)]
    [InlineData("-1.20", true, "-00120", 3, false, false)]
    [InlineData("17:34", false, "  1734", -1, false, true)]
    [InlineData("17:34:56", false, "173456", -1, true, true)]
    [InlineData("00:00:00", false, "000000", -1, true, true)]
    [InlineData("23:59:59", true, "235959", -1, true, true)]
    public async Task RendersDigitsAndSeparators(string value, bool leadingZeros, string expected, int decimalPosition, bool leftColon, bool rightColon)
    {
        XElement svg = await Render(value, leadingZeros);
        XNamespace ns = "http://www.w3.org/2000/svg";
        var groups = svg.Elements(ns + "g").ToArray();
        var digits = groups.Where(group => group.Attribute("transform") != null).ToArray();
        string[] masks = ["1111110", "0110000", "1101101", "1111001", "0110011", "1011011", "1011111", "1110000", "1111111", "1111011"];
        Assert.Equal(6, digits.Length);
        for (int digit = 0; digit < 6; digit++)
        {
            string expectedMask = expected[digit] switch
            {
                ' ' => "0000000",
                '-' => "0000001",
                _ => masks[expected[digit] - '0']
            };
            var segments = digits[digit].Elements(ns + "polygon").ToArray();
            Assert.Equal(7, segments.Length);
            Assert.Equal(expectedMask, string.Concat(segments.Select(segment => (string?)segment.Attribute("fill") == "#ff7777" ? '1' : '0')));
            Assert.Equal(digit == decimalPosition ? "#ff7777" : "#331717", (string?)digits[digit].Element(ns + "circle")?.Attribute("fill"));
        }
        var colons = groups.Where(group => group.Elements(ns + "circle").Count() == 2).ToArray();
        Assert.Equal(2, colons.Length);
        Assert.Equal(leftColon ? "#ff7777" : "#331717", (string?)colons[0].Attribute("fill"));
        Assert.Equal(rightColon ? "#ff7777" : "#331717", (string?)colons[1].Attribute("fill"));
        Assert.Equal($"TM1637 display: {value}", (string?)svg.Attribute("aria-label"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(".123")]
    [InlineData("24:00:00")]
    [InlineData("17:34:60")]
    [InlineData("1:23:45")]
    [InlineData("1.2.3")]
    public async Task RejectsInvalidValues(string value)
    {
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => Render(value));
        Assert.Equal("Value", exception.ParamName);
        Assert.False(TM1637_6Display.IsValidValue(value));
    }

    [Theory]
    [InlineData("1234567")]
    [InlineData("-123456")]
    [InlineData("12.34567")]
    public async Task RejectsValuesThatDoNotFit(string value)
    {
        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => Render(value));
        Assert.Equal("Value", exception.ParamName);
        Assert.False(TM1637_6Display.IsValidValue(value));
    }

    [Fact]
    public async Task HeightScalesProportionally()
    {
        XElement svg = await Render("123456", height: 231);
        Assert.Equal("0 0 616 231", (string?)svg.Attribute("viewBox"));
        Assert.Equal("width: 616px; max-width: 100%; height: auto;", (string?)svg.Attribute("style"));
        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => Render("123456", height: 0));
        Assert.Equal("Height", exception.ParamName);
    }

    [Fact]
    public async Task ChangingValuesClearsPreviousSeparators()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        await using var provider = services.BuildServiceProvider();
        await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            TM1637_6Display? display = null;
            var output = await renderer.RenderComponentAsync<DisplayHost>(ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                [nameof(DisplayHost.Capture)] = (Action<TM1637_6Display>)(component => display = component)
            }));
            Assert.NotNull(display);
            XNamespace ns = "http://www.w3.org/2000/svg";
            foreach (var (value, colors) in new[] { ("17:34:56", new[] { "#ff7777", "#ff7777" }), ("17:34", new[] { "#331717", "#ff7777" }), ("12.3456", new[] { "#331717", "#331717" }) })
            {
                await display.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?> { [nameof(TM1637_6Display.Value)] = value }));
                XElement svg = XElement.Parse(output.ToHtmlString());
                Assert.Equal(colors, svg.Elements(ns + "g").Where(group => group.Elements(ns + "circle").Count() == 2).Select(group => (string?)group.Attribute("fill")));
            }
        });
    }

    public sealed class DisplayHost : ComponentBase
    {
        [Parameter]
        public Action<TM1637_6Display> Capture { get; set; } = null!;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<TM1637_6Display>(0);
            builder.AddComponentReferenceCapture(1, component => Capture((TM1637_6Display)component));
            builder.CloseComponent();
        }
    }

    private static async Task<XElement> Render(string value, bool leadingZeros = false, int? height = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        await using var provider = services.BuildServiceProvider();
        await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
        string html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                [nameof(TM1637_6Display.Value)] = value,
                [nameof(TM1637_6Display.LeadingZeros)] = leadingZeros,
                [nameof(TM1637_6Display.Height)] = height
            });
            return (await renderer.RenderComponentAsync<TM1637_6Display>(parameters)).ToHtmlString();
        });
        return XElement.Parse(html);
    }
}
