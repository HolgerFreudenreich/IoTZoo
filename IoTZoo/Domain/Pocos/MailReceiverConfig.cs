namespace Domain.Pocos
{
    public class MailReceiverConfig
    {
        public bool Enabled { get; set; } = false;
        public string HostName { get; set; } = string.Empty;
        public string? Password { get; set; } = null!;
        public string? UserName { get; set; } = null!;
        public int Port { get; set; } = 993;
    }
}

