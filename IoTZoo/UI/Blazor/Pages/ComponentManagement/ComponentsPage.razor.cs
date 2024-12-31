// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/
//
// --------------------------------------------------------------------------------------------------------------------
// Page for managing components. Components are stored in containers.
// The aim is to obtain an overview of the available components (that can be connected to microcontrollers).
//
// (c) 2025 Holger Freudenreich under MIT license
// --------------------------------------------------------------------------------------------------------------------

using Domain.Pocos;
using Domain.Services.ComponentManagement;
using IotZoo.Dialogs;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace IotZoo.Pages.ComponentManagement;

public class ComponentsPageBase : PageBase
{
   [Inject]
   protected IComponentService ComponentsService
   {
      get;
      set;
   } = null!;

   protected List<Component>? Components { get; set; } = new();

   public async void OpenEditor()
   {
      await OpenEditor(new Component());
   }

   protected async Task OpenEditor(Component component)
   {
      try
      {
         var options = GetDialogOptions();

         var parameters = new DialogParameters { ["Component"] = component };
         IsEditorOpen = true;
         var dialog = await DialogService.ShowAsync<ComponentEditor>("Component Editor",
                                                                     parameters,
                                                                     options);
         var result = await dialog.Result;
      }
      finally
      {
         IsEditorOpen = false;
         await LoadData();
      }
   }

   protected override void OnInitialized()
   {
      DataTransferService.CurrentScreen = ScreenMode.Components;
      base.OnInitialized();
   }

   protected override async Task LoadData()
   {
      Components = await ComponentsService.GetComponents();
      await InvokeAsync(StateHasChanged);
   }

   protected async void Edit(Component component)
   {
      await OpenEditor(component);
   }

   protected async Task Delete(Component component)
   {
      bool? result = await DialogService.ShowMessageBox("Delete",
                                                        $"Do you want to delete the component with Sku {component.Sku}?",
                                                        yesText: "Yes", cancelText: "No");
      if (!result.HasValue)
      {
         return;
      }
      await ComponentsService.DeleteComponent(component);
      await LoadData();
   }

   protected async void Clone(Component component)
   {
      var cloned = Infrastructure.Tools.DeepCopyReflection(component);
      cloned.ComponentId = 0; // force new
      await OpenEditor(cloned);
   }
}
