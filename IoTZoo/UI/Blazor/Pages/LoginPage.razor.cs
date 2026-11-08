using Domain.Interfaces;
using Microsoft.AspNetCore.Components;

namespace IotZoo.Pages;

public partial class LoginPage : ComponentBase
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

    [SupplyParameterFromQuery]
    public string? ReturnUrl
    {
        get;
        set;
    }

    protected string? UnlockCode
    {
        get;
        set;
    }

    protected bool LoginFailed
    {
        get;
        set;
    }

    protected string? ErrorMessage
    {
        get;
        set;
    }

    protected bool IsWaiting
    {
        get;
        set;
    }

    private MudBlazor.MudTextField<string>? _unlockCodeField;

    protected override async Task OnInitializedAsync()
    {
        await AppLock.InitializeAsync();
        if (!AppLock.IsLocked)
        {
            NavigationManager.NavigateTo("/");
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && _unlockCodeField is not null)
        {
            await _unlockCodeField.FocusAsync();
        }
    }

    protected async Task UnlockAsync()
    {
        if (IsWaiting)
        {
            return;
        }

        IsWaiting = true;
        UnlockResult result = await AppLock.UnlockAsync(UnlockCode ?? string.Empty);
        if (result.IsUnlocked)
        {
            NavigationManager.NavigateTo(GetReturnUrl());
            return;
        }

        LoginFailed = true;
        UnlockCode = null;
        await CountdownAsync(result.RetryAfter);
        IsWaiting = false;
    }

    private async Task CountdownAsync(TimeSpan retryAfter)
    {
        for (int seconds = (int)Math.Ceiling(retryAfter.TotalSeconds); seconds > 0; seconds--)
        {
            ErrorMessage = $"Wrong unlock code. Please wait {seconds} seconds before the next attempt.";
            StateHasChanged();
            await Task.Delay(TimeSpan.FromSeconds(1));
        }
        ErrorMessage = "Wrong unlock code. You can try again.";
    }

    private string GetReturnUrl()
    {
        string? returnUrl = ReturnUrl;
        if (string.IsNullOrWhiteSpace(returnUrl)
            || !returnUrl.StartsWith('/')
            || returnUrl.StartsWith("//")
            || returnUrl.StartsWith("/Login", StringComparison.OrdinalIgnoreCase))
        {
            return "/";
        }
        return returnUrl;
    }
}
