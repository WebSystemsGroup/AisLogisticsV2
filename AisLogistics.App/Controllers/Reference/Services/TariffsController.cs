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
    public partial class ReferenceController
    {
        [Breadcrumb("ViewData.Title", FromAction = nameof(Services), FromController = typeof(ReferenceController))]
        [Route("[controller]/Services/Details/{id}/Tariffs")]
        public async Task<IActionResult> ServiceTariffs(Guid id)
        {
            var service = await _referencesService.GetServiceDtoAsync(id);
            if (service is null) return NotFound();
            ViewData["Title"] = service.ServiceNameSmall.Length > 330 ? service.ServiceNameSmall[..330] + "..." : service.ServiceNameSmall;


            var procedures = await _referencesService.GetAllServiceProceduresAsync(id);
            string proceduresOptions = procedures == null ? "" : string.Join("", procedures.Select(x => $"<option value=\"{x.Id}\">{x.ProcedureName}</option>"));
            var customers = await _referencesService.GetAllServiceCustomersAsync(id);
            string customersOptions = customers == null ? "" : string.Join("", customers.Select(x => $"<option value=\"{x.SServicesCustomerTypeId}\">{x.TypeName}</option>"));

            var modelBuilder = new ViewModelBuilder()
                .AddViewTitle(service.ServiceNameSmall).SetInvisibleViewTitle()
                .AddViewDescription("Тарифы")
                .AddModel((customersOptions, proceduresOptions))
                .AddTableMethodAction(Url.Action(nameof(GetServiceCustomerTariffDataJson), new { id = id }))
                .AddEditMethodAction(Url.Action(nameof(ServiceCustomerTariffChangeModal)))
                .AddRemoveMethodAction(Url.Action(nameof(ServiceCustomerTariffRemove)));

            return View("Services/Details/Tariffs", modelBuilder);
        }

        public IActionResult GetServiceCustomerTariffDataJson(IDataTablesRequest request, Guid id)
        {

            (var responseData, int totalCount, int filteredCount) = _referencesService.GetServiceCustomerTariffs(request, id);

            var response = DataTablesResponse.Create(request, totalCount, filteredCount, responseData);

            return new DataTablesJsonResult(response, true);
        }

        public async Task<IActionResult> ServiceCustomerTariffChangeModal(Guid? id, Guid serviceId, ActionType actionType)
        {
            var serviceCustomers = await _referencesService.GetAllServiceCustomersAsync(serviceId);
     
            ViewBag.ServiceCustomers = new SelectList(serviceCustomers, nameof(ServiceCustomerDto.Id), nameof(ServiceCustomerDto.TypeName));
            ViewBag.TariffTypes = new SelectList(await _referencesService.GetAllServiceTariffTypesAsync(), nameof(ServiceTariffTypeDto.Id), nameof(ServiceTariffTypeDto.TariffTypeName));
            ViewBag.DayTypes = new SelectList(await _referencesService.GetAllServiceWeekTypesAsync(), "Key", "Value");
            ViewBag.Procedures = new SelectList(await _referencesService.GetAllServiceProceduresAsync(serviceId),
                nameof(ServicesProcedureDto.Id),
                nameof(ServicesProcedureDto.ProcedureName));

            var model = new ModalViewModelBuilder()
                .AddModel(actionType == ActionType.Add ? new ServiceCustomerTariffModelDto() : await _referencesService.GetServiceCustomerTariffAsync(id))
                .AddModalTitle((actionType == ActionType.Add ? "Добавление" : "Изменение") + " тарифа пользователя")
                .AddModalViewPath("~/Views/Reference/Services/Details/Modals/_ModalChangeCustomerTariff.cshtml")
                .AddActionType(actionType)
                .AddModalType(ModalType.LARGE);
            return ModalLayoutView(model);
        }

        [HttpPost]
        public async Task ServiceCustomerTariffRemove(Guid id, string comment)
        {
            try
            {
                await _referencesService.RemoveServiceCustomerTariffAsync(id, comment);

                ShowSuccess(MessageDescription.RemoveSuccess);
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task ServiceCustomerTariffSaveAsync(ServiceCustomerTariffModelDto input, ActionType actionType)
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
                input.EmployeeFioAdd = employeeName;

                try
                {
                    await _referencesService.AddServiceCustomerTariffAsync(input);

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
                    await _referencesService.UpdateServiceCustomerTariffAsync(input);
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
