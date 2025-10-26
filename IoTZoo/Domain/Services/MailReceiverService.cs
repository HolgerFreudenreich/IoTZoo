using Domain.Interfaces;
using Domain.Pocos;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;
using MQTTnet;
using System.Reflection;

namespace Domain.Services
{
    public class MailReceiverService
    {
        protected ILogger Logger { get; }
        public MailReceiverConfig MailReceiverConfig { get; private set; }

        private ImapClient client = null!;
        private ImapFolder inbox = null!;
        private CancellationTokenSource cancellationTokenIdleLoop = new();
        private readonly SemaphoreSlim imapLock = new SemaphoreSlim(1, 1);
        private Task idleTask = null!;

        protected IDataTransferService DataTransferService { get; }

        protected IMqttClient MqttClient
        {
            get;
            set;
        } = null!;


        public MailReceiverService(ILogger<MailReceiverService> logger,
                                   MailReceiverConfig mailReceiverConfig,
                                   IDataTransferService dataTransferService)
        {
            Logger = logger;
            MailReceiverConfig = mailReceiverConfig;
            DataTransferService = dataTransferService;

            _ = StartIdleAsync();
            _ = InitMqttClientAsync();
        }

        private async Task InitMqttClientAsync()
        {
            try
            {
                var factory = new MqttClientFactory();
                MqttClient = factory.CreateMqttClient();

                var mqttClientOptions = new MqttClientOptionsBuilder().WithTcpServer(DataTransferService.MqttBrokerSettings.Ip,
                                                                                     DataTransferService.MqttBrokerSettings.Port).Build();
                MqttClient.DisconnectedAsync -= MqttClient_DisconnectedAsync;
                MqttClient.DisconnectedAsync += MqttClient_DisconnectedAsync;
                MqttClientConnectResult connectionResult = await MqttClient.ConnectAsync(mqttClientOptions);
                if (connectionResult.ResultCode == MqttClientConnectResultCode.Success)
                {
                    Logger.LogInformation("MQTT connected!");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
            }
        }

        private async Task MqttClient_DisconnectedAsync(MqttClientDisconnectedEventArgs args)
        {
            try
            {
                Logger.LogWarning("MQTT disconnected! Try to reconnect...");
                await Task.Delay(2000);
                await MqttClient.ReconnectAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"{MethodBase.GetCurrentMethod()} failed!");
            }
        }

        public void Dispose()
        {
            MqttClient.DisconnectedAsync -= MqttClient_DisconnectedAsync;
        }

        private async Task IdleLoopAsync()
        {
            while (client.IsConnected)
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