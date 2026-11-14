namespace Domain.Pocos;

public class MailSenderRequest
{
    public OutgoingEmail? Email { get; set; }
}

public class OutgoingEmail
{
    public string? Id { get; set; }
    public string? Priority { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public MailAddressData? Sender { get; set; }
    public MailRecipients? Recipients { get; set; }
    public MailContent? Content { get; set; }
    public List<MailAttachment>? Attachments { get; set; }
    public MailSmtpData? Smtp { get; set; }
    public MailOptions? Options { get; set; }
}

public class MailAddressData
{
    public string? Name { get; set; }
    public string? Address { get; set; }
}

public class MailRecipients
{
    public List<MailAddressData>? To { get; set; }
    public List<MailAddressData>? Cc { get; set; }
    public List<MailAddressData>? Bcc { get; set; }
}

public class MailContent
{
    public string? Subject { get; set; }
    public string? Format { get; set; }
    public string? Text { get; set; }
    public string? Html { get; set; }
}

public class MailAttachment
{
    public string? Filename { get; set; }
    public string? MimeType { get; set; }
    public string? Path { get; set; }
    public long? Size { get; set; }
}

public class MailSmtpData
{
    public string? Host { get; set; }
    public int? Port { get; set; }
    public string? Encryption { get; set; }
    public MailAuthentication? Authentication { get; set; }
}

public class MailAuthentication
{
    public string? Username { get; set; }
    public string? PasswordRef { get; set; }
}

public class MailOptions
{
    public bool RequestReadReceipt { get; set; }
    public bool RequestDeliveryReceipt { get; set; }
    public bool AllowHtml { get; set; }
    public bool SaveToSentFolder { get; set; }
}
