namespace AisLogistics.App.Models.DTO.References
{
    public class OfficeDto
    {
        public Guid Id { get; set; }
        public string OfficeName { get; set; }
        public bool Selected { get; set; } = false;
    }

    public class OfficeParentsDto : OfficeDto
    {
        public Guid? ParentOfficeId { get; set; }
        public string ParentOfficeName { get; set; }
        public int CountChild {get; set; }
    }

    public class OfficeQueueDto
    {
        public string Id { get; set; }
        public string OfficeName { get; set; }
    }

}
