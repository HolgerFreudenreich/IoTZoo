using Domain.Interfaces;
using Domain.Interfaces.Crud;
using Domain.Pocos;
using Domain.Services.MQTT;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace Domain.Services
{
    public class MailReceiverService : MqttPublisher, IAsyncDisposable
    {
        private MailReceiverConfig MailReceiverConfig { get; }
        private IProjectCrudService ProjectCrudService { get; }
        private readonly SemaphoreSlim imapLock = new(1, 1);

        private ImapClient? imapClient;
        private ImapFolder? imapFolderInbox;
        private CancellationTokenSource? idleCancellationToken;
        private Task? idleTask;
        private CancellationTokenSource serviceCancellationToken = new();

        private volatile bool _mailArrived;

        public MailReceiverService(ILogger<MailReceiverService> logger,
            MailReceiverConfig mailReceiverConfig,
            IDataTransferService dataTransferService,
            IProjectCrudService projectCrudService)
            : base(logger, dataTransferService)
        {
            MailReceiverConfig = mailReceiverConfig;
            ProjectCrudService = projectCrudService;
            _ = Task.Run(() => StartIdleAsync(serviceCancellationToken.Token));
        }

        private async Task ConnectAndLoginAsync()
        {
            await imapLock.WaitAsync();
            try
            {
                if (imapClient?.IsConnected == true)
                {
                    await imapClient.DisconnectAsync(true);
                }
                imapClient?.Dispose();

                imapClient = new ImapClient
                {
                    ServerCertificateValidationCallback = (s, c, h, e) => true
                };

                Logger.LogInformation("Connecting to IMAP server {host} as {user}", MailReceiverConfig.HostName, MailReceiverConfig.UserName);
                await imapClient.ConnectAsync(MailReceiverConfig.HostName, 993, SecureSocketOptions.SslOnConnect);
                await imapClient.AuthenticateAsync(MailReceiverConfig.UserName, MailReceiverConfig.Password);

                imapFolderInbox = (ImapFolder)imapClient.Inbox;
                await imapFolderInbox.OpenAsync(FolderAccess.ReadOnly);

                imapFolderInbox.CountChanged -= OnCountChanged;
                imapFolderInbox.CountChanged += OnCountChanged;

                Logger.LogInformation("Connected and authenticated as {user}", MailReceiverConfig.UserName);
            }
            finally
            {
                imapLock.Release();
            }
        }

        private async Task StartIdleAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (imapClient == null || !imapClient.IsConnected || !imapClient.IsAuthenticated)
                    {
                        await ConnectAndLoginAsync();
                    }
                    idleCancellationToken = new CancellationTokenSource();
                    idleTask = IdleLoopAsync(idleCancellationToken.Token);
                    await idleTask;
                }
                catch (OperationCanceledException)
                {
                    Logger.LogInformation("IMAP IDLE loop cancelled for {user}", MailReceiverConfig.UserName);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Idle loop crashed, reconnecting...");
                    await Task.Delay(TimeSpan.FromSeconds(10), token);
                }
            }

            Logger.LogInformation("MailReceiverService stopped for {user}", MailReceiverConfig.UserName);
        }

        private async Task IdleLoopAsync(CancellationToken token)
        {
            Logger.LogInformation("Entering IDLE loop for {user}", MailReceiverConfig.UserName);

            while (!token.IsCancellationRequested)
            {
                try
                {
                    using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(10));
                    using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, timeout.Token);

                    await imapLock.WaitAsync(linked.Token);
                    try
                    {
                        if (imapClient?.IsConnected == true)
                            await imapClient.IdleAsync(linked.Token);
                        else
                            break;
                    }
                    finally
                    {
                        imapLock.Release();
                    }
                }
                catch (OperationCanceledException)
                {
                    // Normal Timeout or Cancel
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Error in IDLE, reconnecting soon...");
                    await Task.Delay(TimeSpan.FromSeconds(5), token);
                    break;
                }

                if (_mailArrived)
                {
                    _mailArrived = false;
                    await HandleNewMailAsync();
                }

                await Task.Delay(1000, token);
            }

            Logger.LogInformation("Exiting IDLE loop for {user}", MailReceiverConfig.UserName);
        }

        private void OnCountChanged(object? sender, EventArgs e)
        {
            Logger.LogInformation("New mail event detected for {user}", MailReceiverConfig.UserName);
            _mailArrived = true;

            // Break IDLE so that the loop reacts.
            idleCancellationToken?.Cancel();
        }

        private async Task HandleNewMailAsync()
        {
            Logger.LogInformation("Processing new mail for {user}", MailReceiverConfig.UserName);

            await imapLock.WaitAsync();
            try
            {
                if (imapClient == null || imapFolderInbox == null || !imapClient.IsConnected)
                {
                    return;
                }
                var projects = await ProjectCrudService.LoadProjects();
                string baseTopic = $"{DataTransferService.NamespaceName}/mail_receiver/{MailReceiverConfig.UserName}";

                int count = imapFolderInbox.Count;
                await MqttClient.PublishAsync($"{baseTopic}/mail_count", count.ToString());

                foreach (var project in projects)
                {
                    string topic = $"{DataTransferService.NamespaceName}/{project.ProjectName}/mail_receiver/{MailReceiverConfig.UserName}/mail_count";
                    await MqttClient.PublishAsync(topic, count.ToString());
                }

                if (count > 0)
                {
                    var headers = await imapFolderInbox.GetHeadersAsync(count - 1);
                    string subject = headers[HeaderId.Subject] ?? "(no subject)";

                    await MqttClient.PublishAsync($"{baseTopic}/subject", subject);

                    foreach (var project in projects)
                    {
                        string topic = $"{DataTransferService.NamespaceName}/{project.ProjectName}/mail_receiver/{MailReceiverConfig.UserName}/subject";
                        await MqttClient.PublishAsync(topic, subject);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "HandleNewMailAsync failed for {user}", MailReceiverConfig.UserName);
            }
            finally
            {
                imapLock.Release();
            }
        }

        public async Task StopAsync()
        {
            Logger.LogInformation("Stopping MailReceiverService for {user}", MailReceiverConfig.UserName);

            serviceCancellationToken.Cancel();
            idleCancellationToken?.Cancel();

            await imapLock.WaitAsync();
            try
            {
                if (imapClient?.IsConnected == true)
                {
                    await imapClient.DisconnectAsync(true);
                }
                imapClient?.Dispose();
            }
            finally
            {
                imapLock.Release();
            }
        }

        public async ValueTask DisposeAsync()
        {
            await StopAsync();
            imapLock.Dispose();
            serviceCancellationToken.Dispose();
            idleCancellationToken?.Dispose();
        }
    }
}