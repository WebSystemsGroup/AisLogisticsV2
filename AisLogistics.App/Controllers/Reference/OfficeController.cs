using AisLogistics.App.Models;
using AisLogistics.App.Models.DTO.References;
using AisLogistics.App.ViewModels.ModelBuilder;
using AisLogistics.DataLayer.Common.Dto.Reference;
using AisLogistics.DataLayer.Entities.Models;
using DataTables.AspNet.AspNetCore;
using DataTables.AspNet.Core;
using Microsoft.AspNetCore.Mvc.Rendering;
using SmartBreadcrumbs.Attributes;

namespace AisLogistics.App.Controllers.Reference
{
    public partial class ReferenceController
    {
        [Breadcrumb("Офисы", FromAction = nameof(Index), FromController = typeof(ReferenceController))]
        public async Task<IActionResult> Offices()
        {
            var officeType = await _referencesService.GetOfficeTypesAsync();
            string officeTypeOptions = officeType == null ? "" : string.Join("", officeType.Select(x => $"<option value=\"{x.Key}\">{x.Value}</option>"));

            var officeParents = await _referencesService.GetOfficesParentAllAsync();
            string officeParentsOptions = officeParents == null ? "" : string.Join("", officeParents.Where(w=>w.CountChild>0).Select(x => $"<option value=\"{x.Id}\">{x.OfficeName} ({x.CountChild})</option>"));

            var modelBuilder = new ViewModelBuilder()
                .AddViewTitle("Офисы").SetInvisibleViewTitle()
                .AddViewDescription("Справочник офисов")
                .AddModel((officeTypeOptions, officeParentsOptions))
                .AddTableMethodAction(Url.Action(nameof(GetOfficesDataJson)))
                .AddEditMethodAction(Url.Action(nameof(OfficeChangeModal)))
                .AddRemoveMethodAction(Url.Action(nameof(OfficeRemove)));
            return View("Offices/Index", modelBuilder);
        }

        [Breadcrumb("ViewData.Title", FromAction = nameof(Offices), FromController = typeof(ReferenceController))]
        public IActionResult OfficeDetails(Guid id)
        {
            ViewData["Title"] = _context.SOffices.Find(id)?.OfficeNameSmall;
            return View("Offices/Details/Index");
        }

        public IActionResult GetOfficesDataJson(IDataTablesRequest request)
        {
            (var responseData, int totalCount, int filteredCount) = _referencesService.GetOffices(request);

            var response = DataTablesResponse.Create(request, totalCount, filteredCount, responseData);

            return new DataTablesJsonResult(response, true);
        }

        public async Task<IActionResult> OfficeChangeModal(Guid? id, ActionType actionType)
        {
            var ftp = await _referencesService.GetFTPAllAsync();

            ViewBag.Offices = new SelectList(await _referencesService.GetAllOfficesAsync(), nameof(OfficeDto.Id), nameof(OfficeDto.OfficeName));
            ViewBag.OfficesType = new SelectList(await _referencesService.GetOfficeTypesAsync(), "Key", "Value");
            ViewBag.FTP = new SelectList(ftp.Select(s=> new {s.Id, Name = $"{s.FtpServer} {s.FtpLogin}" }), "Id", "Name");

            var model = new ModalViewModelBuilder()
                .AddModel(actionType == ActionType.Add ? new OfficeModelDto() : await _referencesService.GetOfficeDtoAsync(id))
                .AddModalTitle((actionType == ActionType.Add ? "Добавление" : "Изменение") + " офиса")
                .AddModalViewPath("~/Views/Reference/Offices/_ModalChangeOffice.cshtml")
                .AddActionType(actionType)
                .AddModalType(ModalType.LARGE);
            return ModalLayoutView(model);
        }

        public async Task<IActionResult> OfficeInfoModal(Guid id)
        {
            var model = new ModalViewModelBuilder()
                .AddModel(await _referencesService.GetOfficeDtoAsync(id))
                .HideSubmitButton()
                .AddModalTitle("Информация об офисе")
                .AddModalViewPath("~/Views/Reference/Offices/_ModalInfoOffice.cshtml")
                .AddActionType(ActionType.Edit)
                .AddModalType(ModalType.LARGE);
            return ModalLayoutView(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task OfficeSaveAsync(OfficeModelDto input, ActionType actionType)
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

                input.SEmployeesIdAdd = (Guid)employeeId;
                input.EmployeesFioAdd = employeeName;

                try
                {
                    //await _context.SOffices.AddAsync(office);
                    //await _context.SaveChangesAsync();
                    await _referencesService.AddOfficeAsync(input);

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
                    await _referencesService.UpdateOfficeAsync(input);
                    ShowSuccess(MessageDescription.EditSuccess);
                }
                catch (Exception ex)
                {
                    ShowError(ex.Message);
                }
                //var update = await _context.SOffices.FindAsync(office.Id);
                //if (update is null)
                //{
                //    ShowError(MessageDescription.Error);
                //    return;
                //}

                //await _context.SaveChangesAsync();
                //ShowSuccess(MessageDescription.EditSuccess);
            }
        }

        [HttpPost]
        public async Task OfficeRemove(Guid id, string comment)
        {
            try
            {
                await _referencesService.RemoveOfficeAsync(id, comment);

                ShowSuccess(MessageDescription.RemoveSuccess);
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }

            //var update = await _context.SOffices.FindAsync(id);
            //if (update is null)
            //{
            //    ShowError(MessageDescription.Error);
            //    return;
            //}
            //update.IsRemove = true;
            //update.Commentt = comment;
            //await _context.SaveChangesAsync();
            //ShowSuccess(MessageDescription.RemoveSuccess);
        }
    }
}
