using AisLogistics.App.Models;
using AisLogistics.App.Models.DTO.References;
using AisLogistics.App.ViewModels.ModelBuilder;
using AisLogistics.DataLayer.Common.Dto.Reference;
using DataTables.AspNet.AspNetCore;
using DataTables.AspNet.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SmartBreadcrumbs.Attributes;

namespace AisLogistics.App.Controllers.Reference
{
    public partial class ReferenceController
    {
        [Breadcrumb("ViewData.Title", FromAction = nameof(Employees), FromController = typeof(ReferenceController))]
        [Route("[controller]/Employees/Details/{id}/ExecutionRoles")]
        public async Task<IActionResult> EmployeeExecutorRoles(Guid id)
        {
            var emlpoyee = await _referencesService.GetEmployeeDtoAsync(id);
            ViewData["Title"] = emlpoyee.EmployeeName;

            var modelBuilder = new ViewModelBuilder()
                .AddViewTitle("Роли").SetInvisibleViewTitle()
                .AddViewDescription("Роли")
                .AddTableMethodAction(Url.Action(nameof(GetEmployeeExecutorRolesDataJson), new { id = id }))
                .AddEditMethodAction(Url.Action(nameof(EmployeeExecutorRoleChangeModal)))
                .AddRemoveMethodAction(Url.Action(nameof(EmployeeExecutorRoleRemove)));
            return View("Employees/Details/ExecutionRoles", modelBuilder);
        }

        public IActionResult GetEmployeeExecutorRolesDataJson(IDataTablesRequest request, Guid id)
        {
            (var responseData, int totalCount, int filteredCount) = _referencesService.GetEmployeeExecutorRoles(ref request, id);

            var response = DataTablesResponse.Create(request, totalCount, filteredCount, responseData);

            return new DataTablesJsonResult(response, true);
        }

        public async Task<IActionResult> EmployeeExecutorRoleChangeModal(Guid employeeId, ActionType actionType)
        {
            var roles = await _referencesService.GetAllRolesExecutorAsync();

            // исключить уже добавленных
            var added = await _referencesService.GetAllEmployeeExecutorRolesAsync(employeeId);
            roles.RemoveAll(x => added.Where(x => x.SEmployeesId == employeeId).Select(a => a.SRolesExecutorId).ToArray().Contains(x.Id));
            // ***************************************************

            ViewBag.Roles = new SelectList(roles, nameof(RoleExecutorDto.Id), nameof(RoleExecutorDto.RoleName));

            var model = new ModalViewModelBuilder()
                .AddModel(new EmployeeExecutionRoleModelDto() { SEmployeesId = employeeId })
                .AddModalTitle("Добавление роли исполнения сотрудника")
                .AddModalViewPath("~/Views/Reference/Employees/Details/Modals/_ModalChangeEmployeeExecutionRole.cshtml")
                .AddActionType(actionType)
                .AddModalType(ModalType.LARGE);
            return ModalLayoutView(model);
        }

        [HttpPost]
        public async Task EmployeeExecutorRoleRemove(Guid id, string comment)
        {
            try
            {
                await _referencesService.RemoveEmployeeExecutorRoleAsync(id, comment);

                ShowSuccess(MessageDescription.RemoveSuccess);
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task EmployeeExecutorRoleSaveAsync(EmployeeRoleExecutorModelDto input, ActionType actionType)
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
                    await _referencesService.AddEmployeeExecutorRoleAsync(input);

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
