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

public class DisplayDimensionsTests
{
    [Theory]
    [InlineData(0, 464, 231)]
    [InlineData(1, 616, 231)]
    [InlineData(2, 600, 250)]
    [InlineData(3, 600, 372)]
    [InlineData(4, 704, 372)]
    public async Task DisplaySupportsWidthAndHeightIndependently(int displayType, int naturalWidth, int naturalHeight)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        await using var provider = services.BuildServiceProvider();
        await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            ComponentBase? display = null;
            var output = await renderer.RenderComponentAsync<DisplayHost>(ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                [nameof(DisplayHost.DisplayType)] = displayType,
                [nameof(DisplayHost.Capture)] = (Action<ComponentBase>)(component => display = component)
            }));
            Assert.NotNull(display);
            async Task<XElement> SetSize(int? width, int? height)
            {
                await display.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?>
                {
                    ["Width"] = width,
                    ["Height"] = height
                }));
                return XElement.Parse(output.ToHtmlString());
            }

            XElement svg = await SetSize(400, null);
            Assert.Equal("width: 400px; max-width: 100%; height: auto;", (string?)svg.Attribute("style"));
            Assert.Equal("xMidYMid meet", (string?)svg.Attribute("preserveAspectRatio"));
            svg = await SetSize(null, naturalHeight);
            Assert.Equal($"width: {naturalWidth}px; max-width: 100%; height: auto;", (string?)svg.Attribute("style"));
            Assert.Equal("xMidYMid meet", (string?)svg.Attribute("preserveAspectRatio"));
            svg = await SetSize(400, 150);
            Assert.Equal("width: 400px; max-width: 100%; height: auto; aspect-ratio: 400 / 150;", (string?)svg.Attribute("style"));
            Assert.Equal("none", (string?)svg.Attribute("preserveAspectRatio"));
            svg = await SetSize(null, null);
            Assert.Equal("max-width: 100%; height: auto;", (string?)svg.Attribute("style"));
            Assert.Equal("xMidYMid meet", (string?)svg.Attribute("preserveAspectRatio"));
        });
    }

    [Theory]
    [InlineData(0, 412, 231, 3, 0)]
    [InlineData(1, 564, 231, 3, 0)]
    [InlineData(2, 530, 250, 1.5, 1.5)]
    [InlineData(3, 530, 372, 1.5, 1.5)]
    [InlineData(4, 634, 372, 1.5, 1.5)]
    public async Task HiddenConnectionsAlignBoardEdgesAndUpdateSizing(int displayType, int boardWidth, int naturalHeight, double viewLeft, double strokeHalf)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        await using var provider = services.BuildServiceProvider();
        await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            ComponentBase? display = null;
            var output = await renderer.RenderComponentAsync<DisplayHost>(ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                [nameof(DisplayHost.DisplayType)] = displayType,
                [nameof(DisplayHost.Capture)] = (Action<ComponentBase>)(component => display = component)
            }));
            Assert.NotNull(display);
            foreach (int? height in new int?[] { null, 150 })
            {
                await display.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?>
                {
                    ["Width"] = 400, ["Height"] = height, ["ShowConnections"] = false
                }));
                XElement svg = XElement.Parse(output.ToHtmlString());
                XNamespace ns = "http://www.w3.org/2000/svg";
                XElement board = svg.Element(ns + "rect")!;
                double left = (double)board.Attribute("x")! - strokeHalf;
                double right = (double)board.Attribute("x")! + (double)board.Attribute("width")! + strokeHalf;
                string[] viewBox = ((string)svg.Attribute("viewBox")!).Split(' ');
                Assert.Equal(viewLeft, double.Parse(viewBox[0], System.Globalization.CultureInfo.InvariantCulture));
                Assert.Equal(boardWidth, double.Parse(viewBox[2], System.Globalization.CultureInfo.InvariantCulture));
                Assert.Equal(viewLeft, left);
                Assert.Equal(viewLeft + boardWidth, right);
                Assert.StartsWith("width: 400px; max-width: 100%; height: auto;", (string?)svg.Attribute("style"));
            }
            await display.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                ["Width"] = null, ["Height"] = naturalHeight, ["ShowConnections"] = false
            }));
            Assert.Equal($"width: {boardWidth}px; max-width: 100%; height: auto;", (string?)XElement.Parse(output.ToHtmlString()).Attribute("style"));
            await display.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?> { ["ShowConnections"] = true }));
            Assert.StartsWith("0 0 ", (string?)XElement.Parse(output.ToHtmlString()).Attribute("viewBox"));
        });
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(0, -1)]
    [InlineData(1, 0)]
    [InlineData(1, -1)]
    [InlineData(2, 0)]
    [InlineData(2, -1)]
    [InlineData(3, 0)]
    [InlineData(3, -1)]
    [InlineData(4, 0)]
    [InlineData(4, -1)]
    public async Task RejectsInvalidWidth(int displayType, int width)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        await using var provider = services.BuildServiceProvider();
        await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
        var error = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => renderer.Dispatcher.InvokeAsync(async () =>
        {
            await renderer.RenderComponentAsync<DisplayHost>(ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                [nameof(DisplayHost.DisplayType)] = displayType,
                [nameof(DisplayHost.Width)] = width,
                [nameof(DisplayHost.Capture)] = (Action<ComponentBase>)(_ => { })
            }));
        }));
        Assert.Equal("Width", error.ParamName);
    }

    public sealed class DisplayHost : ComponentBase
    {
        [Parameter]
        public int DisplayType { get; set; }

        [Parameter]
        public int? Width { get; set; }

        [Parameter]
        public Action<ComponentBase> Capture { get; set; } = null!;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            Type type = DisplayType switch { 0 => typeof(TM1637_4Display), 1 => typeof(TM1637_6Display), 2 => typeof(Lcd1602Display), 3 => typeof(Lcd1604Display), 4 => typeof(Lcd2004Display), _ => throw new ArgumentOutOfRangeException(nameof(DisplayType)) };
            builder.OpenComponent(0, type);
            builder.AddAttribute(1, "Width", Width);
            builder.AddComponentReferenceCapture(2, component => Capture((ComponentBase)component));
            builder.CloseComponent();
        }
    }
}
