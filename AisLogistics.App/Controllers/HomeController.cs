using AisLogistics.App.Models;
using AisLogistics.App.Service;
using AisLogistics.App.ViewModels.Cases;
using AisLogistics.App.ViewModels.ModelBuilder;
using AisLogistics.DataLayer.Entities.Models;
using DataTables.AspNet.AspNetCore;
using DataTables.AspNet.Core;
using log4net;
using Microsoft.AspNetCore.Authorization;
using SmartBreadcrumbs.Attributes;
using System.ComponentModel.Design;
using System.Diagnostics;

namespace AisLogistics.App.Controllers
{
    [Authorize]
    public class HomeController : AbstractController
    {
        private readonly InformationManager _informationManager;
        private readonly ILogger<HomeController> _logger;
        private readonly IReferencesService _referencesService;
        public HomeController(InformationManager informationManager, ILogger<HomeController> logger, IReferencesService referencesService)
        {
            _informationManager = informationManager;
            _logger = logger;
            _referencesService = referencesService;
        }

        [DefaultBreadcrumb("Главная")]
        public async Task<ActionResult> Index()
        {
            ViewBag.Inf = await _informationManager.GetInformationListAsync(5);
            /*ViewBag.Warning = await _informationManager.GetWarningRousteStageListAsync();*/
            ViewBag.Rating = await _informationManager.GetEmployeeRatingAsync(null, false);
            ViewBag.Notes = await _informationManager.GetNotesListAsync();

            return View();
        }

        public async Task<IActionResult> InformationDetailsModal(Guid id)
        {
            var model = await _informationManager.GetInformationDetailssAsync(id);
            var modelBuilder = new ModalViewModelBuilder()
                .AddModalType(ModalType.LARGE)
                .AddModalViewPath("Modals/_ModalInformation")
                .AddModalTitle(model.Type)
                .AddModel(model)
                .HideSubmitButton();

            return ModalLayoutView(modelBuilder);
        }

        public async Task<IActionResult> RatingDetailsModal()
        {
            var mfc = await _referencesService.GetAllOfficesTypeMfcAndTospAsync();
            ViewBag.Mfc = mfc == null ? "" : string.Join("", mfc.Select(x => $"<option value=\"{x.Id}\">{x.OfficeName}</option>"));

            var modelBuilder = new ModalViewModelBuilder()
                .AddModalType(ModalType.LARGE)
                .AddModalViewPath("Modals/_ModalRatinig")
                .AddModalTitle($"Топ сотрудников за {DateTime.Now.AddDays(-1):Y}")
                .HideSubmitButton();

            return ModalLayoutView(modelBuilder);
        }

        public async Task<IActionResult> GetEmployeeRatingDataJson(IDataTablesRequest request)
        {
            var responseData = await _informationManager.GetEmployeeRatingAsync(request,true);

            var response = DataTablesResponse.Create(request,1, 1, responseData);

            return new DataTablesJsonResult(response, true);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

    }
}