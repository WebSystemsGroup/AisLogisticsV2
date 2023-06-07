using AisLogistics.App.Controllers.Reference;
using AisLogistics.App.ViewModels.ModelBuilder;
using DataTables.AspNet.AspNetCore;
using DataTables.AspNet.Core;
using Microsoft.EntityFrameworkCore;
using SmartBreadcrumbs.Attributes;

namespace AisLogistics.App.Controllers.Systems
{
    public partial class SystemsController
    {
        [Breadcrumb("СМС", FromAction = nameof(Index), FromController = typeof(SystemsController))]
        public IActionResult SMS()
        {
            var modelBuilder = new ViewModelBuilder()
                .AddViewTitle("СМС").SetInvisibleViewTitle()
                .AddViewDescription("СМС")
                //.SetInvisibleViewDescription()
                .AddTableMethodAction(Url.Action(nameof(GetSMSDataJson)));
            return View("SMS/Index", modelBuilder);
        }

        public async Task<IActionResult> GetSMSDataJson(IDataTablesRequest request)
        {
            try
            {
                var allDataCount = await _context.DServicesCustomersMessages.CountAsync() + await _context.AServicesCustomersMessages.CountAsync();

                var allData = _context.DServicesCustomersMessages.Select(s => new 
                {
                    Id = s.Id,
                    SetDate = s.DateAdd.ToString("dd.MM.yyyy hh:mm:ss"),
                    EnqueueDate = s.EnqueueDate.HasValue ? s.EnqueueDate.Value.ToString("dd.MM.yyyy hh:mm:ss") : string.Empty,
                    CasesId = s.DCasesId,
                    SmsText = s.TextMessage,
                    PhoneNumber = s.CustomerPhone,
                    SendStatus = s.Status,
                    SmsId = s.SmsId
                }
                ).AsEnumerable()
                .Concat(_context.AServicesCustomersMessages.Select(s => new
                {
                    Id = s.Id,
                    SetDate = s.DateAdd.ToString("dd.MM.yyyy hh:mm:ss"),
                    EnqueueDate = s.EnqueueDate.HasValue ? s.EnqueueDate.Value.ToString("dd.MM.yyyy hh:mm:ss") : string.Empty,
                    CasesId = s.ACasesId,
                    SmsText = s.TextMessage,
                    PhoneNumber = s.CustomerPhone,
                    SendStatus = s.Status,
                    SmsId = s.SmsId
                }
                ))
                .OrderByDescending(o => o.SetDate);

                var filteredData = String.IsNullOrWhiteSpace(request.Search.Value)
                    ? allData
                    : allData.Where(w => EF.Functions.Like(w.CasesId.ToLower(), $"%{request.Search.Value.ToLower()}%"));

                var dataPage = filteredData.Skip(request.Start).Take(request.Length).ToList();

                var response = DataTablesResponse.Create(request, allDataCount, filteredData.Count(), dataPage);

                return new DataTablesJsonResult(response, true);
            }
            catch(Exception ex)
            {
                return null;
            }
            
        }
    }
}
