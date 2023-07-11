using AisLogistics.App.Models;
using AisLogistics.App.ViewModels.Cases;
using AisLogistics.App.ViewModels.ModelBuilder;
using AisLogistics.DataLayer.Entities.Models;
using DataTables.AspNet.AspNetCore;
using DataTables.AspNet.Core;
using Microsoft.AspNetCore.Mvc.Rendering;
using SmartBreadcrumbs.Attributes;

namespace AisLogistics.App.Controllers.Cases
{
    public partial class CasesController
    {
        [Breadcrumb("Реестр Выдачи", FromAction = nameof(Index), FromController = typeof(HomeController))]
        public async Task<IActionResult> CasesIssued()
        {
            var mfc = await _referencesService.GetAllOfficesTypeMfcAndTospAsync();
            var officeId = await _employeeManager.GetOfficeAsync();
            ViewBag.CasesAllView = User.HasClaim(AccessKeyNames.DataCaseAll, AccessKeyValues.View);

            var model = new SearchCasesResponseData()
            {
                MfcId = officeId.GetValueOrDefault(),
                MfcList = new SelectList(mfc, "Id", "OfficeName"),
            };

            var modelBuilder = new ViewModelBuilder()
                .AddViewTitle("Реестр услуг на выдачу")
                .SetElementName("cases-issued-datatable")
                .AddModel(model)
                .AddTableMethodAction(Url.Action(nameof(GetCasesIssuedDataJson)));
            return View(modelBuilder);
        }

        public async Task<IActionResult> GetCasesIssuedDataJson(IDataTablesRequest request, SearchCasesRequestData additionalRequest)
        {
            Guid? id = User.HasClaim(AccessKeyNames.DataCaseAll, AccessKeyValues.View) || User.HasClaim(AccessKeyNames.DataCaseOffice, AccessKeyValues.View) ? null : await _employeeManager.GetIdAsync();

            Guid? office = User.HasClaim(AccessKeyNames.DataCaseAll, AccessKeyValues.View) || User.HasClaim(AccessKeyNames.DataCaseOffice, AccessKeyValues.View) ? await _employeeManager.GetOfficeAsync() : null;
            additionalRequest.EmployeeId = id;

            (var responseData, int totalCount, int filteredCount) = await _caseService.GetIssueCasesV2(request, additionalRequest);

            var response = DataTablesResponse.Create(request, totalCount, filteredCount, responseData);

            return new DataTablesJsonResult(response, true);
        }
    }
}
