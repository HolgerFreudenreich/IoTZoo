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

public class TM1637ConnectionTests
{
    [Theory]
    [InlineData(false, 4)]
    [InlineData(true, 6)]
    public async Task ConnectionsCanBeToggledWithoutChangingDigits(bool sixDigits, int digitCount)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        await using var provider = services.BuildServiceProvider();
        await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            TM1637DisplayBase? display = null;
            var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                [nameof(DisplayHost.SixDigits)] = sixDigits,
                [nameof(DisplayHost.Capture)] = (Action<TM1637DisplayBase>)(component => display = component)
            });
            var output = await renderer.RenderComponentAsync<DisplayHost>(parameters);
            Assert.NotNull(display);
            XNamespace ns = "http://www.w3.org/2000/svg";
            XElement initial = XElement.Parse(output.ToHtmlString());
            var connector = Assert.Single(initial.Elements(ns + "g").Where(group => (string?)group.Attribute("class") == "tm1637-connections"));
            Assert.Equal(9, connector.Elements(ns + "rect").Count());
            var initialDigits = initial.Elements(ns + "g").Where(group => group.Attribute("transform") != null).Select(group => group.ToString()).ToArray();
            Assert.Equal(digitCount, initialDigits.Length);

            await display.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?> { [nameof(TM1637DisplayBase.ShowConnections)] = false }));
            XElement hidden = XElement.Parse(output.ToHtmlString());
            Assert.DoesNotContain(hidden.Elements(ns + "g"), group => (string?)group.Attribute("class") == "tm1637-connections");
            Assert.Equal(initialDigits, hidden.Elements(ns + "g").Where(group => group.Attribute("transform") != null).Select(group => group.ToString()).ToArray());
            Assert.Equal(sixDigits ? "3 0 564 231" : "3 0 412 231", (string?)hidden.Attribute("viewBox"));
            Assert.Equal(sixDigits ? "564" : "412", (string?)hidden.Attribute("width"));
            Assert.Equal((string?)initial.Attribute("style"), (string?)hidden.Attribute("style"));

            await display.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?> { [nameof(TM1637DisplayBase.ShowConnections)] = true }));
            XElement visible = XElement.Parse(output.ToHtmlString());
            Assert.Equal(connector.ToString(), Assert.Single(visible.Elements(ns + "g").Where(group => (string?)group.Attribute("class") == "tm1637-connections")).ToString());
        });
    }

    [Theory]
    [InlineData(false, "4-Digit Display")]
    [InlineData(true, "6-Digit Display")]
    public async Task LabelCanBeChangedOrCleared(bool sixDigits, string defaultLabel)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        await using var provider = services.BuildServiceProvider();
        await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            TM1637DisplayBase? display = null;
            var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                [nameof(DisplayHost.SixDigits)] = sixDigits,
                [nameof(DisplayHost.Capture)] = (Action<TM1637DisplayBase>)(component => display = component)
            });
            var output = await renderer.RenderComponentAsync<DisplayHost>(parameters);
            Assert.NotNull(display);
            XNamespace ns = "http://www.w3.org/2000/svg";
            Assert.Equal(defaultLabel, XElement.Parse(output.ToHtmlString()).Element(ns + "text")?.Value);
            foreach (string? label in new[] { "Temperature & <humidity>", "", null })
            {
                await display.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?> { [nameof(TM1637DisplayBase.Label)] = label }));
                XElement svg = XElement.Parse(output.ToHtmlString());
                Assert.Equal(label ?? defaultLabel, svg.Element(ns + "text")?.Value);
                Assert.Empty(svg.Element(ns + "text")!.Elements());
            }
        });
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
