﻿namespace AisLogistics.App.Models.DTO.Cases
{
    public class ServiceStageSaveDto
    {
        public List<Guid> serviceId { get; set; }
        public Guid employeeId { get; set; }
        public int stageId { get; set; }
        public Guid officeId { get; set; }
    }
}
