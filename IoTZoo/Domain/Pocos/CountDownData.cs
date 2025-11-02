namespace Domain.Pocos
{
    public class CountDownData
    {
        public string ProjectName { get; set; } = null!;
        public string CounterName { get; set; } = null!;
        public int Seconds { get; set; }
        public bool ReportProgress { get; set; } = true;
    }
}