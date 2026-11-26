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

namespace Domain.Pocos;

public class MailSenderConfig
{
    public bool Enabled { get; set; }
    public string HostName { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string Encryption { get; set; } = "STARTTLS";
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string PasswordRef { get; set; } = "SMTP_PASSWORD";
    public string AttachmentDirectory { get; set; } = string.Empty;
    public string ImapHostName { get; set; } = string.Empty;
    public int ImapPort { get; set; } = 993;

    public MailSenderConfig Copy() => (MailSenderConfig)MemberwiseClone();
}
