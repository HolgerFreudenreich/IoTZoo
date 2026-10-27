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

using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace IotZoo.Components;

public partial class MultiSelectAutoComplete<T> : ComponentBase
{
   private MudAutocomplete<T> autoComplete = null!;

   private T? theValue;

   private readonly List<T> selectedValues = new();

   [Parameter]
   public List<T> Values { get; set; } = new();

   [Parameter]
   public EventCallback<List<T>> ValuesChanged { get; set; }

   [Parameter]
   public Func<string, CancellationToken, Task<IEnumerable<T>>> SearchFunc { get; set; } = null!;

   [Parameter]
   public Func<T, string?> GetNameFunc { get; set; } = null!;

   [Parameter]
   public Func<T, object> GetIDFunc { get; set; } = null!;

   [Parameter]
   public string Label { get; set; } = string.Empty;

   [Parameter]
   public bool Required { get; set; }

   [Parameter]
   public string RequiredError { get; set; } = "Required!";

   protected override void OnParametersSet()
   {
      selectedValues.Clear();
      selectedValues.AddRange(Values);
   }

   private async Task<IEnumerable<T>> SearchWrapper(string value, CancellationToken token)
   {
      return await SearchFunc(value, token);
   }

   private async Task RemoveValue(MudChip<T> chip)
   {
      if (chip.Value is null)
      {
         return;
      }

      selectedValues.Remove(chip.Value);
      await ValuesChanged.InvokeAsync(selectedValues.ToList());
   }

   private async Task OnValueChangedAsync(T? value)
   {
      if (value is null || selectedValues.Any(item => Equals(GetIDFunc(item), GetIDFunc(value))))
      {
         theValue = default;
         return;
      }

      selectedValues.Add(value);
      theValue = default;
      await ValuesChanged.InvokeAsync(selectedValues.ToList());
   }
}
