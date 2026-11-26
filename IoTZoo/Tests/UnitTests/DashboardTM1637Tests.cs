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

using System.Text.Json;
using System.Xml.Linq;
using Domain.Interfaces;
using Domain.Interfaces.Crud;
using Domain.Pocos;
using IotZoo.Pages;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Moq;
using MudBlazor;
using MudBlazor.Services;

namespace UnitTests;

public class DashboardTM1637Tests
{
    [Theory]
    [InlineData("123", "TM1637 display: 123")]
    [InlineData("1.234", "TM1637 display: 1.234")]
    [InlineData("17:34", "TM1637 display: 17:34")]
    public async Task DashboardRendersPayloadUsingTM1637(string message, string label)
    {
        string html = await RenderDashboard(message, true, TopicDisplayType.Tm1637_4);
        Assert.Contains("<svg", html);
        Assert.Contains(label, html);
        Assert.DoesNotContain("Cannot display message", html);
    }

    [Theory]
    [InlineData(231, "width: 464px; max-width: 100%; height: auto;")]
    [InlineData(462, "width: 928px; max-width: 100%; height: auto;")]
    public async Task DashboardUsesSavedHeightWithProportionalResponsiveWidth(int height, string expectedStyle)
    {
        var item = new DashboardItem { DisplayType = TopicDisplayType.Tm1637_4 };
        item.GaugeSettings = new GaugeSettings { Height = height };
        var savedItem = JsonSerializer.Deserialize<DashboardItem>(JsonSerializer.Serialize(item))!;
        string html = await RenderDashboard("123", true, savedItem.DisplayType, savedItem.GaugeSettings);
        Assert.Contains($"style=\"{expectedStyle}\"", html);
        Assert.Contains("viewBox=\"0 0 464 231\"", html);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task DashboardReportsInvalidHeight(int height)
    {
        string html = await RenderDashboard("123", true, TopicDisplayType.Tm1637_4, new GaugeSettings { Height = height });
        Assert.Contains("height must be greater than zero", html);
        Assert.DoesNotContain("TM1637 display:", html);
    }

    [Theory]
    [InlineData(TopicDisplayType.Tm1637_4, false, 4, 1)]
    [InlineData(TopicDisplayType.Tm1637_4, true, 4, 4)]
    [InlineData(TopicDisplayType.Tm1637_6, false, 6, 1)]
    [InlineData(TopicDisplayType.Tm1637_6, true, 6, 6)]
    public async Task DashboardAppliesSavedLeadingZeros(TopicDisplayType displayType, bool leadingZeros, int expectedDigits, int litDigits)
    {
        var item = new DashboardItem { DisplayType = displayType };
        item.GaugeSettings = new GaugeSettings { LeadingZeros = leadingZeros, Height = 120 };
        var restoredItem = new DashboardItem { DisplayType = item.DisplayType, SettingsJson = item.SettingsJson };
        Assert.Equal(leadingZeros, restoredItem.GaugeSettings.LeadingZeros);
        Assert.Equal(120, restoredItem.GaugeSettings.Height);
        string html = await RenderDashboard("1", true, restoredItem.DisplayType, restoredItem.GaugeSettings);
        int start = html.IndexOf("<svg", StringComparison.Ordinal);
        int end = html.IndexOf("</svg>", start, StringComparison.Ordinal);
        Assert.True(start >= 0 && end > start);
        XElement svg = XElement.Parse(html[start..(end + "</svg>".Length)]);
        XNamespace ns = "http://www.w3.org/2000/svg";
        var digits = svg.Elements(ns + "g").Where(group => group.Attribute("transform") != null).ToArray();
        Assert.Equal(expectedDigits, digits.Length);
        Assert.Equal(litDigits, digits.Count(digit => digit.Elements(ns + "polygon").Any(segment => (string?)segment.Attribute("fill") == "#ff7777")));
    }

    [Theory]
    [InlineData(TopicDisplayType.Tm1637_4, true, 4)]
    [InlineData(TopicDisplayType.Tm1637_4, false, 4)]
    [InlineData(TopicDisplayType.Tm1637_6, true, 6)]
    [InlineData(TopicDisplayType.Tm1637_6, false, 6)]
    public async Task DashboardAppliesSavedConnectionVisibility(TopicDisplayType displayType, bool showConnections, int digitCount)
    {
        var item = new DashboardItem { DisplayType = displayType };
        item.GaugeSettings = new GaugeSettings { ShowConnections = showConnections, Height = 120 };
        var restoredItem = new DashboardItem { DisplayType = displayType, SettingsJson = item.SettingsJson };
        Assert.Equal(showConnections, restoredItem.GaugeSettings.ShowConnections);
        string html = await RenderDashboard("123", true, displayType, restoredItem.GaugeSettings);
        int start = html.IndexOf("<svg", StringComparison.Ordinal);
        int end = html.IndexOf("</svg>", start, StringComparison.Ordinal);
        Assert.True(start >= 0 && end > start);
        XElement svg = XElement.Parse(html[start..(end + "</svg>".Length)]);
        XNamespace ns = "http://www.w3.org/2000/svg";
        var connectors = svg.Elements(ns + "g").Where(group => (string?)group.Attribute("class") == "tm1637-connections").ToArray();
        Assert.Equal(showConnections ? 1 : 0, connectors.Length);
        if (showConnections)
        {
            Assert.Equal(9, connectors[0].Elements(ns + "rect").Count());
        }
        Assert.Equal(digitCount, svg.Elements(ns + "g").Count(group => group.Attribute("transform") != null));
        Assert.Contains("max-width: 100%; height: auto;", (string?)svg.Attribute("style"));
        Assert.Equal("TM1637 display: 123", (string?)svg.Attribute("aria-label"));
    }

    [Theory]
    [InlineData(TopicDisplayType.Tm1637_4, "4-Digit Display")]
    [InlineData(TopicDisplayType.Tm1637_6, "6-Digit Display")]
    [InlineData(TopicDisplayType.Lcd1602, "LCD1602")]
    [InlineData(TopicDisplayType.Lcd1604, "LCD1604")]
    [InlineData(TopicDisplayType.Lcd2004, "LCD2004")]
    public async Task DashboardUsesSavedLabelAndDefaults(TopicDisplayType displayType, string defaultLabel)
    {
        var item = new DashboardItem { DisplayType = displayType, SettingsJson = "{\"Height\":120}" };
        foreach (var (label, expected) in new (string?, string)[] { (null, defaultLabel), ("Room & sensor", "Room & sensor"), ("", "") })
        {
            var settings = item.GaugeSettings;
            settings.DisplayLabel = label;
            item.GaugeSettings = settings;
            var restored = new DashboardItem { DisplayType = displayType, SettingsJson = item.SettingsJson };
            string html = await RenderDashboard("123", true, displayType, restored.GaugeSettings);
            int start = html.IndexOf("<svg", StringComparison.Ordinal);
            int end = html.IndexOf("</svg>", start, StringComparison.Ordinal);
            Assert.True(start >= 0 && end > start);
            XElement svg = XElement.Parse(html[start..(end + "</svg>".Length)]);
            XNamespace ns = "http://www.w3.org/2000/svg";
            Assert.Equal(expected, svg.Element(ns + "text")?.Value);
        }
    }

    [Theory]
    [InlineData(TopicDisplayType.Tm1637_4)]
    [InlineData(TopicDisplayType.Tm1637_6)]
    public async Task DashboardUsesSavedSegmentColor(TopicDisplayType displayType)
    {
        var item = new DashboardItem { DisplayType = displayType };
        item.GaugeSettings = new GaugeSettings { SegmentColor = "#ffffff" };
        var restored = new DashboardItem { DisplayType = displayType, SettingsJson = item.SettingsJson };
        string html = await RenderDashboard("1.2", true, displayType, restored.GaugeSettings);
        int start = html.IndexOf("<svg", StringComparison.Ordinal);
        int end = html.IndexOf("</svg>", start, StringComparison.Ordinal);
        Assert.True(start >= 0 && end > start);
        XElement svg = XElement.Parse(html[start..(end + "</svg>".Length)]);
        XNamespace ns = "http://www.w3.org/2000/svg";
        var segments = svg.Descendants(ns + "polygon").Select(segment => (string?)segment.Attribute("fill")).ToArray();
        Assert.Contains("#ffffff", segments);
        Assert.Contains("#333333", segments);
        Assert.DoesNotContain("#ff7777", segments);
        var digits = svg.Elements(ns + "g").Where(group => group.Attribute("transform") != null).ToArray();
        Assert.Single(digits.Where(digit => (string?)digit.Element(ns + "circle")?.Attribute("fill") == "#ffffff"));
    }

    [Theory]
    [InlineData(TopicDisplayType.Tm1637_4)]
    [InlineData(TopicDisplayType.Tm1637_6)]
    public async Task DashboardReportsInvalidSegmentColor(TopicDisplayType displayType)
    {
        string html = await RenderDashboard("123", true, displayType, new GaugeSettings { SegmentColor = "bad" });
        Assert.Contains("segment color must be in #RRGGBB format", html);
        Assert.DoesNotContain("TM1637 display:", html);
    }

    [Fact]
    public void ExistingSettingsDefaultToNoLeadingZeros()
    {
        var item = new DashboardItem { SettingsJson = "{\"Height\":120}" };
        Assert.Equal("#ff7777", item.GaugeSettings.SegmentColor);
        Assert.Null(item.GaugeSettings.DisplayLabel);
        Assert.False(item.GaugeSettings.LeadingZeros);
        Assert.True(item.GaugeSettings.ShowConnections);
        Assert.True(new DashboardItem().GaugeSettings.ShowConnections);
        Assert.Equal(120, item.GaugeSettings.Height);
    }

    [Fact]
    public async Task DashboardWaitsForFirstMessage()
    {
        string html = await RenderDashboard("", false, TopicDisplayType.Tm1637_4);
        Assert.Contains("Waiting for a message", html);
        Assert.DoesNotContain("TM1637 display:", html);
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("not a number")]
    [InlineData("24:00")]
    public async Task DashboardReportsInvalidPayloadWithoutRenderingDisplay(string message)
    {
        string html = await RenderDashboard(message, true, TopicDisplayType.Tm1637_4);
        Assert.Contains("Cannot display message on TM1637", html);
        Assert.DoesNotContain("TM1637 display:", html);
    }

    [Fact]
    public async Task ExistingTextDisplayRemainsUnchanged()
    {
        string html = await RenderDashboard("12345", true, TopicDisplayType.Text);
        Assert.Contains("12345", html);
        Assert.DoesNotContain("Cannot display message on TM1637", html);
    }

    [Theory]
    [InlineData("123456")]
    [InlineData("12.3456")]
    [InlineData("17:34:56")]
    public async Task DashboardRendersSixDigitPayload(string value)
    {
        string html = await RenderDashboard(value, true, TopicDisplayType.Tm1637_6, new GaugeSettings { Height = 231 });
        Assert.Contains($"TM1637 display: {value}", html);
        Assert.Contains("viewBox=\"0 0 616 231\"", html);
        Assert.Contains("width: 616px; max-width: 100%; height: auto;", html);
    }

    [Fact]
    public async Task SixDigitDashboardHandlesMissingInvalidAndInvalidHeight()
    {
        Assert.Contains("Waiting for a message", await RenderDashboard("", false, TopicDisplayType.Tm1637_6));
        Assert.Contains("Cannot display message on TM1637", await RenderDashboard("1234567", true, TopicDisplayType.Tm1637_6));
        Assert.Contains("height must be greater than zero", await RenderDashboard("123456", true, TopicDisplayType.Tm1637_6, new GaugeSettings { Height = 0 }));
    }

    [Fact]
    public void DisplayTypeKeepsExistingValuesAndRoundTripsInDashboardItem()
    {
        Assert.Equal(0, (int)TopicDisplayType.Text);
        Assert.Equal(1, (int)TopicDisplayType.Json);
        Assert.Equal(2, (int)TopicDisplayType.Gauge);
        Assert.Equal(3, (int)TopicDisplayType.Tm1637_4);
        Assert.Equal(4, (int)TopicDisplayType.Tm1637_6);
        Assert.Equal(5, (int)TopicDisplayType.Lcd1602);
        Assert.Equal(6, (int)TopicDisplayType.Lcd1604);
        Assert.Equal(7, (int)TopicDisplayType.Lcd2004);
        foreach (var displayType in new[] { TopicDisplayType.Tm1637_4, TopicDisplayType.Tm1637_6, TopicDisplayType.Lcd1602, TopicDisplayType.Lcd1604, TopicDisplayType.Lcd2004 })
        {
            string json = JsonSerializer.Serialize(new DashboardItem { DisplayType = displayType });
            Assert.Equal(displayType, JsonSerializer.Deserialize<DashboardItem>(json)!.DisplayType);
        }
    }

    [Theory]
    [InlineData(TopicDisplayType.Tm1637_4)]
    [InlineData(TopicDisplayType.Tm1637_6)]
    [InlineData(TopicDisplayType.Lcd1602)]
    [InlineData(TopicDisplayType.Lcd1604)]
    [InlineData(TopicDisplayType.Lcd2004)]
    public async Task DashboardUsesSavedWidthAndReportsInvalidWidths(TopicDisplayType displayType)
    {
        var item = new DashboardItem { DisplayType = displayType };
        item.GaugeSettings = new GaugeSettings { Height = 150, Width = 400 };
        var restored = new DashboardItem { DisplayType = item.DisplayType, SettingsJson = item.SettingsJson };
        Assert.Equal(400, restored.GaugeSettings.Width);
        string html = await RenderDashboard("123", true, displayType, restored.GaugeSettings);
        Assert.Contains("width: 400px; max-width: 100%; height: auto; aspect-ratio: 400 / 150;", html);
        Assert.Contains("preserveAspectRatio=\"none\"", html);
        Assert.Contains("width must be greater than zero", await RenderDashboard("123", true, displayType, new GaugeSettings { Width = 0 }));
        Assert.Null(new DashboardItem { SettingsJson = "{\"Height\":120}" }.GaugeSettings.Width);
    }

    [Fact]
    public async Task Lcd2004DashboardHandlesMessagesAndSettings()
    {
        var item = new DashboardItem { DisplayType = TopicDisplayType.Lcd2004 };
        item.GaugeSettings = new GaugeSettings { Height = 372, ShowConnections = false };
        var restored = new DashboardItem { DisplayType = item.DisplayType, SettingsJson = item.SettingsJson };
        string html = await RenderDashboard("one\ntwo\nthree\nfour", true, restored.DisplayType, restored.GaugeSettings);
        Assert.Contains("viewBox=\"1.5 0 634 372\"", html);
        Assert.DoesNotContain("lcd2004-connections", html);
        int start = html.IndexOf("<svg", StringComparison.Ordinal);
        int end = html.IndexOf("</svg>", start, StringComparison.Ordinal);
        XElement svg = XElement.Parse(html[start..(end + "</svg>".Length)]);
        XNamespace ns = "http://www.w3.org/2000/svg";
        Assert.Equal("onetwothreefour", string.Concat(svg.Descendants(ns + "text").Where(element => (string?)element.Attribute("class") == "lcd2004-character").Select(element => element.Value)));
        Assert.Contains("Waiting for a message", await RenderDashboard("", false, TopicDisplayType.Lcd2004));
        Assert.Contains("Cannot display message on LCD2004", await RenderDashboard(new string('x', 81), true, TopicDisplayType.Lcd2004));
        Assert.Contains("height must be greater than zero", await RenderDashboard("test", true, TopicDisplayType.Lcd2004, new GaugeSettings { Height = 0 }));
        Assert.Contains("viewBox=\"0 0 704 372\"", await RenderDashboard("", true, TopicDisplayType.Lcd2004));
    }

    [Fact]
    public async Task Lcd1604DashboardHandlesMessagesAndSettings()
    {
        var item = new DashboardItem { DisplayType = TopicDisplayType.Lcd1604 };
        item.GaugeSettings = new GaugeSettings { Height = 372, ShowConnections = false };
        var restored = new DashboardItem { DisplayType = item.DisplayType, SettingsJson = item.SettingsJson };
        string html = await RenderDashboard("one\ntwo\nthree\nfour", true, restored.DisplayType, restored.GaugeSettings);
        Assert.Contains("viewBox=\"1.5 0 530 372\"", html);
        Assert.DoesNotContain("lcd1604-connections", html);
        int start = html.IndexOf("<svg", StringComparison.Ordinal);
        int end = html.IndexOf("</svg>", start, StringComparison.Ordinal);
        XElement svg = XElement.Parse(html[start..(end + "</svg>".Length)]);
        XNamespace ns = "http://www.w3.org/2000/svg";
        Assert.Equal("onetwothreefour", string.Concat(svg.Descendants(ns + "text").Where(element => (string?)element.Attribute("class") == "lcd1604-character").Select(element => element.Value)));
        Assert.Contains("Waiting for a message", await RenderDashboard("", false, TopicDisplayType.Lcd1604));
        Assert.Contains("Cannot display message on LCD1604", await RenderDashboard(new string('x', 65), true, TopicDisplayType.Lcd1604));
        Assert.Contains("height must be greater than zero", await RenderDashboard("test", true, TopicDisplayType.Lcd1604, new GaugeSettings { Height = 0 }));
        Assert.Contains("viewBox=\"0 0 600 372\"", await RenderDashboard("", true, TopicDisplayType.Lcd1604));
    }

    [Fact]
    public async Task LcdDashboardUsesSettingsAndReportsInvalidInputs()
    {
        var item = new DashboardItem { DisplayType = TopicDisplayType.Lcd1602 };
        item.GaugeSettings = new GaugeSettings { Height = 250, ShowConnections = false };
        var restored = new DashboardItem { DisplayType = item.DisplayType, SettingsJson = item.SettingsJson };
        string html = await RenderDashboard("Hello\nWorld", true, restored.DisplayType, restored.GaugeSettings);
        Assert.Contains("viewBox=\"1.5 0 530 250\"", html);
        Assert.Contains("width: 530px; max-width: 100%; height: auto;", html);
        Assert.DoesNotContain("lcd1602-connections", html);
        int start = html.IndexOf("<svg", StringComparison.Ordinal);
        int end = html.IndexOf("</svg>", start, StringComparison.Ordinal);
        XElement svg = XElement.Parse(html[start..(end + "</svg>".Length)]);
        XNamespace ns = "http://www.w3.org/2000/svg";
        Assert.Equal("HelloWorld", string.Concat(svg.Descendants(ns + "text").Where(element => (string?)element.Attribute("class") == "lcd1602-character").Select(element => element.Value)));
        Assert.Contains("Waiting for a message", await RenderDashboard("", false, TopicDisplayType.Lcd1602));
        Assert.Contains("Cannot display message on LCD1602", await RenderDashboard(new string('x', 33), true, TopicDisplayType.Lcd1602));
        Assert.Contains("height must be greater than zero", await RenderDashboard("test", true, TopicDisplayType.Lcd1602, new GaugeSettings { Height = 0 }));
        Assert.Contains("viewBox=\"0 0 600 250\"", await RenderDashboard("", true, TopicDisplayType.Lcd1602));
    }

    [Theory]
    [InlineData(TopicDisplayType.Lcd1602, 32, "LCD1602", "lcd1602-character")]
    [InlineData(TopicDisplayType.Lcd1604, 64, "LCD1604", "lcd1604-character")]
    [InlineData(TopicDisplayType.Lcd2004, 80, "LCD2004", "lcd2004-character")]
    public async Task LcdDashboardUpdatesAndRecoversFromInvalidPayload(TopicDisplayType displayType, int capacity, string label, string characterClass)
    {
        const string topic = "test/topic";
        await using var provider = CreateServices();
        await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            DashboardPage? page = null;
            var output = await renderer.RenderComponentAsync<DashboardHost>(ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                [nameof(DashboardHost.Capture)] = (Action<DashboardPage>)(component => page = component)
            }));
            Assert.NotNull(page);
            foreach (string value in new[] { "First", new string('x', capacity + 1), "Second" })
            {
                await page.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?>
                {
                    [nameof(DashboardPage.Topics)] = new List<string> { topic },
                    [nameof(DashboardPage.Messages)] = new Dictionary<string, string> { [topic] = value },
                    [nameof(DashboardPage.DisplayTypes)] = new Dictionary<string, TopicDisplayType> { [topic] = displayType },
                    [nameof(DashboardPage.MessageReceipts)] = new Dictionary<string, DateTime> { [topic] = DateTime.UtcNow }
                }));
                string html = output.ToHtmlString();
                if (value.Length > capacity)
                {
                    Assert.Contains($"Cannot display message on {label}", html);
                    Assert.DoesNotContain(characterClass, html);
                }
                else
                {
                    Assert.DoesNotContain($"Cannot display message on {label}", html);
                    Assert.Contains($"{label} display: {value}", html);
                }
            }
        });
    }

    [Fact]
    public async Task DashboardUpdatesAndRecoversFromInvalidPayload()
    {
        const string topic = "test/topic";
        await using var provider = CreateServices();
        await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            DashboardPage? page = null;
            var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                [nameof(DashboardHost.Capture)] = (Action<DashboardPage>)(component => page = component)
            });
            var output = await renderer.RenderComponentAsync<DashboardHost>(parameters);
            Assert.NotNull(page);

            foreach (string value in new[] { "1.234", "invalid", "17:34" })
            {
                await page.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?>
                {
                    [nameof(DashboardPage.Topics)] = new List<string> { topic },
                    [nameof(DashboardPage.Messages)] = new Dictionary<string, string> { [topic] = value },
                    [nameof(DashboardPage.DisplayTypes)] = new Dictionary<string, TopicDisplayType> { [topic] = TopicDisplayType.Tm1637_4 },
                    [nameof(DashboardPage.MessageReceipts)] = new Dictionary<string, DateTime> { [topic] = DateTime.UtcNow }
                }));
                string html = output.ToHtmlString();
                if (value == "invalid")
                {
                    Assert.Contains("Cannot display message on TM1637", html);
                    Assert.DoesNotContain("TM1637 display:", html);
                }
                else
                {
                    Assert.Contains($"TM1637 display: {value}", html);
                    Assert.DoesNotContain("Cannot display message", html);
                }
            }

            await page.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                [nameof(DashboardPage.GaugeSettings)] = new Dictionary<string, GaugeSettings> { [topic] = new GaugeSettings { Height = 231 } }
            }));
            Assert.Contains("style=\"width: 464px; max-width: 100%; height: auto;\"", output.ToHtmlString());
            await page.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                [nameof(DashboardPage.GaugeSettings)] = new Dictionary<string, GaugeSettings> { [topic] = new GaugeSettings { Height = 462 } }
            }));
            Assert.Contains("style=\"width: 928px; max-width: 100%; height: auto;\"", output.ToHtmlString());
        });
    }

    [Fact]
    public async Task SixDigitDashboardUpdatesAndRecoversFromInvalidPayload()
    {
        const string topic = "test/topic";
        await using var provider = CreateServices();
        await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            DashboardPage? page = null;
            var output = await renderer.RenderComponentAsync<DashboardHost>(ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                [nameof(DashboardHost.Capture)] = (Action<DashboardPage>)(component => page = component)
            }));
            Assert.NotNull(page);
            foreach (string value in new[] { "123456", "invalid", "17:34:56" })
            {
                await page.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?>
                {
                    [nameof(DashboardPage.Topics)] = new List<string> { topic },
                    [nameof(DashboardPage.Messages)] = new Dictionary<string, string> { [topic] = value },
                    [nameof(DashboardPage.DisplayTypes)] = new Dictionary<string, TopicDisplayType> { [topic] = TopicDisplayType.Tm1637_6 },
                    [nameof(DashboardPage.MessageReceipts)] = new Dictionary<string, DateTime> { [topic] = DateTime.UtcNow }
                }));
                string html = output.ToHtmlString();
                if (value == "invalid")
                {
                    Assert.Contains("Cannot display message on TM1637", html);
                    Assert.DoesNotContain("TM1637 display:", html);
                }
                else
                {
                    Assert.Contains($"TM1637 display: {value}", html);
                    Assert.DoesNotContain("Cannot display message", html);
                }
            }
        });
    }

    public sealed class DashboardHost : ComponentBase
    {
        [Parameter]
        public Action<DashboardPage> Capture { get; set; } = null!;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<DashboardPage>(0);
            builder.AddComponentReferenceCapture(1, component => Capture((DashboardPage)component));
            builder.CloseComponent();
        }
    }

    private static ServiceProvider CreateServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMudServices();
        services.AddSingleton(Mock.Of<IJSRuntime>());
        services.AddSingleton<NavigationManager, TestNavigationManager>();
        services.AddSingleton(Mock.Of<IDialogService>());
        services.AddSingleton(Mock.Of<IDataTransferService>());
        services.AddSingleton(Mock.Of<ITopicHistoryCrudService>());
        services.AddSingleton(Mock.Of<IKnownTopicsCrudService>());
        return services.BuildServiceProvider();
    }

    private static async Task<string> RenderDashboard(string message, bool received, TopicDisplayType displayType, GaugeSettings? settings = null)
    {
        const string topic = "test/topic";
        await using var provider = CreateServices();
        await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                [nameof(DashboardPage.Topics)] = new List<string> { topic },
                [nameof(DashboardPage.Messages)] = new Dictionary<string, string> { [topic] = message },
                [nameof(DashboardPage.DisplayTypes)] = new Dictionary<string, TopicDisplayType> { [topic] = displayType },
                [nameof(DashboardPage.GaugeSettings)] = new Dictionary<string, GaugeSettings> { [topic] = settings ?? new GaugeSettings() },
                [nameof(DashboardPage.MessageReceipts)] = received
                    ? new Dictionary<string, DateTime> { [topic] = DateTime.UtcNow }
                    : new Dictionary<string, DateTime>()
            });
            var output = await renderer.RenderComponentAsync<DashboardPage>(parameters);
            return output.ToHtmlString();
        });
    }

    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager() => Initialize("http://localhost/", "http://localhost/");
    }
}
