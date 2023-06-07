namespace AisLogistics.App.Models.Statistics
{
    public class StatisticsMfcDataResponse
    {
        public string Name { get; set; }
        public int ReceivedCount { get; set; }
        public decimal ReceivedStateSum { get; set; }
        public int IssuedCount { get; set; }
        public int ConsultationCount { get; set; }
        public int ALLCount { get; set; }
        public int RefusalCount { get; set; }
        public int ExpiredCount { get; set; }
        public decimal ExpiredPercentCount { get; set; }
        public int ServiceStateTaskCount { get; set; }
        public decimal ServiceStateTaskPercentCount { get; set; }
    }
}
