using AisLogistics.App.Models;
using AisLogistics.App.ViewModels.Cases;
using AisLogistics.App.ViewModels.ModelBuilder;
using DataTables.AspNet.AspNetCore;
using DataTables.AspNet.Core;
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
            ViewBag.Mfc = mfc == null ? "" : string.Join("", mfc.Select(x => $"<option value=\"{x.Id}\" {(x.Id == officeId ? "selected" : string.Empty)}>{x.OfficeName}</option>"));
            ViewBag.CasesAllView = User.HasClaim(AccessKeyNames.DataCaseAll, AccessKeyValues.View);

            var model = new SearchCasesResponseData();

            var modelBuilder = new ViewModelBuilder()
                .AddViewTitle("Реестр услуг на выдачу")
                .SetElementName("cases-issued-datatable")
                .AddModel(model)
                .AddTableMethodAction(Url.Action(nameof(GetCasesIssuedDataJson)));
            return View(modelBuilder);
        }

        public async Task<IActionResult> GetCasesIssuedDataJson(IDataTablesRequest request)
        {
            Guid? id = User.HasClaim(AccessKeyNames.DataCaseAll, AccessKeyValues.View) || User.HasClaim(AccessKeyNames.DataCaseOffice, AccessKeyValues.View) ? null : await _employeeManager.GetIdAsync();

            Guid? office = User.HasClaim(AccessKeyNames.DataCaseAll, AccessKeyValues.View) || User.HasClaim(AccessKeyNames.DataCaseOffice, AccessKeyValues.View) ? await _employeeManager.GetOfficeAsync() : null;

            (var responseData, int totalCount, int filteredCount) = await _caseService.GetIssueCasesV2(request, id, office);

            var response = DataTablesResponse.Create(request, totalCount, filteredCount, responseData);

            return new DataTablesJsonResult(response, true);
        }
    }
}
