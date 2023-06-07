using AisLogistics.App.Models.DTO.References;
using AisLogistics.App.Models.DTO.Template;
using AisLogistics.App.Models.Statistics;
using AisLogistics.App.Service;
using AisLogistics.App.ViewModels.ModelBuilder;
using AisLogistics.App.ViewModels.Statistics;
using DataTables.AspNet.AspNetCore;
using DataTables.AspNet.Core;
using log4net;
using SmartBreadcrumbs.Attributes;

namespace AisLogistics.App.Controllers
{
    public class StatisticsController : AbstractController
    {
        private readonly ILogger<StatisticsController> _logger;
        private readonly IReferencesService _referencesService;
        private readonly IStatistics _statistics;
        private readonly int _pageSize;

        public StatisticsController(IReferencesService referencesService,
            EmployeeManager employeeManager, ILogger<StatisticsController> logger, IStatistics statistics) : base(employeeManager)
        {
            _logger = logger;
            _referencesService = referencesService;
            _statistics = statistics;
            _pageSize = 15;
        }

        [Breadcrumb("Аналитика", FromAction = nameof(Index), FromController = typeof(HomeController))]
        public async Task<IActionResult> Index()
        {
            var statistics = await _statistics.GetStatisticsV2Async();

            var modelBuilder = new ViewModelBuilder()
                .AddViewTitle("Аналитика").SetInvisibleViewTitle()
                .AddModel(statistics);
            return View("Index1", modelBuilder);
        }

        [Breadcrumb("Cтатистика", FromAction = nameof(Index), FromController = typeof(HomeController))]
        public async Task<IActionResult> MainReport(string navId, string urlAction)
        {
            var statistics = await _statistics.GetStatisticsAsync();
            var data = await _statistics.StatisticsGeneral();
            ViewBag.Url = urlAction;
            ViewBag.NavId = navId;
            ViewBag.Data = new StatisticsNavViewRequestModel { Statistics = statistics, NavId = navId, Url = urlAction };

            var modelBuilder = new ViewModelBuilder()
                .AddViewTitle("Cтатистика").SetInvisibleViewTitle()
                .AddModel(data);
            return View("Main", modelBuilder);
        }

        

        public async Task<IActionResult> StatisticsGeneral()
        {
            var data = await _statistics.StatisticsGeneral();
            return PartialView("StatisticsGeneral/Index", data);
        }


        [HttpGet]
        public async Task<IActionResult> StatisticsGeneralGraphic(StatisticsViewRequestModel requestModel)
        {

            var data = requestModel.PeriodTypeId switch
            {
                1 => await _statistics.StatisticsGeneralGraphicTypeDay(requestModel),
                2 => await _statistics.StatisticsGeneralGraphicTypeYear(requestModel),
                _ => null
            };
            return Json(data);
        }

        [HttpGet]
        public async Task<IActionResult> StatisticsSmevGraphic(StatisticsViewRequestModel requestModel)
        {

            var data = requestModel.PeriodTypeId switch
            {
                1 => await _statistics.StatisticsSmevGraphicTypeDay(requestModel),
                2 => await _statistics.StatisticsSmevGraphicTypeYear(requestModel),
                _ => null
            };
            return Json(data);
        }


        public async Task<IActionResult?> PartialMfcDataJson(IDataTablesRequest request, StatisticsViewRequestModel additionalRequest)
        {
            (var statistics, int count) = await _statistics.StatisticsMfcData(additionalRequest, request);

            var response = DataTablesResponse.Create(request, count, count, statistics);

            return new DataTablesJsonResult(response, true);
        }

        public async Task<IActionResult?> PartialEmployeeDataJson(IDataTablesRequest request, StatisticsViewRequestModel additionalRequest)
        {
            (var statistics, int count) = await _statistics.StatisticsEmployeeData(additionalRequest, request);

            var response = DataTablesResponse.Create(request, count, count, statistics);

            return new DataTablesJsonResult(response, true);
        }

        public async Task<IActionResult?> PartialProviderDataJson(IDataTablesRequest request, StatisticsViewRequestModel additionalRequest)
        {
            (var statistics, int count) = await _statistics.StatisticsProviderData(additionalRequest, request);

            var response = DataTablesResponse.Create(request, count, count, statistics);

            return new DataTablesJsonResult(response, true);
        }

        public async Task<IActionResult?> PartialServiceDataJson(IDataTablesRequest request, StatisticsViewRequestModel additionalRequest)
        {
            (var statistics, int count) = await _statistics.StatisticsServiceData(additionalRequest, request);

            var response = DataTablesResponse.Create(request, count, count, statistics);

            return new DataTablesJsonResult(response, true);
        }

        public async Task<IActionResult?> PartialCustomerTypeDataJson(IDataTablesRequest request, StatisticsViewRequestModel additionalRequest)
        {
            (var statistics, int count) = await _statistics.StatisticsCustomerTypeData(additionalRequest, request);

            var response = DataTablesResponse.Create(request, count, count, statistics);

            return new DataTablesJsonResult(response, true);
        }

        public async Task<IActionResult?> PartialServiceTypeDataJson(IDataTablesRequest request, StatisticsViewRequestModel additionalRequest)
        {
            (var statistics, int count) = await _statistics.StatisticsServiceTypeData(additionalRequest, request);

            var response = DataTablesResponse.Create(request, count, count, statistics);

            return new DataTablesJsonResult(response, true);
        }

        public async Task<IActionResult?> PartialInteractionTypeDataJson(IDataTablesRequest request, StatisticsViewRequestModel additionalRequest)
        {
            (var statistics, int count) = await _statistics.StatisticsInteractionTypeData(additionalRequest, request);

            var response = DataTablesResponse.Create(request, count, count, statistics);

            return new DataTablesJsonResult(response, true);
        }

        public async Task<IActionResult?> PartialSmevDataJson(IDataTablesRequest request, StatisticsViewRequestModel additionalRequest)
        {
            (var statistics, int count) = await _statistics.StatisticsSmevData(additionalRequest, request);

            var response = DataTablesResponse.Create(request, count, count, statistics);

            return new DataTablesJsonResult(response, true);
        }

        public async Task<IActionResult?> PartialSmevMfcDataJson(IDataTablesRequest request, StatisticsViewRequestModel additionalRequest)
        {
            (var statistics, int count) = await _statistics.StatisticsSmevMfcData(additionalRequest, request);

            var response = DataTablesResponse.Create(request, count, count, statistics);

            return new DataTablesJsonResult(response, true);
        }

        public async Task<IActionResult?> PartialSmevEmployeeDataJson(IDataTablesRequest request, StatisticsViewRequestModel additionalRequest)
        {
            (var statistics, int count) = await _statistics.StatisticsSmevEmployeeData(additionalRequest, request);

            var response = DataTablesResponse.Create(request, count, count, statistics);

            return new DataTablesJsonResult(response, true);
        }


        public async Task<IActionResult> StatisticsServicesGeneral()
        {
            var data = await _statistics.StatisticsServicesGeneral();
            return PartialView("StatisticsServicesGeneral/Index", data);
        }

        [HttpGet]
        public async Task<IActionResult> StatisticsServicesGeneralGraphic(StatisticsViewRequestModel requestModel)
        {

            var data = requestModel.PeriodTypeId switch
            {
                1 => await _statistics.StatisticsServicesGeneralGraphicTypeDay(requestModel),
                2 => await _statistics.StatisticsServicesGeneralGraphicTypeYear(requestModel),
                _ => null
            };
            return Json(data);
        }

        [HttpGet]
        public async Task<IActionResult> StatisticsServicesGeneralServiceType()
        {

            var response = await _statistics.StatisticsServicesGeneralServiceType();
            return Json(response);
        }
        [HttpGet]
        public async Task<IActionResult> StatisticsServicesGeneralCustomerType()
        {

            var response = await _statistics.StatisticsServicesGeneralCustomerType();
            return Json(response);
        }

        [HttpGet]
        public async Task<IActionResult> StatisticsServicesGeneralInteractionType()
        {
            var response = await _statistics.StatisticsServicesGeneralInteractionType();
            return Json(response);
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
        public async Task<ActionResult> GetMfcDataJson()
        {
            var officeId = await _employeeManager.GetOfficeAsync();
            var mfc = await _referencesService.GetAllOfficesTypeMfcAndTospAsync();

            mfc.ForEach(f =>
            {
                if(f.Id == officeId) f.Selected = true;
            });

            if (User.IsInRole("Администратор") || User.IsInRole("Ведущий менеджер"))
            {
                mfc.Add(new OfficeDto { Id = Guid.Empty, OfficeName = "Все" });
            
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
        public async Task<ActionResult> GetCustomerTypeDataJson()
        {
            var data = await _referencesService.GetServiceCustomerMainTypes();
            data.Add(new ServiceCustomerTypeDto { Id = 0, TypeName = "Все" });
            return Json(data);
        }
        [HttpGet]
        public async Task<ActionResult> GetServiceTypeDataJson()
        {
            var data = await _referencesService.GetServiceTypesAll();
            data.Add(new SelectListDto<int> (0, "Все" ));
            return Json(data);
        }
        [HttpGet]
        public async Task<ActionResult> GetInteractionTypeDataJson()
        {
            var data = await _referencesService.GetServiceInteractionsAll();
            data.Add(new SelectListDto<int>(0, "Все"));
            return Json(data);
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
