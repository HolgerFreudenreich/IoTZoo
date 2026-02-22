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

using Domain.Pocos;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Data;
using System.Reflection;

public class PinEditorBase : EditorBase
{
    [Parameter]

    public ConnectedDevice ConnectedDevice { get; set; } = null!;
    public bool IsEditorOpen { get; private set; }

    protected override async Task OnInitializedAsync()
    {
        DialogTitle = "Edit Device Properties " + ConnectedDevice.DeviceType;
        IsNewRecord = false;

        HashCode = GetHashCodeBase64(ConnectedDevice);
        await base.OnInitializedAsync();
    }

    protected override async Task Cancel()
    {
        await base.Cancel(ConnectedDevice);
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    protected override async Task Save()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        try
        {
            Snackbar.Clear();
            TrimTextFields(ConnectedDevice);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
            Snackbar.Add("Unable to save!", Severity.Error);
        }
        MudDialog.Close(DialogResult.Ok(ConnectedDevice));
    }

    /// <summary>
    /// Add a new MQTT datasource to the device. The datasource editor will be opened as a dialog and the new datasource
    /// will be added to the device if the user clicks "Save" in the datasource editor.
    /// </summary>
    /// <returns></returns>
    public async Task OpenDataSourceEditorAsync()
    {
        try
        {
            var options = GetDialogOptions();

            var parameters = new DialogParameters { ["DataSource"] = new DataSource() };
            IsEditorOpen = true;
            var dialog = await this.DialogService.ShowAsync<DataSourceEditor>("Add DataSource",
                                                                        parameters,
                                                                        options);
            var result = await dialog.Result;
            if (null != result)
            {
                if (null != result.Data)
                {
                    var dataSource = (DataSource)result.Data;
                    if (null == ConnectedDevice.DataSources)
                    {
                        ConnectedDevice.DataSources = new List<DataSource>();
                    }
                    ConnectedDevice.DataSources.Add(dataSource);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
        }
        finally
        {
            IsEditorOpen = false;
        }
    }

    protected async Task RemoveDataSource(string topic)
    {
        if (string.IsNullOrEmpty(topic))
        {
            return;
        }
        if (ConnectedDevice?.DataSources == null)
        {
            return;
        }
        // Find the first matching data source.
        DataSource? toRemove = null;
        foreach (var ds in ConnectedDevice.DataSources)
        {
            // Use property name 'Topic' as used in markup.
            if (string.Equals(ds?.Topic, topic, StringComparison.Ordinal))
            {
                toRemove = ds;
                break;
            }
        }

        if (toRemove != null)
        {
            // Attempt to remove; DataSources is assumed to be a mutable collection (e.g., List<T>).
            // Use dynamic removal to avoid assumptions about the concrete collection type.
            try
            {
                ConnectedDevice.DataSources.Remove(toRemove);
            }
            catch
            {
            }

            await InvokeAsync(StateHasChanged);
        }
    }

    protected async Task EditDataSource(string topic)
    {
        try
        {
            if (string.IsNullOrEmpty(topic))
            {
                return;
            }
            if (ConnectedDevice?.DataSources == null)
            {
                return;
            }
            // Find the first matching data source.
            DataSource? toEdit = null;
            foreach (var ds in ConnectedDevice.DataSources)
            {
                // Use property name 'Topic' as used in markup.
                if (string.Equals(ds?.Topic, topic, StringComparison.Ordinal))
                {
                    toEdit = ds;
                    break;
                }
            }

            var options = GetDialogOptions();

            var parameters = new DialogParameters { ["DataSource"] = toEdit };
            IsEditorOpen = true;
            var dialog = await this.DialogService.ShowAsync<DataSourceEditor>("Edit DataSource",
                                                                        parameters,
                                                                        options);
            var result = await dialog.Result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
        }
        finally
        {
            IsEditorOpen = false;
        }
    }
}