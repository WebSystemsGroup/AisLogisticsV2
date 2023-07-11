using AisLogistics.App.Models;
using AisLogistics.App.Models.DTO.Cases;
using AisLogistics.App.Models.DTO.Identity.Applicant;
using AisLogistics.App.Models.DTO.Services;
using AisLogistics.App.Models.Enums;
using AisLogistics.App.ViewModels.Cases;
using AisLogistics.DataLayer.Common.Dto.Case;
using AisLogistics.DataLayer.Entities.Models;
using DataTables.AspNet.Core;

namespace AisLogistics.App.Service
{
    public interface ICaseService
    {
        /// <summary>
        /// Список обращений
        /// </summary>
        /// <param name="employeeId">id сотрудника</param>
        /// <returns></returns>
        (List<CasesDto>, int, int) GetCases(IDataTablesRequest request, Guid? employeeId, Guid? office);
        Task<(List<CasesDto>, int, int)> GetCasesV2(IDataTablesRequest request, Guid? employeeId, Guid? office);
        Task<(List<CasesDto>, int, int)> GetCasesV3(IDataTablesRequest request, SearchCasesRequestData additionalRequest);
        /// <summary>
        /// Обращение по id 
        /// </summary>
        /// <param name="id">id услуги</param>
        /// <returns></returns>
        Task<CasesMainDto> GetCaseByIdAsync(string id, Guid employeeId);
        /// <summary>
        /// Услуги по id обращения
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<List<CaseServiceDto>> GetServicesByCaseIdAsync(string id);

        Task<CasesDto> GetServicesByCaseIdAsyncV2(string id);

        (List<CasesDto>, int, int) GetIssueCases(IDataTablesRequest request, Guid? employeeId, Guid? office);
        Task<(List<CasesDto>, int, int)> GetIssueCasesV2(IDataTablesRequest request, SearchCasesRequestData additionalRequest);
        (List<CasesReestrSmevDto>, int, int) GetCasesReestrSmev(IDataTablesRequest request, Guid? employeeId, Guid? office);
        Task<(List<CasesReestrSmevDto>, int, int)> GetCasesReestrSmevV2(IDataTablesRequest request, SearchCasesRequestData additionalRequest);

        Task ViewCaseAsync(string id);
        Task SetCaseFavoriteAsync(string id);
        Task RemoveCaseFavoriteAsync(string id);
        Task SetServiceFavoriteAsync(Guid id);
        Task RemoveServiceFavoriteAsync(Guid id);
        /// <summary>
        /// Список активных услуг для Физ.лиц
        /// </summary>
        /// <param name="searchData">данные для поиска</param>
        /// <param name="start">skip</param>
        /// <param name="length">take</param>
        /// <returns></returns>
        (List<ActiveServicesDto>, int, int) GetActiveServices(SearchCasesRequestData searchData, int start,
            int length);
        /// <summary>
        /// Список активных услуг для Юр.лиц
        /// </summary>
        /// <param name="searchData">данные для поиска</param>
        /// <param name="start">skip</param>
        /// <param name="length">take</param>
        /// <returns></returns>
        (List<ActiveServicesDto>, int, int) GetActiveServicesForLegal(SearchCasesRequestData searchData, int start,
            int length);

        /// <summary>
        /// Процедуры услуги
        /// </summary>
        /// <param name="serviceId">id услуги</param>
        /// <returns></returns>
        List<ActiveServiceProcedureDto> GetServiceProcedures(Guid serviceId);
        /// <summary>
        /// Тарифы услуги
        /// </summary>
        /// <param name="procedureId">id процедуры</param>
        /// <returns></returns>
        List<ActiveServiceTariffDto> GetServiceTariff(Guid procedureId, int? type);
        /// <summary>
        /// Поставщики услуг
        /// </summary>
        /// <param name="serviceId">id услуги</param>
        /// <returns></returns>
        List<ActiveServiceProviderDto> GetServiceProvidersAll();
        /// <summary>
        /// Создание услуги
        /// </summary>
        /// <param name="requestModel">Модель услуги</param>
        /// <returns></returns>
        Task<CreateCaseResponseModel> CreateCaseAsync(CreateCaseRequestModel requestModel);
        Task<CreateCaseResponseModel> CreateCaseAsync(CreateCaseLegalRequestModel requestModel);
        /// <summary>
        /// Список выполненных СМЭВ запросов по id услуги
        /// </summary>
        /// <param name="id">Id услуги</param>
        /// <returns></returns>
        Task<List<CaseServiceSmevСompletedDto>> GetSmevСompletedByServiceIdAsync(Guid id);
        Task<List<CaseServicePaymentsСompletedDto>> GetPaymentsСompletedByServiceIdAsync(Guid id);
        Task<ServicePaymentsInfo?> GetPaymentsInfoServiceIdAsync(Guid id);
        Task<Guid?> AddServicePaymentAsync(ServicePaymentsRequestModelAddDto request);
        bool IsCheckServicePaymentAsync(Guid Id);

        /// <summary>
        /// Список возможных СМЭВ запросов, для добавления нового запроса в услугу
        /// </summary>
        /// <param name="id">Id услуги</param>
        /// <param name="search">строка поиска</param>
        /// <param name="start">skip</param>
        /// <param name="length">take</param>
        /// <returns></returns>
        List<CaseServiceSmevActiveDto> GetSmevActiveByServiceId(Guid id, string search, int start, int length, out int smevCount, out int smevFilteredCount);

        ApplicantAdditionalDto GetApplicantByServiceId(Guid id);
        Dictionary<string, string> GetBlankMarks(Guid id);
        List<ApplicantAdditionalDto> GetApplicantsByServiceId(Guid id, bool includeLegal = false);

        Task<List<CaseServiceStageDto>> GetStagesByServiceIdAsync(Guid id);
        Task<List<StageDto>> GetStagesNextAllAsync();
        Task<List<StageDto>> GetStagesNextByServiceIdAsync(List<Guid> id);
        Task<List<EmployeesDtO>> GetStagesNextEmployessByServiceIdAsync(ServiceStageNextEmployessDto request);

        Task<bool> AddServiceStageAsync(ServiceStageSaveDto request);
        Task<(bool, int)> AddServiceStageUnclaimedAsync(ServiceStageSaveDto request);

        Task<NotesAddDto?> GetServiceNoteAsync(Guid id);
        Task<bool> AddServiceNotesAsync(NotesAddSaveDto request);
        Task<bool> EditServiceNotesAsync(NotesAddSaveDto request);
        Task<bool> DeleteServiceNotesAsync(Guid id);

        Task<bool> AddServiceDepartamentAsync(ServicesDepartamentDto model);

        Task<List<CaseServiceDocumentsDto>> GetDocumentsByServiceIdAsync(Guid id);
        Task<List<CaseServiceDocumentsDto>> GetDocumentsByServiceIdAsync(string id);
        Task<List<CaseServiceDocumentsDto>> GetDocumentsIssuanceByServiceIdAsync(string id);
        Task<List<CaseServiceDocumentsResultsDto>> GetDocumentsResultsByServiceIdAsync(Guid id);
        Task<bool> UploadServicesDocumentsIsCheckAsync(Guid id, bool isCheck);
        Task<bool> UploadServicesDocumentsIsCheckAsync(List<DocumentsPrintDto> doc);
        Task<bool> UploadServicesDocumentsIssuanceIsCheckAsync(List<DocumentsPrintDto> doc);
        Task<ServiceFileResponseModel> UploadServicesFileAsync(Guid id, DocumentFileAddType fileAddType, IFormFile file); 
        Task<ServiceFileResponseModel> UploadServicesFilesAsync(Guid id, DocumentFileAddType fileAddType, IFormFileCollection files);
        Task<ServiceFileResponseModel> RemoveServicesFileAsync(Guid id);
        Task<ServiceFileResponseModel> UploadServicesFileResultAsync(Guid id, DocumentFileAddType fileAddType, IFormFile file);
        Task<ServiceFileResponseModel> RemoveServicesFileResultAsync(Guid id); 
        Task<FormFile> DownloadServicesFileAsync(Guid id, DocumentType type);
        Task<CaseServiceBlankFile> GetServiceBlankByIdAsync(Guid id);

        byte[] GetBlancFastReportFileAsync(byte[] content, Guid serviceId, string connection);
        byte[] GetBlancDocxFileAsync(byte[] content, Guid serviceId);
        Task<CaseServiceBlankFile> GetServiceFileByIdAsync(Guid id);
        Task<List<CaseCommentsDto>> GetCommentsByCaseIdAsync(string id);
        Task<CaseNotificationsDto> GetNotificationsByCaseIdAsync(string id);
        Task<bool> SaveSMSCaseAsync(CasesSMSSaveDto request, EmployeeInfo employee);
        Task<string> AddNotificationsByCaseIdAsync(string caseid, Guid customerId, string tel);
        Task<CaseCommentsDto> AddCommentAsync(string id, string comment, Guid[] employeesId);
        Task<List<CaseAuditDto>> GetServiceAuditByCaseIdAsync(string id);

        Task<(Guid ServiceId, string? FormPath)> GetAdditionalFormByCaseIdAsync(Guid id);
        Task<T?> GetAdditionalDataByServiceIdAsync<T>(Guid id);
        Task SaveAdditionalData(Guid id, string data, Dictionary<string, string> search);

        Task<List<CaseServiceBlank>?> GetServiceBlanksByCaseIdAsync(Guid id);
        Task<List<CaseServiceBlank>?> GetServiceFilesByCaseIdAsync(Guid id);


        Task<ApplicantsDto?> GetCustomerByDocumentSerialAndNumberAsync(string serial, string number); //TODO переделать
        Task<LegassDto?> GetLegalByInnAsync(string inn); //TODO переделать
        Task<List<ApplicantDto>> GetCustomersByCaseNumberAsync(string caseId);
        Task<CustomerModelDto?> GetCustomerByIdAsync(Guid id); //TODO переделать
        Task<DServicesCustomersLegal?> GetCustomerLegalByIdAsync(Guid id); //TODO переделать

        Task<CaseCustomerResponseModel> AddCustomerAsync(DServicesCustomer customer); //TODO переделать
        Task<CaseCustomerResponseModel> UpdateCustomerAsync(CustomerModelDto request); //TODO переделать
        Task<CaseCustomerResponseModel> UpdateCustomerLegalAsync(DServicesCustomersLegal customer); //TODO переделать


        Task<byte[]?> GetPrintProcessingPersonalData(string caseId, string connection, BlankActionType type);
        Task<byte[]?> GetPrintConsultationService(string caseId, string connection, Guid? id, BlankActionType type);
        Task<byte[]?> GetPrintAcceptDocuments(string caseId, string connection, Guid? id, BlankActionType type);
        Task<byte[]?> GetPrintIssuanceDocuments(string caseId, string connection, string employeeName, BlankActionType type);
    }
}