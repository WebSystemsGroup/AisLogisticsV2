namespace AisLogistics.App.Models.DTO.References
{
    public class EmployeeOfficeDto
    {
        public Guid Id { get; set; }
        public Guid SEmployeesId { get; set; }
        public string OfficeName { get; set; }
        public string JobPositionName { get; set; }
        public decimal EmployeeIntensity { get; set; }
        public decimal EmployeeRate { get; set; }
        public string DateStart { get; set; }
        public string? DateStop { get; set; }
        public string EmployeeFioAdd { get; set; }
        public string DateAdd { get; set; }
    }
}
