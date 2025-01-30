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
using IotZoo.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;


public class KnownTopicCatalogBase : ComponentBase
{
   protected MudTable<KnownTopic> MudTable = null!;

   [Parameter]
   public KnownTopic SelectedKnownTopic { get; set; } = null!;

   [Inject]
   private IKnownTopicsCrudService KnownTopicsService
   {
      get;
      set;
   } = null!;

   [Inject]
   protected IDataTransferService DataTransferService { get; set; } = null!;

   [Inject]
   protected IDialogService DialogService { get; set; } = null!;

   [Inject]
   protected ISearchHelper SearchHelper { get; set; } = null!;

   protected List<KnownTopic>? KnownTopics
   {
      get;
      set;
   } = new();

   [CascadingParameter]
   protected IMudDialogInstance MudDialog { get; set; } = null!;

   protected bool IsEditorOpen
   {
      get;
      set;
   }

   protected override async Task OnInitializedAsync()
   {
      await base.OnInitializedAsync();
      await LoadData();
   }

   protected async Task LoadData()
   {
      KnownTopics = await KnownTopicsService.GetKnownTopics();
      await InvokeAsync(StateHasChanged);
   }

   public async Task OpenKnowTopicEditor()
   {
      await OpenKnownTopicEditor(new KnownTopic());
   }

   protected virtual DialogOptions GetDialogOptions()
   {
      DialogOptions options = new DialogOptions
      {
         BackdropClick = false,
         CloseButton = false,
         MaxWidth = MaxWidth.Large,
         CloseOnEscapeKey = false,
      };

      return options;
   }

   protected void RowClickEvent(TableRowClickEventArgs<KnownTopic> tableRowClickEventArgs)
   {
      SelectedKnownTopic = tableRowClickEventArgs?.Item!;
   }

   protected string SelectedRowClassFunc(KnownTopic knownTopic,
                                         int rowNumber)
   {
      if (MudTable.SelectedItem != null && MudTable.SelectedItem.Equals(knownTopic))
      {
         return "selected";
      }
      return string.Empty;
   }

   public async Task OpenKnownTopicEditor(KnownTopic knownTopic)
   {
      try
      {
         var options = GetDialogOptions();

         var parameters = new DialogParameters { ["KnownTopic"] = knownTopic };
         IsEditorOpen = true;
         var dialog = await this.DialogService.ShowAsync<KnownTopicEditor>("Edit Known Topic",
                                                                           parameters,
                                                                           options);
         var result = await dialog.Result;
         var newKnownTopic = result!.Data as KnownTopic;
         if (newKnownTopic != null)
         {
            this.KnownTopics!.Add(newKnownTopic);
         }
      }
      finally
      {
         IsEditorOpen = false;
         await LoadData();
      }
   }

   protected void Cancel()
   {
      MudDialog.Cancel();
   }

   protected void Take()
   {
      MudDialog.Close(DialogResult.Ok(SelectedKnownTopic));
   }
}