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
    // Результаты
    public partial class ReferenceController
    {
        [Breadcrumb("ViewData.Title", FromAction = nameof(Services), FromController = typeof(ReferenceController))]
        [Route("[controller]/Services/Details/{id}/Results")]
        public async Task<IActionResult> ServiceDocumentsResults(Guid id)
        {
            var service = await _referencesService.GetServiceDtoAsync(id);
            if (service is null) return NotFound();
            ViewData["Title"] = service.ServiceNameSmall.Length > 330 ? service.ServiceNameSmall[..330] + "..." : service.ServiceNameSmall;

            var procedures = await _referencesService.GetAllServiceProceduresAsync(id);
            string proceduresOptions = procedures == null ? "" : string.Join("", procedures.Select(x => $"<option value=\"{x.Id}\">{x.ProcedureName}</option>"));

            var modelBuilder = new ViewModelBuilder()
                .AddViewTitle(service.ServiceNameSmall).SetInvisibleViewTitle()
                .AddModel(proceduresOptions)
                .AddViewDescription("Результаты")
                .AddTableMethodAction(Url.Action(nameof(GetServiceDocumentsResultsDataJson), new { id = id }))
                .AddEditMethodAction(Url.Action(nameof(ServiceDocumentResultChangeModal)))
                .AddRemoveMethodAction(Url.Action(nameof(ServiceDocumentResultRemove)));


            return View("Services/Details/Results", modelBuilder);
        }

        public IActionResult GetServiceDocumentsResultsDataJson(IDataTablesRequest request, Guid id)
        {

            (var responseData, int totalCount, int filteredCount) = _referencesService.GetServiceDocumentsResults(request, id);

            var response = DataTablesResponse.Create(request, totalCount, filteredCount, responseData);

            return new DataTablesJsonResult(response, true);
        }

        public async Task<IActionResult> ServiceDocumentResultChangeModal(Guid? id, Guid serviceId, ActionType actionType)
        {
            try
            {
                var procedure = await _referencesService.GetAllServiceProceduresAsync(serviceId);
                var documents = await _referencesService.GetAllDocumentsAsync();

                //исключить уже добавленных
                var added = await _referencesService.GetAllServiceDocumentsResults(serviceId);
                if (id is null)
                {
                    // добавление
                    documents.RemoveAll(x => added.Where(x => x.SServicesId == serviceId).Select(o => o.SDocumentsId).ToArray().Contains(x.Id));
                }
                else
                {
                    // измениние
                    documents.RemoveAll(x => added.Where(x => x.Id != id && x.SServicesId == serviceId).Select(o => o.SDocumentsId).ToArray().Contains(x.Id));
                }
                // ***************************************************
                ViewBag.Procedure = new SelectList(procedure, nameof(ServicesProcedureDto.Id), nameof(ServicesProcedureDto.ProcedureName));
                ViewBag.Documents = new SelectList(documents, nameof(DocumentDto.Id), nameof(DocumentDto.Name));

                var model = new ModalViewModelBuilder()
                    .AddModel(actionType == ActionType.Add ? new ServiceDocumentResultModelDto() { SServicesId = serviceId } : await _referencesService.GetServiceDocumentResultDtoAsync(id))
                    .AddModalTitle((actionType == ActionType.Add ? "Добавление" : "Изменение") + " результата документа")
                    .AddModalViewPath("~/Views/Reference/Services/Details/Modals/_ModalChangeServiceDocumentResult.cshtml")
                    .AddActionType(actionType)
                    .AddModalType(ModalType.LARGE);
                return ModalLayoutView(model);
            }
            catch (Exception ex)
            {
                var e = ex;
                return null;
            }
        }

        [HttpPost]
        public async Task ServiceDocumentResultRemove(Guid id, string comment)
        {
            try
            {
                await _referencesService.RemoveServiceDocumentResultAsync(id, comment);

                ShowSuccess(MessageDescription.RemoveSuccess);
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task ServiceDocumentResultSaveAsync(ServiceDocumentResultModelDto input, ActionType actionType)
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
                    await _referencesService.AddServiceDocumentResultAsync(input);

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
                    await _referencesService.UpdateServiceDocumentResultAsync(input);
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
