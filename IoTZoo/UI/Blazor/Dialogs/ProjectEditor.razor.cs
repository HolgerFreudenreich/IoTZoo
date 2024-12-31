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
using Domain.Pocos;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Reflection;

namespace IotZoo.Dialogs;

public class ProjectEditorBase : EditorBase
{
   [Parameter]
   public Project Project { get; set; } = null!;

   [Inject]
   private IProjectCrudService ProjectService
   {
      get;
      set;
   } = null!;

    protected override async Task OnInitializedAsync()
   {
      await base.OnInitializedAsync();
      if (Project.ProjectId <= 0)
      {
         DialogTitle = "Add Project";
         IsNewRecord = true;
      }
      else
      {
         DialogTitle = "Edit Project";
         IsNewRecord = false;
      }
      Project.IsNew = IsNewRecord;
      HashCode = GetHashCodeBase64(Project);
   }

   protected override async Task Cancel()
   {
      await base.Cancel(Project);
   }

   protected override async Task Save()
   {
      try
      {
         Snackbar.Clear();

         TrimTextFields(Project);
         if (await ValidateFieldsAsync())
         {
            await ProjectService.Save(Project);
            MudDialog.Close(DialogResult.Ok(Project));
         }
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
         Snackbar.Add("Unable to save script", Severity.Error);
      }
   }

   private async Task<bool> ValidateFieldsAsync()
   {
      if (string.IsNullOrEmpty(Project.ProjectName))
      {
         Snackbar.Add("Project Name must not be empty!", Severity.Error);

         return false;
      }

      var existing = await this.ProjectService.LoadProjectByName(Project.ProjectName);
      if (existing != null)
      {
         Snackbar.Add($"Project with name '{Project.ProjectName}' already exists!", Severity.Error);
         return false;
      }

      return true;
   }
}

