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

namespace IotZoo.Dialogs;

using Domain.Interfaces;
using Domain.Interfaces.Crud;
using Domain.Pocos;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Reflection;
using static MudBlazor.Colors;

public class KnownMicrocontrollerEditorBase : EditorBase
{
   [Parameter]

   public KnownMicrocontroller Microcontroller { get; set; } = null!;

   protected async override Task OnInitializedAsync()
   {
      if (Microcontroller.MicroControllerId <= 0)
      {
         DialogTitle = "Add Known Microcontroller";
         IsNewRecord = true;
      }
      else
      {
         DialogTitle = "Edit Known Microcontroller";
         IsNewRecord = false;
      }

      HashCode = GetHashCodeBase64(Microcontroller);
      await base.OnInitializedAsync();
   }

   [Inject]
   private IMicrocontrollerService MicrocontrollerService
   {
      get;
      set;
   } = null!;

   [Inject]
   private IProjectCrudService ProjectService
   {
      get;
      set;
   } = null!;

   protected Project? SelectedProject { get; set; } = null;

   protected List<Project> ProjectCatalog { get; set; } = null!;

   public KnownMicrocontrollerEditorBase()
   {
   }

   protected async override Task OnAfterRenderAsync(bool firstRender)
   {
      await base.OnAfterRenderAsync(firstRender);
      if (firstRender)
      {
         ProjectCatalog = await ProjectService.LoadProjects();
         if (!IsNewRecord)
         {
            if (null != Microcontroller.ProjectName)
            {
               SelectedProject = ProjectCatalog.Where(x => x.ProjectName == this.Microcontroller.ProjectName).First();
            }
         }
         else
         {
            SelectedProject = DataTransferService.SelectedProject;
         }
      }
      await InvokeAsync(StateHasChanged);
   }

   protected override async Task Cancel()
   {
      await base.Cancel(Microcontroller);
   }

   protected override async Task Save()
   {
      try
      {
         Snackbar.Clear();
         if (null != SelectedProject)
         {
            Microcontroller.ProjectName = SelectedProject.ProjectName;
         }
         TrimTextFields(Microcontroller);
         if (!await ValidateFields())
         {
            return;
         }

         bool isSuccess = await MicrocontrollerService.PushMicrocontrollerConfigToMicrocontroller(Microcontroller);
         if (!isSuccess)
         {
            Snackbar.Add("Unable to transmit the configuration changes to the microcontroller! The changes will not be saved!", Severity.Error);
            // try it with the REST interface
            isSuccess = await MicrocontrollerService.PostMicrocontrollerConfigToMicrocontroller(Microcontroller);
         }
         if (isSuccess)
         {
            await MicrocontrollerService.Save(Microcontroller, pushToMicrocontroller: true);
         }
         else
         {
            Snackbar.Add("Unable to transmit the configuration changes to the microcontroller! The changes will not be saved!", Severity.Error);
         }
         MudDialog.Close(DialogResult.Ok(Microcontroller));
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
         Snackbar.Add("Unable to save known microcontroller!", Severity.Error);
      }
   }

   private async Task<bool> ValidateFields()
   {
      bool result = true;
      if (string.IsNullOrEmpty(Microcontroller.BoardType))
      {
         result = false;
         Snackbar.Add("Board type is required!", Severity.Error);
      }
      if (string.IsNullOrEmpty(Microcontroller.MacAddress))
      {
         result = false;
         Snackbar.Add("MacAddress is required!", Severity.Error);
      }
      if (string.IsNullOrEmpty(Microcontroller.IpAddress))
      {
         result = false;
         Snackbar.Add("IpAddress is required!", Severity.Error);
      }
      if (result)
      {
         if (IsNewRecord)
         {
            var existingMicrocontroller = await MicrocontrollerService.GetMicrocontroller(Microcontroller.MacAddress);
            if (existingMicrocontroller != null)
            {
               Microcontroller.MicroControllerId = existingMicrocontroller.MicroControllerId;
               IsNewRecord = false;
            }
         }
      }
      return result;
   }
}