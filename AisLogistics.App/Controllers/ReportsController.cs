using AisLogistics.App.Models.DTO.References;
using AisLogistics.App.Models.Enums;
using AisLogistics.App.Models.Reports;
using AisLogistics.App.Service;
using AisLogistics.App.ViewModels.ModelBuilder;
using AisLogistics.App.ViewModels.Reports;
using AisLogistics.DataLayer.Entities.Models;
using AisLogistics.DataLayer.Utils;
using FastReport;
using FastReport.Export.OoXML;
using FastReport.Export.Pdf;
using FastReport.Utils;
using FastReport.Web;
using SmartBreadcrumbs.Attributes;
using System.Collections;

namespace AisLogistics.App.Controllers
{
    public class ReportsController : AbstractController
    {
        private readonly ILogger<ReportsController> _logger;
        private readonly IReferencesService _referencesService;
        private readonly IConfiguration _configuration;
        private readonly IReports _reports;

        public ReportsController(IReferencesService referencesService, EmployeeManager employeeManager, 
            ILogger<ReportsController> logger, IConfiguration configuration, IReports reports) : base(employeeManager)
        {
            _logger = logger;
            _referencesService = referencesService;
            _configuration = configuration;
            _reports = reports;
        }

        private async Task GetParametrs(WebReport rep, ReportViewRequestModel requestModel, string? employeeFio, string connection)
        {
            if (!string.IsNullOrEmpty(employeeFio))
            {
                rep.Report.SetParameterValue("EmployeeFio", employeeFio);
            }
            if (!string.IsNullOrEmpty(requestModel.DateStart))
            {
                rep.Report.SetParameterValue("DateStart", DateTime.Parse(requestModel.DateStart));
            }
            if (!string.IsNullOrEmpty(requestModel.DateStop))
            {
                rep.Report.SetParameterValue("DateStop", DateTime.Parse(requestModel.DateStop));
            }
            if (requestModel.MfcId.HasValue)
            {
                var Name = await _referencesService.GetOfficeDtoAsync(requestModel.MfcId.Value);
                rep.Report.SetParameterValue("Office", Name?.OfficeName ?? string.Empty);
            }
            if (requestModel.MfcIdList!=null)
            {
                string Name = string.Empty;
                if (requestModel.MfcIdList.Any(a => a == Guid.Empty))
                {
                    Name = "ВСЕ";
                }
                else
                {
                    var List = await _referencesService.GetAllOfficesTypeMfcAndTospAsync(requestModel.MfcIdList);
                    Name = string.Join(", ", List.Select(s => s.OfficeName).ToList());

                }
                rep.Report.SetParameterValue("Office", Name);
            }
            if (requestModel.EmployeeId.HasValue)
            {
                var Name = await _referencesService.GetEmployeeDtoAsync(requestModel.EmployeeId.Value);
                rep.Report.SetParameterValue("Employee", Name?.EmployeeName ?? string.Empty);
            }
            if (requestModel.EmployeeIdList != null)
            {
                string Name = string.Empty;
                if (requestModel.EmployeeIdList.Any(a => a == Guid.Empty))
                {
                    Name = "ВСЕ";
                }
                else
                {
                    var List = await _referencesService.GetEmployeesDtoAsync(requestModel.EmployeeIdList);
                    Name = string.Join(", ", List.Select(s => s.EmployeeName).ToList());

                }
                rep.Report.SetParameterValue("Employee", Name);
            }

            if (requestModel.ProviderId.HasValue)
            {
                var Name = await _referencesService.GetOfficeDtoAsync(requestModel.ProviderId.Value);
                rep.Report.SetParameterValue("Provider", Name?.OfficeName ?? string.Empty);
            }
            if (requestModel.ProviderIdList != null)
            {
                string Name = string.Empty;
                if (requestModel.ProviderIdList.Any(a => a == Guid.Empty))
                {
                    Name = "ВСЕ";
                }
                else
                {
                    var List = await _referencesService.GetServiceProvidersAll(requestModel.ProviderIdList);
                    Name = string.Join(", ", List.Select(s => s.OfficeName).ToList());

                }
                rep.Report.SetParameterValue("Provider", Name);
            }
            
            if (requestModel.ServiceId.HasValue)
            {
                var Name = await _referencesService.GetServiceDtoAsync(requestModel.ServiceId.Value);
                rep.Report.SetParameterValue("Services", Name?.ServiceName ?? string.Empty);
            }
            if (requestModel.ServiceIdList != null)
            {
                string Name = string.Empty;
                if (requestModel.ServiceIdList.Any(a => a == Guid.Empty))
                {
                    Name = "ВСЕ";
                }
                else
                {
                    var List = await _referencesService.GetServicesAll(requestModel.ServiceIdList);
                    Name = string.Join(", ", List.Select(s => s.ServiceName).ToList());
                }
                rep.Report.SetParameterValue("Services", Name);
            }
            if (requestModel.SmevId.HasValue)
            {
                var Name = await _referencesService.GetSmevServiceModelDtoAsync(requestModel.SmevId.Value);
                rep.Report.SetParameterValue("SmevServices", Name?.SmevName ?? string.Empty);
            }
            if (requestModel.SmevIdList != null)
            {
                string Name = string.Empty;
                if (requestModel.SmevIdList.Any(a => a == Guid.Empty))
                {
                    Name = "ВСЕ";
                }
                else
                {
                    var List = await _referencesService.GetAllSmevServicesAsync(requestModel.SmevIdList);
                    Name = string.Join(", ", List.Select(s => s.SmevName).ToList());
                }
                rep.Report.SetParameterValue("SmevServices", Name);
            }
            if (requestModel.SmevRequestId.HasValue)
            {
                var Name = await _referencesService.GetSmevRequestModelDtoAsync(requestModel.SmevRequestId.Value);
                rep.Report.SetParameterValue("SmevRequest", Name?.RequestName ?? string.Empty);
            }
            if (requestModel.SmevRequestIdList != null)
            {
                string Name = string.Empty;
                if (requestModel.SmevRequestIdList.Any(a => a == 0))
                {
                    Name = "ВСЕ";
                }
                else
                {
                    var List = await _referencesService.GetAllSmevRequestAsync(requestModel.SmevRequestIdList);
                    Name = string.Join(", ", List.Select(s => s.RequestName).ToList());
                }
                rep.Report.SetParameterValue("SmevRequest", Name);
            }
            if (rep.Report.Dictionary.Connections.Count != 0)
                rep.Report.Dictionary.Connections[0].ConnectionString = connection;
        }

        private async Task<byte[]?> ReportDataPrepare(byte[] reportFile, IList data, ReportViewRequestModel requestModel, BlankActionType type)
        {
            var connection = _configuration.GetConnectionString("DefaultConnection");
            var employee = await _employeeManager.GetNameAsync();

            RegisteredObjects.AddConnection(typeof(FastReport.Data.PostgresDataConnection));

            var rep = new WebReport();

            rep.Report.Load(new MemoryStream(reportFile));

            //rep.Report.Load(reportFile2);

            await GetParametrs(rep, requestModel, employee, connection);

            rep.Report.RegisterData(data, "Reports");
            rep.Report.GetDataSource("Reports").Enabled = true;
            ((DataBand)rep.Report.FindObject("Data1")).DataSource = rep.Report.GetDataSource("Reports");

            var isPrepare = rep.Report.Prepare();

            if (!isPrepare) return null;

            var strm = new MemoryStream();

            switch(type)
            {
                case BlankActionType.Pdf:
                    var export = new PDFExport();
                    rep.Report.Export(export, strm);
                    export.Dispose();
                    break;
                case BlankActionType.Exel:
                    var Excelexport = new Excel2007Export();
                    rep.Report.Export(Excelexport, strm);
                    Excelexport.Dispose();
                    break;
            }
            
            return strm.ToArray();
        }

        [Breadcrumb("Отчеты", FromAction = nameof(Index), FromController = typeof(HomeController))]
        public async Task<IActionResult> Index()
        {
            var repots = await _reports.GetReportsAsync();

            var modelBuilder = new ViewModelBuilder()
                .AddViewTitle("Отчеты").SetInvisibleViewTitle()
                .AddModel(new ReportNavViewRequestModel { Reports = repots, NavId = string.Empty });
            return View("Index", modelBuilder);
        }

        [Breadcrumb("Отчеты", FromAction = nameof(Index), FromController = typeof(HomeController))]
        public async Task<IActionResult> MainReport(string navId, string urlAction)
        {
            ViewBag.Url = urlAction;
            ViewBag.NavId = navId;
            var repots = await _reports.GetReportsAsync();
            var modelBuilder = new ViewModelBuilder()
                .AddViewTitle("Отчеты").SetInvisibleViewTitle()
                .AddModel(new ReportNavViewRequestModel { Reports = repots, NavId = navId, Url = urlAction });
            return View("Main", modelBuilder);
        }

        public async Task<IActionResult> ReportData(ReportViewRequestModel requestModel)
        {
            try
            {
                var report = await _reports.GetReportAsync(requestModel.Id);
                if (report == null) { ShowError(MessageDescription.Error); return NotFound(); }
                ReportViewResponseModel responseModel = new ReportViewResponseModel()
                {
                    DataList = await _reports.GetDataReport(requestModel, report.Function)
                };

                if (responseModel.DataList == null) throw new Exception();

                return PartialView($"{report.Path}/Report", responseModel);
            }
            catch (Exception e)
            {
                ShowError(MessageDescription.Error);
                return null;
            }
        }


        public async Task<ActionResult?> ReportDataPrint(ReportViewRequestModel requestModel)
        {
            try
            {
                var report = await _reports.GetReportAsync(requestModel.Id);
                if (report == null) { ShowError(MessageDescription.Error); return null; }

                var data = await _reports.GetDataReport(requestModel, report.Function);
                if (data == null) { ShowError(MessageDescription.Error); return null; }

                var strm = await ReportDataPrepare(report.File, data, requestModel, BlankActionType.Pdf);
                if(strm == null)
                {
                    ShowError(MessageDescription.Error);
                    return null;
                }

                return Json(Convert.ToBase64String(strm.ToArray()));

            }
            catch (Exception e)
            {
                ShowError(MessageDescription.Error);
                return null;
            }
        }

        public async Task<ActionResult?> ReportDataDownLoad(ReportViewRequestModel requestModel)
        {
            try
            {
                var report = await _reports.GetReportAsync(requestModel.Id);
                if (report == null) { ShowError(MessageDescription.Error); return null; }

                var data = await _reports.GetDataReport(requestModel, report.Function);
                if (data == null) { ShowError(MessageDescription.Error); return null; }

                var strm = await ReportDataPrepare(report.File, data, requestModel, BlankActionType.Exel);
                if (strm == null)
                {
                    ShowError(MessageDescription.Error);
                    return null;
                }

                return File(strm.ToArray(), MimeTypeMap.GetMimeType(".xlsx"), "Отчет.xlsx");

            }
            catch (Exception e)
            {
                ShowError(MessageDescription.Error);
                return null;
            }
        }

        [HttpGet]
        public async Task<ActionResult> GetProvidersDataJson()
        {
            var providers = await _referencesService.GetServiceProvidersAll();
            providers.Add(new OfficeDto { Id = Guid.Empty, OfficeName = "Все" });
            return Json(providers);
        }

        public async Task<ActionResult> GetServicesForProviderDataJson(List<Guid> providersId)
        {
            var services = await _referencesService.GetServicesForProviders(providersId);
            services.Add(new ServicesDto { Id = Guid.Empty, ServiceName = "Все" });

            return Json(services);
        }

        [HttpGet]
        public async Task<ActionResult> GetMfcDataJson(bool isAll)
        {
            var officeId = await _employeeManager.GetOfficeAsync();
            var mfc = await _referencesService.GetAllOfficesTypeMfcAndTospAsync();

            if (User.IsInRole("Администратор") || User.IsInRole("Ведущий менеджер"))
            {
                if (isAll)
                {
                    mfc.Add(new OfficeDto { Id = Guid.Empty, OfficeName = "Все" });
                }
                return Json(mfc);
            }
            else if (User.IsInRole("Аудитор"))
            {
                var tosp = await _referencesService.GetTospDtoAsync(officeId.Value);

                mfc = mfc.Where(w => w.Id == officeId).ToList();

                mfc.AddRange(tosp);

                return Json(mfc);
            }
            else
            {
                return Json(mfc.Where(w => w.Id == officeId).ToList());
            }
        }

        [HttpGet]
        public async Task<ActionResult> GetEmployeesForMfcDataJson(List<Guid> employeeId)
        {
            var emoloyee = await _employeeManager.GetIdAsync();
            var mfc = await _referencesService.GetEmployeesForMfc(employeeId);
            mfc.Add(new OfficeDto { Id = Guid.Empty, OfficeName = "Все" });

            if (User.IsInRole("Администратор") || User.IsInRole("Ведущий менеджер") || User.IsInRole("Аудитор"))
            {
                return Json(mfc);
            }
            else
            {
                return Json(mfc.Where(w => w.Id == emoloyee).ToList());
            }
        }

        [HttpGet]
        public async Task<ActionResult> GetSmevDataJson()
        {
            var providers = await _referencesService.GetAllSmevServicesAsync();
            providers.Add(new SmevServiceDto { Id = Guid.Empty, SmevName = "Все" });
            return Json(providers);
        }

        [HttpGet]
        public async Task<ActionResult> GetRequestForSmevDataJson(List<Guid> smevId)
        {
            var services = await _referencesService.GetSmevRequestBySmevService(smevId);
            services.Add(new SmevRequestDto { Id = 0, RequestName = "Все" });

            return Json(services);
        }
    }
}
