using System;

namespace AisLogistics.DataLayer.Common.Dto.Systems
{
    public class SOfficeAcquiringDto
    {
        public Guid Id { get; set; }
        public string AcquiringName { get; set; }
        public string IpAddress { get; set; }
        public Guid SOfficesId { get; set; }
    }
}
