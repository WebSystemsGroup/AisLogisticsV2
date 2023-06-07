using AisLogistics.App.Models;
using AisLogistics.App.Models.DTO.References;
using AisLogistics.App.Models.DTO.Services;
using AisLogistics.App.Models.DTO.Template;
using AisLogistics.App.ViewModels.References;
using AisLogistics.DataLayer.Common.Dto.Reference;
using AisLogistics.DataLayer.Entities.Models;
using DataTables.AspNet.Core;

namespace AisLogistics.App.Service
{
    public interface IReferencesService
    {
        #region Документы

        Task<List<DocumentDto>> GetAllDocumentsAsync();

        #endregion

        #region Офисы

        /// <summary>
        /// Поучить список офисов
        /// </summary>
        /// <param name="request">Запрос</param>
        (List<OfficeParentsDto>, int, int) GetOffices(IDataTablesRequest request);

        Task<List<OfficeParentsDto>> GetOfficesParentAllAsync();

        Task<List<OfficeDto>> GetTospDtoAsync(Guid id);
        Task<List<OfficeDto>> GetActiveEmployeeOfficesDtoAsync(Guid id);
        Task<OfficeDto?> GetActiveEmployeeOfficeDtoAsync(Guid id);
        Task<List<OfficeDto>> GetAllOfficesAsync();
        Task<List<OfficeDto>> GetAllOfficesTypeAsync(int? type);
        Task<List<OfficeDto>> GetAllOfficesTypeMfcAsync();
        Task<List<OfficeDto>> GetAllOfficesTypeMfcAndTospAsync();
        Task<List<OfficeDto>> GetAllOfficesTypeMfcAndTospAsync(List<Guid> id);
        Task<List<OfficeDto>> GetServiceProvidersAll();
        Task<List<OfficeDto>> GetServiceProvidersAll(List<Guid> id);
        Task<List<OfficeDto>> GetServiceProvidersDepartement(List<Guid> id);
        Task<OfficeDto> GetOfficeForQueueAsync(int queueId);
        Task<List<EmployeeInfo>> GetEmployeesForQueueAsync(Guid officeId);
        Task<List<OfficeQueueDto>> GetOfficesQueueAsync();

        Task<OfficeModelDto> GetOfficeDtoAsync(Guid? id);
        Task<Dictionary<string, string>> GetOfficeTypesAsync();
        Task AddOfficeAsync(OfficeModelDto model);
        Task UpdateOfficeAsync(OfficeModelDto model);
        Task RemoveOfficeAsync(Guid id, string comment);
        Task<List<OfficeDto>> GetEmployeesForMfc(List<Guid> employeeId);

        #endregion 

        #region Роли исполнителя

        /// <summary>
        /// Поучить список ролей исполнителя
        /// </summary>
        /// <param name="request">Запрос</param>
        (List<RoleExecutorDto>, int, int) GetRolesExecutor(IDataTablesRequest request);

        //(List<RoleExecutorDto>, int, int) GetRolesExecutor(int start, int length);
        //(List<RoleExecutorDto>, int, int) GetRolesExecutor(Func<SRolesExecutor, bool> searchPredicate, int start, int length);
        Task<RoleExecutorModelDto> GetRolesExecutorModelDtoAsync(Guid? id);
        Task AddRoleExecutorAsync(RoleExecutorModelDto model);
        Task UpdateRoleExecutorAsync(RoleExecutorModelDto model);
        Task RemoveRolesExecutorAsync(Guid id, string comment);

        #endregion

        #region Календарь

        Task<IEnumerable<FullCalendarDateEventDto>> GetHolidaysAsync(DateTime dateStart, DateTime dateEnd);
        Task<IEnumerable<FullCalendarDateEventDto>> GetCalendarDatesAsync(DateTime dateStart, DateTime dateEnd);
        Task<CalendarDateModelDto> GetCalendarDateAsync(DateTime date);
        Task UpdateCalendarDateAsync(CalendarDateModelDto model);
        Task RemoveCalendarDateAsync(int id, string comment);

        #endregion

        #region Файлы

        /// <summary>
        /// Поучить список файлов
        /// </summary>
        /// <param name="request">Запрос</param>
        (List<ReferencesServicesFileDto>, int, int) GetReferencesServicesFiles(IDataTablesRequest request);
        ////Task<List<ReferencesServicesFileDto>> GetAllReferencesServicesFilessAsync();
        //(List<ReferencesServicesFileDto>, int, int) GetReferencesServicesFiles(Func<SServicesFile, bool> searchPredicate, int start, int length);
        //(List<ReferencesServicesFileDto>, int, int) GetReferencesServicesFiles(int start, int length);
        Task RemoveReferencesServicesFileAsync(Guid Id, string comment);

        /// <summary>
        /// Добавить файл
        /// </summary>
        Task AddReferencesServicesFileAsync(ReferencesServicesFileModelDto model);

        /// <summary>
        /// Модель для редактирования файла
        /// </summary>
        Task<ReferencesServicesFileModelDto> GetReferencesServicesFileDtoAsync(Guid? Id);

        /// <summary>
        /// Обновить файл
        /// </summary>
        Task UpdateReferencesServicesFileAsync(ReferencesServicesFileModelDto model);

        #endregion

        #region Услуги

        /// <summary>
        /// Поучить список услуг
        /// </summary>
        /// <param name="request">Запрос</param>
        Task<(List<ServicesDto>, int, int)> GetServices(IDataTablesRequest request);
        Task<List<ServicesDto>> GetServicesAll();
        Task<List<ServicesDto>> GetServicesAll(List<Guid> id);
        Task<List<ServicesDto>> GetServicesForProviders(List<Guid> provides);
        Task<ServiceModelDto> GetServiceDtoAsync(Guid? Id);
        Task AddServiceAsync(ServiceModelDto model);
        Task UpdateServiceAsync(ServiceModelDto model);
        Task RemoveServiceAsync(Guid Id, string comment);
        Task CopyServiceAsync(Guid Id, string EmployeeFio);
        Task<List<SelectListDto<int>>> GetServiceTypesAll();
        Task<List<SelectListDto<int>>> GetServiceInteractionsAll();
        Task<List<SelectListDto<Guid>>> GetServiceLivingSituationAll();

        Task<List<SelectListDto<int>>> GetServiceHashtagAll();

        /// <summary>
        /// Поучить список получателей услуги
        /// </summary>
        /// <param name="request">Запрос</param>
        /// <param name="serviceId">Id услуги</param>
        (List<ServiceCustomerDto>, int, int) GetServiceCustomers(IDataTablesRequest request, Guid serviceId);
        List<ServiceCustomerTypeDto> GetServiceCustomerTypes(Func<SServicesCustomerType, bool> searchPredicate);
        Task<List<ServiceCustomerTypeDto>> GetServiceCustomerTypes();
        Task<List<ServiceCustomerTypeDto>> GetServiceCustomerMainTypes();
        Task RemoveServiceCustomerAsync(Guid Id, string comment);
        Task UpdateServiceCustomerAsync(ServiceCustomerModelDto model);
        Task AddServiceCustomerAsync(ServiceCustomerModelDto model);
        Task<ServiceCustomerModelDto> GetServiceCustomerDtoAsync(Guid? Id);
        Task<List<ServiceCustomerDto>> GetAllServiceCustomersAsync(Guid serviceId);



        /// <summary>
        /// Поучить список документов услуги
        /// </summary>
        /// <param name="request">Запрос</param>
        /// <param name="serviceId">Id услуги</param>
        (List<ServiceDocumentDto>, int, int) GetServiceDocuments(IDataTablesRequest request, Guid serviceId);

        //(List<ServiceDocumentDto>, int, int) GetServiceDocuments(Func<SServicesDocument, bool> searchPredicate, Guid serviceId, int start, int length);
        //(List<ServiceDocumentDto>, int, int) GetServiceDocuments(Guid serviceId, int start, int length);
        Task<List<ServiceDocumentDto>> GetAllServiceDocumentsAsync(Guid serviceId);
        Task<ServiceDocumentModelDto> GetServiceDocumentDtoAsync(Guid? Id);
        Task AddServiceDocumentAsync(ServiceDocumentModelDto model);
        Task UpdateServiceDocumentAsync(ServiceDocumentModelDto model);
        Task RemoveServiceDocumentAsync(Guid Id, string comment);


        (List<ServiceDocumentResultDto>, int, int) GetServiceDocumentsResults(IDataTablesRequest request, Guid serviceId);
        Task<ServiceDocumentResultModelDto> GetServiceDocumentResultDtoAsync(Guid? Id);
        Task<List<ServiceDocumentResultDto>> GetAllServiceDocumentsResults(Guid serviceId);
        Task AddServiceDocumentResultAsync(ServiceDocumentResultModelDto model);
        Task UpdateServiceDocumentResultAsync(ServiceDocumentResultModelDto model);
        Task RemoveServiceDocumentResultAsync(Guid Id, string comment);

        /// <summary>
        /// Поучить список запросов СМЭВ услуги
        /// </summary>
        /// <param name="request">Запрос</param>
        /// <param name="serviceId">Id услуги</param>
        (List<ServiceSmevDto>, int, int) GetServiceSmevRequests(IDataTablesRequest request, Guid serviceId);

        Task<List<ServiceSmevRequestDto>> GetAllSmevRequestsAsync();
        List<ServiceSmevDto> GetAllServiceSmevRequests(Guid serviceId);
        Task<ServiceSmevRequestModelDto> GetServiceSmevRequestAsync(Guid? Id);
        Task RemoveServiceSmevRequestAsync(Guid Id, string comment);
        Task AddServiceSmevRequestAsync(ServiceSmevRequestModelDto model);
        Task UpdateServiceSmevRequestAsync(ServiceSmevRequestModelDto model);

        (List<ServiceCustomerTariffDto>, int, int) GetServiceCustomerTariffs(IDataTablesRequest request, Guid serviceId);
        Task<ServiceCustomerTariffModelDto> GetServiceCustomerTariffAsync(Guid? Id);
        List<ServiceCustomerTariffDto> GetAllServiceCustomerTariffs(Guid serviceId);
        Task UpdateServiceCustomerTariffAsync(ServiceCustomerTariffModelDto model);
        Task AddServiceCustomerTariffAsync(ServiceCustomerTariffModelDto model);
        Task RemoveServiceCustomerTariffAsync(Guid Id, string comment);
        Task<List<ServiceTariffTypeDto>> GetAllServiceTariffTypesAsync();
        Task<Dictionary<string, string>> GetAllServiceWeekTypesAsync();

        /// <summary>
        /// Поучить список файлов услуги
        /// </summary>
        /// <param name="request">Запрос</param>
        /// <param name="serviceId">Id услуги</param>
        (List<ServiceFileDto>, int, int) GetServiceFiles(IDataTablesRequest request, Guid serviceId);
        Task<List<ServiceFileDto>> GetAllServiceFiles(Guid serviceId);
        Task<ServiceFileModelDto> GetServiceFileAsync(Guid? Id);
        Task RemoveServiceFileAsync(Guid Id, string comment);
        Task AddServiceFileAsync(ServiceFileModelDto model);
        Task UpdateServiceFileAsync(ServiceFileModelDto model);

        /// <summary>
        /// Поучить список бланков услуги
        /// </summary>
        /// <param name="request">Запрос</param>
        /// <param name="serviceId">Id услуги</param>
        (List<ServiceBlankDto>, int, int) GetServiceBlancs(IDataTablesRequest request, Guid serviceId);
        Task<List<ServiceBlankDto>> GetAllServiceBlancs(Guid serviceId);
        Task<ServiceBlancModelDto> GetServiceBlancsAsync(Guid? Id);
        Task RemoveServiceBlancsAsync(Guid Id, string comment);
        Task AddServiceBlancsAsync(ServiceBlancModelDto model);
        Task UpdateServiceBlancsAsync(ServiceBlancModelDto model);


        /// <summary>
        /// Поучить список способов обращений услуги
        /// </summary>
        /// <param name="request">Запрос</param>
        /// <param name="serviceId">Id услуги</param>
        /// <param name="wt">Тип обращения</param>
        (List<ServiceWayGetDto>, int, int) GetServiceWaysGet(IDataTablesRequest request, Guid serviceId, int wt);

        Task<List<WayGetDto>> GetAllServiceWaysGetAsync();
        List<ServiceWayGetDto> GetAllServiceWaysGet(Guid serviceId, int wayType);
        Task<ServiceWayGetModelDto> GetServiceWayGetAsync(Guid? Id);
        Task RemoveServiceWayGetAsync(Guid Id, string comment);
        Task AddServiceWayGetAsync(ServiceWayGetModelDto model);
        Task UpdateServiceWayGetAsync(ServiceWayGetModelDto model);

        /// <summary>
        /// Поучить список оснований услуги
        /// </summary>
        /// <param name="request">Запрос</param>
        /// <param name="serviceId">Id услуги</param>
        /// <param name="rt">Тип основания</param>
        (List<ServiceReasonDto>, int, int) GetServiceReasons(IDataTablesRequest request, Guid serviceId, int rt);
        Task<ServiceReasonModelDto> GetServiceReasonAsync(Guid? Id);
        Task RemoveServiceReasonAsync(Guid Id, string comment);
        Task AddServiceReasonAsync(ServiceReasonModelDto model);
        Task UpdateServiceReasonAsync(ServiceReasonModelDto model);
        (List<ServiceQualityTypeDto>, int, int) GetServiceQualityTypes(Guid serviceId, int start, int length);
        List<ServiceQualityTypeDto> GetAllServiceQualityTypes(Guid serviceId);
        Task<List<QualityTypeDto>> GetAllQualityTypesAsync();
        Task<ServiceQualityTypeModelDto> GetServiceQualityTypeDtoAsync(Guid? Id);
        Task RemoveServiceQualityTypeAsync(Guid Id, string comment);
        Task AddServiceQualityTypeAsync(ServiceQualityTypeModelDto model);
        Task UpdateServiceQualityTypeAsync(ServiceQualityTypeModelDto model);

        Task<(List<ServiceActivityDto>, int, int)> GetServiceActivitiesAsync(Guid serviceId, int start, int length);
        Task<List<StageDto>> GetAllRoutesStagesAsync();
        Task<List<StatusDto>> GetAllStatuseAsync();
        /// <summary>
        /// Поучить список процедур услуги
        /// </summary>
        /// <param name="serviceId">Id услуги</param>
        /// <param name="start">Брать начиная с</param>
        /// <param name="length">Кол-во элементов</param>
        Task<(List<ServicesProcedureDto>, int, int)> GetServiceProceduresAsync(Guid serviceId, int start, int length);

        /// <summary>
        /// Поучить список всех процедур услуги
        /// </summary>
        /// <param name="serviceId">Id услуги</param>
        Task<List<ServicesProcedureDto>> GetAllServiceProceduresAsync(Guid serviceId);

        /// <summary>
        /// Модель для добавления / редактирования процедуры услуги
        /// </summary>
        Task<ServiceProcedureModelDto> GetServiceProcedureDtoAsync(Guid? Id);

        /// <summary>
        /// Пометить на удаление процедуру услуги
        /// </summary>
        /// <param name="Id">Id записи</param>
        /// <param name="comment">Причина удаления</param>
        Task RemoveServiceProcedureAsync(Guid Id, string comment);

        /// <summary>
        /// Добавить процедуру услуги
        /// </summary>
        Task AddServiceProcedureAsync(ServiceProcedureModelDto model);

        /// <summary>
        /// Обновить данные процедуры услуги
        /// </summary>
        Task UpdateServiceProcedureAsync(ServiceProcedureModelDto model);

        /// <summary>
        /// Получить список этапов услуги
        /// </summary>
        /// <param name="request">Параметры запроса</param>
        /// <param name="serviceId">ID услуги</param>
        /// <param name="parentId">ID родителя</param>
        (List<ServicesRoutesStageDto>, int, int) GetServiceStages(IDataTablesRequest? request, Guid serviceId, Guid parentId);

        #endregion

        #region СМЭВ

        /// <summary>
        /// Поучить список сервисов СМЭВ
        /// </summary>
        /// <param name="request">Запрос</param>
        (List<SmevServiceDto>, int, int) GetSmevServices(IDataTablesRequest request);
        Task<SmevServiceModelDto> GetSmevServiceModelDtoAsync(Guid? Id);
        Task RemoveSmevServiceAsync(Guid Id, string comment);
        Task AddSmevServiceAsync(SmevServiceModelDto model);
        Task UpdateSmevServiceAsync(SmevServiceModelDto model);

        /// <summary>
        /// Поучить список запросов СМЭВ
        /// </summary>
        /// <param name="request">Запрос</param>
        (List<SmevRequestDto>, int, int) GetSmevRequests(IDataTablesRequest request);
        //(List<SmevRequestDto>, int, int) GetSmevRequests(int start, int length);

        Task<SmevRequestModelDto> GetSmevRequestModelDtoAsync(int? Id);
        Task<List<SmevRequestDto>> GetAllSmevRequestAsync(List<int> id);
        Task<List<SmevServiceDto>> GetAllSmevServicesAsync();
        Task<List<SmevServiceDto>> GetAllSmevServicesAsync(List<Guid> id);
        Task<List<SelectListDto<string>>> GetAllSmevProvidersAsync();
        Task<List<SmevRequestDto>> GetSmevRequestBySmevService(List<Guid> id);
        Task RemoveSmevRequestAsync(int Id, string comment);
        Task AddSmevRequestAsync(SmevRequestModelDto model);
        Task UpdateSmevRequestAsync(SmevRequestModelDto model);
        
        Task<List<SmevReferenceServiceDto>> GetServicesForSmevAsync(int id);
        Task<bool> AddServicesForSmevAsync(AddSmevReferenceServiceDto request);
        Task<bool> DeleteServicesForSmevAsync(Guid id);

        #endregion

        #region Сотрудники

        Task<EmployeeModelDto> GetEmployeeDtoAsync(Guid? Id);
        Task<List<EmployeeModelDto>> GetEmployeesDtoAsync(List<Guid> Id);
        Task UpdateEmployeeAsync(EmployeeModelDto model);

        /// <summary>
        /// Получить должности и офисы сотрудника
        /// </summary>
        /// <param name="request">Запрос</param>
        /// <param name="id">Id сотрудника</param>
        (List<EmployeeOfficeDto>, int, int) GetEmployeeOffices(ref IDataTablesRequest request, Guid id);

        (List<EmployeesOfficesJoinDto> Employees, int TotalCount, int FilteredCount) GetReferencesEmployees(Func<EmployeesOfficesJoinDto, bool> searchPredicate, int? start = null, int? length = null);
        (List<EmployeesOfficesJoinDto> Employees, int TotalCount, int FilteredCount) GetReferencesEmployees(int? start = null, int? length = null);
        Task<EmployeeOfficeModelDto> GetEmployeeOfficeDtoAsync(Guid? Id);
        Task<List<EmployeesJobDto>> GetAllJobsAsync();
        Task<EmployeeOfficeModelDto> GetLastEmployeesOfficeDtoAsync(Guid employeeId);
        Task AddEmployeeOfficeAsync(EmployeeOfficeModelDto model);
        Task RemoveEmployeeOfficeAsync(Guid Id, string comment);

        /// <summary>
        /// Получить статусы сотрудника
        /// </summary>
        /// <param name="request">Запрос</param>
        /// <param name="id">Id сотрудника</param>
        (List<EmployeeStatusDto>, int, int) GetEmployeeStatuses(ref IDataTablesRequest request, Guid id);

        Task<Dictionary<string, string>> GetAllEmployeeStatusesAsync();
        Task<EmployeeStatusModelDto> GetLastEmployeeStatusDtoAsync(Guid employeeId);
        Task RemoveEmployeeStatusAsync(Guid Id, string comment);
        Task AddEmployeeStatusAsync(EmployeeStatusModelDto model);

        /// <summary>
        /// Получить исполнения сотрудника
        /// </summary>
        /// <param name="request">Запрос</param>
        /// <param name="id">Id сотрудника</param>
        (List<EmployeeExecutionDto>, int, int) GetEmployeeExecutions(ref IDataTablesRequest request, Guid id);

        Task<EmployeeExecutionModelDto> GetLastEmployeeExectionDtoAsync(Guid employeeId);
        Task<EmployeeExecutionModelDto> GetEmployeeExectionDtoAsync(Guid id);
        Task<bool> GetIsEmployeeExectionAsync(Guid employeeId);
        Task AddEmployeeExecutionAsync(EmployeeExecutionModelDto model);
        Task RemoveEmployeeExecutionAsync(Guid Id, string comment);

        /// <summary>
        /// Получить роли исполнения сотрудника
        /// </summary>
        /// <param name="request">Запрос</param>
        /// <param name="id">Id сотрудника</param>
        (List<EmployeeExecutorRoleDto>, int, int) GetEmployeeExecutorRoles(ref IDataTablesRequest request, Guid id);

        Task<List<RoleExecutorDto>> GetAllRolesExecutorAsync();
        Task<List<EmployeeExecutorRoleDto>> GetAllEmployeeExecutorRolesAsync(Guid id);
        Task RemoveEmployeeExecutorRoleAsync(Guid Id, string comment);
        Task AddEmployeeExecutorRoleAsync(EmployeeRoleExecutorModelDto model);

        /// <summary>
        /// Получить активности сотрудника
        /// </summary>
        /// <param name="request">Запрос</param>
        /// <param name="id">Id сотрудника</param>
        (List<EmployeeActivityDto>, int, int) GetEmployeeActivities(ref IDataTablesRequest request, Guid id);

        Task<EmployeeActivityModelDto> GetLastEmployeeActivityDtoAsync(Guid employeeId);
        Task<EmployeeActivityModelDto> GetEmployeeActivityDtoAsync(Guid id);
        Task<bool> GetIsEmployeeActivityAsync(Guid employeeId);
        Task RemoveEmployeeActivityAsync(Guid Id, string comment);
        Task AddEmployeeActivityAsync(EmployeeActivityModelDto model);
        Task<ServiceActivityModelDto> GetLastServiceActivityDtoAsync(Guid serviceId);
        Task RemoveServiceActivityAsync(Guid Id, string comment);
        Task AddServiceActivityAsync(ServiceActivityModelDto model);
        Task EmployeePasswordChange(EmployeePasswordChangeDto password);
        (List<EmployeesOfficesJoinDto>, int, int) GetReferencesEmployees(IDataTablesRequest request);

        /// <summary>
        /// Добавить сотрудника
        /// </summary>
        Task<Guid> AddEmployeeAsync(EmployeeModelDto model);

        /// <summary>
        /// Добавить сотрудника и связанные с ним данные
        /// </summary>
        Task<Guid> AddEmployeeWithLinkedDataAsync(EmployeeAddModelDto model);

        /// <summary>
        /// Модель для добавления / редактирования этапа услуги
        /// </summary>
        Task<ServiceStageModelDto> GetServiceStageDtoAsync(Guid? Id);

        /// <summary>
        /// Получить список этапов услуги
        /// </summary>
        /// <param name="serviceId">ID услуги</param>
        /// <param name="parentId">ID родителя</param>
        Task<List<ServicesRoutesStageDto>> GetServiceStagesAsync(Guid serviceId, Guid parentId);

        /// <summary>
        /// Добавить этап услуги
        /// </summary>
        Task AddServiceStageAsync(ServiceStageModelDto model);

        /// <summary>
        /// Обновить этап услуги
        /// </summary>
        Task UpdateServiceStageAsync(ServiceStageModelDto model);

        /// <summary>
        /// Пометить на удаление этап услуги
        /// </summary>
        /// <param name="Id">Id записи об этапе услуги</param>
        /// <param name="comment">Причина удаления</param>
        Task RemoveServiceStageAsync(Guid Id, string comment);

        /// <summary>
        /// Добавить роль исполнителя к этапу услуги
        /// </summary>
        Task AddServiceStageRoleAsync(ServicesStageRoleModelDto model);

        /// <summary>
        /// Получить все роли исполнителя этапа
        /// </summary>
        /// <param name="Id">ID этапа</param>
        Task<List<RoleExecutorDto>> GetServiceStageRolesAsync(Guid Id);

        /// <summary>
        /// Пометить на удаление роль исполнителя этапа услуги
        /// </summary>
        /// <param name="Id">Id роли</param>
        /// <param name="comment">Причина удаления</param>
        Task RemoveServiceStageRoleAsync(Guid Id, string comment);

        Task<Guid?> GetEmployeeAspNetUserId(Guid Id);

        ///// <summary>
        ///// Все стадии услуг
        ///// </summary>
        //Task<List<ServicesRoutesStageDto>> GetAllServiceStagesAsync();
        #endregion

        #region Информация

        /// <summary>
        /// Получить список "Информация"
        /// </summary>
        (List<InformationDto>, int, int) GetInformations(IDataTablesRequest request);

        /// <summary>
        /// Типы информации
        /// </summary>
        Task<Dictionary<string, string>> GetInformationTypesAsync();

        /// <summary>
        /// Добавить информацию
        /// </summary>
        Task AddInformationAsync(InformationModelDto model);

        /// <summary>
        /// Пометить на удаление информацию
        /// </summary>
        /// <param name="Id">Id записи</param>
        /// <param name="comment">Причина удаления</param>
        Task RemoveInfornmationAsync(Guid Id, string comment);

        /// <summary>
        /// Скачать информационный файл
        /// </summary>
        /// <param name="Id">Id записи</param>
        Task<FormFile> DownloadInformationFileAsync(Guid Id);

        /// <summary>
        /// Получить файлы информации
        /// </summary>
        /// <param name="InformationId">Id информации</param>
        Task<List<InformationFileDto>> GetInformationFilesAsync(Guid InformationId);

        /// <summary>
        /// Модель для редактирования информации
        /// </summary>
        Task<InformationModelDto> GetInformationDtoAsync(Guid? Id);

        /// <summary>
        /// Получить офисы, связанные с информацией
        /// </summary>
        /// <param name="InformationId">Id информации</param>
        Task<List<Guid>> GetInformationOfficesAsync(Guid? InformationId);

        /// <summary>
        /// Удалить информационный файл
        /// </summary>
        /// <param name="Id">Id файла(записи)</param>
        Task RemoveInformationFileAsync(Guid Id);

        /// <summary>
        /// Обновить информацию
        /// </summary>
        Task UpdateInformationAsync(InformationModelDto model);

        /// <summary>
        /// Добавить файлы к информации
        /// </summary>
        Task<InformationFileResponseModel> UploadInformationFilesAsync(UploadInformationFilesModel model);

        /// <summary>
        /// Скачать превью информационного файла
        /// </summary>
        /// <param name="Id">Id записи</param>
        Task<FormFile> DownloadInformationFileThumbnailAsync(Guid Id);

        #endregion

        #region FTP

        /// <summary>
        /// Получить список "FTP"
        /// </summary>
        (List<FTPDto>, int, int) GetFTP(IDataTablesRequest request);

        /// <summary>
        /// Получить  FTP
        /// </summary>
        Task<FtpModelDto> GetFTPDtoAsync(Guid? Id);

        /// <summary>
        /// Получить список всех  FTP
        /// </summary>
        Task<List<FtpModelDto>> GetFTPAllAsync();

        /// <summary>
        /// Добавить FTP
        /// </summary>
        Task AddFTPAsync(FtpModelDto model);

        /// <summary>
        /// Обновить FTP
        /// </summary>
        Task UpdateFTPAsync(FtpModelDto model);

        /// <summary>
        /// Пометить на удаление FTP
        /// </summary>
        /// <param name="Id">Id записи</param>
        /// <param name="comment">Причина удаления</param>
        Task RemoveFTPAsync(Guid Id, string comment);

        #endregion

        #region LivingSituation

        /// <summary>
        /// Получить список ""жизненныt ситуации"
        /// </summary>
        (List<LivingSituationsDto>, int, int) GetLivingSituation(IDataTablesRequest request);

        /// <summary>
        /// Получить  "жизненныt ситуации"
        /// </summary>
        Task<LivingSituationModelDto> GetLivingSituationDtoAsync(Guid? Id);

        /// <summary>
        /// Добавить "жизненныt ситуации"
        /// </summary>
        Task AddLivingSituationAsync(LivingSituationModelDto model);

        /// <summary>
        /// Обновить "жизненныt ситуации"
        /// </summary>
        Task UpdateLivingSituationAsync(LivingSituationModelDto model);

        /// <summary>
        /// Пометить на удаление "жизненныt ситуации"
        /// </summary>
        /// <param name="Id">Id записи</param>
        /// <param name="comment">Причина удаления</param>
        Task RemoveLivingSituationAsync(Guid Id, string comment);

        #endregion

        #region Гос задние

        /// <summary>
        /// Получить список гос задание
        /// </summary>
        Task<(List<StateTasksDto>, int, int)> GetStateTask(IDataTablesRequest request);

        /// <summary>
        /// Получить гос задание
        /// </summary>
        Task<StateTaskDto> GetStateTaskDtoAsync(Guid? Id);

        /// <summary>
        /// Добавить гос задание
        /// </summary>
        Task AddStateTaskAsync(StateTaskDto model, string employeeFioAdd);

        /// <summary>
        /// Обновить гос задание
        /// </summary>
        Task UpdateStateTaskAsync(StateTaskDto model);

        /// <summary>
        /// Пометить на удаление гос задание
        /// </summary>
        /// <param name="Id">Id записи</param>
        /// <param name="comment">Причина удаления</param>
        Task RemoveStateTaskAsync(Guid Id, string comment);

        #endregion

        #region Системные

        Task<byte[]?> SystemFiles(int type);
        Task<List<SelectListDto<string>>> GetTerminalsByOffice(Guid? officeId);
        Task<List<SelectListDto<Guid>>> GetRecipientPaymentByProviders(Guid providerId, Guid? officeId);

        //Task<List<DataBaseDictonaryDto>> DataBaseDictonary(DataBaseDictonaryType type);


        #endregion
    }
}