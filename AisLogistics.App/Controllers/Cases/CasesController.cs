using AisLogistics.App.Hubs;
using AisLogistics.App.Models;
using AisLogistics.App.Models.DTO.Cases;
using AisLogistics.App.Models.DTO.Identity.Applicant;
using AisLogistics.App.Models.DTO.Services;
using AisLogistics.App.Models.Enums;
using AisLogistics.App.Service;
using AisLogistics.App.Settings;
using AisLogistics.App.ViewModels.Cases;
using AisLogistics.App.ViewModels.ModelBuilder;
using AisLogistics.DataLayer.Common.Dto.Case;
using AisLogistics.DataLayer.Entities.Models;
using AisLogistics.DataLayer.Utils;
using DataTables.AspNet.AspNetCore;
using DataTables.AspNet.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Sentry;
using SmartBreadcrumbs.Attributes;
using SmevGate.Client.Core;
using SmevRouter;

namespace AisLogistics.App.Controllers.Cases
{
    [Authorize]
    public partial class CasesController : AbstractController
    {
        private readonly ICaseService _caseService;
        private readonly IReferencesService _referencesService;
        private readonly ISmevService _smevService;
        private readonly IConfiguration _configuration;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<CasesController> _logger;

        public CasesController(ICaseService caseService, IReferencesService referencesService, ISmevService smevService, IOptions<SmevSettings> smevOptions,
            EmployeeManager employeeManager, IConfiguration configuration, IHubContext<NotificationHub> hubContext, ILogger<CasesController> logger) : base(employeeManager)
        {
            _caseService = caseService;
            _referencesService = referencesService;
            _smevService = smevService;
            _configuration = configuration;
            _hubContext = hubContext;
            _logger = logger;
            SmevClient.Init(smevOptions.Value.SmevConnection, smevOptions.Value.AuthCode);
        }

        #region Reestr

        [Breadcrumb("Услуги", FromAction = nameof(Index), FromController = typeof(HomeController))]
        public async Task<IActionResult> Index()
        {
            var offices = await _referencesService.GetAllOfficesAsync();
            var stages = await _referencesService.GetAllRoutesStagesAsync();
            var services = await _referencesService.GetServicesAll();
            var status = await _referencesService.GetAllStatuseAsync();
            var mfc = await _referencesService.GetAllOfficesTypeMfcAndTospAsync();
            var officeId = await _employeeManager.GetOfficeAsync();

            //ViewBag.Offices = offices == null ? "" : string.Join("", offices.Select(x => $"<option value=\"{x.Id}\">{x.OfficeName}</option>"));
            //ViewBag.Stages = stages == null ? "" : string.Join("", stages.Select(x => $"<option value=\"{x.Id}\">{x.Name}</option>"));
            //ViewBag.Services = services == null ? "" : string.Join("", services.Select(x => $"<option value=\"{x.Id}\">{(x.ServiceName.Length > 150 ? x.ServiceName[..150] + "..." : x.ServiceName)}</option>"));
            //ViewBag.Statuses = status == null ? "" : string.Join("", status.Select(x => $"<option value=\"{x.Id}\">{x.Name}</option>"));
            //ViewBag.Mfc = mfc == null ? "" : string.Join("", mfc.Select(x => $"<option value=\"{x.Id}\" {(x.Id == officeId ? "selected" : string.Empty)}>{x.OfficeName}</option>"));
            ViewBag.CasesAllView = User.HasClaim(AccessKeyNames.DataCaseAll, AccessKeyValues.View);

            var model = new SearchCasesResponseData()
            {
                MfcId = officeId.GetValueOrDefault(),
                MfcList = new SelectList(mfc, "Id", "OfficeName"),
                OfficesList = new SelectList(offices, "Id", "OfficeName"),
                ServicesList = new SelectList(services, "Id", "ServiceName"),
                StatusList = new SelectList(status, "Id", "Name"),
                StagesList = new SelectList(stages, "Id", "Name")
            };

            var modelBuilder = new ViewModelBuilder()
                .AddViewTitle("Реестр услуг")
                .SetElementName("cases-datatable")
                .AddModel(model)
                .AddTableMethodAction(Url.Action(nameof(GetEmployeeCasesDataJson)));
            return View(modelBuilder);
        }

        public async Task<IActionResult> GetEmployeeCasesDataJson(IDataTablesRequest request, SearchCasesRequestData additionalRequest)
        {
            Guid? id = User.HasClaim(AccessKeyNames.DataCaseAll, AccessKeyValues.View) || User.HasClaim(AccessKeyNames.DataCaseOffice, AccessKeyValues.View) ? null : await _employeeManager.GetIdAsync();

            Guid? office = User.HasClaim(AccessKeyNames.DataCaseAll, AccessKeyValues.View) || User.HasClaim(AccessKeyNames.DataCaseOffice, AccessKeyValues.View) ? await _employeeManager.GetOfficeAsync() : null;
            additionalRequest.EmployeeId = id;

            (var responseData, int totalCount, int filteredCount) = await _caseService.GetCasesV3(request, additionalRequest);

            var response = DataTablesResponse.Create(request, totalCount, filteredCount, responseData);

            return new DataTablesJsonResult(response, true);
        }

        [HttpGet]
        public async Task<IActionResult> GetServicesForProviderDataJson(List<Guid> providersId)
        {
            var services = await _referencesService.GetServicesForProviders(providersId);
            return Json(services.Select(s => new { s.Id, ServiceName = s.ServiceName.Length > 200 ? s.ServiceName[..200] + "..." : s.ServiceName }));
        }

        #endregion

        #region Case Create 

        [Breadcrumb("Новая услуга", FromAction = nameof(Index), FromController = typeof(CasesController))]
        [Route("[controller]/Create")]
        public async Task<IActionResult> Create()
        {
            var providers = await _referencesService.GetServiceProvidersAll();
            var customerTypes = await _referencesService.GetServiceCustomerMainTypes();
            var hashtag = await _referencesService.GetServiceHashtagAll();

            ViewBag.Providers = new SelectList(providers, nameof(ActiveServiceProviderDto.Id), nameof(ActiveServiceProviderDto.OfficeName));
            ViewBag.CustomerTypes = new SelectList(customerTypes, "Id", "TypeName");
            ViewBag.Hashtag = new SelectList(hashtag, "Name", "Name"); ;

            var modelBuilder = new ViewModelBuilder()
                .AddViewTitle("Новая услуга")
                .AddModel(new CreateCaseRequestModel());
            return View("Create/Index", modelBuilder);
        }

        [Breadcrumb("Новая услуга (Юр.лицо)", FromAction = nameof(Index), FromController = typeof(CasesController))]
        [Route("[controller]/Create/Legal")]
        public IActionResult CreateLegal()
        {
            var modelBuilder = new ViewModelBuilder()
                .AddViewTitle("Новая услуга (Юр.лицо)")
                .AddModel(new CreateCaseLegalRequestModel()
                {
                    //Customer = new DServicesCustomer(),
                    CustomerLegal = new DServicesCustomersLegal()
                });
            return View("Create/Legal", modelBuilder);
        }

        [HttpPost]
        public async Task<string> CreateSave(CreateCaseRequestModel requestModel)
        {
            if (ModelState.IsValid == false)
            {
                ShowError("Неверные данные");
                return new CreateCaseResponseModel().CreatedError("Неверные данные").ToJson();
            }

            try
            {
                var responseModel = await _caseService.CreateCaseAsync(requestModel);

                if (responseModel.CreateCaseStatus.IsCreated == true)
                {
                    ShowSuccess("Обращение создано");

                    if (requestModel.Customer is not null)
                    {
                        if (requestModel.IsGetCustomerSnils && string.IsNullOrEmpty(requestModel.Customer.CustomerSnils))
                        {
                            await GetCustomerSnils(responseModel.DserviceId, requestModel.Customer);
                        }
                        if (requestModel.IsGetCustomerInn && string.IsNullOrEmpty(requestModel.Customer.CustomerInn))
                        {
                            await GetCustomerInn(responseModel.DserviceId, requestModel.Customer);
                        }
                    }
                    if (requestModel.Representative is not null)
                    {
                        if (requestModel.IsGetRepresentativeSnils && string.IsNullOrEmpty(requestModel.Representative.CustomerSnils))
                        {
                            await GetCustomerSnils(responseModel.DserviceId, requestModel.Representative);
                        }

                        if (requestModel.IsGetRepresentativeInn && string.IsNullOrEmpty(requestModel.Representative.CustomerInn))
                        {
                            await GetCustomerInn(responseModel.DserviceId, requestModel.Representative);
                        }
                    }
                }

                if (responseModel.CreateCaseStatus.IsCreated == false) ShowError(responseModel.CreateCaseStatus.Message);

                return responseModel.ToJson();
            }
            catch
            {
                ShowError("Ошибка сервера");
                return new CreateCaseResponseModel().CreatedError("Ошибка сервера").ToJson();
            }
        }

        private async Task GetCustomerSnils(Guid dServiceId, CustomerModelDto customer)
        {
            var smevRequestId = await _smevService.CreateNewSmevRequestAsync(dServiceId, 30,
                                                                   $"{customer.LastName} {customer.FirstName} {customer.SecondName}");

            var requestSmev = new GetSnilsByAdditionalDataRequestData
            {
                DataServicesRequestSmevId = smevRequestId,
                Fio = new FioType
                {
                    FirstName = customer.FirstName,
                    LastName = customer.LastName,
                    SecondName = customer.SecondName,
                },
                BirthDate = customer.DocumentBirthDate.Value,
                Gender = (GenderType)(customer.CustomerGender - 1),
            };

            SmevClient.RequestSnilsByAdditionalData(requestSmev);
        }
        private async Task GetCustomerInn(Guid dServiceId, CustomerModelDto customer)
        {
            var smevRequestId = await _smevService.CreateNewSmevRequestAsync(dServiceId, 319,
                                                                    $"{customer.LastName} {customer.FirstName} {customer.SecondName}");

            var requestSmev = new GetInnSingularRequestData
            {
                DataServicesRequestSmevId = smevRequestId,
                Fio = new FioType
                {
                    FirstName = customer.FirstName,
                    LastName = customer.LastName,
                    SecondName = customer.SecondName,
                },
                BirthDate = customer.DocumentBirthDate.Value,
                BirthPlace = customer.DocumentBirthPlace,
                DocumentType = string.Format("{0:d2}", customer.SDocumentsIdentityId),
                DocumentSeriesNumber = $"{customer.DocumentSerial} {customer.DocumentNumber}",
                DocumentIssuerCode = customer.DocumentCode,
                DocumentIssuer = customer.DocumentIssueBody,
                DocumentDate = customer.DocumentIssueDate.Value
            };

            SmevClient.RequestInnSingular(requestSmev);
        }

        [HttpPost]
        public async Task<string> CreateLegalSave(CreateCaseLegalRequestModel requestModel)
        {
            var responseModel = await _caseService.CreateCaseAsync(requestModel);

            if (responseModel.CreateCaseStatus.IsCreated == true) ShowSuccess("Обращение создано");
            if (responseModel.CreateCaseStatus.IsCreated == false) ShowError("Обращение не создано");

            return responseModel.ToJson();
        }

        public IActionResult GetServicesDataJson(IDataTablesRequest request, SearchCasesRequestData additionalRequest)
        {
            var services = _caseService.GetActiveServices(additionalRequest, request.Start, request.Length);

            var response = DataTablesResponse.Create(request, services.Item2, services.Item3, services.Item1);

            return new DataTablesJsonResult(response, true);
        }

        public IActionResult GetServicesForLegalDataJson(IDataTablesRequest request, SearchCasesRequestData additionalRequest)
        {
            var services = _caseService.GetActiveServicesForLegal(additionalRequest, request.Start, request.Length);

            var response = DataTablesResponse.Create(request, services.Item2, services.Item3, services.Item1);

            return new DataTablesJsonResult(response, true);
        }

        public IActionResult GetServiceProceduresDataJson(IDataTablesRequest request, Guid serviceId)
        {
            var procedure = _caseService.GetServiceProcedures(serviceId);

            var response = DataTablesResponse.Create(request, procedure.Count, procedure.Count, procedure);

            return new DataTablesJsonResult(response, true);
        }

        public IActionResult GetServiceTariffDataJson(IDataTablesRequest request, Guid procedureId, int? type)
        {
            var tariff = _caseService.GetServiceTariff(procedureId, type);

            var response = DataTablesResponse.Create(request, tariff.Count, tariff.Count, tariff);

            return new DataTablesJsonResult(response, true);
        }

        //TODO переделать
        public Task<ApplicantsDto?> FindCustomerByDocumentDataJson(string serial, string number)
        {
            return _caseService.GetCustomerByDocumentSerialAndNumberAsync(serial, number);
        }
        public Task<LegassDto?> FindLegalByInnDataJson(string inn)
        {
            return _caseService.GetLegalByInnAsync(inn);
        }

        /// <summary>
        /// Получение справочника
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> DataBaseGetDictionary(DataBaseDictonaryType type)
        {
            //return Json(await _referencesService.DataBaseDictonary(type));
            return Json(string.Empty);
        }

        #endregion

        #region Case Details

        #region Index

        [Breadcrumb("ViewData.Title", FromAction = nameof(Index), FromController = typeof(CasesController))]
        public async Task<IActionResult> Details(string id)
        {
            var services = await _caseService.GetServicesByCaseIdAsyncV2(id);
            if (services is null) return NotFound();
            await _caseService.ViewCaseAsync(id);
            var modelBuilder = new ViewModelBuilder()
                .AddViewTitle(id)
                .AddModel(new CaseDetailsResponseModel<CasesDto>(services));
            return View("Details/Index", modelBuilder);
        }

        #endregion

        #region Departament

        public async Task<IActionResult> ServiceDepartamentAddModal(Guid id, Guid officeid, Guid? departamentId)
        {
            var offices = await _referencesService.GetServiceProvidersDepartement(new List<Guid> { officeid });

            var request = new ServicesDepartamentDto() { ServiceId = id };
            if (departamentId is not null) request.DepartamentId = departamentId.Value;

            ViewBag.Offices = new SelectList(offices, "Id", "OfficeName");

            var modelBuilder = new ModalViewModelBuilder()
                .AddModalType(ModalType.SMALL)
                .AddModalViewPath("Details/Modals/_ModalDepartamentAdd")
                .AddModalTitle("Добавление отдела")
                .AddModel(request);

            if (offices is null || !offices.Any()) modelBuilder.HideSubmitButton();

            return ModalLayoutView(modelBuilder);
        }

        public async Task<bool> ServiceDepartamentSave(ServicesDepartamentDto request)
        {
            var responce = await _caseService.AddServiceDepartamentAsync(request);
            if (responce) ShowSuccess("Отдел добавлен");
            else ShowError("Добавления отдела невозможно");
            return responce;
        }

        #endregion

        #region Favorite

        public async Task SetCaseFavorite(string id) => await _caseService.SetCaseFavoriteAsync(id);
        public async Task RemoveCaseFavorite(string id) => await _caseService.RemoveCaseFavoriteAsync(id);
        public async Task SetServiceFavorite(Guid id) => await _caseService.SetServiceFavoriteAsync(id);
        public async Task RemoveServiceFavorite(Guid id) => await _caseService.RemoveServiceFavoriteAsync(id);

        #endregion

        #region Comment

        [Breadcrumb("ViewData.Title", FromAction = nameof(Index), FromController = typeof(CasesController))]
        [Route("[controller]/Details/{id}/Comments")]
        public async Task<IActionResult> DetailsComments(string id)
        {
            ViewBag.Id = id;
            var comments = await _caseService.GetCommentsByCaseIdAsync(id);
            var modelBuilder = new ModalViewModelBuilder()
                .AddModalType(ModalType.LARGE)
                .AddModalViewPath("Details/Comments")
                .AddModel(comments)
                .HideSubmitButton()
                .AddModalTitle("Комментарии");

            return ModalLayoutView(modelBuilder);
        }

        public IActionResult CommentEmployeesPick(string id)
        {
            var employees = _referencesService.GetReferencesEmployees();
            var modelBuilder = new ModalViewModelBuilder()
                .AddModalType(ModalType.SMALL)
                .AddModalViewPath("Details/Modals/_ModalCommentEmployeesPick")
                .AddModalTitle("Выбор сотрудников")
                .AddModel(employees.Item1);
            return ModalLayoutView(modelBuilder);
        }

        public async Task<IActionResult> CommentAdd(string id, string comment, Guid[] employeesId)
        {
            await _caseService.AddCommentAsync(id, comment, employeesId);
            return RedirectToAction(nameof(DetailsComments), new { id });
        }

        #endregion

        #region Stage

        public async Task<IActionResult> ServiceStages(Guid id)
        {
            ViewBag.Id = id;
            var stages = await _caseService.GetStagesByServiceIdAsync(id);
            var modelBuilder = new ViewModelBuilder()
                .AddViewTitle("Этапы")
                .AddModel(stages);
            return PartialView("Details/Sidebar/_SidebarStages", modelBuilder);
        }

        public async Task<IActionResult> ServiceStageAddModal(Guid id)
        {
            var officeId = await _employeeManager.GetOfficeAsync();
            var employeeId = await _employeeManager.GetIdAsync();
            var stageModelAdd = new StageAddDto
            {
                ServiceId = new List<Guid>() { id },
                OfficeId = officeId.Value,
                EmployeeId = employeeId.Value
            };

            stageModelAdd.Stages.AddRange(User.HasClaim(AccessKeyNames.DataCaseServiceAllStage, AccessKeyValues.Add)
                ? await _caseService.GetStagesNextAllAsync()
                : await _caseService.GetStagesNextByServiceIdAsync(new List<Guid>() { id }));

            ViewBag.Offices = new SelectList(await _referencesService.GetAllOfficesTypeMfcAndTospAsync(), "Id", "OfficeName", officeId);

            var modelBuilder = new ModalViewModelBuilder()
                .AddModalType(ModalType.SMALL)
                .AddModalViewPath("Details/Modals/_ModalStageAdd")
                .AddModalTitle("Добавление этапа")
                .AddModel(stageModelAdd);

            if (stageModelAdd.Stages.Any() == false) modelBuilder.HideSubmitButton();

            return ModalLayoutView(modelBuilder);
        }

        public async Task<IActionResult> ServicesStageAddModal(List<Guid> id)
        {
            var officeId = await _employeeManager.GetOfficeAsync();
            var employeeId = await _employeeManager.GetIdAsync();
            var stageModelAdd = new StageAddDto
            {
                ServiceId = id,
                OfficeId = officeId.Value,
                EmployeeId = employeeId.Value
            };

            stageModelAdd.Stages.AddRange(User.HasClaim(AccessKeyNames.DataCaseServiceAllStage, AccessKeyValues.Add)
                ? await _caseService.GetStagesNextAllAsync()
                : await _caseService.GetStagesNextByServiceIdAsync(id));

            ViewBag.Offices = new SelectList(await _referencesService.GetAllOfficesTypeMfcAndTospAsync(), "Id", "OfficeName", officeId);

            var modelBuilder = new ModalViewModelBuilder()
                .AddModalType(ModalType.SMALL)
                .AddModalViewPath("Details/Modals/_ModalStageAdd")
                .AddModalTitle("Добавление этапа")
                .AddModel(stageModelAdd);

            if (stageModelAdd.Stages.Any() == false) modelBuilder.HideSubmitButton();

            return ModalLayoutView(modelBuilder);
        }

        public async Task<IActionResult> ServiceStageNextEmployess(ServiceStageNextEmployessDto request)
        {
            var employeeId = await _employeeManager.GetIdAsync();
            var employee = await _caseService.GetStagesNextEmployessByServiceIdAsync(request);
            if (employee == null) ShowError("Сотрудник не найден");

            if (!employee.Any(a => a.Id == employeeId))
            {
                var name = await _employeeManager.GetNameAsync();
                employee.Add(new EmployeesDtO { Id = (Guid)employeeId, Name = name });
            }

            return Json(employee);
        }

        [HttpPost]
        public async Task ServiceStageSave(ServiceStageSaveDto request)
        {
            var responce = await _caseService.AddServiceStageAsync(request);
            if (responce) ShowSuccess("Этап добавлен");
            else ShowError("Добавления этапа невозможно");
        }
        #endregion

        #region Smev

        public async Task<IActionResult> ServiceSmevCompleted(Guid id)
        {
            ViewBag.Id = id;
            var smevСompleted = await _caseService.GetSmevСompletedByServiceIdAsync(id);
            var modelBuilder = new ViewModelBuilder()
                .AddViewTitle("СМЭВ запросы")
                .AddModel(smevСompleted);
            return PartialView("Details/Sidebar/_SidebarSmevCompleted", modelBuilder);
        }

        public IActionResult ServiceSmevCreate(Guid id)
        {
            var modelBuilder = new ModalViewModelBuilder()
                .AddModalType(ModalType.FULL)
                .AddModalViewPath("Details/Modals/_ModalSmevCreate")
                .AddModel(new SmevCreateResponseModel { ServiceId = id })
                .HideModalFooter();
            return ModalLayoutView(modelBuilder);
        }

        public IActionResult GetServiceSmevActiveDataJson(IDataTablesRequest request, Guid serviceId)
        {
            var smevActive = _caseService.GetSmevActiveByServiceId(serviceId, request.Search.Value, request.Start, request.Length, out var smevCount, out var smevFilteredCount);

            var response = DataTablesResponse.Create(request, smevCount, smevFilteredCount, smevActive);

            return new DataTablesJsonResult(response, true);
        }

        #endregion

        #region Payments
        public async Task<IActionResult> ServicePaymentsCompleted(Guid id)
        {
            ViewBag.Id = id;
            var smevСompleted = await _caseService.GetPaymentsСompletedByServiceIdAsync(id);
            var modelBuilder = new ViewModelBuilder()
                .AddViewTitle("Оплата")
                .AddModel(smevСompleted);
            return PartialView("Details/Sidebar/_SidebarPaymentsCompleted", modelBuilder);
        }

        public async Task<IActionResult> ServicePaymentsCreate(Guid id)
        {
            var officeId = await _employeeManager.GetOfficeAsync();
            var service = await _caseService.GetPaymentsInfoServiceIdAsync(id);
            var customers = await _caseService.GetCustomersByCaseNumberAsync(service.DCaseId);
            var acquirings = await _referencesService.GetTerminalsByOffice(officeId);
            var benificiary = await _referencesService.GetRecipientPaymentByProviders(service.SOfficesIdProvider, officeId);

            var model = new PaymentsCreateResponseModel()
            {
                DCaseId = service.DCaseId,
                DServicesId = service.DServicesId,
                SOfficesIdProvider = service.SOfficesIdProvider,
                ServiceName = service.ServiceName,
                TariffState = service.TariffState,
                TariffMfc = service.TariffMfc,
                CustomerPayments = new SelectList(customers, "Id", "Name"),
                DAcquirings = new SelectList(acquirings, "Id", "Name"),
                PaymentBeneficiaries = new SelectList(benificiary, "Id", "Name")
            };

            var modelBuilder = new ModalViewModelBuilder()
                .AddModalType(ModalType.SMALL)
                .AddModalTitle("Добавление платежа")
                .AddModalViewPath("Details/Modals/_ModalPaymentsCreate")
                .AddModel(model);
            return ModalLayoutView(modelBuilder);
        }
        #endregion

        #region Customer

        public IActionResult ServiceCustomers(Guid id)
        {
            ViewBag.Id = id;
            var customers = _caseService.GetApplicantsByServiceId(id, true);
            var modelBuilder = new ViewModelBuilder()
                .AddViewTitle("Заявители")
                .AddModel(customers);
            return PartialView("Details/Sidebar/_SidebarCustomers", modelBuilder);
        }

        public async Task<IActionResult?> ServiceCustomerAddModal(Guid id, SubjectCustomerType subjectCustomerType, ActionType actionType)
        {
            ViewBag.Id = id;
            object? customer = subjectCustomerType == SubjectCustomerType.Physical ?
                await _caseService.GetCustomerByIdAsync(id) :
                await _caseService.GetCustomerLegalByIdAsync(id);

            if (customer == null)
            {
                ShowError("Данные не найдены");
                return null;
            }
            var modelBuilder = new ModalViewModelBuilder()
                .AddModalType(ModalType.LARGE)
                .AddModalViewPath(subjectCustomerType == SubjectCustomerType.Physical ?
                    "Details/Modals/_ModalCustomerAdd" :
                    "Details/Modals/_ModalCustomerLegalAdd")
                .AddModalTitle("Изменение заявителя")
                .AddModel(customer);
            return ModalLayoutView(modelBuilder);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<CaseCustomerResponseModel> ServiceCustomerSave(CustomerModelDto customer)
        {
            var response = await _caseService.UpdateCustomerAsync(customer);
            if (response.ResponseStatus.IsSuccess) ShowSuccess(MessageDescription.EditSuccess);
            else ShowError(MessageDescription.Error);

            return response;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<CaseCustomerResponseModel> ServiceCustomerLegalSave(DServicesCustomersLegal customerLegal)
        {
            var response = await _caseService.UpdateCustomerLegalAsync(customerLegal);
            if (response.ResponseStatus.IsSuccess) ShowSuccess(MessageDescription.EditSuccess);
            else ShowError(MessageDescription.Error);

            return response;
        }

        #endregion

        #region Document

        public async Task<IActionResult> ServiceDocuments(Guid id)
        {
            ViewBag.Id = id;
            var documents = await _caseService.GetDocumentsByServiceIdAsync(id);
            var modelBuilder = new ViewModelBuilder()
                .AddViewTitle("Документы")
                .AddModel(documents);
            return PartialView("Details/Sidebar/_SidebarDocuments", modelBuilder);
        }

        [HttpPost]
        public async Task<IActionResult> ServiceDocumentsJson(Guid id)
        {
            var documents = await _caseService.GetDocumentsByServiceIdAsync(id);
            return Json(documents);
        }

        public async Task<IActionResult> ServiceDocumentAddModal(Guid id, Guid documentId, DocumentFileAddType fileAddType)
        {
            var model = new CaseServicesDocumentAddModalDto(id, documentId, fileAddType);
            var modelBuilder = new ModalViewModelBuilder()
                .AddModalType(ModalType.LARGE)
                .AddModalViewPath("Details/Modals/_ModalDocumentAdd")
                .AddModalTitle("Добавление документа")
                .AddModel(model);

            if (fileAddType == DocumentFileAddType.Desktop)
                modelBuilder.HideSubmitButton();

            return ModalLayoutView(modelBuilder);
        }

        [HttpPost]
        public async Task<bool> ServiceDocumentIsCheckSave(Guid documentId, bool isCheck)
        {
            return await _caseService.UploadServicesDocumentsIsCheckAsync(documentId, isCheck);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ServiceFileResponseModel> ServiceDocumentSave(Guid documentId, DocumentFileAddType fileAddType, IFormFile file)
        {
            return await _caseService.UploadServicesFileAsync(documentId, fileAddType, file);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ServiceFileResponseModel> ServiceDocumentsSave(Guid documentId, DocumentFileAddType fileAddType, IFormFileCollection files)
        {
            return await _caseService.UploadServicesFilesAsync(documentId, fileAddType, files);
        }

        [HttpPost]
        //[ValidateAntiForgeryToken]
        public async Task<ServiceFileResponseModel> ServiceDocumentRemove(Guid id)
        {
            var response = await _caseService.RemoveServicesFileAsync(id);
            if (response.ResponseStatus.IsSuccess) ShowSuccess("Запись удалена");
            else ShowError("Ошибка");

            return response;
        }

        public async Task<IActionResult> DownloadFileCase(Guid id, Models.DocumentType type)
        {
            var file = await _caseService.DownloadServicesFileAsync(id, type);
            if (file is null) return NotFound();
            return File(file.OpenReadStream(), file.ContentType, file.FileName);
        }

        #endregion

        #region DocumentResult

        public async Task<IActionResult> ServiceDocumentsResults(Guid id)
        {
            ViewBag.Id = id;
            var documents = await _caseService.GetDocumentsResultsByServiceIdAsync(id);
            var modelBuilder = new ViewModelBuilder()
                .AddViewTitle("Результат")
                .AddModel(documents);
            return PartialView("Details/Sidebar/_SidebarDocumentsResults", modelBuilder);
        }

        public async Task<IActionResult> ServiceDocumentResultAddModal(Guid id, Guid documentId, DocumentFileAddType fileAddType)
        {
            ViewBag.Id = id;
            var model = new CaseServicesDocumentResultsAddModalDto(id, documentId, fileAddType);
            var modelBuilder = new ModalViewModelBuilder()
                .AddModalType(ModalType.LARGE)
                .AddModalViewPath("Details/Modals/_ModalDocumentResultAdd")
                .AddModalTitle("Добавление результата")
                .HideSubmitButton()
                .AddModel(model);
            return ModalLayoutView(modelBuilder);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ServiceFileResponseModel> ServiceDocumentResultSave(Guid documentId, DocumentFileAddType fileAddType, IFormFile file)
        {
            return await _caseService.UploadServicesFileResultAsync(documentId, fileAddType, file);
        }

        [HttpPost]
        //[ValidateAntiForgeryToken]
        public async Task<ServiceFileResponseModel> ServiceDocumentResultRemove(Guid id)
        {
            var response = await _caseService.RemoveServicesFileResultAsync(id);
            if (response.ResponseStatus.IsSuccess) ShowSuccess("Запись удалена");
            else ShowError("Ошибка");

            return response;
        }

        #endregion

        #region Notification
        public async Task<IActionResult> DetailsNotification(string id)
        {
            ViewBag.Id = id;
            var notifications = await _caseService.GetNotificationsByCaseIdAsync(id);

            var modelBuilder = new ModalViewModelBuilder()
              .AddModalType(ModalType.EXTRA)
              .AddModalViewPath("Details/Notification")
              .AddModel(notifications)
              .HideSubmitButton()
              .AddModalTitle("Оповещение");

            return ModalLayoutView(modelBuilder);
        }

        public async Task<IActionResult> CasesSMSAddModal(CasesSMSAddDto model)
        {
            var modelBuilder = new ModalViewModelBuilder()
              .AddModalType(ModalType.SMALL)
                .AddModalViewPath("Details/Modals/_ModalSMSAdd")
                .AddModalTitle("Добавление СМС")
                .AddModel(new CasesSMSSaveDto
                {
                    CasesId = model.CasesId,
                    CustomerName = model.CustomerName,
                    CustomerPhone = model.CustomerPhone,
                });

            return ModalLayoutView(modelBuilder);
        }


        public async Task<IActionResult> SMSCasesSave(CasesSMSSaveDto request)
        {
            var employee = await _employeeManager.GetFullInfoAsync();
            var response = await _caseService.SaveSMSCaseAsync(request, employee);
            if (response) ShowSuccess("СМС добавлен");
            else ShowError("При отправке СМС произошла ошибка");
            return await Details(request.CasesId);
        }


        [HttpGet]
        public async Task<string> CallPhoneV3(string caseid, Guid customerId, string phone)
        {
            var lineCALL = await _caseService.AddNotificationsByCaseIdAsync(caseid, customerId, phone);
            if (lineCALL is null) ShowError("Вызов не выполнен");
            return lineCALL;
        }

        #endregion

        #region AUdit
        public async Task<IActionResult> DetailsAudit(string id)
        {
            ViewBag.Id = id;
            var audits = await _caseService.GetServiceAuditByCaseIdAsync(id);

            var modelBuilder = new ModalViewModelBuilder()
             .AddModalType(ModalType.EXTRA)
             .AddModalViewPath("Details/Modals/_ModalAudit")
             .AddModel(audits)
             .HideSubmitButton()
             .AddModalTitle("Действия");

            return ModalLayoutView(modelBuilder);
        }
        #endregion

        #region Print
        public async Task<IActionResult> PrintProcessingPersonalData(string id)
        {
            var connection = _configuration.GetConnectionString("DefaultConnection");
            var content = await _caseService.GetPrintProcessingPersonalData(id, connection, BlankActionType.Pdf);
            return PdfLayoutView(content);
        }

        public async Task<IActionResult> DownloadProcessingPersonalData(string id)
        {
            var connection = _configuration.GetConnectionString("DefaultConnection");
            var content = await _caseService.GetPrintProcessingPersonalData(id, connection, BlankActionType.Word);

            return File(content, MimeTypeMap.GetMimeType(".docx"), "Согласие на ОПД.docx");
        }

        public async Task<IActionResult> PrintConsultationService(string id)
        {
            var mfcId = await _employeeManager.GetOfficeAsync();
            var connection = _configuration.GetConnectionString("DefaultConnection");
            var content = await _caseService.GetPrintConsultationService(id, connection, mfcId, BlankActionType.Pdf);
            return PdfLayoutView(content);
        }

        public async Task<IActionResult> DownloadConsultationService(string id)
        {
            var mfcId = await _employeeManager.GetOfficeAsync();
            var connection = _configuration.GetConnectionString("DefaultConnection");
            var content = await _caseService.GetPrintConsultationService(id, connection, mfcId, BlankActionType.Word);

            return File(content, MimeTypeMap.GetMimeType(".docx"), "Консультация.docx");
        }

        public async Task<IActionResult> ModalPrintAcceptDocuments(string id)
        {
            ViewBag.Id = id;
            var documents = await _caseService.GetDocumentsByServiceIdAsync(id);

            var modelBuilder = new ModalViewModelBuilder()
             .AddModalType(ModalType.EXTRA)
             .AddModalViewPath("Details/Modals/_ModalPrintAcceptDocuments")
             .AddModel(documents)
             .HideSubmitButton()
             .HideModalFooter()
             .AddModalTitle("Доументы");

            return ModalLayoutView(modelBuilder);
        }

        public async Task<bool> UploadAcceptDocuments(List<DocumentsPrintDto> doc)
        {
            var isUpload = await _caseService.UploadServicesDocumentsIsCheckAsync(doc);
            return isUpload;
        }

        public async Task<string> PrintAcceptDocuments(string id, List<DocumentsPrintDto> doc)
        {
            var response = await UploadAcceptDocuments(doc);

            if (!response)
            {
                ShowError("Ошибка при обработке документа");
                return null;
            }

            var employeeId = await _employeeManager.GetIdAsync();
            var connection = _configuration.GetConnectionString("DefaultConnection");
            var content = await _caseService.GetPrintAcceptDocuments(id, connection, employeeId, BlankActionType.Pdf);
            if (content == null)
            {
                ShowError("Ошибка при печати");
                return null;
            }
            return Convert.ToBase64String(content);
        }

        public async Task<IActionResult> DownloadAcceptDocuments(string id)
        {
            var employeeId = await _employeeManager.GetIdAsync();
            var connection = _configuration.GetConnectionString("DefaultConnection");
            var content = await _caseService.GetPrintAcceptDocuments(id, connection, employeeId, BlankActionType.Word);
            if (content == null)
            {
                ShowError("Ошибка при печати");
                return null;
            }

            return File(content, MimeTypeMap.GetMimeType(".docx"), "Реестр о принятие документов.docx");
        }


        public async Task<IActionResult> ModalPrintIssuanceDocuments(string id)
        {
            ViewBag.Id = id;
            var documents = await _caseService.GetDocumentsIssuanceByServiceIdAsync(id);

            var modelBuilder = new ModalViewModelBuilder()
             .AddModalType(ModalType.EXTRA)
             .AddModalViewPath("Details/Modals/_ModalPrintIssuanceDocuments")
             .AddModel(documents)
             .HideSubmitButton()
             .HideModalFooter()
             .AddModalTitle("Доументы");

            return ModalLayoutView(modelBuilder);
        }


        public async Task<bool> UploadIssuanceDocuments(List<DocumentsPrintDto> doc)
        {
            var isUpload = await _caseService.UploadServicesDocumentsIssuanceIsCheckAsync(doc);
            return isUpload;
        }

        public async Task<string> PrintIssuanceDocuments(string id, List<DocumentsPrintDto> doc)
        {
            var response = await UploadIssuanceDocuments(doc);

            if (!response)
            {
                ShowError("Ошибка при печати");
                return null;
            }

            var connection = _configuration.GetConnectionString("DefaultConnection");
            var employee = await _employeeManager.GetNameAsync();
            var content = await _caseService.GetPrintIssuanceDocuments(id, connection, employee, BlankActionType.Pdf);
            if (content == null)
            {
                ShowError("Ошибка при печати");
                return null;
            }
            return Convert.ToBase64String(content);
        }

        public async Task<IActionResult> DownloadIssuanceDocuments(string id, List<DocumentsPrintDto> doc)
        {
            var connection = _configuration.GetConnectionString("DefaultConnection");
            var employee = await _employeeManager.GetNameAsync();
            var content = await _caseService.GetPrintIssuanceDocuments(id, connection, employee, BlankActionType.Word);
            if (content == null)
            {
                ShowError("Ошибка при печати");
                return null;
            }
            return File(content, MimeTypeMap.GetMimeType(".docx"), "Реестр выдачи документов.docx");
        }

        #endregion

        #region Notes
        public async Task<IActionResult> ServiceNotesChangeModal(string caseid, Guid? id, ActionType actionType)
        {
            var NotesModelAdd = new NotesAddDto
            {
                DCaseId = caseid
            };

            var modelBuilder = new ModalViewModelBuilder()
                .AddModalType(ModalType.SMALL)
                .AddModalViewPath("Details/Modals/_ModalNotesAdd")
                .AddModalTitle((actionType == ActionType.Add ? "Добавление" : "Изменение") + " заметки")
                .AddModel(actionType == ActionType.Add ? NotesModelAdd : await _caseService.GetServiceNoteAsync(id.Value))
                .AddActionType(actionType);

            return ModalLayoutView(modelBuilder);
        }


        [HttpPost]
        public async Task ServiceNotesSave(NotesAddSaveDto request, ActionType actionType)
        {
            if (actionType == ActionType.Add)
            {
                var employeeId = await _employeeManager.GetIdAsync();
                request.EmployeeId = employeeId.Value;
                var responce = await _caseService.AddServiceNotesAsync(request);
                if (responce) ShowSuccess("Заметка добавлена");
                else ShowError("Добавление заметки невозможно");
            }
            else
            {
                var responce = await _caseService.EditServiceNotesAsync(request);
                if (responce) ShowSuccess("Заметка изменена");
                else ShowError("Изменение заметки невозможно");
            }
        }

        [HttpPost]
        public async Task ServiceNotesDelete(Guid id)
        {
            var responсe = await _caseService.DeleteServiceNotesAsync(id);

            if (responсe) ShowSuccess("Заметка удалена");
            else ShowError("Удаление заметки невозможно");
        }

        #endregion

        #region SignalR
        [AllowAnonymous]
        public async Task signalRAlerts(Guid id)
        {
            var Id = await _referencesService.GetEmployeeAspNetUserId(id);

            if (Id.HasValue)
            {
                await _hubContext.Clients.User(Id.Value.ToString()).SendAsync("Alert", true);
            }

        }

        #endregion

        #endregion

    }
}
