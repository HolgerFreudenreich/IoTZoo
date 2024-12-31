// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/ 
//
// --------------------------------------------------------------------------------------------------------------------
// (c) 2025 Holger Freudenreich under MIT license
// --------------------------------------------------------------------------------------------------------------------
// Editor for boxes/totes. Each box can contain one or more components.
// It would also be conceivable to put boxes in boxes.
// --------------------------------------------------------------------------------------------------------------------

namespace IotZoo.Dialogs;

using Domain.Pocos;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Reflection;

public class CronJobEditorBase : EditorBase
{
   [Parameter]
   public CronJob CronJob { get; set; } = null!;

   protected override async Task OnInitializedAsync()
   {
      if (CronJob.CronId <= 0)
      {
         DialogTitle = "Add Cronjob";
         IsNewRecord = true;
      }
      else
      {
         DialogTitle = "Edit Cronjob";
         IsNewRecord = false;
      }

      HashCode = GetHashCodeBase64(CronJob);
      await base.OnInitializedAsync();
   }

   protected override async Task Cancel()
   {
      await Cancel(CronJob);
   }

   protected override Task Save()
   {
      try
      {
         Snackbar.Clear();
      }
      catch (Exception exception)
      {
         Logger.LogError(exception, $"{MethodBase.GetCurrentMethod()} failed!");
         Snackbar.Add("Unable to save!", Severity.Error);
      }
      MudDialog.Close(DialogResult.Ok(CronJob));
      return Task.CompletedTask;
   }
}