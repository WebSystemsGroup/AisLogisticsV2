using AisLogistics.App.Models;
using AisLogistics.App.Models.AdditionalForms;
using AisLogistics.App.ViewModels.ModelBuilder;
using AisLogistics.DataLayer.Entities.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace AisLogistics.App.Controllers.Cases;

public partial class CasesController
{
    public async Task<IActionResult> AdditionalFormChangeModal(Guid id)
    {
        var form = await _caseService.GetAdditionalFormByCaseIdAsync(id);
        var modelBuilder = new ModalViewModelBuilder()
            .AddModalType(ModalType.FULL)
            .AddModalViewPath("Details/Modals/_ModalAdditionalFormChange")
            .AddModalTitle("Дополнительные данные")
            .AddModel(form);

        if (form.FormPath is null) modelBuilder.HideSubmitButton();

        return ModalLayoutView(modelBuilder);
    }

    public async Task AdditionalFormSave(Guid id, string data)
    {
        var dictonary = new Dictionary<string, string>();

        while (Regex.IsMatch(data, "{\"\\d+\":\\s*.+?}}"))
        {
            string array = Regex.Match(data, "{\"\\d+\":\\s*.+?}}").Value;
            array = Regex.Replace(array, "\"\\d+\":\\s*", "");
            array = Regex.Replace(array, "{{", "[{");
            array = Regex.Replace(array, "}}", "}]");
            data = data.Replace(Regex.Match(data, "{\"\\d+\":\\s*.+?}}").Value, array);
        }

        if (!string.IsNullOrEmpty(data))
        {
            var jsonObject = JObject.Parse(data);
            if (jsonObject.TryGetValue("NumberPKPVD", out JToken? Token))
            {
                string? numberPKPVD = (string?)Token;
                if (!string.IsNullOrEmpty(numberPKPVD)) dictonary.Add("NumberPKPVD", numberPKPVD);
            }
        }

        await _caseService.SaveAdditionalData(id, data, dictonary);
        ShowSuccess("Данные обновлены.");
    }

    public async Task<IActionResult> GuardianshipForm(Guid? id)
    {
        if (!id.HasValue) return PartialView("AdditionalForms/_Guardianship", new GuardianshipModel());
        var guardianshipModel = await _caseService.GetAdditionalDataByServiceIdAsync<GuardianshipModel>(id.Value);
        return PartialView("AdditionalForms/_Guardianship", guardianshipModel);
    }

    public async Task<IActionResult> MvdRegistrationForm(Guid? id)
    {
        if (!id.HasValue) return PartialView("AdditionalForms/_MvdRegistration", new MvdRegistrationModel());
        var mvdRegistrationModel = await _caseService.GetAdditionalDataByServiceIdAsync<MvdRegistrationModel>(id.Value);
        return PartialView("AdditionalForms/_MvdRegistration", mvdRegistrationModel ?? new MvdRegistrationModel());
    }
    public async Task<IActionResult> MvdRegistrationPlaceStayForm(Guid? id)
    {
        if (!id.HasValue) return PartialView("AdditionalForms/_MvdRegistrationPlaceStay", new MvdRegistrationPlaceStayModel());
        var model = await _caseService.GetAdditionalDataByServiceIdAsync<MvdRegistrationPlaceStayModel>(id.Value);
        return PartialView("AdditionalForms/_MvdRegistrationPlaceStay", model ?? new MvdRegistrationPlaceStayModel());
    }
    public async Task<IActionResult> MvdDe_RegistrationForm(Guid? id)
    {
        if (!id.HasValue) return PartialView("AdditionalForms/_MvdDe_Registration", new MvdDe_RegistrationModel());
        var model = await _caseService.GetAdditionalDataByServiceIdAsync<MvdDe_RegistrationModel>(id.Value);
        return PartialView("AdditionalForms/_MvdDe_Registration", model ?? new MvdDe_RegistrationModel());
    }
    public async Task<IActionResult> MvdDe_RegistrationStayForm(Guid? id)
    {
        if (!id.HasValue) return PartialView("AdditionalForms/_MvdDe_RegistrationStay", new MvdDe_RegistrationStayModel());
        var model = await _caseService.GetAdditionalDataByServiceIdAsync<MvdDe_RegistrationStayModel>(id.Value);
        return PartialView("AdditionalForms/_MvdDe_RegistrationStay", model ?? new MvdDe_RegistrationStayModel());
    }

    public async Task<IActionResult> GetDriverLicenseForm(Guid? id)
    {
        if (!id.HasValue) return PartialView("AdditionalForms/_GetDriverLicense", new GetDriverLicenseModel());
        var driverLicenseModel = await _caseService.GetAdditionalDataByServiceIdAsync<GetDriverLicenseModel>(id.Value);
        return PartialView("AdditionalForms/_GetDriverLicense", driverLicenseModel ?? new GetDriverLicenseModel());
    }

    public async Task<IActionResult> DrugsCertificateForm(Guid? id)
    {
        if (!id.HasValue) return PartialView("AdditionalForms/_DrugsCertificate", new DrugsCertificateModel());
        var model = await _caseService.GetAdditionalDataByServiceIdAsync<DrugsCertificateModel>(id.Value);
        return PartialView("AdditionalForms/_DrugsCertificate", model ?? new DrugsCertificateModel());
    }

    public async Task<IActionResult> FnsRealEstateNotificationForm(Guid? id)
    {
        if (!id.HasValue) return PartialView("AdditionalForms/_FnsRealEstateNotification", new FnsRealEstateNotificationModel());
        var model = await _caseService.GetAdditionalDataByServiceIdAsync<FnsRealEstateNotificationModel>(id.Value);
        return PartialView("AdditionalForms/_FnsRealEstateNotification", model ?? new FnsRealEstateNotificationModel());
    }

    public async Task<IActionResult> FnsLandPlotNotificationForm(Guid? id)
    {
        if (!id.HasValue) return PartialView("AdditionalForms/_FnsLandPlotNotification", new FnsLandPlotNotificationModel());
        var model = await _caseService.GetAdditionalDataByServiceIdAsync<FnsLandPlotNotificationModel>(id.Value);
        return PartialView("AdditionalForms/_FnsLandPlotNotification", model ?? new FnsLandPlotNotificationModel());
    }

    public async Task<IActionResult> FnsKnd1153006Form(Guid? id)
    {
        if (!id.HasValue) return PartialView("AdditionalForms/_FnsKnd1153006", new FnsKnd1153006Model());
        var model = await _caseService.GetAdditionalDataByServiceIdAsync<FnsKnd1153006Model>(id.Value);
        return PartialView("AdditionalForms/_FnsKnd1153006", model ?? new FnsKnd1153006Model());
    }

    public async Task<IActionResult> FnsKnd1150075Form(Guid? id)
    {
        if (!id.HasValue) return PartialView("AdditionalForms/_FnsKnd1150075", new FnsKnd1150075Model());
        var model = await _caseService.GetAdditionalDataByServiceIdAsync<FnsKnd1150075Model>(id.Value);
        return PartialView("AdditionalForms/_FnsKnd1150075", model ?? new FnsKnd1150075Model());
    }

    public async Task<IActionResult> FnsAccountForm(Guid? id)
    {
        if (!id.HasValue) return PartialView("AdditionalForms/_FnsAccount", new FnsAccountModel());
        var model = await _caseService.GetAdditionalDataByServiceIdAsync<FnsAccountModel>(id.Value);
        return PartialView("AdditionalForms/_FnsAccount", model ?? new FnsAccountModel());
    }
    
    public async Task<IActionResult> FssInsurantRegistrationForm(Guid? id)
    {
        if (!id.HasValue) return PartialView("AdditionalForms/_FssInsurantRegistration", new FssInsurantRegistrationModel());
        var model = await _caseService.GetAdditionalDataByServiceIdAsync<FssInsurantRegistrationModel>(id.Value);
        return PartialView("AdditionalForms/_FssInsurantRegistration", model ?? new FssInsurantRegistrationModel());
    }
    
    public async Task<IActionResult> PfrAppointMonthlyCashPaymentsForm(Guid? id)
    {
        if (!id.HasValue) return PartialView("AdditionalForms/_PfrAppointMonthlyCashPayments", new PfrAppointMonthlyCashPaymentsModel());
        var model = await _caseService.GetAdditionalDataByServiceIdAsync<PfrAppointMonthlyCashPaymentsModel>(id.Value);
        return PartialView("AdditionalForms/_PfrAppointMonthlyCashPayments", model ?? new PfrAppointMonthlyCashPaymentsModel());
    }

    public async Task<IActionResult> PfrChildBenefitsForm(Guid? id)
    {
        if (!id.HasValue) return PartialView("AdditionalForms/_PfrChildBenefits", new PfrChildBenefitsModel());
        var model = await _caseService.GetAdditionalDataByServiceIdAsync<PfrChildBenefitsModel>(id.Value);
        return PartialView("AdditionalForms/_PfrChildBenefits", model ?? new PfrChildBenefitsModel());
    }

    public async Task<IActionResult> PfrMilitaryChildBenefitsForm(Guid? id)
    {
        if (!id.HasValue) return PartialView("AdditionalForms/_PfrMilitaryChildBenefits", new PfrMilitaryChildBenefitsModel());
        var model = await _caseService.GetAdditionalDataByServiceIdAsync<PfrMilitaryChildBenefitsModel>(id.Value);
        return PartialView("AdditionalForms/_PfrMilitaryChildBenefits", model ?? new PfrMilitaryChildBenefitsModel());
    }

    public async Task<IActionResult> PfrTransferChildFamilyForm(Guid? id)
    {
        if (!id.HasValue) return PartialView("AdditionalForms/_PfrTransferChildFamily", new PfrTransferChildFamilyModel());
        var model = await _caseService.GetAdditionalDataByServiceIdAsync<PfrTransferChildFamilyModel>(id.Value);
        return PartialView("AdditionalForms/_PfrTransferChildFamily", model ?? new PfrTransferChildFamilyModel());
    }

    public async Task<IActionResult> PfrPregnantWomanFiredForm(Guid? id)
    {
        if (!id.HasValue) return PartialView("AdditionalForms/_PfrChildBenefits", new PfrChildBenefitsModel());
        var model = await _caseService.GetAdditionalDataByServiceIdAsync<PfrChildBenefitsModel>(id.Value);
        return PartialView("AdditionalForms/_PfrChildBenefits", model ?? new PfrChildBenefitsModel());
    }

    public async Task<IActionResult> FssDeregistrationForm(Guid? id)
    {
        if (!id.HasValue) return PartialView("AdditionalForms/_FssDeregistration", new FssDeregistrationModel());
        var model = await _caseService.GetAdditionalDataByServiceIdAsync<FssDeregistrationModel>(id.Value);
        return PartialView("AdditionalForms/_FssDeregistration", model ?? new FssDeregistrationModel());
    }

    public async Task<IActionResult> FssReceptionDocumentsForm(Guid? id)
    {
        if (!id.HasValue) return PartialView("AdditionalForms/_FssReceptionDocuments", new FssReceptionDocumentsModel());
        var model = await _caseService.GetAdditionalDataByServiceIdAsync<FssReceptionDocumentsModel>(id.Value);
        return PartialView("AdditionalForms/_FssReceptionDocuments", model ?? new FssReceptionDocumentsModel());
    }

    public async Task<IActionResult> FssTechnicalMeansRehabilitationForm(Guid? id)
    {
        if (!id.HasValue) return PartialView("AdditionalForms/_FssTechnicalMeansRehabilitation", new FssTechnicalMeansRehabilitationModel());
        var model = await _caseService.GetAdditionalDataByServiceIdAsync<FssTechnicalMeansRehabilitationModel>(id.Value);
        return PartialView("AdditionalForms/_FssTechnicalMeansRehabilitation", model ?? new FssTechnicalMeansRehabilitationModel());
    }

    public async Task<IActionResult> FssSpaTreatmentForm(Guid? id)
    {
        if (!id.HasValue) return PartialView("AdditionalForms/_FssSpaTreatment", new FssSpaTreatmentModel());
        var model = await _caseService.GetAdditionalDataByServiceIdAsync<FssSpaTreatmentModel>(id.Value);
        return PartialView("AdditionalForms/_FssSpaTreatment", model ?? new FssSpaTreatmentModel());
    }

    public async Task<IActionResult> RosImProvidingExtractsForm(Guid? id)
    {
        if (!id.HasValue) return PartialView("AdditionalForms/_RosImProvidingExtracts", new RosImProvidingExtractsModel());
        var model = await _caseService.GetAdditionalDataByServiceIdAsync<RosImProvidingExtractsModel>(id.Value);
        return PartialView("AdditionalForms/_RosImProvidingExtracts", model ?? new RosImProvidingExtractsModel());
    }

    public async Task<IActionResult> FnsKnd1150076Form(Guid? id)
    {
        if (!id.HasValue) return PartialView("AdditionalForms/_FnsKnd1150076", new FnsKnd1150076Model());
        var model = await _caseService.GetAdditionalDataByServiceIdAsync<FnsKnd1150076Model>(id.Value);
        return PartialView("AdditionalForms/_FnsKnd1150076", model ?? new FnsKnd1150076Model());
    }

    public async Task<IActionResult> PfrMaternalCapitalForm(Guid? id)
    {
        if (!id.HasValue) return PartialView("AdditionalForms/_PfrMaternalCapital", new PfrMaternalCapitalModel());
        var model = await _caseService.GetAdditionalDataByServiceIdAsync<PfrMaternalCapitalModel>(id.Value);
        return PartialView("AdditionalForms/_PfrMaternalCapital", model ?? new PfrMaternalCapitalModel());
    }
    public async Task<IActionResult> _67_7Form(Guid? id)
    {
        if (!id.HasValue) return PartialView("AdditionalForms/_67_7", new _67_7Model());
        var model = await _caseService.GetAdditionalDataByServiceIdAsync<_67_7Model>(id.Value);
        return PartialView("AdditionalForms/_67_7", model ?? new _67_7Model());
    }
    public async Task<IActionResult> DisqualifiedPersonsForm(Guid? id)
    {
        if (!id.HasValue) return PartialView("AdditionalForms/DisqualifiedPersons", new DisqualifiedPersonsModel());
        var model = await _caseService.GetAdditionalDataByServiceIdAsync<DisqualifiedPersonsModel>(id.Value);
        return PartialView("AdditionalForms/DisqualifiedPersons", model ?? new DisqualifiedPersonsModel());
    }
    public async Task<IActionResult> FnsAccounting(Guid? id)
    {
        if (!id.HasValue) return PartialView("AdditionalForms/_FnsAccounting", new FnsAccountingModel());
        var model = await _caseService.GetAdditionalDataByServiceIdAsync<FnsAccountingModel>(id.Value);
        return PartialView("AdditionalForms/_FnsAccounting", model ?? new FnsAccountingModel());
    }
    
    public async Task<IActionResult> PfrEightToSeventeenForm(Guid? id)
    {
        if (!id.HasValue) return PartialView("AdditionalForms/_PfrEightToSeventeen", new PfrEightToSeventeenModel());
        var model = await _caseService.GetAdditionalDataByServiceIdAsync<PfrEightToSeventeenModel>(id.Value);
        return PartialView("AdditionalForms/_PfrEightToSeventeen", model ?? new PfrEightToSeventeenModel());
    }
    public async Task<IActionResult> PassportIssuancForm(Guid? id)
    {
        if (!id.HasValue) return PartialView("AdditionalForms/_PassportIssuanc", new PassportIssuancModel());
        var model = await _caseService.GetAdditionalDataByServiceIdAsync<PassportIssuancModel>(id.Value);
        return PartialView("AdditionalForms/_PassportIssuanc", model ?? new PassportIssuancModel());
    }
    
    public async Task<IActionResult> PassportForm(Guid? id)
    {
        if (!id.HasValue) return PartialView("AdditionalForms/_Passport", new PassportModel( ));
        var model = await _caseService.GetAdditionalDataByServiceIdAsync<PassportModel>(id.Value);
        return PartialView("AdditionalForms/_Passport", model ?? new PassportModel());
    }
    public async Task<IActionResult> Rosreestr(Guid? id)
    {
        if (!id.HasValue) return PartialView("AdditionalForms/Rosreestr", new RosreestrModel( ));
        var model = await _caseService.GetAdditionalDataByServiceIdAsync<RosreestrModel>(id.Value);
        return PartialView("AdditionalForms/Rosreestr", model ?? new RosreestrModel());
    }
}