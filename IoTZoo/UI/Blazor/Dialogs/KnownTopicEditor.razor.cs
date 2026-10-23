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

namespace IotZoo.Dialogs;

using Domain.Interfaces.Crud;
using Domain.Pocos;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Reflection;
using System.Threading.Tasks;

public class KnownTopicEditorBase : EditorBase
{
   [Parameter]
   public KnownTopic KnownTopic { get; set; } = null!;

   protected override async Task OnInitializedAsync()
   {
      if (KnownTopic.KnownTopicId <= 0)
      {
         DialogTitle = "Add Known Topic";
         IsNewRecord = true;
      }
      else
      {
         DialogTitle = "Edit Known Topic";
         IsNewRecord = false;
      }

      HashCode = GetHashCodeBase64(KnownTopic);

      Dashboards = await DashboardService.GetDashboards();
      if (KnownTopic.KnownTopicId > 0)
      {
         var dashboardItems = await DashboardItemsService.GetDashboardItemsByKnownTopicId(KnownTopic.KnownTopicId);
         SelectedDashboards = Dashboards.Where(dashboard => dashboardItems.Any(item => item.DashboardId == dashboard.DashboardId)).ToList();
      }

      await base.OnInitializedAsync();
   }

   [Inject]
   private IKnownTopicsCrudService KnownTopicsService
   {
      get;
      set;
   } = null!;

   [Inject]
   private IDashboardCrudService DashboardService
   {
      get;
      set;
   } = null!;

   [Inject]
   private IDashboardItemsCrudService DashboardItemsService
   {
      get;
      set;
   } = null!;

   protected List<Dashboard> Dashboards { get; set; } = new();

   protected List<Dashboard> SelectedDashboards { get; set; } = new();

   public KnownTopicEditorBase()
   {
   }

   protected override async Task Cancel()
   {
      await Cancel(KnownTopic);
   }

   protected Task<IEnumerable<Dashboard>> SearchDashboards(string value, CancellationToken token)
   {
      IEnumerable<Dashboard> result = string.IsNullOrWhiteSpace(value)
         ? Dashboards
         : Dashboards.Where(dashboard => dashboard.DashboardName.Contains(value, StringComparison.OrdinalIgnoreCase));
      return Task.FromResult(result);
   }

   private async Task<bool> ValidateFieldsAsync()
   {
      bool result = true;
      if (string.IsNullOrEmpty(KnownTopic.NamespaceName))
      {
         result = false;
         Snackbar.Add("Namespace is required!", Severity.Error);
      }
      if (string.IsNullOrEmpty(KnownTopic.ProjectName))
      {
         result = false;
         Snackbar.Add("Project name is required!", Severity.Error);
      }
      if (string.IsNullOrEmpty(KnownTopic.Topic))
      {
         result = false;
         Snackbar.Add("Topic is required!", Severity.Error);
      }
      if (result)
      {
         if (IsNewRecord)
         {
            var existingKnownTopic = await KnownTopicsService.GetKnownTopicByTopicName(KnownTopic.ProjectName, KnownTopic.Topic);
            if (existingKnownTopic != null)
            {
               KnownTopic.KnownTopicId = existingKnownTopic.KnownTopicId;
               IsNewRecord = false;
               KnownTopic.MessageDirection = MessageDirection.Internal;
            }
         }
      }
      return result;
   }

   protected override async Task Save()
   {
      try
      {
         Snackbar.Clear();

         TrimTextFields(KnownTopic);
         if (!(await ValidateFieldsAsync()))
         {
            return;
         }

         await KnownTopicsService.Save(KnownTopic);
         await DashboardItemsService.SaveDashboardsForKnownTopic(KnownTopic.KnownTopicId, SelectedDashboards.Select(dashboard => dashboard.DashboardId));
         MudDialog.Close(DialogResult.Ok(KnownTopic));
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
         Snackbar.Add("Unable to save known topic!", Severity.Error);
      }
   }
}