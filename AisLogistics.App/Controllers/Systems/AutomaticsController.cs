using AisLogistics.App.Models.DTO.Automatics;
using AisLogistics.App.ViewModels.ModelBuilder;
using AisLogistics.App.ViewModels.Systems.Automatics;
using DataTables.AspNet.AspNetCore;
using DataTables.AspNet.Core;
using Microsoft.EntityFrameworkCore;
using Sentry;
using SmartBreadcrumbs.Attributes;

namespace AisLogistics.App.Controllers.Systems
{
    public partial class SystemsController
    {
        [Breadcrumb("Автоматика", FromAction = nameof(Index), FromController = typeof(SystemsController))]
        public async Task<IActionResult> Automatics()
        {
            var data = await _context.SAutomaticOperations
                .Select(s => new AutomaticsDto
                {
                    Id = s.Id,
                    OperationName = s.OperationName,
                    DateStart = s.DAutomaticLogs.OrderByDescending(o => o.DateStart).ThenByDescending(t => t.TimeStart).Select(s => s.DateStart.Add(s.TimeStart)).FirstOrDefault(),
                })
                .OrderBy(o => o.Id).ToListAsync();

            var modelBuilder = new ViewModelBuilder()
                .AddViewTitle("Автоматика")
                .SetElementName("automatic-logs-datatable")
                .AddModel(data)
                .AddTableMethodAction(Url.Action(nameof(GetAutomaticLogsDataJson)));

            return View("Automatics/Index2", modelBuilder);
        }

        public async Task<IActionResult> GetAutomaticOperrationDataJson(IDataTablesRequest request)
        {
            var count = await _context.SAutomaticOperations.CountAsync();
            var data = await _context.SAutomaticOperations.Select(s => new { s.Id, s.OperationName }).OrderBy(o => o.Id).Skip(request.Start).Take(request.Length).ToListAsync();

            var response = DataTablesResponse.Create(request, count, count, data);

            return new DataTablesJsonResult(response, true);
        }

        //[Breadcrumb("Автоматика", FromAction = nameof(Index), FromController = typeof(SystemsController))]
        public async Task<IActionResult> AutomaticsLogs(int Id)
        {

            var data = await _context.DAutomaticLogs
                  .Where(w => w.SAutomaticOperationId == Id)
                  .OrderByDescending(o => o.DateStart)
                  .ThenByDescending(t => t.TimeStart)
                  .Select(s => new AutomaticsLogsDto
                  {
                      Id = s.Id,
                      DateStart = s.DateStart.Add(s.TimeStart),
                      TimeStart = s.TimeStart.ToString(@"dd\:hh\:mm")
                  })
                  .ToListAsync();


            var modelBuilder = new ViewModelBuilder()
               .AddViewTitle("Автоматика1").SetInvisibleViewTitle()
               .AddViewDescription("Автоматика2").SetInvisibleViewDescription()
               .AddTableMethodAction(Url.Action(nameof(GetAutomaticLogsDataJson)));
            //.AddModel(data);

            return PartialView("Automatics/Logs3", modelBuilder);
        }

        public async Task<IActionResult> GetAutomaticLogsDataJson(IDataTablesRequest request, SearchAutomaticLogsRequestData requestData)
        {
            try
            {
                var count = await _context.DAutomaticLogs.Where(w => w.SAutomaticOperationId == requestData.AutomaticId).CountAsync();

                var data = await _context.DAutomaticLogs
                    .Where(w => w.SAutomaticOperationId == requestData.AutomaticId)
                    .OrderByDescending(o => o.DateStart)
                    .ThenByDescending(t => t.TimeStart)
                    .Select(s => new { s.Id, DateStart = s.DateStart.Add(s.TimeStart).ToString() })
                    .Skip(request.Start).Take(request.Length)
                    .ToListAsync();

                var response = DataTablesResponse.Create(request, count, count, data);

                return new DataTablesJsonResult(response, true);
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}
