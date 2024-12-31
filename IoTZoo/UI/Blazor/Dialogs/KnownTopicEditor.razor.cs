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
      await base.OnInitializedAsync();
   }

   [Inject]
   private IKnownTopicsCrudService KnownTopicsService
   {
      get;
      set;
   } = null!;

   public KnownTopicEditorBase()
   {
   }

   protected override async Task Cancel()
   {
      await Cancel(KnownTopic);
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
         MudDialog.Close(DialogResult.Ok(KnownTopic));
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
         Snackbar.Add("Unable to save known topic!", Severity.Error);
      }
   }
}