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

using Domain.Interfaces;
using Domain.Interfaces.Crud;
using Domain.Pocos;
using Infrastructure;
using IotZoo.Dialogs;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Moq;
using MudBlazor;
using MudBlazor.Services;

namespace UnitTests;

public class KnownTopicEditorDisplayTests
{
    [Theory]
    [InlineData(TopicDisplayType.Tm1637_4, true)]
    [InlineData(TopicDisplayType.Tm1637_6, true)]
    [InlineData(TopicDisplayType.Lcd1602, false)]
    [InlineData(TopicDisplayType.Lcd1604, false)]
    [InlineData(TopicDisplayType.Lcd2004, false)]
    [InlineData(TopicDisplayType.Text, false)]
    [InlineData(TopicDisplayType.Json, false)]
    [InlineData(TopicDisplayType.Gauge, false)]
    public async Task EditorShowsOnlyRelevantDisplayControls(TopicDisplayType displayType, bool showLeadingZeros)
    {
        var dashboard = new Dashboard { DashboardId = 1, DashboardName = "Test dashboard" };
        var dashboardService = new Mock<IDashboardCrudService>();
        dashboardService.Setup(service => service.GetDashboards()).ReturnsAsync([dashboard]);
        var itemsService = new Mock<IDashboardItemsCrudService>();
        itemsService.Setup(service => service.GetDashboardItemsByKnownTopicId(1))
            .ReturnsAsync([new DashboardItem { DashboardId = 1, KnownTopicId = 1, DisplayType = displayType }]);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMudServices();
        services.AddSingleton(Mock.Of<IJSRuntime>());
        services.AddSingleton(Mock.Of<IHashingService>());
        services.AddSingleton(Mock.Of<IDataTransferService>());
        services.AddSingleton(Mock.Of<IKnownTopicsCrudService>());
        services.AddSingleton(dashboardService.Object);
        services.AddSingleton(itemsService.Object);
        services.AddSingleton<NavigationManager, TestNavigationManager>();
        await using var provider = services.BuildServiceProvider();
        await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
        string html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var output = await renderer.RenderComponentAsync<MudDialogProvider>();
            var parameters = new DialogParameters { [nameof(KnownTopicEditorBase.KnownTopic)] = new KnownTopic { KnownTopicId = 1 } };
            await provider.GetRequiredService<IDialogService>().ShowAsync<KnownTopicEditor>("Edit Known Topic", parameters);
            return output.ToHtmlString();
        });
        Assert.Contains("Height (px)", html);
        if (showLeadingZeros || displayType is TopicDisplayType.Lcd1602 or TopicDisplayType.Lcd1604 or TopicDisplayType.Lcd2004)
        {
            Assert.Contains("Width (px)", html);
        }
        else
        {
            Assert.DoesNotContain("Width (px)", html);
        }
        if (showLeadingZeros)
        {
            Assert.Contains("Leading Zeros", html);
            Assert.Contains("Show Connections", html);
            Assert.Contains("Display label", html);
            Assert.Contains("Segment color", html);
            Assert.DoesNotContain("Font size (px)", html);
        }
        else
        {
            Assert.DoesNotContain("Leading Zeros", html);
            Assert.DoesNotContain("Segment color", html);
            if (displayType is TopicDisplayType.Lcd1602 or TopicDisplayType.Lcd1604 or TopicDisplayType.Lcd2004)
            {
                Assert.Contains("Show Connections", html);
                Assert.Contains("Display label", html);
                Assert.Contains(displayType == TopicDisplayType.Lcd1602 ? "LCD1602" : displayType == TopicDisplayType.Lcd1604 ? "LCD1604" : "LCD2004", html);
                Assert.DoesNotContain("Font size (px)", html);
            }
            else
            {
                Assert.DoesNotContain("Show Connections", html);
                Assert.DoesNotContain("Display label", html);
                Assert.Contains("Font size (px)", html);
            }
        }
    }

    [Theory]
    [InlineData(TopicDisplayType.Tm1637_4)]
    [InlineData(TopicDisplayType.Tm1637_6)]
    public void EditingLeadingZerosKeepsOtherDashboardSettings(TopicDisplayType displayType)
    {
        var item = new DashboardItem { DashboardId = 1, DisplayType = displayType, FontSize = 17 };
        item.GaugeSettings = new GaugeSettings { Height = 123, Label = "Temperature" };
        var otherItem = new DashboardItem { DashboardId = 2, DisplayType = displayType };
        var editor = new TestKnownTopicEditor(item, otherItem);
        editor.SetLeadingZeros(1, true);
        editor.SetShowConnections(1, false);
        editor.SetDisplayLabel(1, "Temperature");
        editor.SetSegmentColor(1, "#ffffff");

        var reloaded = new DashboardItem { SettingsJson = item.SettingsJson };
        Assert.Equal("#ffffff", reloaded.GaugeSettings.SegmentColor);
        Assert.Equal("#ff7777", otherItem.GaugeSettings.SegmentColor);
        Assert.Equal("Temperature", reloaded.GaugeSettings.DisplayLabel);
        Assert.Null(otherItem.GaugeSettings.DisplayLabel);
        Assert.True(reloaded.GaugeSettings.LeadingZeros);
        Assert.False(reloaded.GaugeSettings.ShowConnections);
        Assert.True(otherItem.GaugeSettings.ShowConnections);
        Assert.Equal(123, reloaded.GaugeSettings.Height);
        Assert.Equal("Temperature", reloaded.GaugeSettings.Label);
        Assert.Equal(17, item.FontSize);
        Assert.False(otherItem.GaugeSettings.LeadingZeros);

        editor.SetDisplayType(1, TopicDisplayType.Text);
        editor.SetDisplayType(1, displayType);
        Assert.True(item.GaugeSettings.LeadingZeros);
        Assert.False(item.GaugeSettings.ShowConnections);
        editor.SetLeadingZeros(1, false);
        editor.SetShowConnections(1, true);
        var savedSettings = new DashboardItem { SettingsJson = item.SettingsJson }.GaugeSettings;
        Assert.False(savedSettings.LeadingZeros);
        Assert.True(savedSettings.ShowConnections);
    }

    [Theory]
    [InlineData(TopicDisplayType.Lcd1602)]
    [InlineData(TopicDisplayType.Lcd1604)]
    [InlineData(TopicDisplayType.Lcd2004)]
    public void EditingLcdLabelPersistsPerDashboardAndCanBeCleared(TopicDisplayType displayType)
    {
        var item = new DashboardItem { DashboardId = 1, DisplayType = displayType };
        var other = new DashboardItem { DashboardId = 2, DisplayType = displayType };
        var editor = new TestKnownTopicEditor(item, other);
        editor.SetDisplayLabel(1, "Temperature");
        Assert.Equal("Temperature", new DashboardItem { SettingsJson = item.SettingsJson }.GaugeSettings.DisplayLabel);
        Assert.Null(other.GaugeSettings.DisplayLabel);
        editor.SetDisplayLabel(1, "");
        Assert.Equal("", new DashboardItem { SettingsJson = item.SettingsJson }.GaugeSettings.DisplayLabel);
    }

    [Theory]
    [InlineData(TopicDisplayType.Tm1637_4)]
    [InlineData(TopicDisplayType.Tm1637_6)]
    [InlineData(TopicDisplayType.Lcd1602)]
    [InlineData(TopicDisplayType.Lcd1604)]
    [InlineData(TopicDisplayType.Lcd2004)]
    public void EditingWidthPersistsPerDashboardAndCanBeCleared(TopicDisplayType displayType)
    {
        var item = new DashboardItem { DashboardId = 1, DisplayType = displayType };
        var other = new DashboardItem { DashboardId = 2, DisplayType = displayType };
        var editor = new TestKnownTopicEditor(item, other);
        editor.SetWidth(1, 400);
        Assert.Equal(400, new DashboardItem { SettingsJson = item.SettingsJson }.GaugeSettings.Width);
        Assert.Null(other.GaugeSettings.Width);
        editor.SetWidth(1, null);
        Assert.Null(new DashboardItem { SettingsJson = item.SettingsJson }.GaugeSettings.Width);
    }

    private sealed class TestKnownTopicEditor : KnownTopicEditorBase
    {
        public TestKnownTopicEditor(params DashboardItem[] items)
        {
            SelectedDashboardItems = items.ToDictionary(item => item.DashboardId);
        }

        public void SetLeadingZeros(int id, bool value) => OnGaugeSettingsChanged(id, settings => settings.LeadingZeros = value);
        public void SetShowConnections(int id, bool value) => OnGaugeSettingsChanged(id, settings => settings.ShowConnections = value);
        public void SetWidth(int id, int? value) => OnGaugeSettingsChanged(id, settings => settings.Width = value);
        public void SetDisplayLabel(int id, string value) => OnGaugeSettingsChanged(id, settings => settings.DisplayLabel = value);
        public void SetSegmentColor(int id, string value) => OnGaugeSettingsChanged(id, settings => settings.SegmentColor = value);
        public void SetDisplayType(int id, TopicDisplayType type) => OnDisplayTypeChanged(id, type);
    }

    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager() => Initialize("http://localhost/", "http://localhost/");
    }
}
