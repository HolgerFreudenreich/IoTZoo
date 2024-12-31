// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/ 
// --------------------------------------------------------------------------------------------------------------------
// Connect «Things» with microcontrollers without programming knowledge.
// MIT License
// --------------------------------------------------------------------------------------------------------------------
// (c) 2025 Holger Freudenreich under MIT license
// --------------------------------------------------------------------------------------------------------------------

using Domain.Interfaces.Crud;
using Domain.Pocos;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace IotZoo.Pages;

public class ScriptsPageBase : PageBase
{
   [Inject]
   public IScriptCrudService ScriptsService
   {
      get;
      set;
   } = null!;

   protected List<Script>? Scripts
   {
      get;
      set;
   } = new();

   protected override void OnInitialized()
   {
      DataTransferService.CurrentScreen = ScreenMode.Scripts;
      base.OnInitialized();
   }

   protected override async Task OnAfterRenderAsync(bool firstRender)
   {
      await base.OnAfterRenderAsync(firstRender);
      if (firstRender)
      {
         await LoadData();
      }
   }

   protected override async Task LoadData()
   {
      Scripts = await this.ScriptsService.LoadScripts();
      await InvokeAsync(StateHasChanged);
   }

   private async Task OpenScriptEditor(Script script)
   {
      try
      {
         var options = GetDialogOptions();

         var parameters = new DialogParameters { ["Script"] = script };
         IsEditorOpen = true;
         var dialog = await DialogService.ShowAsync<Dialogs.ScriptEditor>("Edit Script",
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

   public async Task OpenScriptEditorAsync()
   {
      await OpenScriptEditor(new Script());
   }

   protected async Task EditScript(Script script)
   {
      await OpenScriptEditor(script);
   }

   protected async Task DeleteScript(Script script)
   {
      bool? result = await DialogService.ShowMessageBox("Delete",
                                                        message: $"Do you want to delete the Script '{script.ScriptName}'?",
                                                        yesText: "Yes", cancelText: "No");
      if (!result.HasValue)
      {
         return;
      }
      await ScriptsService.Delete(script);
      await LoadData();
   }

   protected async Task CloneScript(Script script)
   {
      var clonedScript = Infrastructure.Tools.DeepCopyReflection(script);
      clonedScript.ScriptId = 0; // force new
      await OpenScriptEditor(clonedScript);
   }
}
