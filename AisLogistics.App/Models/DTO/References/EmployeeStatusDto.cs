﻿namespace AisLogistics.App.Models.DTO.References
{
    public class EmployeeStatusDto
    {
        public Guid Id { get; set; }
        public Guid SEmployeesId { get; set; }
        public string StatusName { get; set; }
        public string DateStart { get; set; }
        public string? DateStop { get; set; }
        public string Commentt { get; set; }
        public string DateAdd { get; set; }
        public string EmployeesFioAdd { get; set; }
    }
}
