// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \\/ /       / / / __ \\/ __ \\
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\\____/_/       /____|____/\\____/   P L A Y G R O U N D
// --------------------------------------------------------------------------------------------------------------------
// Connect «Things» with microcontrollers in a simple way.
// --------------------------------------------------------------------------------------------------------------------
// (c) 2025 - 2026 Holger Freudenreich under the MIT license
// --------------------------------------------------------------------------------------------------------------------

using System.Text.Json;

namespace Domain.Pocos;

public class DashboardItem
{
   public int DashboardItemId { get; set; }

   public int DashboardId { get; set; }

   public int IndexInDashboard { get; set; }

   public int KnownTopicId { get; set; }

       public TopicDisplayType DisplayType { get; set; } = TopicDisplayType.Text;

       public string? SettingsJson { get; set; }

       public GaugeSettings GaugeSettings
       {
           get => string.IsNullOrWhiteSpace(SettingsJson)
               ? new GaugeSettings()
               : JsonSerializer.Deserialize<GaugeSettings>(SettingsJson) ?? new GaugeSettings();
           set => SettingsJson = JsonSerializer.Serialize(value);
       }
   }
