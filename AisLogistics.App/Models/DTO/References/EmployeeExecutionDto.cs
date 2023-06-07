﻿namespace AisLogistics.App.Models.DTO.References
{
    public class EmployeeExecutionDto
    {
        public Guid Id { get; set; }
        public Guid SEmployeesId { get; set; }
        public bool IsExecution { get; set; }
        public string DateStart { get; set; }
        public string? DateStop { get; set; }
        public string Commentt { get; set; }
        public string EmployeesFioAdd { get; set; }
        public string DateAdd { get; set; }
    }
}
