namespace Domain.Pocos
{
    public class MailReceiverConfig
    {
        public bool Enabled { get; set; } = false;
        public string HostName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public int Port { get; set; } = 993;
    }
}

