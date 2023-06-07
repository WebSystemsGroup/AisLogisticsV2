using AisLogistics.App.Models;
using AisLogistics.App.Models.DTO.References;
using AisLogistics.App.ViewModels.ModelBuilder;
using AisLogistics.DataLayer.Common.Dto.Reference;
using AisLogistics.DataLayer.Common.Dto.Systems;
using AisLogistics.DataLayer.Entities.Models;
using DataTables.AspNet.AspNetCore;
using DataTables.AspNet.Core;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SmartBreadcrumbs.Attributes;

namespace AisLogistics.App.Controllers.Systems
{
    public partial class SystemsController
    {
        [Breadcrumb("Терминалы", FromAction = nameof(Index), FromController = typeof(SystemsController))]
        public IActionResult Terminals()
        {
            var modelBuilder = new ViewModelBuilder()
                .AddViewTitle("Терминалы").SetInvisibleViewTitle()
                .AddViewDescription("Терминалы оплаты")
                .AddTableMethodAction(Url.Action(nameof(GetTerminalsDataJson)))
                .AddEditMethodAction(Url.Action(nameof(TerminalChangeModal)))
                .AddRemoveMethodAction(Url.Action(nameof(TerminalRemove)));
            return View("Terminals/Index", modelBuilder);
        }

        public async Task<IActionResult> GetTerminalsDataJson(IDataTablesRequest request)
        {
            var allDataCount = _context.SOfficesAcquirings.Count();
            var allData = _context.SOfficesAcquirings.OrderByDescending(o => o.AcquiringName);

            var filteredData = String.IsNullOrWhiteSpace(request.Search.Value)
                ? allData
                : allData.Where(w => EF.Functions.Like(w.AcquiringName.ToLower(), $"%{request.Search.Value.ToLower()}%"));

            var dataPage = filteredData.Skip(request.Start).Take(request.Length)
                .Select(s => new
                {
                    s.Id,
                    s.AcquiringName,
                    s.IpAddress,
                    s.SOffices.OfficeName
                });

            var response = DataTablesResponse.Create(request, allDataCount, filteredData.Count(), dataPage);

            return new DataTablesJsonResult(response, true);
        }

        public async Task<IActionResult> TerminalChangeModal(Guid id, ActionType actionType)
        {

            ViewBag.Offices = new SelectList(await _referencesService.GetAllOfficesTypeMfcAsync(), nameof(OfficeDto.Id), nameof(OfficeDto.OfficeName));

            var request = new SOfficeAcquiringDto();

            if (actionType == ActionType.Edit)
            {
                var result = await _context.SOfficesAcquirings.FindAsync(id);
                if (result == null)
                    throw new InvalidOperationException("Данные не найдены!");

                request = _mapper.Map<SOfficeAcquiringDto>(result);
            }

            var model = new ModalViewModelBuilder()
                .AddModel(request)
                .AddModalTitle((actionType == ActionType.Add ? "Добавление" : "Изменение") + " терминала")
                .AddModalViewPath("~/Views/Systems/Terminals/_ModalChangeTerminal.cshtml")
                .AddActionType(actionType)
                .AddModalType(ModalType.LARGE);
            return ModalLayoutView(model);
        }

        [HttpPost]
        public async Task TermianlSaveAsync(SOfficeAcquiringDto model, ActionType actionType)
        {
            try
            {
                if(actionType ==ActionType.Add)
                {
                    var entity = _mapper.Map<SOfficesAcquiring>(model);

                    await _context.SOfficesAcquirings.AddAsync(entity);
                    await _context.SaveChangesAsync();
                    ShowSuccess(MessageDescription.AddSuccess);
                }
                else
                {
                    var entity = await _context.SOfficesAcquirings.FindAsync(model.Id);
                    if (entity == null)
                        throw new InvalidOperationException("Данные не найдены!");

                    _mapper.Map(model, entity);

                    await _context.SaveChangesAsync();
                    ShowSuccess(MessageDescription.EditSuccess);
                }
            }
            catch(Exception ex) 
            {
                ShowError(ex.Message);
            }
          
        }

        [HttpPost]
        public async Task TerminalRemove(Guid id, string comment)
        {
            var result = await _context.SOfficesAcquirings.FindAsync(id);
            if (result is null)
            {
                ShowError(MessageDescription.Error);
                return;
            }

            await _context.SOfficesAcquirings.SingleDeleteAsync(result);
            await _context.SaveChangesAsync();
            ShowSuccess(MessageDescription.RemoveSuccess);
        }

    }
}
