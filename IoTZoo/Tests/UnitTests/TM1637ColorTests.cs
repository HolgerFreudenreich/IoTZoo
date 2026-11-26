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

public class TM1637ColorTests
{
    [Theory]
    [InlineData(false, "17:34", "#0000ff", "#000033")]
    [InlineData(true, "17:34:56", "#0000ff", "#000033")]
    [InlineData(false, "17:34", "#00ff00", "#003300")]
    [InlineData(true, "17:34:56", "#ffffff", "#333333")]
    public async Task ChangingColorUpdatesLitAndUnlitElements(bool sixDigits, string value, string color, string unlitColor)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        await using var provider = services.BuildServiceProvider();
        await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            TM1637DisplayBase? display = null;
            var output = await renderer.RenderComponentAsync<DisplayHost>(ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                [nameof(DisplayHost.SixDigits)] = sixDigits,
                [nameof(DisplayHost.Capture)] = (Action<TM1637DisplayBase>)(component => display = component)
            }));
            Assert.NotNull(display);
            await display.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?> { [nameof(TM1637DisplayBase.Value)] = value }));
            XElement original = XElement.Parse(output.ToHtmlString());
            XNamespace ns = "http://www.w3.org/2000/svg";
            var originalSegments = original.Descendants(ns + "polygon").Select(segment => (string?)segment.Attribute("fill")).ToArray();
            Assert.Contains("#ff7777", originalSegments);
            Assert.Contains("#331717", originalSegments);

            await display.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?> { [nameof(TM1637DisplayBase.SegmentColor)] = color }));
            XElement updated = XElement.Parse(output.ToHtmlString());
            var updatedSegments = updated.Descendants(ns + "polygon").Select(segment => (string?)segment.Attribute("fill")).ToArray();
            Assert.Equal(originalSegments.Select(segment => segment == "#ff7777" ? color : unlitColor), updatedSegments);
            var colons = updated.Elements(ns + "g").Where(group => group.Elements(ns + "circle").Count() == 2).ToArray();
            Assert.Equal(sixDigits ? 2 : 1, colons.Length);
            Assert.All(colons, colon => Assert.Equal(color, (string?)colon.Attribute("fill")));

            await display.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?> { [nameof(TM1637DisplayBase.Value)] = "1.2" }));
            XElement number = XElement.Parse(output.ToHtmlString());
            var points = number.Elements(ns + "g").Where(group => group.Attribute("transform") != null).Select(group => (string?)group.Element(ns + "circle")?.Attribute("fill")).ToArray();
            Assert.Equal(1, points.Count(point => point == color));
            Assert.Equal((sixDigits ? 6 : 4) - 1, points.Count(point => point == unlitColor));
            Assert.All(number.Elements(ns + "g").Where(group => group.Elements(ns + "circle").Count() == 2),
                colon => Assert.Equal(unlitColor, (string?)colon.Attribute("fill")));
        });
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("red")]
    [InlineData("#fff")]
    [InlineData("#gg0000")]
    [InlineData("#ffffff00")]
    [InlineData("#ffffff\n")]
    public async Task RejectsInvalidColors(string? color)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        await using var provider = services.BuildServiceProvider();
        await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => renderer.Dispatcher.InvokeAsync(async () =>
        {
            await renderer.RenderComponentAsync<TM1637_4Display>(ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                [nameof(TM1637DisplayBase.SegmentColor)] = color
            }));
        }));
        Assert.Equal("SegmentColor", exception.ParamName);
    }

    public sealed class DisplayHost : ComponentBase
    {
        [Parameter]
        public bool SixDigits { get; set; }

        [Parameter]
        public Action<TM1637DisplayBase> Capture { get; set; } = null!;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent(0, SixDigits ? typeof(TM1637_6Display) : typeof(TM1637_4Display));
            builder.AddComponentReferenceCapture(1, component => Capture((TM1637DisplayBase)component));
            builder.CloseComponent();
        }
    }
}
