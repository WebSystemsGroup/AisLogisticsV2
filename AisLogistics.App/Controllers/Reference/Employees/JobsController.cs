using AisLogistics.App.Models;
using AisLogistics.App.Models.DTO.References;
using AisLogistics.App.ViewModels.ModelBuilder;
using AisLogistics.DataLayer.Common.Dto.Reference;
using DataTables.AspNet.AspNetCore;
using DataTables.AspNet.Core;
using Microsoft.AspNetCore.Mvc.Rendering;
using SmartBreadcrumbs.Attributes;

namespace AisLogistics.App.Controllers.Reference
{
    public partial class ReferenceController
    {
        [Breadcrumb("ViewData.Title", FromAction = nameof(Employees), FromController = typeof(ReferenceController))]
        [Route("[controller]/Employees/Details/{id}/Jobs")]
        public async Task<IActionResult> EmployeeJobs(Guid id)
        {
            var emlpoyee = await _referencesService.GetEmployeeDtoAsync(id);
            ViewData["Title"] = emlpoyee.EmployeeName;

            var modelBuilder = new ViewModelBuilder()
                .AddViewTitle("Должность").SetInvisibleViewTitle()
                .AddViewDescription("Должность")
                .AddTableMethodAction(Url.Action(nameof(GetEmployeeOfficesDataJson), new { id = id }))
                .AddEditMethodAction(Url.Action(nameof(EmployeeOfficeChangeModal)))
                .AddRemoveMethodAction(Url.Action(nameof(EmployeeOfficeRemove)));
            return View("Employees/Details/Jobs", modelBuilder);
        }

        public IActionResult GetEmployeeOfficesDataJson(IDataTablesRequest request, Guid id)
        {
            (var responseData, int totalCount, int filteredCount) = _referencesService.GetEmployeeOffices(ref request, id);

            var response = DataTablesResponse.Create(request, totalCount, filteredCount, responseData);

            return new DataTablesJsonResult(response, true);
        }

        public async Task<IActionResult> EmployeeOfficeChangeModal(Guid? id, Guid employeeId, ActionType actionType)
        {

            if (id is null)
            {
                // добавление
            }
            else
            {
                // измениние                
            }

            var lastModified = await _referencesService.GetLastEmployeesOfficeDtoAsync(employeeId);

            var offices = await _referencesService.GetAllOfficesAsync();
            var jobs = await _referencesService.GetAllJobsAsync();

            ViewBag.Offices = new SelectList(offices, nameof(OfficeDto.Id), nameof(OfficeDto.OfficeName), lastModified.SOfficesId); 
            ViewBag.Jobs = new SelectList(jobs, nameof(EmployeesJobDto.Id), nameof(EmployeesJobDto.JobPositionName), lastModified.SEmployeesJobPositionId);
            ViewBag.MaxDate = lastModified.DateStart;

            var model = new ModalViewModelBuilder()
                .AddModel(lastModified)
                .AddModalTitle("Добавление должности и офиса сотрудника")
                .AddModalViewPath("~/Views/Reference/Employees/Details/Modals/_ModalChangeEmployeeOffice.cshtml")
                .AddActionType(actionType)
                .AddModalType(ModalType.LARGE);
            return ModalLayoutView(model);
        }

        [HttpPost]
        public async Task EmployeeOfficeRemove(Guid id, string comment)
        {
            try
            {
                await _referencesService.RemoveEmployeeOfficeAsync(id, comment);

                ShowSuccess(MessageDescription.RemoveSuccess);
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task EmployeeOfficeSaveAsync(EmployeeOfficeModelDto input, ActionType actionType)
        {
            // всегда добавить
            if (actionType == ActionType.Add)
            {
                var employeeId = await _employeeManager.GetIdAsync();

                if (employeeId is null)
                {
                    ShowError(MessageDescription.Error);
                    return;
                }

                var employeeName = await _employeeManager.GetNameAsync();
                input.EmployeeFioAdd = employeeName;

                try
                {
                    await _referencesService.AddEmployeeOfficeAsync(input);

                    ShowSuccess(MessageDescription.AddSuccess);
                }
                catch (Exception ex)
                {
                    ShowError(ex.Message);
                }
            }
            //else
            //{

            //}
        }
    }
}
