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
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace IotZoo.Components;

public partial class JsonTreeView : ComponentBase
{
   [Parameter]
   public string? Json { get; set; }

   // Nodes up to this depth are initially expanded.
   [Parameter]
   public int ExpandLevels { get; set; } = 1;

   private JsonElement? root;
   private string? errorMessage;
   private JsonDocument? document;
   private string? parsedJson;

   protected override void OnParametersSet()
   {
      if (Json == parsedJson)
      {
         return;
      }

      parsedJson = Json;
      document?.Dispose();
      document = null;
      root = null;
      errorMessage = null;

      if (string.IsNullOrWhiteSpace(Json))
      {
         return;
      }

      try
      {
         document = JsonDocument.Parse(Json);
         root = document.RootElement;
      }
      catch (JsonException ex)
      {
         errorMessage = $"Invalid JSON: {ex.Message}";
      }
   }

   private RenderFragment RenderNode(string? name, JsonElement element, int level) => builder =>
   {
      string label = name ?? "root";
      switch (element.ValueKind)
      {
         case JsonValueKind.Object:
            {
               var properties = element.EnumerateObject().ToList();
               builder.OpenComponent<MudTreeViewItem<string>>(0);
               builder.AddAttribute(1, nameof(MudTreeViewItem<string>.Text), label);
               builder.AddAttribute(2, nameof(MudTreeViewItem<string>.Icon), Icons.Material.Filled.DataObject);
               builder.AddAttribute(3, nameof(MudTreeViewItem<string>.Expanded), level < ExpandLevels);
               builder.AddAttribute(4, nameof(MudTreeViewItem<string>.EndText), $"{{{properties.Count}}}");
               builder.AddAttribute(5, nameof(MudTreeViewItem<string>.ChildContent), (RenderFragment)(b =>
               {
                  foreach (var property in properties)
                  {
                     b.AddContent(6, RenderNode(property.Name, property.Value, level + 1));
                  }
               }));
               builder.CloseComponent();
               break;
            }
         case JsonValueKind.Array:
            {
               var items = element.EnumerateArray().ToList();
               builder.OpenComponent<MudTreeViewItem<string>>(0);
               builder.AddAttribute(1, nameof(MudTreeViewItem<string>.Text), label);
               builder.AddAttribute(2, nameof(MudTreeViewItem<string>.Icon), Icons.Material.Filled.DataArray);
               builder.AddAttribute(3, nameof(MudTreeViewItem<string>.Expanded), level < ExpandLevels);
               builder.AddAttribute(4, nameof(MudTreeViewItem<string>.EndText), $"[{items.Count}]");
               builder.AddAttribute(5, nameof(MudTreeViewItem<string>.ChildContent), (RenderFragment)(b =>
               {
                  for (int i = 0; i < items.Count; i++)
                  {
                     b.AddContent(6, RenderNode($"[{i}]", items[i], level + 1));
                  }
               }));
               builder.CloseComponent();
               break;
            }
         default:
            builder.OpenComponent<MudTreeViewItem<string>>(0);
            builder.AddAttribute(1, nameof(MudTreeViewItem<string>.Text), label);
            builder.AddAttribute(2, nameof(MudTreeViewItem<string>.Icon), LeafIcon(element));
            builder.AddAttribute(3, nameof(MudTreeViewItem<string>.EndText), LeafText(element));
            builder.CloseComponent();
            break;
      }
   };

   private static string LeafText(JsonElement element) => element.ValueKind switch
   {
      JsonValueKind.String => $"\"{element.GetString()}\"",
      JsonValueKind.Null => "null",
      JsonValueKind.True => "true",
      JsonValueKind.False => "false",
      _ => element.GetRawText()
   };

   private static string LeafIcon(JsonElement element) => element.ValueKind switch
   {
      JsonValueKind.String => Icons.Material.Filled.TextFields,
      JsonValueKind.Number => Icons.Material.Filled.Numbers,
      JsonValueKind.True or JsonValueKind.False => Icons.Material.Filled.ToggleOn,
      _ => Icons.Material.Filled.Block
   };
}
