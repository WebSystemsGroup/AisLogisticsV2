using AisLogistics.App.Models;
using AisLogistics.App.ViewModels.ModelBuilder;
using AisLogistics.DataLayer.Common.Dto.Reference;
using DataTables.AspNet.AspNetCore;
using DataTables.AspNet.Core;
using DocumentFormat.OpenXml.Office2010.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SmartBreadcrumbs.Attributes;

namespace AisLogistics.App.Controllers.Reference
{
    public partial class ReferenceController
    {
        [Breadcrumb("ViewData.Title", FromAction = nameof(Employees), FromController = typeof(ReferenceController))]
        [Route("[controller]/Employees/Details/{id}/Executions")]
        public async Task<IActionResult> EmployeeExecutions(Guid id)
        {
            var emlpoyee = await _referencesService.GetEmployeeDtoAsync(id);
            ViewData["Title"] = emlpoyee.EmployeeName;

            ViewBag.isActive = await _referencesService.GetIsEmployeeActivityAsync(id);

            var modelBuilder = new ViewModelBuilder()
                .AddViewTitle("Исполнение").SetInvisibleViewTitle()
                .AddViewDescription("Исполнение")
                .AddTableMethodAction(Url.Action(nameof(GetEmployeeExecutionsDataJson), new { id = id }))
                .AddEditMethodAction(Url.Action(nameof(EmployeeExecutionChangeModal)))
                .AddRemoveMethodAction(Url.Action(nameof(EmployeeExecutionRemove)));
            return View("Employees/Details/Executions", modelBuilder);
        }

        public IActionResult GetEmployeeExecutionsDataJson(IDataTablesRequest request, Guid id)
        {
            (var responseData, int totalCount, int filteredCount) = _referencesService.GetEmployeeExecutions(ref request, id);

            var response = DataTablesResponse.Create(request, totalCount, filteredCount, responseData);

            return new DataTablesJsonResult(response, true);
        }

        public async Task<IActionResult> EmployeeExecutionChangeModal(Guid id, Guid employeeId, ActionType actionType)
        {
            var model = new ModalViewModelBuilder()
                .AddModel(actionType == ActionType.Add ? new EmployeeExecutionModelDto() { SEmployeesId = employeeId } : await _referencesService.GetEmployeeExectionDtoAsync(id))
                .AddModalTitle((actionType == ActionType.Add ? "Добавление" : "Изменение") + " исполнения сотрудника")
                .AddModalViewPath("~/Views/Reference/Employees/Details/Modals/_ModalChangeEmployeeExecution.cshtml")
                .AddActionType(actionType)
                .AddModalType(ModalType.LARGE);
            return ModalLayoutView(model);
        }

        [HttpPost]
        public async Task EmployeeExecutionRemove(Guid id, string comment)
        {
            try
            {
                await _referencesService.RemoveEmployeeExecutionAsync(id, comment);

                ShowSuccess(MessageDescription.RemoveSuccess);
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task EmployeeExecutionSaveAsync(EmployeeExecutionModelDto input, ActionType actionType)
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
                input.EmployeesFioAdd = employeeName;

                try
                {
                    await _referencesService.AddEmployeeExecutionAsync(input);

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
