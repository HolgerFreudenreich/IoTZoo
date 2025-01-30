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
// Page for managing boxes/totes. Each box can contain one or more components.
// It would also be conceivable to put boxes in boxes.
// --------------------------------------------------------------------------------------------------------------------

using Domain.Interfaces.Crud;
using Domain.Pocos;
using IotZoo.Dialogs;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace IotZoo.Pages.ComponentManagement;

public class BoxesPageBase : PageBase
{
    [Inject]
    private IStorageBinCrudService BoxesService
    {
        get;
        set;
    } = null!;

    protected List<StorageBin>? Boxes { get; set; } = new();

    public async Task OpenEditor()
    {
        await OpenEditor(new StorageBin());
    }

    protected override void OnInitialized()
    {
        DataTransferService.CurrentScreen = ScreenMode.Boxes;
        base.OnInitialized();
    }

    protected async Task OpenEditor(StorageBin storageBin)
    {
        try
        {
            var options = GetDialogOptions();

            var parameters = new DialogParameters { ["Box"] = storageBin };
            IsEditorOpen = true;
            var dialog = await DialogService.ShowAsync<BoxEditor>("Box Editor",
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
        Boxes = await BoxesService.GetStorageBins();
        await InvokeAsync(StateHasChanged);
    }

    protected async Task EditAsync(StorageBin component)
    {
        await OpenEditor(component);
    }

    protected async Task Delete(StorageBin storageBin)
    {
        bool? result = await DialogService.ShowMessageBox("Delete",
            $"Do you want to delete the box with Label {storageBin.BoxNr}?",
            yesText: "Yes", cancelText: "No");
        if (!result.HasValue)
        {
            return;
        }
        await BoxesService.DeleteStorageBin(storageBin);
        await LoadData();
    }
}