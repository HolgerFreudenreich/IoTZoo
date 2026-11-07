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

using Domain.Pocos;
using MailKit.Net.Imap;
using MailKit.Security;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace IotZoo.Components;

public class MailSettingsComponentBase : ComponentBase
{
    [Parameter]
    public MailReceiverConfig MailReceiverConfig { get; set; } = null!;

    [Inject]
    protected ISnackbar Snackbar { get; set; } = null!;

    protected bool TestingConnection { get; set; }

    protected async Task TestConnectionAsync()
    {
        TestingConnection = true;
        try
        {
            using var client = new ImapClient
            {
                ServerCertificateValidationCallback = (s, c, h, e) => true
            };
            await client.ConnectAsync(MailReceiverConfig.HostName, MailReceiverConfig.Port, SecureSocketOptions.SslOnConnect);
            await client.AuthenticateAsync(MailReceiverConfig.UserName ?? string.Empty, MailReceiverConfig.Password ?? string.Empty);
            await client.DisconnectAsync(true);
            Snackbar.Add("IMAP connection successful.", MudBlazor.Severity.Success);
            if (!MailReceiverConfig.Enabled)
            {
                Snackbar.Add("The mail receiver is disabled. Enable it and save the settings to receive mails.", MudBlazor.Severity.Warning);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"IMAP connection failed: {ex.Message}", MudBlazor.Severity.Error);
        }
        finally
        {
            TestingConnection = false;
        }
    }
}

