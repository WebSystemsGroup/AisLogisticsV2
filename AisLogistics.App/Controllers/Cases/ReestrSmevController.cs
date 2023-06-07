using AisLogistics.App.Models;
using AisLogistics.App.ViewModels.Cases;
using AisLogistics.App.ViewModels.ModelBuilder;
using DataTables.AspNet.AspNetCore;
using DataTables.AspNet.Core;
using Microsoft.AspNetCore.Mvc.Rendering;
using SmartBreadcrumbs.Attributes;

namespace AisLogistics.App.Controllers.Cases
{
    public partial class CasesController
    {
        [Breadcrumb("Реестр СМЭВ", FromAction = nameof(Index), FromController = typeof(HomeController))]
        public async Task<IActionResult> CasesReestrSmev()
        {
            var offices = await _referencesService.GetAllOfficesAsync();
            var smev = await _referencesService.GetAllSmevServicesAsync();
            var smevRequest = await _referencesService.GetAllSmevRequestsAsync();
            var mfc = await _referencesService.GetAllOfficesTypeMfcAndTospAsync();
            var officeId = await _employeeManager.GetOfficeAsync();

            //ViewBag.Offices = offices == null ? "" : string.Join("", offices.Select(x => $"<option value=\"{x.Id}\">{x.SmevName}</option>"));
            //ViewBag.Services = services == null ? "" : string.Join("", services.Select(x => $"<option value=\"{x.Id}\">{(x.ServiceName.Length > 150 ? x.ServiceName[..150] + "..." : x.ServiceName)}</option>"));
            //ViewBag.Mfc = mfc == null ? "" : string.Join("", mfc.Select(x => $"<option value=\"{x.Id}\" {(x.Id == officeId ? "selected" : string.Empty)}>{x.OfficeName}</option>"));
            ViewBag.CasesAllView = User.HasClaim(AccessKeyNames.DataCaseAll, AccessKeyValues.View);

            var model = new SearchCasesResponseData()
            {
                MfcId = officeId.GetValueOrDefault(),
                MfcList = new SelectList(mfc, "Id", "OfficeName"),
                OfficesList = new SelectList(offices, "Id", "OfficeName"),
                SmevServicesList = new SelectList(smev, "Id", "SmevName"),
                SmevRequestList = new SelectList(smevRequest, "Id", "RequestName"),
            };

            var modelBuilder = new ViewModelBuilder()
                .AddViewTitle("Реестр услуг на выдачу")
                .SetElementName("cases-reestr-datatable")
                .AddModel(model)
                .AddTableMethodAction(Url.Action(nameof(GetCasesReestrSmevDataJson)));
            return View("CasesReestrSmev1", modelBuilder);
        }

        public async Task<IActionResult> GetCasesReestrSmevDataJson(IDataTablesRequest request, SearchCasesRequestData additionalRequest)
        {
            Guid? id = User.HasClaim(AccessKeyNames.DataCaseAll, AccessKeyValues.View) || User.HasClaim(AccessKeyNames.DataCaseOffice, AccessKeyValues.View) ? null : await _employeeManager.GetIdAsync();

            Guid? office = User.HasClaim(AccessKeyNames.DataCaseAll, AccessKeyValues.View) || User.HasClaim(AccessKeyNames.DataCaseOffice, AccessKeyValues.View) ? await _employeeManager.GetOfficeAsync() : null;
            additionalRequest.EmployeeId = id;

            (var responseData, int totalCount, int filteredCount) =await _caseService.GetCasesReestrSmevV2(request, additionalRequest);

            var response = DataTablesResponse.Create(request, totalCount, filteredCount, responseData);

            return new DataTablesJsonResult(response, true);
        }
    }
}
