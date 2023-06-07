﻿using AisLogistics.App.Models;
using AisLogistics.App.Models.DTO.References;
using AisLogistics.App.ViewModels.ModelBuilder;
using AisLogistics.DataLayer.Common.Dto.Reference;
using DataTables.AspNet.AspNetCore;
using DataTables.AspNet.Core;
using Microsoft.AspNetCore.Mvc.Rendering;
using SmartBreadcrumbs.Attributes;

namespace AisLogistics.App.Controllers.Reference
{
    // Способы обращений за услугой
    public partial class ReferenceController
    {
        [Breadcrumb("ViewData.Title", FromAction = nameof(Services), FromController = typeof(ReferenceController))]
        [Route("[controller]/Services/Details/{id}/WaysGet/{wt}")]
        public async Task<IActionResult> ServiceWaysGet(Guid id, int wt)
        {
            var service = await _referencesService.GetServiceDtoAsync(id);
            if (service is null) return NotFound();
            ViewData["Title"] = service.ServiceNameSmall.Length > 330 ? service.ServiceNameSmall[..330] + "..." : service.ServiceNameSmall;

            var modelBuilder = new ViewModelBuilder()
                .AddViewTitle(service.ServiceNameSmall).SetInvisibleViewTitle()
                .AddViewDescription("Способы обращения")
                .AddTableMethodAction(Url.Action(nameof(GetServiceWaysGetDataJson), new { id = id, wt = wt }))
                .AddEditMethodAction(Url.Action(nameof(ServiceWaysGetChangeModal)))
                .AddRemoveMethodAction(Url.Action(nameof(ServiceWaysGetRemove)));


            return View("Services/Details/WaysGet", modelBuilder);
        }

        public IActionResult GetServiceWaysGetDataJson(IDataTablesRequest request, Guid id, int wt)
        {

            (var responseData, int totalCount, int filteredCount) = _referencesService.GetServiceWaysGet(request, id, wt);

            var response = DataTablesResponse.Create(request, totalCount, filteredCount, responseData);

            return new DataTablesJsonResult(response, true);
        }

        public async Task<IActionResult> ServiceWaysGetChangeModal(Guid? id, Guid serviceId, int wt, ActionType actionType)
        {
            var waysGet = await _referencesService.GetAllServiceWaysGetAsync();

            //исключить уже добавленных
            var added = _referencesService.GetAllServiceWaysGet(serviceId, wt);

            if (id is null)
            {
                // добавление
                waysGet.RemoveAll(x => added.Where(x => x.SServicesId == serviceId).Select(a => a.SServicesWayGetId).ToArray().Contains(x.Id));
            }
            else
            {
                // измениние
                waysGet.RemoveAll(x => added.Where(x => x.Id != id && x.SServicesId == serviceId).Select(a => a.SServicesWayGetId).ToArray().Contains(x.Id));
            }

            // ***************************************************

            ViewBag.WaysGet = new SelectList(waysGet, nameof(WayGetDto.Id), nameof(WayGetDto.NameWay));

            var model = new ModalViewModelBuilder()
                .AddModel(actionType == ActionType.Add ?
                    new ServiceWayGetModelDto() { SServicesId = serviceId, WayType = wt } :
                    await _referencesService.GetServiceWayGetAsync(id))
                .AddModalTitle((actionType == ActionType.Add ? "Добавление" : "Изменение") + " способа обращения")
                .AddModalViewPath("~/Views/Reference/Services/Details/Modals/_ModalChangeWaysGet.cshtml")
                .AddActionType(actionType)
                .AddModalType(ModalType.LARGE);
            return ModalLayoutView(model);
        }

        [HttpPost]
        public async Task ServiceWaysGetRemove(Guid id, string comment)
        {
            try
            {
                await _referencesService.RemoveServiceWayGetAsync(id, comment);

                ShowSuccess(MessageDescription.RemoveSuccess);
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task ServiceWayGetSaveAsync(ServiceWayGetModelDto input, ActionType actionType)
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
                    await _referencesService.AddServiceWayGetAsync(input);

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
                    await _referencesService.UpdateServiceWayGetAsync(input);
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
