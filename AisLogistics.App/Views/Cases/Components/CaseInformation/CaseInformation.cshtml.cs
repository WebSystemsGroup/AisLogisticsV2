using AisLogistics.App.Models.DTO.Services;
using AisLogistics.App.Service;

namespace AisLogistics.App.Views.Cases.Components.CaseInformation
{
    public class CaseInformation : ViewComponent
    {
        private readonly ICaseService _caseService;
        private readonly EmployeeManager _employeeManager;

        public CaseInformation(ICaseService caseService, EmployeeManager employeeManager)
        {
            _caseService = caseService;
            _employeeManager = employeeManager;
        }

        /*public async Task<IViewComponentResult> InvokeAsync(string id)
        {
            var employeeId = await _employeeManager.GetIdAsync();
            var caseDto = await _caseService.GetCaseByIdAsync(id, employeeId.Value);

            return View("~/Views/Cases/Components/CaseInformation/CaseInformation.cshtml", caseDto);
        }*/
        public async Task<IViewComponentResult> InvokeAsync(CasesMainDto caseDto)
        {
            //var employeeId = await _employeeManager.GetIdAsync();
            //var caseDto = await _caseService.GetCaseByIdAsync(id, employeeId.Value);

            return View("~/Views/Cases/Components/CaseInformation/CaseInformation.cshtml", caseDto);
        }
    }
}
