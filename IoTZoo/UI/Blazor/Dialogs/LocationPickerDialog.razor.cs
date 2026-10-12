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
// Dialog to choose a location (latitude/longitude) on a map.
// --------------------------------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace IotZoo.Dialogs;

public class LocationPickerDialogBase : DialogBase, IAsyncDisposable
{
    [CascadingParameter]
    protected IMudDialogInstance MudDialog { get; set; } = null!;

    [Inject]
    protected IJSRuntime JS { get; set; } = null!;

    [Parameter]
    public double Latitude { get; set; }

    [Parameter]
    public double Longitude { get; set; }

    protected string MapId { get; } = $"location-picker-{Guid.NewGuid():N}";
    protected double SelectedLatitude { get; set; }

    protected double SelectedLongitude { get; set; }

    private DotNetObjectReference<LocationPickerDialogBase>? _dotNetRef;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        SelectedLatitude = Latitude;
        SelectedLongitude = Longitude;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender)
        {
            _dotNetRef = DotNetObjectReference.Create(this);
            await JS.InvokeVoidAsync("locationPicker.create", MapId, Latitude, Longitude, _dotNetRef);
        }
    }

    [JSInvokable]
    public void SetLocation(double latitude, double longitude)
    {
        SelectedLatitude = latitude;
        SelectedLongitude = longitude;
        InvokeAsync(StateHasChanged);
    }

    protected void Confirm()
    {
        MudDialog.Close(DialogResult.Ok((SelectedLatitude, SelectedLongitude)));
    }

    protected void Cancel()
    {
        MudDialog.Cancel();
    }

    public async ValueTask DisposeAsync()
    {
        await JS.InvokeVoidAsync("locationPicker.dispose", MapId);
        _dotNetRef?.Dispose();
    }
}
