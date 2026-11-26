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

using Domain.Interfaces;
using IotZoo.Pages;
using Microsoft.AspNetCore.Components;

namespace IotZoo.Components;

public partial class UnlockGuard : ComponentBase
{
    [Inject]
    protected IAppLockService AppLock
    {
        get;
        set;
    } = null!;

    [Inject]
    protected NavigationManager NavigationManager
    {
        get;
        set;
    } = null!;

    [Parameter]
    public Type? PageType
    {
        get;
        set;
    }

    [Parameter]
    public RenderFragment? ChildContent
    {
        get;
        set;
    }

    private bool allowed;

    protected override async Task OnParametersSetAsync()
    {
        await AppLock.InitializeAsync();
        if (!AppLock.IsLocked || PageType == typeof(LoginPage))
        {
            allowed = true;
            return;
        }

        allowed = false;
        string returnUrl = "/" + NavigationManager.ToBaseRelativePath(NavigationManager.Uri);
        NavigationManager.NavigateTo($"/Login?returnUrl={Uri.EscapeDataString(returnUrl)}", replace: true);
    }
}
