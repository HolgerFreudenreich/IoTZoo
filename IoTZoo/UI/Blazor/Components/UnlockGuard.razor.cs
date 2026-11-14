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
