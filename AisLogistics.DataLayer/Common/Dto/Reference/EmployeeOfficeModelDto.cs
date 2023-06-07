using System;

namespace AisLogistics.DataLayer.Common.Dto.Reference
{
    public class EmployeeOfficeModelDto
    {
        public Guid Id { get; set; }
        public Guid SEmployeesId { get; set; }
        public Guid SOfficesId { get; set; }
        public int SEmployeesJobPositionId { get; set; }
        public decimal EmployeeIntensity { get; set; }
        public decimal EmployeeRate { get; set; }
        public DateTime DateStart { get; set; }
        public string EmployeeFioAdd { get; set; }
    }
}
