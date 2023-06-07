using AisLogistics.App.Models;
using AisLogistics.App.Models.DTO.Identity;
using AisLogistics.App.Models.DTO.References;
using AisLogistics.App.ViewModels.ModelBuilder;
using DataTables.AspNet.AspNetCore;
using DataTables.AspNet.Core;
using Microsoft.AspNetCore.Mvc;
using SmartBreadcrumbs.Attributes;

namespace AisLogistics.App.Controllers.Reference
{
    public partial class ReferenceController
    {
        [Breadcrumb("ViewData.Title", FromAction = nameof(Employees), FromController = typeof(ReferenceController))]
        [Route("[controller]/Employees/Details/{id}/Auth")]
        public async Task<IActionResult> EmployeeAuthDetails(Guid id)
        {
            var emlpoyee = await _referencesService.GetEmployeeDtoAsync(id);
            ViewData["Title"] = emlpoyee.EmployeeName;

            var modelBuilder = new ViewModelBuilder()
                .AddViewTitle("Авторизация")
                .SetInvisibleViewTitle()
                .AddViewDescription("Авторизация")
                //.SetElementName("calendar")
                .AddTableMethodAction(Url.Action(nameof(GetEmployeeRolesDataJson), new { id = emlpoyee.AspNetUserId }))
                .AddEditMethodAction(Url.Action(nameof(EmployeeChangeRole)))
                //.AddRemoveMethodAction(Url.Action(nameof(CalendarRemove)))
                .AddModel(emlpoyee.AspNetUserId);
            return View("Employees/Details/AuthDetails", modelBuilder);
        }

        public async Task<IActionResult> GetEmployeeRolesDataJson(IDataTablesRequest request, Guid id)
        {
            var userRoles = await _identityService.GetUserRolesAsync(id);

            var responseData = (await _identityService.GetRolesAsync())
                .Select(x => new UserRoleItem(x.Id, x.Name, userRoles.Any(r => r.Id == x.Id)))
                .OrderBy(x => x.RoleName)
                .ToList();

            int totalCount = responseData.Count;

            var response = DataTablesResponse.Create(request, totalCount, totalCount, responseData);

            return new DataTablesJsonResult(response, true);
        }

        public async Task<IActionResult> EmployeePasswordChangeModal(Guid id)
        {
            var model = new ModalViewModelBuilder()
                .AddModel(new EmployeePasswordChangeDto()
                {
                    EmployeeId = id
                })
                .AddModalTitle("Изменить пароль сотрудника")
                .AddModalViewPath("~/Views/Reference/Employees/Details/Modals/_ModalChangeEmployeePassword.cshtml")
                //.AddActionType(actionType)
                .AddModalType(ModalType.LARGE);

            return ModalLayoutView(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task EmployeePasswordSaveAsync(EmployeePasswordChangeDto input)
        {
            var editedEmployee = await _referencesService.GetEmployeeDtoAsync(input.EmployeeId);

            try
            {
                await _identityService.UserChangePasswordAsync(new UserPasswordChangeDto<Guid>()
                {
                    UserId = (Guid)editedEmployee.AspNetUserId,
                    NewPassword = input.NewPassword,
                    ConfirmPassword = input.ConfirmPassword
                });

                ShowSuccess(MessageDescription.EditSuccess);
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        [HttpPost]
        public async Task EmployeeChangeRole(UserRoleChangeDto input)
        {
            try
            {
                if (input.Selected)
                {
                    await _identityService.CreateUserRoleAsync(input.UserId, input.RoleId);
                }
                else 
                {
                    await _identityService.DeleteUserRoleAsync(input.UserId, input.RoleId);
                }

                ShowSuccess(MessageDescription.EditSuccess);
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }
    }
}
