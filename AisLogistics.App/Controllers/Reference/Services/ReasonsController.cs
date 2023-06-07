using AisLogistics.App.Models;
using AisLogistics.App.ViewModels.ModelBuilder;
using AisLogistics.DataLayer.Common.Dto.Reference;
using DataTables.AspNet.AspNetCore;
using DataTables.AspNet.Core;
using Microsoft.AspNetCore.Mvc.Rendering;
using SmartBreadcrumbs.Attributes;

namespace AisLogistics.App.Controllers.Reference
{
    // Основания
    public partial class ReferenceController
    {
        [Breadcrumb("ViewData.Title", FromAction = nameof(Services), FromController = typeof(ReferenceController))]
        [Route("[controller]/Services/Details/{id}/Reasons/{rt}")]
        public async Task<IActionResult> ServiceReasons(Guid id, int rt)
        {
            var service = await _referencesService.GetServiceDtoAsync(id);
            if (service is null) return NotFound();
            ViewData["Title"] = service.ServiceNameSmall.Length > 330 ? service.ServiceNameSmall[..330] + "..." : service.ServiceNameSmall;

            var modelBuilder = new ViewModelBuilder()
                .AddViewTitle(service.ServiceNameSmall).SetInvisibleViewTitle()
                .AddViewDescription("Основания")
                .AddTableMethodAction(Url.Action(nameof(GetServiceReasonsDataJson), new { id = id, rt = rt }))
                .AddEditMethodAction(Url.Action(nameof(ServiceReasonsChangeModal)))
                .AddRemoveMethodAction(Url.Action(nameof(ServiceReasonRemove)));


            return View("Services/Details/Reasons", modelBuilder);
        }

        public IActionResult GetServiceReasonsDataJson(IDataTablesRequest request, Guid id, int rt)
        {
            (var responseData, int totalCount, int filteredCount) = _referencesService.GetServiceReasons(request, id, rt);

            var response = DataTablesResponse.Create(request, totalCount, filteredCount, responseData);

            return new DataTablesJsonResult(response, true);

            //ViewBag.DayTypes = new SelectList(_referencesService.GetAllServiceWeekTypes(), "Key", "Value");
        }

        public async Task<IActionResult> ServiceReasonsChangeModal(Guid? id, Guid serviceId, int rt, ActionType actionType)
        {
            ViewBag.DayTypes = new SelectList(await _referencesService.GetAllServiceWeekTypesAsync(), "Key", "Value");

            var model = new ModalViewModelBuilder()
                .AddModel(actionType == ActionType.Add ?
                    new ServiceReasonModelDto() { SServicesId = serviceId, ReasonType = rt } :
                    await _referencesService.GetServiceReasonAsync(id))
                .AddModalTitle((actionType == ActionType.Add ? "Добавление" : "Изменение") + " основания")
                .AddModalViewPath("~/Views/Reference/Services/Details/Modals/_ModalChangeReason.cshtml")
                .AddActionType(actionType)
                .AddModalType(ModalType.LARGE);
            return ModalLayoutView(model);
        }

        [HttpPost]
        public async Task ServiceReasonRemove(Guid id, string comment)
        {
            try
            {
                await _referencesService.RemoveServiceReasonAsync(id, comment);

                ShowSuccess(MessageDescription.RemoveSuccess);
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task ServiceReasonSaveAsync(ServiceReasonModelDto input, ActionType actionType)
        {
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
                    await _referencesService.AddServiceReasonAsync(input);

                    ShowSuccess(MessageDescription.AddSuccess);
                }
                catch (Exception ex)
                {
                    ShowError(ex.Message);
                }
            }
            else
            {
                try
                {
                    await _referencesService.UpdateServiceReasonAsync(input);
                    ShowSuccess(MessageDescription.EditSuccess);
                }
                catch (Exception ex)
                {
                    ShowError(ex.Message);
                }
            }
        }
    }
}
