using Domain.Interfaces;
using Domain.Pocos;
using Domain.Services.MQTT;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;
using System.Reflection;

namespace Domain.Services
{
    public class MailReceiverService : MqttPublisher
    {

        public MailReceiverConfig MailReceiverConfig { get; private set; }

        private ImapClient client = null!;
        private ImapFolder inbox = null!;
        private CancellationTokenSource cancellationTokenIdleLoop = new();
        private readonly SemaphoreSlim imapLock = new SemaphoreSlim(1, 1);
        private Task idleTask = null!;


        public MailReceiverService(ILogger<MailReceiverService> logger,
                                   MailReceiverConfig mailReceiverConfig,
                                   IDataTransferService dataTransferService) : base(logger, dataTransferService)
        {
            MailReceiverConfig = mailReceiverConfig;

            _ = StartIdleAsync();
        }

        private async Task IdleLoopAsync()
        {

            using (var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(25)))
            using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationTokenIdleLoop.Token))
            {
                try
                {
                    await client.IdleAsync(linkedCts.Token);
                }
                catch (OperationCanceledException)
                {
                    // normal
                }
                catch (Exception exception)
                {
                    Logger.LogError(exception, $"{MethodBase.GetCurrentMethod()} failed!");
                    await Task.Delay(2000);
                }
            }
        }

        public async Task StartIdleAsync()
        {
            try
            {
                client = new ImapClient();

                await client.ConnectAsync(MailReceiverConfig.HostName, 993, SecureSocketOptions.SslOnConnect);
                await client.AuthenticateAsync(MailReceiverConfig.UserName, MailReceiverConfig.Password);

                inbox = (ImapFolder)client.Inbox;
                await inbox.OpenAsync(FolderAccess.ReadOnly);

                inbox.CountChanged -= Inbox_CountChangedAsync;
                inbox.CountChanged += Inbox_CountChangedAsync;

                idleTask = IdleLoopAsync();
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, $"{MethodBase.GetCurrentMethod()} failed!");
            }
        }

        private async void Inbox_CountChangedAsync(object? sender, EventArgs e)
        {
            try
            {
                cancellationTokenIdleLoop.Cancel();

                // Short delay so that MailKit can end the IDLE session.
                await Task.Delay(250);

                HeaderList headers;
                // Access synchronized on the client
                await imapLock.WaitAsync();
                try
                {
                    if (sender is not ImapFolder folder || !client.IsConnected)
                    {
                        return;
                    }
                    string topic = $"{DataTransferService.NamespaceName}/mail_receiver/{MailReceiverConfig.UserName}/mail_count";
                    await MqttClient.PublishAsync(topic, Convert.ToString(folder.Count));
                    headers = await folder.GetHeadersAsync(folder.Count - 1);
                }
                finally
                {
                    imapLock.Release();
                }

                if (null != headers)
                {
                    string subject = headers[HeaderId.Subject];
                    string topic = $"{DataTransferService.NamespaceName}/mail_receiver/{MailReceiverConfig.UserName}/subject";
                    await MqttClient.PublishAsync(topic, subject);
                }

                // Restart with a new token and IdleAsync
                cancellationTokenIdleLoop = new CancellationTokenSource();
                idleTask = IdleLoopAsync();
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, $"{MethodBase.GetCurrentMethod()} failed!");
            }
        }
    }
}