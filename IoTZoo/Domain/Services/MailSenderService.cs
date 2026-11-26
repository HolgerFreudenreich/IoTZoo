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
using Domain.Interfaces.Crud;
using Domain.Pocos;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Domain.Services;

public class MailSenderService
{
    public const string SendTopic = "mail_sender/send";

    private readonly ISettingsCrudService settings;
    private readonly IKnownTopicsCrudService knownTopics;
    private readonly IDataTransferService dataTransfer;
    private readonly ILogger<MailSenderService> logger;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly Regex ReadStart = new(@"(?<!\w)Read\s*\(", RegexOptions.CultureInvariant);
    private static readonly Regex ReadCall = new(@"Read\s*\(\s*'(?<topic>[^']+)'\s*\)", RegexOptions.CultureInvariant);

    public MailSenderService(ISettingsCrudService settings, IKnownTopicsCrudService knownTopics,
        IDataTransferService dataTransfer, ILogger<MailSenderService> logger)
    {
        this.settings = settings;
        this.knownTopics = knownTopics;
        this.dataTransfer = dataTransfer;
        this.logger = logger;
    }

    public async Task HandleAsync(string? payload, bool retained)
    {
        if (retained)
        {
            logger.LogWarning("Ignoring retained mail sender command");
            return;
        }

        bool sendStarted = false;
        bool sent = false;
        MailSenderConfig? config = null;
        try
        {
            config = await settings.GetObject(SettingCategory.Mail, SettingKey.MailSenderConfig) as MailSenderConfig;
            if (config?.Enabled != true)
            {
                logger.LogWarning("Mail sender is disabled or not configured");
                return;
            }
            MailSenderRequest? request;
            try
            {
                request = JsonSerializer.Deserialize<MailSenderRequest>(payload ?? string.Empty, JsonOptions);
            }
            catch (JsonException exception)
            {
                logger.LogError(exception, "Invalid mail sender command: publish a JSON object with an email property to {topic}; no email was sent", SendTopic);
                return;
            }
            var email = request?.Email ?? throw new ArgumentException("The email object is required.");
            ValidateSmtp(email.Smtp, config);
            if (email.Content != null)
                await ResolveContentAsync(email.Content);
            var message = CreateMessage(email, config);
            using var smtp = CreateSmtpClient();
            await smtp.ConnectAsync(config.HostName, config.Port, GetEncryption(config.Encryption));
            if (!string.IsNullOrWhiteSpace(config.UserName))
                await smtp.AuthenticateAsync(config.UserName, config.Password);

            var sendRequest = new SmtpSendRequest(message);
            if (email.Options?.RequestDeliveryReceipt == true)
            {
                if (!smtp.Capabilities.Contains(SmtpCapability.Dsn))
                    throw new InvalidOperationException("The SMTP server does not support delivery status notifications.");
                sendRequest.DeliveryStatusNotifications = DeliveryStatusNotification.Success | DeliveryStatusNotification.Failure;
            }

            using var imap = CreateImapClient();
            IMailFolder? sentFolder = null;
            if (email.Options?.SaveToSentFolder == true)
            {
                if (string.IsNullOrWhiteSpace(config.ImapHostName))
                    throw new InvalidOperationException("An IMAP host is required to save sent messages.");
                await imap.ConnectAsync(config.ImapHostName, config.ImapPort, SecureSocketOptions.SslOnConnect);
                await imap.AuthenticateAsync(config.UserName, config.Password);
                sentFolder = imap.GetFolder(SpecialFolder.Sent)
                    ?? throw new InvalidOperationException("The IMAP server did not advertise a Sent folder.");
            }

            sendStarted = true;
            await smtp.SendAsync(sendRequest);
            sent = true;
            logger.LogInformation("Mail {id} accepted by SMTP server", email.Id);
            if (sentFolder != null)
            {
                try
                {
                    await sentFolder.AppendAsync(message, MessageFlags.Seen);
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Mail {id} was sent, but could not be saved to the Sent folder; do not retry the send", email.Id);
                    return;
                }
                await imap.DisconnectAsync(true);
            }
            await smtp.DisconnectAsync(true);
        }
        catch (MailKit.Security.AuthenticationException exception)
        {
            logger.LogError(exception, "Mail server authentication failed; check the mail sender settings");
            if (config != null)
                await DisableAfterAuthenticationFailureAsync(config);
        }
        catch (Exception exception)
        {
            if (sent)
                logger.LogError(exception, "Mail was accepted by SMTP, but a subsequent operation failed; do not retry the send");
            else if (sendStarted)
                logger.LogError(exception, "Mail sender command failed during SMTP send; delivery may be uncertain if the SMTP connection was interrupted");
            else
                logger.LogError(exception, "Mail sender command failed before SMTP send; no email was sent");
        }
    }

    public async Task<bool> DisableAfterAuthenticationFailureAsync(MailSenderConfig attemptedConfig)
    {
        try
        {
            bool disabled = await settings.DisableMailSenderIfMatches(attemptedConfig);
            if (disabled)
                logger.LogWarning("Mail sender disabled after authentication failure; correct the credentials and re-enable it in Settings");
            else
                logger.LogWarning("Mail sender settings were not disabled because they were already disabled, missing, or changed during authentication");
            return disabled;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unable to persist mail sender deactivation after authentication failure");
            return false;
        }
    }

    protected virtual SmtpClient CreateSmtpClient() => new();
    protected virtual ImapClient CreateImapClient() => new();

    public async Task ResolveContentAsync(MailContent content)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        string? subject = await ResolveTextAsync(content.Subject, values);
        string? text = await ResolveTextAsync(content.Text, values);
        string? html = await ResolveTextAsync(content.Html, values);
        content.Subject = subject;
        content.Text = text;
        content.Html = html;
    }

    private async Task<string?> ResolveTextAsync(string? input, Dictionary<string, string> values)
    {
        if (input == null) return null;
        var output = new StringBuilder();
        int position = 0;
        while (true)
        {
            var start = ReadStart.Match(input, position);
            if (!start.Success)
            {
                output.Append(input, position, input.Length - position);
                return output.ToString();
            }
            var call = ReadCall.Match(input, start.Index);
            if (!call.Success || call.Index != start.Index)
                throw new ArgumentException("Invalid Read expression in mail content; use Read('namespace/project/topic').");
            string fullTopic = call.Groups["topic"].Value;
            if (!values.TryGetValue(fullTopic, out var value))
            {
                var parts = fullTopic.Split('/', 3);
                if (parts.Length != 3 || parts.Any(string.IsNullOrWhiteSpace) ||
                    !string.Equals(parts[0], dataTransfer.NamespaceName, StringComparison.Ordinal))
                    throw new ArgumentException($"Invalid or unknown namespace in mail Read topic '{fullTopic}'.");
                var knownTopic = await knownTopics.GetKnownTopicByTopicName(parts[1], parts[2]);
                if (knownTopic?.LastPayload == null)
                    throw new InvalidOperationException($"No LastPayload is available for mail Read topic '{fullTopic}'.");
                value = knownTopic.LastPayload;
                values.Add(fullTopic, value);
            }
            output.Append(input, position, start.Index - position);
            output.Append(value);
            position = call.Index + call.Length;
        }
    }

    public static SecureSocketOptions GetEncryption(string encryption) => encryption.ToUpperInvariant() switch
    {
        "STARTTLS" => SecureSocketOptions.StartTls,
        "SSL" or "TLS" => SecureSocketOptions.SslOnConnect,
        _ => throw new ArgumentException("SMTP encryption must be STARTTLS or SSL.")
    };

    private static void ValidateSmtp(MailSmtpData? smtp, MailSenderConfig config)
    {
        if (smtp == null) return;
        if ((!string.IsNullOrEmpty(smtp.Host) && !string.Equals(smtp.Host, config.HostName, StringComparison.OrdinalIgnoreCase)) ||
            (smtp.Port.HasValue && smtp.Port != config.Port) ||
            (!string.IsNullOrEmpty(smtp.Encryption) && !string.Equals(smtp.Encryption, config.Encryption, StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrEmpty(smtp.Authentication?.Username) && smtp.Authentication.Username != config.UserName) ||
            (!string.IsNullOrEmpty(smtp.Authentication?.PasswordRef) && smtp.Authentication.PasswordRef != config.PasswordRef))
            throw new ArgumentException("SMTP parameters do not match the configured mail sender.");
    }

    public static MimeMessage CreateMessage(OutgoingEmail email, MailSenderConfig config)
    {
        var sender = ToMailbox(email.Sender);
        var recipients = email.Recipients ?? throw new ArgumentException("Recipients are required.");
        var content = email.Content ?? throw new ArgumentException("Content is required.");
        var message = new MimeMessage();
        message.From.Add(sender);
        AddAddresses(message.To, recipients.To);
        AddAddresses(message.Cc, recipients.Cc);
        AddAddresses(message.Bcc, recipients.Bcc);
        if (message.To.Count + message.Cc.Count + message.Bcc.Count == 0)
            throw new ArgumentException("At least one recipient is required.");
        message.Subject = content.Subject ?? string.Empty;
        if (email.CreatedAt.HasValue) message.Date = email.CreatedAt.Value;
        if (!string.IsNullOrEmpty(email.Id)) message.Headers.Add("X-IoTZoo-Mail-Id", email.Id);
        switch (email.Priority?.ToLowerInvariant())
        {
            case null: case "normal": break;
            case "high": message.Priority = MessagePriority.Urgent; break;
            case "low": message.Priority = MessagePriority.NonUrgent; break;
            default: throw new ArgumentException("Unsupported mail priority.");
        }
        if (email.Options?.RequestReadReceipt == true)
            message.Headers.Add("Disposition-Notification-To", sender.ToString());
        var body = new BodyBuilder { TextBody = content.Text };
        switch (content.Format?.ToLowerInvariant())
        {
            case null: case "text": break;
            case "html":
                if (email.Options?.AllowHtml != true) throw new ArgumentException("HTML requires allowHtml to be true.");
                if (string.IsNullOrEmpty(content.Html)) throw new ArgumentException("HTML content is required.");
                body.HtmlBody = content.Html;
                break;
            default: throw new ArgumentException("Unsupported mail content format.");
        }
        foreach (var attachment in email.Attachments ?? [])
        {
            string path = GetAttachmentPath(config.AttachmentDirectory, attachment.Path);
            var info = new FileInfo(path);
            if (attachment.Size.HasValue && attachment.Size != info.Length)
                throw new ArgumentException("Attachment size does not match the file.");
            var type = string.IsNullOrWhiteSpace(attachment.MimeType) ? null : ContentType.Parse(attachment.MimeType);
            var part = type == null ? body.Attachments.Add(path) : body.Attachments.Add(path, type);
            if (!string.IsNullOrWhiteSpace(attachment.Filename))
            {
                part.ContentDisposition ??= new ContentDisposition(ContentDisposition.Attachment);
                part.ContentDisposition.FileName = part.ContentType.Name = attachment.Filename;
            }
        }
        message.Body = body.ToMessageBody();
        return message;
    }

    private static MailboxAddress ToMailbox(MailAddressData? address)
    {
        if (string.IsNullOrWhiteSpace(address?.Address) || !MailboxAddress.TryParse(address.Address, out var mailbox))
            throw new ArgumentException("A valid mail address is required.");
        return new MailboxAddress(address.Name ?? mailbox.Name, mailbox.Address);
    }

    private static void AddAddresses(InternetAddressList target, List<MailAddressData>? addresses)
    {
        if (addresses == null) return;
        foreach (var address in addresses) target.Add(ToMailbox(address));
    }

    private static string GetAttachmentPath(string directory, string? path)
    {
        if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("An attachment directory and path are required.");
        string root = Path.GetFullPath(directory);
        string fullPath = Path.GetFullPath(path, root);
        string relative = Path.GetRelativePath(root, fullPath);
        if (Path.IsPathRooted(relative) || relative == ".." || relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new ArgumentException("Attachment is outside the configured directory.");
        string current = root;
        if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
            throw new ArgumentException("Attachment directory cannot be a symbolic link.");
        foreach (string part in relative.Split(Path.DirectorySeparatorChar))
        {
            current = Path.Combine(current, part);
            if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new ArgumentException("Attachment paths cannot contain symbolic links.");
        }
        return fullPath;
    }
}
