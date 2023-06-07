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
    //Активность
    public partial class ReferenceController
    {
        [Breadcrumb("ViewData.Title", FromAction = nameof(Services), FromController = typeof(ReferenceController))]
        [Route("[controller]/Services/Details/{id}/Activities")]
        public async Task<IActionResult> ServiceActivities(Guid id, int rt)
        {
            var service = await _referencesService.GetServiceDtoAsync(id);
            if (service is null) return NotFound();
            ViewData["Title"] = service.ServiceNameSmall.Length > 330 ? service.ServiceNameSmall[..330] + "..." : service.ServiceNameSmall;

            var modelBuilder = new ViewModelBuilder()
                .AddViewTitle(service.ServiceNameSmall).SetInvisibleViewTitle()
                .AddViewDescription("Активность")
                .AddTableMethodAction(Url.Action(nameof(GetServiceAcyivitiesDataJson), new { id = id }))
                .AddEditMethodAction(Url.Action(nameof(ServiceActivityChangeModal)))
                .AddRemoveMethodAction(Url.Action(nameof(ServiceActivityRemove)));


            return View("Services/Details/Activities", modelBuilder);
        }

        public async Task<IActionResult> GetServiceAcyivitiesDataJson(IDataTablesRequest request, Guid id)
        {
            (var responseData, int totalCount, int filteredCount) = await _referencesService.GetServiceActivitiesAsync(id, request.Start, request.Length);

            var response = DataTablesResponse.Create(request, totalCount, filteredCount, responseData);

            return new DataTablesJsonResult(response, true);
        }

        public async Task<IActionResult> ServiceActivityChangeModal(Guid serviceId, ActionType actionType)
        {
            var offices = await _referencesService.GetAllOfficesTypeMfcAndTospAsync();

            ViewBag.Offices = new SelectList(offices, nameof(OfficeDto.Id), nameof(OfficeDto.OfficeName));

            /*var lastModified = await _referencesService.GetLastServiceActivityDtoAsync(serviceId);
            ViewBag.MaxDate = lastModified.DateStart;*/

            var model = new ModalViewModelBuilder()
                .AddModel(new ServiceActivityModelDto {SServicesId=serviceId })
                .AddModalTitle("Добавление активности услуги")
                .AddModalViewPath("~/Views/Reference/Services/Details/Modals/_ModalChangeServiceActivity.cshtml")
                .AddActionType(actionType)
                .AddModalType(ModalType.LARGE);
            return ModalLayoutView(model);
        }

        [HttpPost]
        public async Task ServiceActivityRemove(Guid id, string comment)
        {
            try
            {
                await _referencesService.RemoveServiceActivityAsync(id, comment);

                ShowSuccess(MessageDescription.RemoveSuccess);
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task ServiceActivitySaveAsync(ServiceActivityModelDto input, ActionType actionType)
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

                input.SEmployeesIdAdd = (Guid)employeeId;
                input.EmployeeFioAdd = employeeName;

                try
                {
                    await _referencesService.AddServiceActivityAsync(input);

                    ShowSuccess(MessageDescription.AddSuccess);
                }
                catch (Exception ex)
                {
                    ShowError(ex.Message);
                }
            }
        }
    }
}
