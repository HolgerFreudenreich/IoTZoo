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


using Domain.Pocos;
using Domain.Services.ComponentManagement;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace IotZoo.Components;

public partial class ElectricalComponent : ComponentBase
{

    protected string ClassName = "show";


    [Inject]
    IStockingService StockingService
    {
        get;
        set;
    } = null!;


    [Inject]
    protected IDialogService DialogService
    {
        get;
        set;
    } = null!;

    [Parameter]
    public Stocking Stocking { get; set; } = null!;

    private void OnValueChanged(int newValue)
    {
        Stocking.Quantity = newValue;
        StockingService.UpdateStocking(Stocking);
    }

    private async Task RemoveComponentFromStocking()
    {
        bool? result = await DialogService.ShowMessageBox("Warning!",
                                                          $"Do you want to remove sku '{Stocking.Component.Sku}' from the box '{Stocking.StorageBin.BoxNr}'?",
                                                          yesText: "Yes",
                                                          cancelText: "No");
        if (!result.HasValue)
        {
            return;
        }

        await StockingService.DeleteStocking(Stocking);
        ClassName = "hide";
        await InvokeAsync(StateHasChanged);
    }
}