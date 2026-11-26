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

public class TM1637DisplayTests
{
    [Theory]
    [InlineData("0", false, "   0")]
    [InlineData("123", false, " 123")]
    [InlineData("123", true, "0123")]
    [InlineData("4567", false, "4567")]
    [InlineData("8901", false, "8901")]
    [InlineData("9999", false, "9999")]
    [InlineData("-1", false, "  -1")]
    [InlineData("-1", true, "-001")]
    [InlineData("-999", false, "-999")]
    public async Task RendersExpectedSegments(string value, bool leadingZeros, string expected)
    {
        XElement svg = await Render(value, leadingZeros);
        AssertSegments(svg, expected);
        Assert.Equal($"TM1637 display: {value}", (string?)svg.Attribute("aria-label"));
    }

    private static void AssertSegments(XElement svg, string expected)
    {
        XNamespace ns = "http://www.w3.org/2000/svg";
        var digits = svg.Elements(ns + "g").Where(group => group.Attribute("transform") != null).ToArray();
        string[] masks = ["1111110", "0110000", "1101101", "1111001", "0110011", "1011011", "1011111", "1110000", "1111111", "1111011"];

        Assert.Equal(4, digits.Length);
        for (int digit = 0; digit < 4; digit++)
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
        }
    }

    [Fact]
    public async Task SegmentsHaveVisibleGaps()
    {
        XElement svg = await Render("8888");
        XNamespace ns = "http://www.w3.org/2000/svg";
        var digits = svg.Elements(ns + "g").Where(group => group.Attribute("transform") != null).ToArray();
        Assert.Equal(4, digits.Length);

        foreach (var digit in digits)
        {
            var bounds = digit.Elements(ns + "polygon").Select(segment =>
            {
                var points = ((string)segment.Attribute("points")!).Split(' ')
                    .Select(point => point.Split(',').Select(value => decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture)).ToArray()).ToArray();
                return (MinX: points.Min(point => point[0]), MaxX: points.Max(point => point[0]),
                        MinY: points.Min(point => point[1]), MaxY: points.Max(point => point[1]));
            }).ToArray();
            Assert.Equal(7, bounds.Length);
            Assert.Equal(0.5m, bounds[1].MinY - bounds[0].MaxY);
            Assert.Equal(0.5m, bounds[5].MinY - bounds[0].MaxY);
            Assert.Equal(0.5m, bounds[3].MinY - bounds[2].MaxY);
            Assert.Equal(0.5m, bounds[3].MinY - bounds[4].MaxY);
            Assert.Equal(0.5m, bounds[6].MinY - bounds[1].MaxY);
            Assert.Equal(0.5m, bounds[2].MinY - bounds[6].MaxY);

            for (int i = 0; i < bounds.Length; i++)
            {
                for (int j = i + 1; j < bounds.Length; j++)
                {
                    Assert.True(bounds[i].MaxX + 0.5m <= bounds[j].MinX || bounds[j].MaxX + 0.5m <= bounds[i].MinX ||
                                bounds[i].MaxY + 0.5m <= bounds[j].MinY || bounds[j].MaxY + 0.5m <= bounds[i].MinY,
                                $"Segments {i} and {j} overlap or touch.");
                }
            }
        }
    }

    [Theory]
    [InlineData("17:34", false, "1734", -1, true)]
    [InlineData("00:00", false, "0000", -1, true)]
    [InlineData("23:59", true, "2359", -1, true)]
    [InlineData("1.234", false, "1234", 0, false)]
    [InlineData("12,34", false, "1234", 1, false)]
    [InlineData("123.4", false, "1234", 2, false)]
    [InlineData("1.2", false, "  12", 2, false)]
    [InlineData("1.2", true, "0012", 2, false)]
    [InlineData("-1.2", false, " -12", 2, false)]
    [InlineData("-1.2", true, "-012", 2, false)]
    [InlineData("1.20", false, " 120", 1, false)]
    [InlineData("123", false, " 123", -1, false)]
    [InlineData("0,000", false, "0000", 0, false)]
    public async Task ValueRendersTimeAndDecimalPositions(string value, bool leadingZeros, string expected, int decimalPosition, bool colonOn)
    {
        XElement svg = await Render(value, leadingZeros);
        AssertSegments(svg, expected);
        Assert.Equal($"TM1637 display: {value}", (string?)svg.Attribute("aria-label"));
        XNamespace ns = "http://www.w3.org/2000/svg";
        var groups = svg.Elements(ns + "g").ToArray();
        var digits = groups.Where(group => group.Attribute("transform") != null).ToArray();
        Assert.Equal(4, digits.Length);
        for (int digit = 0; digit < digits.Length; digit++)
        {
            Assert.Equal(digit == decimalPosition ? "#ff7777" : "#331717", (string?)digits[digit].Element(ns + "circle")?.Attribute("fill"));
        }
        var colon = groups.Single(group => group.Elements(ns + "circle").Count() == 2);
        Assert.Equal(colonOn ? "#ff7777" : "#331717", (string?)colon.Attribute("fill"));
    }

    [Fact]
    public async Task DefaultSizeRemainsUnchanged()
    {
        XElement svg = await Render();
        Assert.Equal("464", (string?)svg.Attribute("width"));
        Assert.Equal("231", (string?)svg.Attribute("height"));
        Assert.Equal("max-width: 100%; height: auto;", (string?)svg.Attribute("style"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task RejectsInvalidHeight(int height)
    {
        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => Render(height: height));
        Assert.Equal("Height", exception.ParamName);
    }

    [Fact]
    public async Task IntegerValueDoesNotLightSeparators()
    {
        XElement svg = await Render("42");
        XNamespace ns = "http://www.w3.org/2000/svg";
        var groups = svg.Elements(ns + "g").ToArray();
        Assert.Equal("#331717", (string?)groups.Single(group => group.Elements(ns + "circle").Count() == 2).Attribute("fill"));
        Assert.All(groups.Where(group => group.Attribute("transform") != null),
            digit => Assert.Equal("#331717", (string?)digit.Element(ns + "circle")?.Attribute("fill")));
    }

    [Fact]
    public async Task UpdatingValueClearsPreviousSeparators()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        await using var provider = services.BuildServiceProvider();
        await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            TM1637_4Display? display = null;
            var hostParameters = ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                [nameof(DisplayHost.Capture)] = (Action<TM1637_4Display>)(component => display = component)
            });
            var output = await renderer.RenderComponentAsync<DisplayHost>(hostParameters);
            Assert.NotNull(display);
            XNamespace ns = "http://www.w3.org/2000/svg";
            AssertSegments(XElement.Parse(output.ToHtmlString()), "   0");

            await display.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?> { [nameof(TM1637_4Display.Value)] = "17:34" }));
            XElement svg = XElement.Parse(output.ToHtmlString());
            Assert.Equal("#ff7777", (string?)svg.Elements(ns + "g").Single(group => group.Elements(ns + "circle").Count() == 2).Attribute("fill"));

            await display.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?> { [nameof(TM1637_4Display.Value)] = "1.234" }));
            svg = XElement.Parse(output.ToHtmlString());
            Assert.Equal("#331717", (string?)svg.Elements(ns + "g").Single(group => group.Elements(ns + "circle").Count() == 2).Attribute("fill"));
            Assert.Equal("#ff7777", (string?)svg.Elements(ns + "g").First(group => group.Attribute("transform") != null).Element(ns + "circle")?.Attribute("fill"));

            await display.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?> { [nameof(TM1637_4Display.Value)] = "42" }));
            svg = XElement.Parse(output.ToHtmlString());
            AssertSegments(svg, "  42");
            Assert.All(svg.Elements(ns + "g").Where(group => group.Attribute("transform") != null),
                digit => Assert.Equal("#331717", (string?)digit.Element(ns + "circle")?.Attribute("fill")));
        });
    }

    public sealed class DisplayHost : ComponentBase
    {
        [Parameter]
        public Action<TM1637_4Display> Capture { get; set; } = null!;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<TM1637_4Display>(0);
            builder.AddComponentReferenceCapture(1, component => Capture((TM1637_4Display)component));
            builder.CloseComponent();
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData(".123")]
    [InlineData("1.")]
    [InlineData("1.2.3")]
    [InlineData("1:23")]
    [InlineData("24:00")]
    [InlineData("17:60")]
    [InlineData("1:2.3")]
    [InlineData("abc")]
    [InlineData("12\n")]
    [InlineData("NaN")]
    [InlineData("1e2")]
    public async Task RejectsInvalidValue(string value)
    {
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => Render(value));
        Assert.Equal("Value", exception.ParamName);
    }

    [Fact]
    public async Task RejectsNullValue()
    {
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => Render(null));
        Assert.Equal("Value", exception.ParamName);
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("12.345")]
    [InlineData("-1234")]
    [InlineData("0.00001")]
    [InlineData("-1000")]
    [InlineData("10000")]
    public async Task RejectsValuesThatDoNotFit(string value)
    {
        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => Render(value));
        Assert.Equal("Value", exception.ParamName);
    }

    private static async Task<XElement> Render(string? value = "0", bool leadingZeros = false, int? height = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        await using var provider = services.BuildServiceProvider();
        await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
        string html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                [nameof(TM1637_4Display.Value)] = value,
                [nameof(TM1637_4Display.LeadingZeros)] = leadingZeros,
                [nameof(TM1637_4Display.Height)] = height
            });
            var output = await renderer.RenderComponentAsync<TM1637_4Display>(parameters);
            return output.ToHtmlString();
        });
        return XElement.Parse(html);
    }
}
