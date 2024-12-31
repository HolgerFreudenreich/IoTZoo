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

public class RuleEditorBase : EditorBase
{
   [Parameter]
   public Rule Rule { get; set; } = null!;

   protected override async Task OnInitializedAsync()
   {
      await base.OnInitializedAsync();
      Rule.NamespaceName = DataTransferService.NamespaceName;
      if (Rule.RuleId <= 0)
      {
         DialogTitle = "Add Rule";
         Rule.Enabled = true;
         IsNewRecord = true;
      }
      else
      {
         DialogTitle = "Edit Rule";
         IsNewRecord = false;
      }
      if (null != DataTransferService.SelectedProject)
      {
         Rule.ProjectName = DataTransferService.SelectedProject.ProjectName;
         DialogTitle += $" for Project {Rule.ProjectName}";
      }

      knownInboundTopics = await KnownTopicsService.GetKnownTopicsByProjectName(Rule.ProjectName, new List<MessageDirection> { MessageDirection.Inbound, MessageDirection.Internal });
      knownOutboundTopics = await KnownTopicsService.GetKnownTopicsByProjectName(Rule.ProjectName, new List<MessageDirection> { MessageDirection.Outbound, MessageDirection.Internal });

      if (null != Rule.SourceTopic)
      {
         KnownSourceTopic.Topic = Rule.SourceTopic;
      }
      if (null != Rule.TargetTopic)
      {
         KnownTargetTopic.Topic = Rule.TargetTopic;
      }
      HashCode = GetHashCodeBase64(Rule);
   }

   [Inject]
   private IRulesCrudService RulesService
   {
      get;
      set;
   } = null!;

   [Inject]
   private IKnownTopicsCrudService KnownTopicsService
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

   /// <summary>
   /// Catalog for the field SourceTopic.
   /// </summary>
   List<KnownTopic> knownInboundTopics = new();

   /// <summary>
   /// Catalog for the field TargetTopic.
   /// </summary>
   List<KnownTopic> knownOutboundTopics = new();

   protected KnownTopic KnownSourceTopic
   {
      get;
      set;
   } = new();

   protected KnownTopic KnownTargetTopic
   {
      get;
      set;
   } = new();

   public RuleEditorBase()
   {
   }

   protected override async Task Cancel()
   {
      await base.Cancel(Rule);
   }

   protected async Task OpenEditorDialogNewKnownTopicAsSourceTopic()
   {
      await OpenEditorDialogNewKnownTopicAsSourceTopic(new KnownTopic());
   }

   protected async Task OpenEditorDialogNewKnownTopicAsSourceTopic(KnownTopic knownTopic)
   {
      try
      {
         var options = GetDialogOptions();

         var parameters = new DialogParameters { ["KnownTopic"] = knownTopic };

         var dialog = await this.DialogService.ShowAsync<KnownTopicEditor>("Edit Known Topic",
                                                                           parameters,
                                                                           options);
         var result = await dialog.Result;
         var newKnownTopic = result!.Data as KnownTopic;
         if (newKnownTopic != null)
         {
            knownInboundTopics.Add(newKnownTopic);
            Rule.SourceTopic = newKnownTopic.Topic;
            KnownSourceTopic = newKnownTopic;
         }
      }
      finally
      {
         await InvokeAsync(StateHasChanged);
      }
   }

   protected async Task OpenKnownTopicCatalogForSource()
   {
      try
      {
         var result = await OpenKnownTopicCatalog();
         if (result != null)
         {
            var selectedKnownTopic = result;

            KnownSourceTopic = selectedKnownTopic;
            Rule.SourceTopic = selectedKnownTopic.Topic;
         }
      }
      finally
      {
         await InvokeAsync(StateHasChanged);
      }
   }

   protected async Task OpenKnownTopicCatalogForTarget()
   {
      try
      {
         var result = await OpenKnownTopicCatalog();
         if (result != null)
         {
            var selectedKnownTopic = result;

            KnownTargetTopic = selectedKnownTopic;
            Rule.TargetTopic = selectedKnownTopic.Topic;
         }
      }
      finally
      {
         await InvokeAsync(StateHasChanged);
      }
   }

   protected async Task<KnownTopic?> OpenKnownTopicCatalog()
   {
      var options = GetDialogOptions();

      var dialog = await this.DialogService.ShowAsync<KnownTopicCatalog>("Known Topic Catalog",
                                                                         new DialogParameters(),
                                                                         options);
      DialogResult? result = await dialog.Result;
      return result?.Data as KnownTopic;
   }

   protected async Task OpenEditorDialogNewKnownTopicAsTargetTopic()
   {
      await OpenEditorDialogNewKnownTopicAsTargetTopic(new KnownTopic());
   }

   protected async Task OpenEditorDialogNewKnownTopicAsTargetTopic(KnownTopic knownTopic)
   {
      try
      {
         var options = GetDialogOptions();

         var parameters = new DialogParameters { ["KnownTopic"] = knownTopic };

         var dialog = await this.DialogService.ShowAsync<KnownTopicEditor>("Edit Known Topic",
                                                                           parameters,
                                                                           options);
         var result = await dialog.Result;
         var newKnownTopic = result!.Data as KnownTopic;
         if (newKnownTopic != null)
         {
            knownOutboundTopics.Add(newKnownTopic);
            Rule.TargetTopic = newKnownTopic.Topic;
            KnownTargetTopic = newKnownTopic;
         }
      }
      finally
      {
         await InvokeAsync(StateHasChanged);
      }
   }

   protected override async Task Save()
   {
      try
      {
         Snackbar.Clear();
         Rule.SourceTopic = KnownSourceTopic.Topic;
         Rule.TargetTopic = KnownTargetTopic.Topic;
         var knownSourceTopic = await KnownTopicsService.GetKnownTopicByTopicName(KnownSourceTopic.ProjectName, KnownSourceTopic.Topic);
         if (knownSourceTopic != null)
         {
            if (!string.IsNullOrEmpty(knownSourceTopic.ProjectName))
            {
               var project = await ProjectService.LoadProjectByName(knownSourceTopic.ProjectName);
               if (project != null)
               {
                  Rule.ProjectName = project.ProjectName;
               }
            }
         }

         TrimTextFields(Rule);
         if (ValidateFields())
         {
            await RulesService.Save(Rule);
            MudDialog.Close(DialogResult.Ok(Rule));
         }
      }
      catch (Exception ex)
      {
         Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
         Snackbar.Add("Unable to save rule!", Severity.Error);
      }
   }

   private bool ValidateFields()
   {
      bool result = true;
      if (string.IsNullOrEmpty(Rule.SourceTopic))
      {
         Snackbar.Add("SourceTopic is required!", Severity.Error);
         result = false;
      }
      if (string.IsNullOrEmpty(Rule.TargetTopic))
      {
         Snackbar.Add("TargetTopic is required!", Severity.Error);
         result = false;
      }
      if (!string.IsNullOrEmpty(Rule.SourceTopic) && !string.IsNullOrEmpty(Rule.TargetTopic))
      {
         if (Rule.SourceTopic == Rule.TargetTopic)
         {
            Snackbar.Add("Endless loop detected. SourceTopic must differ from TargetTopic!", Severity.Error);
            return false;
         }
      }
      return result;
   }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
   protected async Task<IEnumerable<KnownTopic>> SearchSourceTopic(string value, CancellationToken token)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
   {
      if (string.IsNullOrEmpty(value))
      {
         return this.knownInboundTopics;
      }

      IEnumerable<KnownTopic> matching = from data in this.knownInboundTopics
                                         where
                                         data.Topic.Contains(value, StringComparison.InvariantCultureIgnoreCase)
                                         select data;
      return matching;
   }

   protected async Task<IEnumerable<KnownTopic>> SearchTargetTopic(string value, CancellationToken token)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
   {
      if (string.IsNullOrEmpty(value))
      {
         return this.knownOutboundTopics;
      }

      IEnumerable<KnownTopic> matching = from data in this.knownOutboundTopics
                                         where
                                         data.Topic.Contains(value, StringComparison.InvariantCultureIgnoreCase)
                                         select data;
      return matching;
   }
}