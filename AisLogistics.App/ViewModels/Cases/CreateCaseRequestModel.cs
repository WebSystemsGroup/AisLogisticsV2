using AisLogistics.DataLayer.Common.Dto.Case;
using AisLogistics.DataLayer.Entities.Models;
using System.ComponentModel.DataAnnotations;

namespace AisLogistics.App.ViewModels.Cases
{
    public class CreateCaseRequestModel
    {
        [Required]
        public Guid ServiceId { get; set; }
        [Required]
        public Guid ProcedureId { get; set; }
        [Required]
        public Guid TariffId { get; set; }
        [Required]
        public int StageId { get; set; }
        public string AdditionalData { get; set; }
        public CustomerModelDto? Customer { get; set; }
        public CustomerModelDto? Representative { get; set; }
        public DServicesCustomersLegal? CustomerLegal { get; set; }
        public bool IsGetCustomerSnils { get; set; }
        public bool IsGetCustomerInn { get; set; }
        public bool IsGetRepresentativeSnils { get; set; }
        public bool IsGetRepresentativeInn { get; set; }

        //public DServicesCustomer? Customer { get; set; }
        //public DServicesCustomer? Representative { get; set; }
        //public DServicesCustomersLegal? CustomerLegal { get; set; }

        //public AddressDetails? AddressDetailsApplicant { get; set; }
        //public AddressDetails? AddressDetailsRepresentative { get; set; }
    }
    public class CreateCaseLegalRequestModel : CreateCaseRequestModel
    {
        public DServicesCustomersLegal CustomerLegal { get; set; }
    }

}
