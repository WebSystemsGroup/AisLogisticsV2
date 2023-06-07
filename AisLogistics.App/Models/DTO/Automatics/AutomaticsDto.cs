namespace AisLogistics.App.Models.DTO.Automatics
{
    public class AutomaticsDto
    {
        public int Id { get; set; }
        public string OperationName { get; set; }
        public DateTime? DateStart { get; set; }
    }

    public class AutomaticsLogsDto
    {
        public Guid Id {get;set;} 
        public DateTime DateStart {get;set;}
        public string TimeStart {get;set;}
    }
}
