using Microsoft.AspNetCore.Mvc.Rendering;

namespace AisLogistics.App.Models.Queue
{
    public class RegistrationModelDto
    {
        public List<SelectListItem> MFC { get; set; }
    }

    public class GetDatePreRegestrationModelDto : RegistrationModelDto
    {
        public GetDatePreRegestrationModelDto()
        {
            MFC = new List<SelectListItem>();
            Date = new List<PreRegestratonList>();
            Time = new List<PreRegestratonList>();
        }
        public List<PreRegestratonList> Date { get; set; }
        public List<PreRegestratonList> Time { get; set; }
    }

    public class PreRegestratonList
    {
        public int idx { get; set; }
        public string value { get; set; }
        public string key { get; set; }
    }

    public class GetDatePreRegistetionResponceData
    {
        public string date { get; set; }
        public string date_format { get; set; }
        public List<string> time { get; set; }

    }

    public class PreRegestrationRequestModel
    {
        public string source { get; set; } = "ais";
        public string email { get; set; } = "";
        public string fio { get; set; }
        public string phone { get; set; }
        public string date { get; set; }
        public string time { get; set; }
        public int service { get; set; }
    }
    public class AddPreRegestrationRequestModel : PreRegestrationRequestModel
    {
        public int queue_id { get; set; }
    }

    public class AddPreRegestrationResponceModel
    {
        public string code { get; set; }
    }

    public class PreRegestrationCanselRequestModel
    {
        public string date { get; set; }
        public string code { get; set; }

    }
    public class AddCancelPreRegestrationRequestModel : PreRegestrationCanselRequestModel
    {
        public int queue_id { get; set; }
    }


}
