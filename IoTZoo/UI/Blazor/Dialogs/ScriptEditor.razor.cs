// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/   P L A Y G R O U N D
// --------------------------------------------------------------------------------------------------------------------
// Connect «Things» with microcontrollers in a simple way.
// --------------------------------------------------------------------------------------------------------------------
// (c) 2025 Holger Freudenreich under the MIT license
// --------------------------------------------------------------------------------------------------------------------

using Domain.Interfaces.Crud;
using Domain.Interfaces.RuleEngine;
using Domain.Pocos;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Reflection;

namespace IotZoo.Dialogs;

public class ScriptEditorBase : EditorBase
{
   [Parameter]
   public Script Script { get; set; } = null!;

   [Inject]
   private IScriptCrudService ScriptCrudService
   {
      get;
      set;
   } = null!;

   [Inject]
   private IScriptService ScriptService
   {
      get;
      set;
   } = null!;

   protected override async Task OnInitializedAsync()
   {
      await base.OnInitializedAsync();
      if (Script.ScriptId <= 0)
      {
         DialogTitle = "Add C# Script";
         IsNewRecord = true;
      }
      else
      {
         DialogTitle = "Edit Script";
         IsNewRecord = false;
      }

      HashCode = GetHashCodeBase64(Script);
   }

   protected override async Task Cancel()
   {
      await base.Cancel(Script);
   }

   protected override async Task Save()
   {
      try
      {
         Snackbar.Clear();
         await Task.Delay(0);
         TrimTextFields(Script);
         if (ValidateFields())
         {
            await ScriptCrudService.Save(Script);
            MudDialog.Close(DialogResult.Ok(Script));
         }
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
         Snackbar.Add("Unable to save script", Severity.Error);
      }
   }

   protected async void Compile()
   {
      try
      {
         Snackbar.Clear();

         TrimTextFields(Script);
         if (ValidateFields())
         {
            DateTime startDateTime = DateTime.UtcNow;
            string? error = await ScriptService.EvaluateScript(Script);
            if (error != null)
            {
               Snackbar.Add($"Unable to compile script{Environment.NewLine}{error}",
                            Severity.Error);
            }
            else
            {
               Snackbar.Add($"Successful ({(int)(DateTime.UtcNow - startDateTime).TotalMilliseconds}ms)!", Severity.Success);
            }
         }
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
         Snackbar.Add($"Unable to compile script {ex.GetBaseException().Message}",
                      Severity.Error);
      }
   }

   private bool ValidateFields()
   {
      if (string.IsNullOrEmpty(Script.ScriptName))
      {
         Snackbar.Add("Script Name must not be empty!", Severity.Error);

         return false;
      }
      return true;
   }
}

