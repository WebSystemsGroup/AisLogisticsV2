using AisLogistics.App.Extensions;
using AisLogistics.App.Models;
using AisLogistics.App.Models.DTO.Cases;
using AisLogistics.App.Models.DTO.Identity.Applicant;
using AisLogistics.App.Models.DTO.Services;
using AisLogistics.App.Models.Enums;
using AisLogistics.App.Service.MDM;
using AisLogistics.App.Settings;
using AisLogistics.App.Utils;
using AisLogistics.App.ViewModels.Cases;
using AisLogistics.DataLayer.Common.Dto.Case;
using AisLogistics.DataLayer.Concrete;
using AisLogistics.DataLayer.Entities.Models;
using AisLogistics.DataLayer.Utils;
using AutoMapper;
using Clave.Expressionify;
using DataTables.AspNet.Core;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using FastReport.Export.OoXML;
using FastReport.Export.Pdf;
using FastReport.Utils;
using FastReport.Web;
using FluentFTP;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using NinjaNye.SearchExtensions;
using NuGet.Packaging;
using System.Text.RegularExpressions;
using Z.EntityFramework.Plus;
using Text = DocumentFormat.OpenXml.Wordprocessing.Text;

namespace AisLogistics.App.Service
{
    public class CaseService : ICaseService
    {
        private readonly IFtpService _ftpService;
        private readonly IMdmService _mdmService;
        private readonly IMapper _mapper;
        private readonly AisLogisticsContext _context;
        private readonly EmployeeManager _employeeManager;


        public CaseService(AisLogisticsContext context, EmployeeManager employeeManager, IFtpService ftpService, IMdmService mdmService, IMapper mapper)
        {
            _ftpService = ftpService;
            _context = context;
            _employeeManager = employeeManager;
            _mdmService = mdmService;
            _mapper = mapper;
        }

        private async Task<string> GenerateCaseId(string officeMnemo)
        {
            try
            {
                var newNumber = await _context.GenerateCaseNumber.FromSqlRaw("SELECT NEXTVAL('core.cases_number_id_seq')").SingleAsync();
                return $"{officeMnemo}{newNumber.Number:D8}";
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Список обращений
        /// </summary>
        /// <param name="employeeId">id сотрудника</param>
        /// <returns></returns>
        public (List<CasesDto>, int, int) GetCases(IDataTablesRequest request, Guid? employeeId, Guid? office)
        {
            try
            {
                var date = DateTime.Today;

                DateTime? dateAdd = null;
                var dateAddColumn = request?.Columns?.Where(w => w.Name == "dateAdd").Select(s => s.Search.Value).FirstOrDefault();
                if (dateAddColumn != null) dateAdd = DateTime.Parse(dateAddColumn);

                string? caseId = null;
                var caseIdColumn = request?.Columns?.Where(w => w.Name == "caseId").Select(s => s.Search.Value).FirstOrDefault();
                if (caseIdColumn != null) caseId = caseIdColumn;

                string? customer = null;
                var customerColumn = request?.Columns?.Where(w => w.Name == "customer").Select(s => s.Search.Value).FirstOrDefault();
                if (customerColumn != null) customer = customerColumn;

                Guid? servicesId = null;
                var servicesIdColumn = request?.Columns?.Where(w => w.Name == "servicesId").Select(s => s.Search.Value).FirstOrDefault();
                if (servicesIdColumn != null) servicesId = new Guid(servicesIdColumn);

                Guid? officesId = null;
                var officesIdColumn = request?.Columns?.Where(w => w.Name == "officesId").Select(s => s.Search.Value).FirstOrDefault();
                if (officesIdColumn != null) officesId = new Guid(officesIdColumn);

                string? currentEmployee = null;
                var currentEmployeeColumn = request?.Columns?.Where(w => w.Name == "currentEmployee").Select(s => s.Search.Value).FirstOrDefault();
                if (currentEmployeeColumn != null) currentEmployee = currentEmployeeColumn.ToLower();

                int? statusId = null;
                var statusIdColumn = request?.Columns?.Where(w => w.Name == "statusId").Select(s => s.Search.Value).FirstOrDefault();
                if (statusIdColumn != null) statusId = int.Parse(statusIdColumn);

                int? stageId = null;
                var stageIdColumn = request?.Columns?.Where(w => w.Name == "stageId").Select(s => s.Search.Value).FirstOrDefault();
                if (stageIdColumn != null) stageId = int.Parse(stageIdColumn);

                int? filter = null;
                var filterIdColumn = request?.Columns?.Where(w => w.Name == "filterSelect").Select(s => s.Search.Value).FirstOrDefault();
                filter = filterIdColumn != null ? int.Parse(filterIdColumn) : 0;

                var MfcIdColumn = request?.Columns?.Where(w => w.Name == "officeFilterSelect").Select(s => s.Search.Value).FirstOrDefault();
                if (MfcIdColumn != null) office = new Guid(MfcIdColumn);

                var cases = _context.DServicesCustomers
                        .AsNoTracking()
                        .AsSplitQuery()
                        .Expressionify()
                        .Where(w => w.DCases.DServicesRoutesStages.Any(a => (!employeeId.HasValue || a.SEmployeesId == employeeId) &&
                                        (!office.HasValue || (office.Value == Guid.Empty || a.SOfficesId == office)) && (!stageId.HasValue || a.SRoutesStageId == stageId) && (currentEmployee == null || EF.Functions.Like(a.SEmployees.EmployeeName.ToLower(), $"%{currentEmployee}%")) && !a.DateFact.HasValue) &&
                                       (caseId == null || EF.Functions.Like(w.DCasesId, $"%{caseId}%")) && (w.DCases.DServicesCustomersLegals.Any() || w.CustomerType == (int)CustomerType.Applicant) &&
                                       //(!dateAdd.HasValue || w.DCases.DateAdd.Date == dateAdd) &&
                                       w.DCases.DServices.Any(a => (!officesId.HasValue || a.SOfficesIdProvider == officesId) && (!servicesId.HasValue || a.SServicesId == servicesId) &&
                                                                (!statusId.HasValue || a.SServicesStatusId == statusId) && (!dateAdd.HasValue || w.DateAdd.Date == dateAdd))
                        );


                int casesCount = cases.Count();

                var filteredData = String.IsNullOrWhiteSpace(customer)
                     ? cases
                     : cases.Search(s => s.SecondName.ToLower(),
                            s => s.FirstName.ToLower(),
                            s => s.LastName.ToLower(),
                            s => s.CustomerAddress.ToLower(),
                            s => s.CustomerPhone1.ToLower(),
                            s => s.CustomerPhone2.ToLower()
                        ).ContainingAll(customer.ToLower().Split(""));

                var orderFilterData = filter switch
                {
                    0 => filteredData.OrderByDescending(o => o.DCases.DateAdd),
                    1 => filteredData.OrderBy(o => o.DCases.DateAdd),
                    2 => filteredData.OrderByDescending(o => o.DCases.DateAdd),
                    _ => filteredData
                };

                var data = orderFilterData.Select(s => new CasesDto
                {
                    casesMainDto = new CasesMainDto
                    {
                        CaseId = s.DCasesId,
                        DateAdd = s.DCases.DateAdd.ToShortDateString(),
                        Applicant = new ApplicantDto(s.Fio(), s.CustomerAddress, s.CustomerPhone1),
                    },
                    Service = s.DCases.DServices.Select(ss => new CaseServiceDto
                    {
                        Id = ss.Id,
                        Name = ss.SServices.ServiceName,
                        Office = ss.SServices.SOffice.OfficeNameSmall,
                        EmployeeAdd = new EmployeeDto(ss.SEmployeesIdAddNavigation.Id,
                                 NameSplitter.GetInitials(ss.SEmployeesIdAddNavigation.EmployeeName),
                                 ss.SEmployeesJobPositionIdAddNavigation.JobPositionName),
                        Status = new StatusDto(ss.SServicesStatusId, ss.SServicesStatus.StatusName),
                        SerivesStage = ss.DServicesRoutesStages.OrderByDescending(o => o.DateAdd).Select(sss =>
                                        new SerivesStageDto
                                        {
                                            EmployeeCurrent = new EmployeeDto(sss.SEmployeesId, NameSplitter.GetInitials(sss.SEmployees.EmployeeName), sss.SEmployeesJobPosition.JobPositionName),
                                            Stage = new StageDto(sss.SRoutesStageId, sss.SRoutesStage.StageName),
                                            Office = sss.SEmployees.SEmployeesOfficesJoins.Where(w => w.DateStart <= date && (w.DateStop == null || w.DateStop >= date) && !w.IsRemove).Select(s => s.SOffices.OfficeName).First(),
                                            Days = sss.DateReg.HasValue ? sss.DateReg.Value.Subtract(date).Days : null
                                        }).First(),
                    }).FirstOrDefault()
                }).AsParallel();


                int casesFilteredCount = filteredData.Count();

                var dataPage = filter switch
                {
                    3 => data.OrderBy(o => o.Service.Days).Skip(request.Start).Take(request.Length).ToList(),
                    4 => data.OrderByDescending(o => o.Service.Days).Skip(request.Start).Take(request.Length).ToList(),
                    _ => data.Skip(request.Start).Take(request.Length).ToList()
                };

                return (dataPage, casesCount, casesFilteredCount);
            }
            catch
            {
                return (new List<CasesDto>(), 0, 0);
            }
        }

        /// <summary>
        /// Список обращений
        /// </summary>
        /// <param name="employeeId">id сотрудника</param>
        /// <returns></returns>
        public async Task<(List<CasesDto>, int, int)> GetCasesV3(IDataTablesRequest request, SearchCasesRequestData additionalRequest)
        {
            try
            {
                var date = DateTime.Today;

                //DateTime? dateAdd = null;
                //var dateAddColumn = request?.Columns?.Where(w => w.Name == "dateAdd").Select(s => s.Search.Value).FirstOrDefault();
                //if (dateAddColumn != null) dateAdd = DateTime.Parse(dateAddColumn);

                //string? caseId = null;
                //var caseIdColumn = request?.Columns?.Where(w => w.Name == "caseId").Select(s => s.Search.Value).FirstOrDefault();
                //if (caseIdColumn != null) caseId = caseIdColumn.ToLower();

                //string? customer = null;
                //var customerColumn = request?.Columns?.Where(w => w.Name == "customer").Select(s => s.Search.Value).FirstOrDefault();
                //if (customerColumn != null) customer = customerColumn;

                //Guid? servicesId = null;
                //var servicesIdColumn = request?.Columns?.Where(w => w.Name == "servicesId").Select(s => s.Search.Value).FirstOrDefault();
                //if (servicesIdColumn != null) servicesId = new Guid(servicesIdColumn);

                //Guid? officesId = null;
                //var officesIdColumn = request?.Columns?.Where(w => w.Name == "officesId").Select(s => s.Search.Value).FirstOrDefault();
                //if (officesIdColumn != null) officesId = new Guid(officesIdColumn);

                //string? currentEmployee = null;
                //var currentEmployeeColumn = request?.Columns?.Where(w => w.Name == "currentEmployee").Select(s => s.Search.Value).FirstOrDefault();
                //if (currentEmployeeColumn != null) currentEmployee = currentEmployeeColumn.ToLower();

                //int? statusId = null;
                //var statusIdColumn = request?.Columns?.Where(w => w.Name == "statusId").Select(s => s.Search.Value).FirstOrDefault();
                //if (statusIdColumn != null) statusId = int.Parse(statusIdColumn);

                //int? stageId = null;
                //var stageIdColumn = request?.Columns?.Where(w => w.Name == "stageId").Select(s => s.Search.Value).FirstOrDefault();
                //if (stageIdColumn != null) stageId = int.Parse(stageIdColumn);

                //int? filter = null;
                //var filterIdColumn = request?.Columns?.Where(w => w.Name == "filterSelect").Select(s => s.Search.Value).FirstOrDefault();
                //filter = filterIdColumn != null ? int.Parse(filterIdColumn) : 0;

                //var MfcIdColumn = request?.Columns?.Where(w => w.Name == "officeFilterSelect").Select(s => s.Search.Value).FirstOrDefault();
                //if (MfcIdColumn != null) office = new Guid(MfcIdColumn);

                if (additionalRequest.PeriodId.HasValue)
                {
                    switch (additionalRequest.PeriodId)
                    {
                        case 1:
                            additionalRequest.DateStart = DateTime.Now;
                            additionalRequest.DateStop = DateTime.Now;
                            break;
                        case 2:
                            additionalRequest.DateStart = DateTime.Now.AddDays(-1);
                            additionalRequest.DateStop = DateTime.Now;
                            break;
                        case 3:
                            additionalRequest.DateStart = DateTime.Now.AddMonths(-1);
                            additionalRequest.DateStop = DateTime.Now;
                            break;
                        default:
                            break;
                    }
                }

                var cases = _context.DServices
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Expressionify()

                    .Where(w => (!additionalRequest.EmployeeId.HasValue || w.SEmployeesIdCurrent == additionalRequest.EmployeeId) &&
                               (!additionalRequest.MfcId.HasValue || (additionalRequest.MfcId.Value == Guid.Empty || w.SOfficesIdCurrent == additionalRequest.MfcId)) &&
                               (!additionalRequest.Stages.Any() || additionalRequest.Stages.Contains((int)w.SRoutesStageIdCurrent)) &&
                               (string.IsNullOrEmpty(additionalRequest.EmployeeCurrent) || EF.Functions.Like(w.SEmployeesIdCurrentNavigation.EmployeeName.ToLower(), $"%{additionalRequest.EmployeeCurrent}%")) &&
                               (string.IsNullOrEmpty(additionalRequest.EmployeeAdd) || EF.Functions.Like(w.SEmployeesIdAddNavigation.EmployeeName.ToLower(), $"%{additionalRequest.EmployeeAdd}%")) &&
                               (!additionalRequest.OfficeId.Any() || additionalRequest.OfficeId.Contains(w.SOfficesIdProvider)) &&
                               (!additionalRequest.ServiceId.Any() || additionalRequest.ServiceId.Contains(w.SServicesId)) &&
                               (!additionalRequest.Status.Any() || additionalRequest.Status.Contains(w.SServicesStatusId)) &&
                               (!additionalRequest.DateStart.HasValue || w.DateAdd >= additionalRequest.DateStart.Value) &&
                               (!additionalRequest.DateStop.HasValue || w.DateAdd <= additionalRequest.DateStop.Value)
                    );

                int casesCount = cases.Count();

                var filteredData = string.IsNullOrEmpty(additionalRequest.Query)
                     ? cases
                     : cases.Search(
                            s => s.DCasesId.ToLower(),
                            s => s.ExtraInfo.ToLower(),
                            s => s.CustomerName.ToLower(),
                            s => s.CustomerAddress.ToLower(),
                            s => s.CustomerPhone1.ToLower(),
                            s => s.CustomerPhone2.ToLower()
                        ).ContainingAll(additionalRequest.Query.ToLower().Split(""));

                //var orderFilterData = filter switch
                //{
                //    0 => filteredData.OrderByDescending(o => o.DateAdd),
                //    1 => filteredData.OrderBy(o => o.DateAdd),
                //    2 => filteredData.OrderByDescending(o => o.DateAdd),
                //    _ => filteredData
                //};

                var dataPage = await filteredData.OrderByDescending(o => o.DateAdd).Select(s => new CasesDto
                {
                    casesMainDto = new CasesMainDto
                    {
                        CaseId = s.DCasesId,
                        DateAdd = s.DateAdd.ToShortDateString(),
                        Applicant = new ApplicantDto(s.CustomerName, s.CustomerAddress, s.CustomerPhone1),
                        NumberPKPVD = string.IsNullOrEmpty(s.ExtraInfo) ? null : (string?)JObject.Parse(s.ExtraInfo).SelectToken("NumberPKPVD"),
                        SearchString = s.SearchString
                    },
                    Service = new CaseServiceDto
                    {
                        Id = s.Id,
                        Name = s.SServices.ServiceName,
                        Office = s.SServices.SOffice.OfficeNameSmall,
                        EmployeeAdd = new EmployeeDto(s.SEmployeesIdAddNavigation.Id,
                                    NameSplitter.GetInitials(s.SEmployeesIdAddNavigation.EmployeeName),
                                    s.SEmployeesJobPositionIdAddNavigation.JobPositionName),
                        Status = new StatusDto(s.SServicesStatusId, s.SServicesStatus.StatusName),
                        SerivesStage = new SerivesStageDto
                        {
                            EmployeeCurrent = new EmployeeDto(s.SEmployeesIdCurrent, NameSplitter.GetInitials(s.SEmployeesIdCurrentNavigation.EmployeeName), s.SEmployeesJobPositionIdCurrentNavigation.JobPositionName),
                            Stage = new StageDto((int)s.SRoutesStageIdCurrent, s.SRoutesStage.StageName),
                            Office = s.SOfficesIdCurrentNavigation.OfficeName,
                            //Office = sss.SEmployees.SEmployeesOfficesJoins.Where(w => w.DateStart <= date && (w.DateStop == null || w.DateStop >= date) && !w.IsRemove).Select(s => s.SOffices.OfficeName).First(),
                            Days = s.RoutesStageDateRegCurrent.HasValue ? s.RoutesStageDateRegCurrent.Value.Subtract(date).Days : null
                        }
                    }
                }).Skip(request.Start).Take(request.Length).ToListAsync();

                int casesFilteredCount = filteredData.Count();

                //var dataPage = filter switch
                //{
                //    3 =>await data.OrderBy(o => o.Service.Days).Skip(request.Start).Take(request.Length).ToListAsync(),
                //    4 => await data.OrderByDescending(o => o.Service.Days).Skip(request.Start).Take(request.Length).ToListAsync(),
                //    _ =>await data.Skip(request.Start).Take(request.Length).ToListAsync()
                //};

                return (dataPage, casesCount, casesFilteredCount);
            }
            catch (Exception ex)
            {
                return (new List<CasesDto>(), 0, 0);
            }
        }

        /// <summary>
        /// Список обращений
        /// </summary>
        /// <param name="employeeId">id сотрудника</param>
        /// <returns></returns>
        public async Task<(List<CasesDto>, int, int)> GetCasesV2(IDataTablesRequest request, Guid? employeeId, Guid? office)
        {
            try
            {
                var date = DateTime.Today;

                DateTime? dateAdd = null;
                var dateAddColumn = request?.Columns?.Where(w => w.Name == "dateAdd").Select(s => s.Search.Value).FirstOrDefault();
                if (dateAddColumn != null) dateAdd = DateTime.Parse(dateAddColumn);

                string? caseId = null;
                var caseIdColumn = request?.Columns?.Where(w => w.Name == "caseId").Select(s => s.Search.Value).FirstOrDefault();
                if (caseIdColumn != null) caseId = caseIdColumn.ToLower();

                string? customer = null;
                var customerColumn = request?.Columns?.Where(w => w.Name == "customer").Select(s => s.Search.Value).FirstOrDefault();
                if (customerColumn != null) customer = customerColumn;

                Guid? servicesId = null;
                var servicesIdColumn = request?.Columns?.Where(w => w.Name == "servicesId").Select(s => s.Search.Value).FirstOrDefault();
                if (servicesIdColumn != null) servicesId = new Guid(servicesIdColumn);

                Guid? officesId = null;
                var officesIdColumn = request?.Columns?.Where(w => w.Name == "officesId").Select(s => s.Search.Value).FirstOrDefault();
                if (officesIdColumn != null) officesId = new Guid(officesIdColumn);

                string? currentEmployee = null;
                var currentEmployeeColumn = request?.Columns?.Where(w => w.Name == "currentEmployee").Select(s => s.Search.Value).FirstOrDefault();
                if (currentEmployeeColumn != null) currentEmployee = currentEmployeeColumn.ToLower();

                int? statusId = null;
                var statusIdColumn = request?.Columns?.Where(w => w.Name == "statusId").Select(s => s.Search.Value).FirstOrDefault();
                if (statusIdColumn != null) statusId = int.Parse(statusIdColumn);

                int? stageId = null;
                var stageIdColumn = request?.Columns?.Where(w => w.Name == "stageId").Select(s => s.Search.Value).FirstOrDefault();
                if (stageIdColumn != null) stageId = int.Parse(stageIdColumn);

                int? filter = null;
                var filterIdColumn = request?.Columns?.Where(w => w.Name == "filterSelect").Select(s => s.Search.Value).FirstOrDefault();
                filter = filterIdColumn != null ? int.Parse(filterIdColumn) : 0;

                var MfcIdColumn = request?.Columns?.Where(w => w.Name == "officeFilterSelect").Select(s => s.Search.Value).FirstOrDefault();
                if (MfcIdColumn != null) office = new Guid(MfcIdColumn);

                var cases = _context.DServices
                        .AsNoTracking()
                        .AsSplitQuery()
                        .Expressionify()
                        .Where(w => (!employeeId.HasValue || w.SEmployeesIdCurrent == employeeId) &&
                                   (!office.HasValue || (office.Value == Guid.Empty || w.SOfficesIdCurrent == office)) &&
                                   (!stageId.HasValue || w.SRoutesStageIdCurrent == stageId) &&
                                   (currentEmployee == null || EF.Functions.Like(w.SEmployeesIdCurrentNavigation.EmployeeName.ToLower(), $"%{currentEmployee}%")) &&
                                   (caseId == null || EF.Functions.Like(w.DCasesId.ToLower(), $"%{caseId}%") || EF.Functions.Like(w.ExtraInfo.ToLower(), $"%{caseId}%")) &&
                                   (!officesId.HasValue || w.SOfficesIdProvider == officesId) &&
                                   (!servicesId.HasValue || w.SServicesId == servicesId) &&
                                   (!statusId.HasValue || w.SServicesStatusId == statusId) &&
                                   (!dateAdd.HasValue || w.DateAdd.Date == dateAdd)
                        );

                int casesCount = cases.Count();

                var filteredData = String.IsNullOrWhiteSpace(customer)
                     ? cases
                     : cases.Search(s => s.CustomerName.ToLower(),
                            s => s.CustomerAddress.ToLower(),
                            s => s.CustomerPhone1.ToLower(),
                            s => s.CustomerPhone2.ToLower()
                        ).ContainingAll(customer.ToLower().Split(""));

                var orderFilterData = filter switch
                {
                    0 => filteredData.OrderByDescending(o => o.DateAdd),
                    1 => filteredData.OrderBy(o => o.DateAdd),
                    2 => filteredData.OrderByDescending(o => o.DateAdd),
                    _ => filteredData
                };

                var data = orderFilterData.Select(s => new CasesDto
                {
                    casesMainDto = new CasesMainDto
                    {
                        CaseId = s.DCasesId,
                        DateAdd = s.DateAdd.ToShortDateString(),
                        Applicant = new ApplicantDto(s.CustomerName, s.CustomerAddress, s.CustomerPhone1),
                        NumberPKPVD = (string?)JObject.Parse(s.ExtraInfo).SelectToken("NumberPKPVD"),
                        SearchString = s.SearchString
                    },
                    Service = new CaseServiceDto
                    {
                        Id = s.Id,
                        Name = s.SServices.ServiceName,
                        Office = s.SServices.SOffice.OfficeNameSmall,
                        EmployeeAdd = new EmployeeDto(s.SEmployeesIdAddNavigation.Id,
                                 NameSplitter.GetInitials(s.SEmployeesIdAddNavigation.EmployeeName),
                                 s.SEmployeesJobPositionIdAddNavigation.JobPositionName),
                        Status = new StatusDto(s.SServicesStatusId, s.SServicesStatus.StatusName),
                        SerivesStage = new SerivesStageDto
                        {
                            EmployeeCurrent = new EmployeeDto(s.SEmployeesIdCurrent, NameSplitter.GetInitials(s.SEmployeesIdCurrentNavigation.EmployeeName), s.SEmployeesJobPositionIdCurrentNavigation.JobPositionName),
                            Stage = new StageDto((int)s.SRoutesStageIdCurrent, s.SRoutesStage.StageName),
                            Office = s.SOfficesIdCurrentNavigation.OfficeName,
                            //Office = sss.SEmployees.SEmployeesOfficesJoins.Where(w => w.DateStart <= date && (w.DateStop == null || w.DateStop >= date) && !w.IsRemove).Select(s => s.SOffices.OfficeName).First(),
                            Days = s.RoutesStageDateRegCurrent.HasValue ? s.RoutesStageDateRegCurrent.Value.Subtract(date).Days : null
                        }
                    }
                });


                int casesFilteredCount = filteredData.Count();

                var dataPage = filter switch
                {
                    3 => await data.OrderBy(o => o.Service.Days).Skip(request.Start).Take(request.Length).ToListAsync(),
                    4 => await data.OrderByDescending(o => o.Service.Days).Skip(request.Start).Take(request.Length).ToListAsync(),
                    _ => await data.Skip(request.Start).Take(request.Length).ToListAsync()
                };

                //foreach(var d in dataPage)
                //{
                //    if(!string.IsNullOrEmpty(d.casesMainDto.SearchString))
                //    {
                //        var dict = d.casesMainDto.SearchString.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries)
                //       .Select(part => part.Split('='))
                //       .ToDictionary(split => split[0], split => split[1]);

                //        d.casesMainDto.NumberPKPVD = dict.Where(w => w.Key == "NumberPKPVD").Select(s => s.Value).FirstOrDefault();
                //    }

                //}


                return (dataPage, casesCount, casesFilteredCount);
            }
            catch (Exception ex)
            {
                return (new List<CasesDto>(), 0, 0);
            }
        }


        /// <summary>
        /// Список обращений
        /// </summary>
        /// <param name="employeeId">id сотрудника</param>
        /// <returns></returns>
        public (List<CasesReestrSmevDto>, int, int) GetCasesReestrSmev(IDataTablesRequest request, Guid? employeeId, Guid? office)
        {
            try
            {
                var date = DateTime.Today;

                DateTime? dateAdd = null;
                var dateAddColumn = request?.Columns?.Where(w => w.Name == "dateAdd").Select(s => s.Search.Value).FirstOrDefault();
                if (dateAddColumn != null) dateAdd = DateTime.Parse(dateAddColumn);

                DateTime? dateResponce = null;
                var dateResponceColumn = request?.Columns?.Where(w => w.Name == "dateResponce").Select(s => s.Search.Value).FirstOrDefault();
                if (dateResponceColumn != null) dateResponce = DateTime.Parse(dateResponceColumn);

                string? caseId = null;
                var caseIdColumn = request?.Columns?.Where(w => w.Name == "caseId").Select(s => s.Search.Value).FirstOrDefault();
                if (caseIdColumn != null) caseId = caseIdColumn;

                string? customer = null;
                var customerColumn = request?.Columns?.Where(w => w.Name == "customer").Select(s => s.Search.Value).FirstOrDefault();
                if (customerColumn != null) customer = customerColumn;

                Guid? servicesId = null;
                var servicesIdColumn = request?.Columns?.Where(w => w.Name == "servicesId").Select(s => s.Search.Value).FirstOrDefault();
                if (servicesIdColumn != null) servicesId = new Guid(servicesIdColumn);

                Guid? officesId = null;
                var officesIdColumn = request?.Columns?.Where(w => w.Name == "officesId").Select(s => s.Search.Value).FirstOrDefault();
                if (officesIdColumn != null) officesId = new Guid(officesIdColumn);

                string? currentEmployee = null;
                var currentEmployeeColumn = request?.Columns?.Where(w => w.Name == "currentEmployee").Select(s => s.Search.Value).FirstOrDefault();
                if (currentEmployeeColumn != null) currentEmployee = currentEmployeeColumn.ToLower();

                int? statusId = null;
                var statusIdColumn = request?.Columns?.Where(w => w.Name == "statusId").Select(s => s.Search.Value).FirstOrDefault();
                if (statusIdColumn != null) statusId = int.Parse(statusIdColumn);

                int? filter = null;
                var filterIdColumn = request?.Columns?.Where(w => w.Name == "filterSelect").Select(s => s.Search.Value).FirstOrDefault();
                filter = filterIdColumn != null ? int.Parse(filterIdColumn) : 0;

                var MfcIdColumn = request?.Columns?.Where(w => w.Name == "officeFilterSelect").Select(s => s.Search.Value).FirstOrDefault();
                if (MfcIdColumn != null) office = new Guid(MfcIdColumn);

                var cases = _context.DServicesSmevRequests
                        .AsNoTracking()
                        .AsSplitQuery()
                        .Expressionify()
                        .Where(w => (!employeeId.HasValue || w.SEmployeesId == employeeId) &&
                                (!office.HasValue || (office.Value == Guid.Empty || w.SOfficesId == office)) &&
                                (caseId == null || EF.Functions.Like(w.DCasesId, $"%{caseId}%")) &&
                                (currentEmployee == null || EF.Functions.Like(w.EmployeeFioAdd, $"%{currentEmployee}%")) &&
                                (!dateAdd.HasValue || w.DateAdd >= dateAdd) &&
                                ((!dateResponce.HasValue) || (w.DateFact.HasValue && w.DateFact >= dateResponce)) &&
                                 (!officesId.HasValue || w.SSmevRequest.SSmevId == officesId) &&
                                w.DCases.DServicesCustomers.Any(a => a.CustomerType == (int)CustomerType.Applicant) &&
                                (!servicesId.HasValue || w.DServices.SServicesId == servicesId)
                        );

                int casesCount = cases.Count();

                var filteredData = String.IsNullOrWhiteSpace(customer)
                     ? cases
                     : cases.Search(s => s.DCases.DServicesCustomers.First(a => a.CustomerType == (int)CustomerType.Applicant).SecondName.ToLower(),
                            s => s.DCases.DServicesCustomers.First(a => a.CustomerType == (int)CustomerType.Applicant).FirstName.ToLower(),
                            s => s.DCases.DServicesCustomers.First(a => a.CustomerType == (int)CustomerType.Applicant).LastName.ToLower(),
                            s => s.DCases.DServicesCustomers.First(a => a.CustomerType == (int)CustomerType.Applicant).CustomerAddress.ToLower(),
                            s => s.DCases.DServicesCustomers.First(a => a.CustomerType == (int)CustomerType.Applicant).CustomerPhone1.ToLower(),
                            s => s.DCases.DServicesCustomers.First(a => a.CustomerType == (int)CustomerType.Applicant).CustomerPhone2.ToLower()
                        ).Containing(customer.ToLower().Split(""));

                var statusFilter = statusId switch
                {
                    (int)SmevStatusType.Expired => filteredData.Where(w => w.DateFact == null && w.DateReg < DateTime.Now && w.RequestHtml != null),
                    (int)SmevStatusType.Sent => filteredData.Where(w => w.DateFact == null && w.DateReg >= DateTime.Now && w.RequestHtml != null),
                    (int)SmevStatusType.Error => filteredData.Where(w => (w.DateFact != null || w.DateFact == null) && w.RequestHtml == null),
                    (int)SmevStatusType.ResponseSuccess => filteredData.Where(w => w.DateFact != null),
                    _ => filteredData
                };

                var orderFilterData = filter switch
                {
                    0 => statusFilter.OrderByDescending(o => o.DateRequest),
                    1 => statusFilter.OrderBy(o => o.DateRequest),
                    2 => statusFilter.OrderByDescending(o => o.DateRequest),
                    _ => statusFilter
                };

                var data = orderFilterData
                       .Skip(request.Start).Take(request.Length)
                       .Select(s => new CasesReestrSmevDto
                       {
                           Applicant = s.DCases.DServicesCustomers.Where(a => a.CustomerType == (int)CustomerType.Applicant).Select(ss => new ApplicantDto(ss.Fio(), ss.CustomerAddress, ss.CustomerPhone1)).FirstOrDefault(),
                           Id = s.Id,
                           Name = s.SSmevRequest.RequestName,
                           SmevService = s.SSmevRequest.SSmev.SmevName,
                           Provider = s.SSmevRequest.SSmev.SmevProvider,
                           DateCreate = s.DateRequest.Value.ToShortDateString() + " " + s.TimeRequest.Value.ToLongTimeString(),
                           DateResponseFact = s.DateFact.HasValue ? s.DateFact.Value.ToShortDateString() + " " + s.TimeFact.Value.ToLongTimeString() : "-",
                           DateResponseReg = s.DateReg.ToShortDateString(),
                           Status = s.SmevStatus(),
                           EmployeeAdd = new EmployeeDto(s.SEmployeesId, NameSplitter.GetInitials(s.EmployeeFioAdd),
                            s.SEmployeesJobPosition.JobPositionName),
                           Description = s.Commentt,
                           Service = new ServiceDto(s.DServicesId, s.DServices.SServices.ServiceNameSmall,
                           s.DServices.SServices.SOffice.OfficeNameSmall),
                           CaseId = s.DCasesId,
                           Comment = s.Commentt
                       })
                       .AsParallel()
                       .ToList();

                int casesFilteredCount = orderFilterData.Count();

                return (data, casesCount, casesFilteredCount);
            }
            catch (Exception e)
            {
                return (new List<CasesReestrSmevDto>(), 0, 0);
            }
        }

        /// <summary>
        /// Список обращений
        /// </summary>
        /// <param name="employeeId">id сотрудника</param>
        /// <returns></returns>
        public async Task<(List<CasesReestrSmevDto>, int, int)> GetCasesReestrSmevV2(IDataTablesRequest request, SearchCasesRequestData additionalRequest)
        {
            try
            {
                var date = DateTime.Today;

                //DateTime? dateAdd = null;
                //var dateAddColumn = request?.Columns?.Where(w => w.Name == "dateAdd").Select(s => s.Search.Value).FirstOrDefault();
                //if (dateAddColumn != null) dateAdd = DateTime.Parse(dateAddColumn);

                //DateTime? dateResponce = null;
                //var dateResponceColumn = request?.Columns?.Where(w => w.Name == "dateResponce").Select(s => s.Search.Value).FirstOrDefault();
                //if (dateResponceColumn != null) dateResponce = DateTime.Parse(dateResponceColumn);

                //string? caseId = null;
                //var caseIdColumn = request?.Columns?.Where(w => w.Name == "caseId").Select(s => s.Search.Value).FirstOrDefault();
                //if (caseIdColumn != null) caseId = caseIdColumn;

                //string? customer = null;
                //var customerColumn = request?.Columns?.Where(w => w.Name == "customer").Select(s => s.Search.Value).FirstOrDefault();
                //if (customerColumn != null) customer = customerColumn;

                //Guid? servicesId = null;
                //var servicesIdColumn = request?.Columns?.Where(w => w.Name == "servicesId").Select(s => s.Search.Value).FirstOrDefault();
                //if (servicesIdColumn != null) servicesId = new Guid(servicesIdColumn);

                //Guid? officesId = null;
                //var officesIdColumn = request?.Columns?.Where(w => w.Name == "officesId").Select(s => s.Search.Value).FirstOrDefault();
                //if (officesIdColumn != null) officesId = new Guid(officesIdColumn);

                //string? currentEmployee = null;
                //var currentEmployeeColumn = request?.Columns?.Where(w => w.Name == "currentEmployee").Select(s => s.Search.Value).FirstOrDefault();
                //if (currentEmployeeColumn != null) currentEmployee = currentEmployeeColumn.ToLower();

                //int? statusId = null;
                //var statusIdColumn = request?.Columns?.Where(w => w.Name == "statusId").Select(s => s.Search.Value).FirstOrDefault();
                //if (statusIdColumn != null) statusId = int.Parse(statusIdColumn);

                //int? filter = null;
                //var filterIdColumn = request?.Columns?.Where(w => w.Name == "filterSelect").Select(s => s.Search.Value).FirstOrDefault();
                //filter = filterIdColumn != null ? int.Parse(filterIdColumn) : 0;

                //var MfcIdColumn = request?.Columns?.Where(w => w.Name == "officeFilterSelect").Select(s => s.Search.Value).FirstOrDefault();
                //if (MfcIdColumn != null) office = new Guid(MfcIdColumn);


                if (additionalRequest.PeriodId.HasValue)
                {
                    switch (additionalRequest.PeriodId)
                    {
                        case 1:
                            additionalRequest.DateStart = DateTime.Now;
                            additionalRequest.DateStop = DateTime.Now;
                            break;
                        case 2:
                            additionalRequest.DateStart = DateTime.Now.AddDays(-1);
                            additionalRequest.DateStop = DateTime.Now;
                            break;
                        case 3:
                            additionalRequest.DateStart = DateTime.Now.AddMonths(-1);
                            additionalRequest.DateStop = DateTime.Now;
                            break;
                        default:
                            break;
                    }
                }

                var cases = _context.DServicesSmevRequests
                        .AsNoTracking()
                        .AsSplitQuery()
                        .Expressionify()
                        .Where(w => (!additionalRequest.EmployeeId.HasValue || w.SEmployeesId == additionalRequest.EmployeeId) &&
                                (!additionalRequest.MfcId.HasValue || (additionalRequest.MfcId.Value == Guid.Empty || w.SOfficesId == additionalRequest.MfcId)) &&
                                (string.IsNullOrEmpty(additionalRequest.EmployeeCurrent) || EF.Functions.Like(w.EmployeeFioAdd, $"%{additionalRequest.EmployeeCurrent}%")) &&
                                (!additionalRequest.SmevServices.Any() || additionalRequest.SmevServices.Contains(w.SSmevRequest.SSmevId)) &&
                                (!additionalRequest.SmevRequest.Any() || additionalRequest.SmevRequest.Contains(w.SSmevRequestId)) &&
                                (!additionalRequest.DateStart.HasValue || w.DateAdd >= additionalRequest.DateStart.Value) &&
                                (!additionalRequest.DateStop.HasValue || w.DateAdd <= additionalRequest.DateStop.Value)
                        //((!dateResponce.HasValue) || (w.DateFact.HasValue && w.DateFact >= dateResponce)) &&
                        //(caseId == null || EF.Functions.Like(w.DCasesId, $"%{caseId}%")) &&
                        //w.DCases.DServicesCustomers.Any(a => a.CustomerType == (int)CustomerType.Applicant) &&
                        //(!servicesId.HasValue || w.DServices.SServicesId == servicesId)
                        );

                int casesCount = cases.Count();

                var filteredData = string.IsNullOrWhiteSpace(additionalRequest.Query)
                     ? cases
                     : cases.Search(
                            s => s.DCasesId,
                            s => s.DServices.CustomerName.ToLower(),
                            s => s.DServices.CustomerAddress.ToLower(),
                            s => s.DServices.CustomerPhone1.ToLower(),
                            s => s.DServices.CustomerPhone2.ToLower(),
                            s => s.Commentt.ToLower()
                        ).ContainingAll(additionalRequest.Query.ToLower().Split(""));

                var statusFilter = additionalRequest.SmevStatusId switch
                {
                    (int)SmevStatusType.Expired => filteredData.Where(w => w.DateFact == null && w.DateReg < DateTime.Now && w.RequestHtml != null),
                    (int)SmevStatusType.Sent => filteredData.Where(w => w.DateFact == null && w.DateReg >= DateTime.Now && w.RequestHtml != null),
                    (int)SmevStatusType.Error => filteredData.Where(w => (w.DateFact != null || w.DateFact == null) && w.RequestHtml == null),
                    (int)SmevStatusType.ResponseSuccess => filteredData.Where(w => w.DateFact != null),
                    _ => filteredData
                };

                int casesFilteredCount = statusFilter.Count();

                //var orderFilterData = filter switch
                //{
                //    0 => statusFilter.OrderByDescending(o => o.DateRequest),
                //    1 => statusFilter.OrderBy(o => o.DateRequest),
                //    2 => statusFilter.OrderByDescending(o => o.DateRequest),
                //    _ => statusFilter
                //};

                var data = await statusFilter
                       .OrderByDescending(o => o.DateRequest)
                       .Skip(request.Start).Take(request.Length)
                       .Select(s => new CasesReestrSmevDto
                       {
                           Applicant = new ApplicantDto(s.DServices.CustomerName, s.DServices.CustomerAddress, s.DServices.CustomerPhone1),
                           Id = s.Id,
                           Name = s.SSmevRequest.RequestName,
                           SmevService = s.SSmevRequest.SSmev.SmevName,
                           Provider = s.SSmevRequest.SSmev.SmevProvider,
                           DateCreate = s.DateRequest.Value.ToShortDateString() + " " + s.TimeRequest.Value.ToLongTimeString(),
                           DateResponseFact = s.DateFact.HasValue ? s.DateFact.Value.ToShortDateString() + " " + s.TimeFact.Value.ToLongTimeString() : "-",
                           DateResponseReg = s.DateReg.ToShortDateString(),
                           Status = s.SmevStatus(),
                           EmployeeAdd = new EmployeeDto(s.SEmployeesId, NameSplitter.GetInitials(s.EmployeeFioAdd),
                            s.SEmployeesJobPosition.JobPositionName),
                           Description = s.Commentt,
                           Service = new ServiceDto(s.DServicesId, s.DServices.SServices.ServiceNameSmall,
                           s.DServices.SServices.SOffice.OfficeNameSmall),
                           CaseId = s.DCasesId,
                           Comment = s.Commentt
                       })
                       .ToListAsync();

                return (data, casesCount, casesFilteredCount);
            }
            catch (Exception e)
            {
                return (new List<CasesReestrSmevDto>(), 0, 0);
            }
        }

        /// <summary>
        /// Список обращений на выдачу
        /// </summary>
        /// <param name="employeeId">id сотрудника</param>
        /// <returns></returns>
        public (List<CasesDto>, int, int) GetIssueCases(IDataTablesRequest request, Guid? employeeId, Guid? office)
        {
            try
            {
                List<int> statusId = new() { 1, 5 };
                const int issueStage = 6;
                var date = DateTime.Today;

                var MfcIdColumn = request?.Columns?.Where(w => w.Name == "officeFilterSelect").Select(s => s.Search.Value).FirstOrDefault();
                if (MfcIdColumn != null) office = new Guid(MfcIdColumn);

                var cases = _context.DServicesCustomers
                       .AsNoTracking()
                       .AsSplitQuery()
                       .Expressionify()
                       .Where(w => w.DCases.DServicesRoutesStages.Any(a => a.DServicesIdParent == Guid.Empty &&
                                   (!employeeId.HasValue || a.SEmployeesId == employeeId) &&
                                    (!office.HasValue || (office.Value == Guid.Empty || a.SOfficesId == office)) &&
                                   a.SRoutesStageId == issueStage && !a.DateFact.HasValue) &&
                                   w.DCases.DServices.All(a => statusId.Contains(a.SServicesStatusId)));

                int casesCount = cases.Count();

                var filteredResult = string.IsNullOrEmpty(request.Search.Value)
                                       ? cases
                                       : cases.Search(s => s.DCasesId.ToLower())
                                       .Containing(request.Search.Value.ToLower().Split(""));

                int filteredCount = filteredResult.Count();

                var data = filteredResult
                       .Select(s => new CasesDto
                       {
                           casesMainDto = new CasesMainDto
                           {
                               CaseId = s.DCasesId,
                               DateAdd = s.DCases.DateAdd.ToShortDateString(),
                               Applicant = new ApplicantDto(s.Fio(), s.CustomerAddress, s.CustomerPhone1),
                           },
                           Service = s.DCases.DServices
                                .Select(ss => new CaseServiceDto
                                {
                                    Id = ss.Id,
                                    Name = ss.SServices.ServiceName,
                                    Office = ss.SServices.SOffice.OfficeNameSmall,
                                    EmployeeAdd = new EmployeeDto(ss.SEmployeesIdAddNavigation.Id,
                                            NameSplitter.GetInitials(ss.SEmployeesIdAddNavigation.EmployeeName),
                                            ss.SEmployeesJobPositionIdAddNavigation.JobPositionName),
                                    Status = new StatusDto(ss.SServicesStatusId, ss.SServicesStatus.StatusName),
                                    SerivesStage = ss.DServicesRoutesStages.OrderByDescending(o => o.DateAdd).Select(sss =>
                                                    new SerivesStageDto
                                                    {
                                                        EmployeeCurrent = new EmployeeDto(sss.SEmployeesId, NameSplitter.GetInitials(sss.SEmployees.EmployeeName), sss.SEmployeesJobPosition.JobPositionName),
                                                        Office = sss.SEmployees.SEmployeesOfficesJoins.Where(w => w.DateStart <= date && (w.DateStop == null || w.DateStop >= date) && !w.IsRemove).Select(s => s.SOffices.OfficeName).First(),
                                                        Stage = new StageDto(sss.SRoutesStageId, sss.SRoutesStage.StageName),
                                                        Days = sss.DateReg.HasValue ? sss.DateReg.Value.Subtract(date).Days : null
                                                    }).First(),
                                }).FirstOrDefault()
                       })
                       .AsParallel()
                       .ToList();

                return (data, casesCount, 0);
            }
            catch
            {
                return (new List<CasesDto>(), 0, 0);
            }
        }

        /// <summary>
        /// Список обращений на выдачу
        /// </summary>
        /// <param name="employeeId">id сотрудника</param>
        /// <returns></returns>
        public async Task<(List<CasesDto>, int, int)> GetIssueCasesV2(IDataTablesRequest request, Guid? employeeId, Guid? office)
        {
            try
            {
                List<int> statusId = new() { 1, 5 };
                const int issueStage = 6;
                var date = DateTime.Today;

                var MfcIdColumn = request?.Columns?.Where(w => w.Name == "officeFilterSelect").Select(s => s.Search.Value).FirstOrDefault();
                if (MfcIdColumn != null) office = new Guid(MfcIdColumn);

                var cases = _context.DServices
                       .AsNoTracking()
                       .AsSplitQuery()
                       .Expressionify()
                       .Where(w => (!employeeId.HasValue || w.SEmployeesIdCurrent == employeeId) &&
                                  (!office.HasValue || (office.Value == Guid.Empty || w.SOfficesIdCurrent == office)) &&
                                  w.SRoutesStageIdCurrent == issueStage &&
                                  statusId.Contains(w.SServicesStatusId)
                       );

                int casesCount = cases.Count();

                var filteredResult = string.IsNullOrEmpty(request.Search.Value)
                                       ? cases
                                       : cases.Search(s => s.DCasesId.ToLower())
                                       .Containing(request.Search.Value.ToLower().Split(""));

                int filteredCount = filteredResult.Count();

                var data = await filteredResult
                       .Select(s => new CasesDto
                       {
                           casesMainDto = new CasesMainDto
                           {
                               CaseId = s.DCasesId,
                               DateAdd = s.DCases.DateAdd.ToShortDateString(),
                               Applicant = new ApplicantDto(s.CustomerName, s.CustomerAddress, s.CustomerPhone1),
                           },
                           Service = new CaseServiceDto
                           {
                               Id = s.Id,
                               Name = s.SServices.ServiceName,
                               Office = s.SServices.SOffice.OfficeNameSmall,
                               EmployeeAdd = new EmployeeDto(s.SEmployeesIdAddNavigation.Id,
                                            NameSplitter.GetInitials(s.SEmployeesIdAddNavigation.EmployeeName),
                                            s.SEmployeesJobPositionIdAddNavigation.JobPositionName),
                               Status = new StatusDto(s.SServicesStatusId, s.SServicesStatus.StatusName),
                               SerivesStage = new SerivesStageDto
                               {
                                   EmployeeCurrent = new EmployeeDto(s.SEmployeesIdCurrent, NameSplitter.GetInitials(s.SEmployeesIdCurrentNavigation.EmployeeName), s.SEmployeesJobPositionIdCurrentNavigation.JobPositionName),
                                   //Office = sss.SEmployees.SEmployeesOfficesJoins.Where(w => w.DateStart <= date && (w.DateStop == null || w.DateStop >= date) && !w.IsRemove).Select(s => s.SOffices.OfficeName).First(),
                                   Office = s.SOfficesIdCurrentNavigation.OfficeName,
                                   Stage = new StageDto((int)s.SRoutesStageIdCurrent, s.SRoutesStage.StageName),
                                   Days = s.RoutesStageDateRegCurrent.HasValue ? s.RoutesStageDateRegCurrent.Value.Subtract(date).Days : null
                               }
                           }
                       })
                       .ToListAsync();

                return (data, casesCount, filteredCount);
            }
            catch
            {
                return (new List<CasesDto>(), 0, 0);
            }
        }

        /// <summary>
        /// Обращение по id /// здесь
        /// </summary>
        /// <param name="id">id услуги</param>
        /// <returns></returns>
        public async Task<CasesMainDto> GetCaseByIdAsync(string id, Guid employeeId)
        {
            try
            {
                var caseDto = await _context.DCases
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Expressionify()
                    .Select(s => new CasesMainDto
                    {
                        CaseId = s.Id,
                        DateAdd = s.DateAdd.ToShortDateString(),
                        Applicant = s.DServicesCustomersLegals.Count == 0 ? s.DServicesCustomers.Where(w => w.CustomerType == (int)CustomerType.Applicant).Select(ss => new ApplicantDto(ss.Id, ss.Fio(), ss.CustomerAddress, ss.CustomerPhone1)).First() :
                                                                               s.DServicesCustomersLegals.Select(ss => new ApplicantDto(ss.Id, ss.CustomerName, ss.CustomerAddress, ss.CustomerPhone1)).First(),
                        Recipient = s.DServicesCustomers.Where(w => w.CustomerType == (int)CustomerType.Representative)
                            .Select(ss => new ApplicantDto(ss.Id, ss.Fio(), ss.CustomerAddress, ss.CustomerPhone1)).FirstOrDefault(),
                        CommentsCount = s.DServicesCommentts.Count,
                        NotificationsCount = s.DServicesCustomersCalls.Count + s.DServicesCustomersMessages.Count,
                        AuditCounts = s.DCasesViews.Count
                    }).FirstAsync(f => f.CaseId == id);

                caseDto.Notes.AddRange(await _context.DNotes.Where(w => w.DCasesId == id && w.SEmployeesId == employeeId)
                    .Select(s =>
                    new NotesDto
                    {
                        Id = s.Id,
                        NotesText = s.NoteText,
                        DateReminder = s.DateReminder
                    }).ToListAsync());

                return caseDto;
            }
            catch (Exception e)
            {
                var a = e.Message;
                return new CasesMainDto();
            }
        }

        public ApplicantAdditionalDto GetApplicantByServiceId(Guid id)
        {
            return _context.DServices
                .Include(i => i.DCases).ThenInclude(i => i.DServicesCustomers.Where(w => w.CustomerType == (int)CustomerType.Applicant))
                .First(f => f.Id == id).DCases.DServicesCustomers
                .Select(s => new ApplicantAdditionalDto
                {
                    Id = s.Id,
                    Name = s.Fio(),
                    FirstName = s.FirstName,
                    LastName = s.LastName,
                    SecondName = s.SecondName,
                    Gender = s.CustomerGender,
                    BirthDate = s.DocumentBirthDate,
                    Address = s.CustomerAddress,
                    BirthPlace = s.DocumentBirthPlace,
                    Phone = s.CustomerPhone1,
                    Email = s.CustomerEmail,
                    Snils = s.CustomerSnils,
                    Inn = s.CustomerInn,
                    Type = (CustomerType)s.CustomerType,
                    DocumentTypeCode = s.SDocumentsIdentityId.ToString(),
                    DocumentSerial = s.DocumentSerial,
                    DocumentNumber = s.DocumentNumber,
                    DocumentIssueDate = s.DocumentIssueDate,
                    DocumentIssueBody = s.DocumentIssueBody,
                    DocumentCode = s.DocumentCode,
                    SubjectCustomerType = SubjectCustomerType.Physical,
                    AddressDetails = JsonConvert.DeserializeObject<AddressDetails>(s.CustomerAddressDetail ?? String.Empty)
                }).First();
        }

        public List<ApplicantAdditionalDto> GetApplicantsByServiceId(Guid id, bool includeLegal = false)
        {
            var applicants = _context.DServices
                .Include(i => i.DCases).ThenInclude(i => i.DServicesCustomers)
                .First(f => f.Id == id).DCases.DServicesCustomers
                .Select(s => new ApplicantAdditionalDto()
                {
                    Id = s.Id,
                    Name = s.Fio(),
                    FirstName = s.FirstName,
                    LastName = s.LastName,
                    SecondName = s.SecondName,
                    Gender = s.CustomerGender,
                    BirthDate = s.DocumentBirthDate,
                    Address = s.CustomerAddress,
                    BirthPlace = s.DocumentBirthPlace,
                    Phone = s.CustomerPhone1,
                    Email = s.CustomerEmail,
                    Snils = s.CustomerSnils,
                    Inn = s.CustomerInn,
                    Type = (CustomerType)s.CustomerType,
                    DocumentTypeCode = s.SDocumentsIdentityId.ToString(),
                    DocumentSerial = s.DocumentSerial,
                    DocumentNumber = s.DocumentNumber,
                    DocumentIssueDate = s.DocumentIssueDate,
                    DocumentIssueBody = s.DocumentIssueBody,
                    DocumentCode = s.DocumentCode,
                    SubjectCustomerType = SubjectCustomerType.Physical,
                    AddressDetails = JsonConvert.DeserializeObject<AddressDetails>(s.CustomerAddressDetail ?? String.Empty)
                }).ToList();

            if (includeLegal == false) return applicants;

            var legalList = _context.DServices
                .Include(i => i.DCases).ThenInclude(i => i.DServicesCustomersLegals)
                .First(f => f.Id == id).DCases.DServicesCustomersLegals
                .Select(s => new ApplicantAdditionalDto()
                {
                    Id = s.Id,
                    Name = s.CustomerName,
                    FirstName = s.HeadFirstName,
                    LastName = s.HeadLastName,
                    SecondName = s.HeadSecondName,
                    Address = s.CustomerAddress,
                    Phone = s.CustomerPhone1,
                    Email = s.CustomerEmail,
                    Inn = s.CustomerInn,
                    SubjectCustomerType = SubjectCustomerType.Legal
                }).ToList();

            applicants.AddRange(legalList);

            return applicants;
        }

        public async Task<List<CaseServiceStageDto>> GetStagesByServiceIdAsync(Guid id)
        {
            return await _context.DServicesRoutesStages
                .AsNoTracking()
                .Where(w => w.DServicesId == id)
                .Select(s => new CaseServiceStageDto
                {
                    Id = s.Id,
                    Name = s.SRoutesStage.StageName,
                    Days = s.DateReg.HasValue
                        ? (s.DateFact.HasValue
                            ? s.DateReg.Value.Subtract(s.DateFact.Value).Days
                            : s.DateReg.Value.Subtract(DateTime.Now).Days)
                        : null,
                    DateAdd = s.DateAdd,
                    EmployeeCurrent = new EmployeeDto(s.SEmployeesId, NameSplitter.GetInitials(s.SEmployees.EmployeeName),
                        s.SEmployeesJobPosition.JobPositionName),
                    EmployeeAdd = new EmployeeDto(s.SEmployeesIdAdd, NameSplitter.GetInitials(s.EmployeeFioAdd),
                        s.SEmployeesJobPositionIdAddNavigation.JobPositionName),
                    DateReg = s.DateReg,
                    DateFact = s.DateFact,
                    IsAutomaticallyTransferred = s.PassAutomatically
                })
                .OrderByDescending(o => o.DateAdd)
                .ToListAsync();
        }

        public async Task<List<StageDto>> GetStagesNextAllAsync()
        {
            return await _context.SRoutesStages
                .AsNoTracking()
                .OrderBy(o => o.StageName)
                .Select(s => new StageDto(s.Id, s.StageName, s.DayExcution, s.Commentt))
                .ToListAsync();
        }

        public async Task<List<StageDto>> GetStagesNextByServiceIdAsync(List<Guid> id)
        {
            try
            {
                var services = await _context.DServices.Where(w => id.Contains(w.Id))
                    .AsNoTracking()
                    .Expressionify()
                    .Select(s => new
                    {
                        CurrentstageId = s.DServicesRoutesStages.OrderByDescending(o => o.DateAdd).Select(s => s.SRoutesStageId).First(),
                        ServiceId = s.SServicesId,
                        Stages = s.SServices.SServicesRoutesStages.Where(w => !w.IsRemove).Select(ss => new { routeStages = ss, stage = ss.SRoutesStage }).ToList()
                    }).ToListAsync();

                if (services is null or { Count: 0 }) throw new Exception(ErrorDescription.ServiceNotFound);

                List<StageDto> stages = new();
                Guid stageParentId;

                foreach (var ser in services)
                {
                    stageParentId = ser.Stages.Where(w => w.routeStages.SRoutesStageId == ser.CurrentstageId && w.routeStages.ParentId == Guid.Empty).Select(s => s.routeStages.Id).First();
                    ser.Stages?.RemoveAll(a => a.routeStages.ParentId != stageParentId);
                    if (ser.Stages is not null) stages.AddRange(ser.Stages.Select(s => new StageDto(s.stage.Id, s.stage.StageName, s.stage.DayExcution, s.stage.Commentt)).ToList());
                }

                if (stages.Count == 0) return new List<StageDto>();

                return stages.GroupBy(gb => gb.Id, (_, g) => g.First()).ToList();
            }
            catch (Exception e)
            {
                var a = e.Message;
                return null;
            }
        }

        public async Task<List<EmployeesDtO>> GetStagesNextEmployessByServiceIdAsync(ServiceStageNextEmployessDto request)
        {
            try
            {
                const int statusActive = 0;
                const int statusSick = 4;

                var services = await _context.DServices.Where(w => request.serviceId.Contains(w.Id))
                    .Select(s => new
                    {
                        roles = s.SServices.SServicesRoutesStages.Where(w => w.SRoutesStageId == request.stageId && w.ParentId == Guid.Empty).Select(ss => ss.SServicesRoutesStageRoles.Select(s => s.SRolesExecutorId).ToList()).First()
                    }).ToListAsync();
                if (services is null or { Count: 0 }) throw new Exception(ErrorDescription.ServiceNotFound);

                List<Guid> roles = new();

                foreach (var ser in services)
                {
                    roles.AddRange(ser.roles);
                }

                roles = roles.Distinct().ToList();

                var employees = await _context.SEmployeesOfficesJoins.Where(w => w.SOfficesId == request.officeId && !w.IsRemove
                        && w.SEmployees.SEmployeesStatusJoins.Any(a => a.DateStart.CompareTo(DateTime.Today) <= 0 &&
                                                                       (a.DateStop == null || a.DateStop.Value.CompareTo(DateTime.Today) >= 0) && (a.SEmployeesStatusId == statusActive || a.SEmployeesStatusId == statusSick))
                        && w.SEmployees.SEmployeesExecutions.Any(a => !a.IsRemove && a.DateStart.CompareTo(DateTime.Today) <= 0 &&
                                                                      (a.DateStop == null || a.DateStop.Value.CompareTo(DateTime.Today) >= 0))
                        && w.DateStart.CompareTo(DateTime.Today) <= 0
                        && (w.DateStop == null || w.DateStop.Value.CompareTo(DateTime.Today) >= 0)
                        && (!roles.Any() || w.SEmployees.SEmployeesRolesExecutors.Any(a => roles.Contains(a.SRolesExecutorId)))
                    )
                    .Select(s => new EmployeesDtO { Id = s.SEmployeesId, Name = s.SEmployees.EmployeeName })
                    .ToListAsync();

                if (employees is null or { Count: 0 }) throw new Exception(ErrorDescription.EmployeeNotFound);

                //employees.Add(new EmployeesDtO { Id = Guid.Empty, Name = "Автоматически" });

                return employees.OrderBy(o => o.Id).ToList();
            }
            catch (Exception e)
            {
                var a = e.Message;
                return null;
            }
        }


        /// <summary>
        /// Услуги по id обращения
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<List<CaseServiceDto>> GetServicesByCaseIdAsync(string id)
        {
            var userId = await _employeeManager.GetIdAsync();
            var services = await _context.DServices
                .AsNoTracking()
                .AsSplitQuery()
                /*  .Include(i => i.DCases).ThenInclude(i => i.DCasesFavorites)
                  .Include(i => i.DServicesRoutesStages).ThenInclude(i => i.SEmployees)
                  .Include(i => i.SServicesStatus)
                  .Include(i => i.SEmployeesIdAddNavigation)*/
                .Expressionify()
                .Where(w => w.DCasesId == id)
                .Select(s => new CaseServiceDto
                {
                    Id = s.Id,
                    IsRoot = s.DServicesIdParent == Guid.Empty,
                    Name = s.SServices.ServiceName,
                    InteractionName = s.SServices.SServicesInteraction.InteractionName,
                    ProcedureName = s.SServicesProcedure.ProcedureName,
                    OfficeId = s.SOfficesIdProvider,
                    Office = s.SServices.SOffice.OfficeNameSmall,
                    DepartamentId = s.SOfficesIdProviderDepartament,
                    DepartamentName = s.SOfficesIdProviderDepartamentNavigation.OfficeName,
                    Status = new StatusDto(s.SServicesStatusId, s.SServicesStatus.StatusName),
                    SerivesStage = s.DServicesRoutesStages.OrderByDescending(o => o.DateAdd).Select(ss => new SerivesStageDto
                    {
                        EmployeeCurrent = new EmployeeDto(ss.SEmployeesId, NameSplitter.GetInitials(ss.SEmployees.EmployeeName), ss.SEmployeesJobPosition.JobPositionName),
                        Stage = new StageDto(ss.SRoutesStageId, ss.SRoutesStage.StageName),

                    }).First(),
                    /* EmployeeCurrent = new EmployeeDto(s.DServicesRoutesStages.OrderByDescending(o => o.DateAdd).First().SEmployeesId,
                         NameSplitter.GetInitials(s.DServicesRoutesStages.OrderByDescending(o => o.DateAdd).First().SEmployees.EmployeeName),
                         s.DServicesRoutesStages.OrderByDescending(o => o.DateAdd).First().SEmployeesJobPosition.JobPositionName),

                     Stage = new StageDto(s.DServicesRoutesStages.OrderByDescending(o => o.DateAdd).First().SRoutesStageId,
                         s.DServicesRoutesStages.OrderByDescending(o => o.DateAdd).First().SRoutesStage.StageName),*/
                    Days = s.DateFact.HasValue
                        ? s.DateReg.Subtract(s.DateFact.Value).Days
                        : s.DateReg.Subtract(DateTime.Today).Days,
                    IsFavorite = s.DCases.DCasesFavorites.Any(a => a.SEmployeesId == userId)
                }).ToListAsync();
            return services;
        }

        /// <summary>
        /// Услуги по id обращения
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<CasesDto> GetServicesByCaseIdAsyncV2(string id)
        {
            var userId = await _employeeManager.GetIdAsync();
            var services = await _context.DCases
                .AsNoTracking()
                .AsSplitQuery()
                .Expressionify()
                .Where(w => w.Id == id)
                .Select(x => new CasesDto
                {
                    casesMainDto = new CasesMainDto
                    {
                        CaseId = x.Id,
                        DateAdd = x.DateAdd.ToShortDateString(),
                        Applicant = x.DServicesCustomersLegals.Count == 0 ? x.DServicesCustomers.Where(w => w.CustomerType == (int)CustomerType.Applicant).Select(ss => new ApplicantDto(ss.Id, ss.Fio(), ss.CustomerAddress, ss.CustomerPhone1)).First() :
                                                                               x.DServicesCustomersLegals.Select(ss => new ApplicantDto(ss.Id, ss.CustomerName, ss.CustomerAddress, ss.CustomerPhone1)).First(),
                        CustomerType = x.DServicesCustomersLegals.Count == 0 ? 0 : 1,
                        Recipient = x.DServicesCustomers.Where(w => w.CustomerType == (int)CustomerType.Representative)
                            .Select(ss => new ApplicantDto(ss.Id, ss.Fio(), ss.CustomerAddress, ss.CustomerPhone1)).FirstOrDefault(),
                        CommentsCount = x.DServicesCommentts.Count,
                        NotificationsCount = x.DServicesCustomersCalls.Count + x.DServicesCustomersMessages.Count,
                        AuditCounts = x.DCasesViews.Count
                    },
                    Services = x.DServices.Select(s => new CaseServiceDto
                    {
                        Id = s.Id,
                        IsRoot = s.DServicesIdParent == Guid.Empty,
                        Name = s.SServices.ServiceName,
                        InteractionName = s.SServices.SServicesInteraction.InteractionName,
                        ProcedureName = s.SServicesProcedure.ProcedureName,
                        OfficeId = s.SOfficesIdProvider,
                        Office = s.SServices.SOffice.OfficeNameSmall,
                        DepartamentId = s.SOfficesIdProviderDepartament,
                        DepartamentName = s.SOfficesIdProviderDepartamentNavigation.OfficeName,
                        Status = new StatusDto(s.SServicesStatusId, s.SServicesStatus.StatusName),
                        IsFavorite = x.DCasesFavorites.Any(a => a.SEmployeesId == userId),
                        EmployeeAdd = new EmployeeDto(s.SEmployeesIdAdd, NameSplitter.GetInitials(s.SEmployeesIdAddNavigation.EmployeeName), s.SEmployeesJobPositionIdAddNavigation.JobPositionName),
                        //SerivesStage = s.DServicesRoutesStages.OrderByDescending(o => o.DateAdd).Select(ss => new SerivesStageDto
                        //{
                        //    EmployeeCurrent = new EmployeeDto(ss.SEmployeesId, NameSplitter.GetInitials(ss.SEmployees.EmployeeName), ss.SEmployeesJobPosition.JobPositionName),
                        //    Stage = new StageDto(ss.SRoutesStageId, ss.SRoutesStage.StageName),

                        //}).First(),
                        SerivesStage = new SerivesStageDto
                        {
                            EmployeeCurrent = new EmployeeDto(s.SEmployeesIdCurrent, NameSplitter.GetInitials(s.SEmployeesIdCurrentNavigation.EmployeeName), s.SEmployeesJobPositionIdCurrentNavigation.JobPositionName),
                            Stage = new StageDto((int)s.SRoutesStageIdCurrent, s.SRoutesStage.StageName),

                        },
                        Days = s.DateFact.HasValue
                                ? s.DateReg.Subtract(s.DateFact.Value).Days
                                : s.DateReg.Subtract(DateTime.Today).Days,
                    }).ToList()

                }).FirstAsync();

            services.casesMainDto.Notes.AddRange(await _context.DNotes.Where(w => w.DCasesId == id && w.SEmployeesId == userId)
                .Select(s =>
                new NotesDto
                {
                    Id = s.Id,
                    NotesText = s.NoteText,
                    DateReminder = s.DateReminder
                }).ToListAsync());

            return services;
        }

        public async Task SetCaseFavoriteAsync(string id)
        {
            var userId = await _employeeManager.GetIdAsync();
            await _context.DCasesFavorites.AddAsync(new DCasesFavorite()
            {
                DCasesId = id,
                SEmployeesId = userId.Value
            });
            await _context.SaveChangesAsync();
        }

        public async Task RemoveCaseFavoriteAsync(string id)
        {
            var userId = await _employeeManager.GetIdAsync();
            await _context.DCasesFavorites.Where(w => w.DCasesId == id && w.SEmployeesId == userId.Value).DeleteAsync();
        }
        public async Task SetServiceFavoriteAsync(Guid id)
        {
            var userId = await _employeeManager.GetIdAsync();
            await _context.DEmployeesServicesFavorites.AddAsync(new DEmployeesServicesFavorite()
            {
                SServicesId = id,
                SEmployeesId = userId.Value
            });
            await _context.SaveChangesAsync();
        }

        public async Task RemoveServiceFavoriteAsync(Guid id)
        {
            var userId = await _employeeManager.GetIdAsync();
            await _context.DEmployeesServicesFavorites.Where(w => w.SServicesId == id && w.SEmployeesId == userId.Value).DeleteAsync();
        }

        public async Task ViewCaseAsync(string id)
        {
            var employee = await _employeeManager.GetFullInfoAsync();
            await _context.DCasesViews.AddAsync(new DCasesView
            {
                DCasesId = id,
                EmployeesName = employee.Name,
                JobPositionName = employee.Job.Name,
                OfficeName = employee?.Office?.Name,
                SEmployeesId = employee.Id
            });
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Список выполненных СМЭВ запросов по id услуги
        /// </summary>
        /// <param name="id">Id услуги</param>
        /// <returns></returns>
        public async Task<List<CaseServiceSmevСompletedDto>> GetSmevСompletedByServiceIdAsync(Guid id)
        {
            try
            {
                return await _context.DServicesSmevRequests
                .AsNoTracking()
                .Where(w => w.DServicesId == id)
                .OrderByDescending(o => o.DateAdd)
                .Select(s => new CaseServiceSmevСompletedDto
                {
                    Id = s.Id,
                    Name = s.SSmevRequest.RequestName,
                    Provider = s.SSmevRequest.SSmev.SmevProvider,
                    DateCreate = s.DateRequest.Value.Add(s.TimeRequest.Value.ToTimeSpan()),
                    DateResponse = s.DateFact.HasValue ? s.DateFact.Value.Add(s.TimeFact.Value.ToTimeSpan()) : s.DateReg,
                    Status = s.SmevStatus(),
                    EmployeeAdd = new EmployeeDto(s.SEmployeesId, NameSplitter.GetInitials(s.EmployeeFioAdd),
                    s.SEmployeesJobPosition.JobPositionName),
                    Description = s.Commentt,
                    Service = new ServiceDto(s.DServicesId, s.DServices.SServices.ServiceNameSmall,
                    s.DServices.SServices.SOffice.OfficeNameSmall)
                }).ToListAsync();
            }
            catch (Exception e)
            {
                return new List<CaseServiceSmevСompletedDto>();
            }

        }

        /// <summary>
        /// Список выполненных платежей по id услуги
        /// </summary>
        /// <param name="id">Id услуги</param>
        /// <returns></returns>
        public async Task<List<CaseServicePaymentsСompletedDto>> GetPaymentsСompletedByServiceIdAsync(Guid id)
        {
            try
            {
                return await _context.DAcquirings
                .AsNoTracking()
                .Where(w => w.DServicesId == id)
                .OrderByDescending(o => o.DateAdd)
                .Select(s => new CaseServicePaymentsСompletedDto
                {
                    Id = s.Id,
                    //EmployeeFio = s.SE.employee_fio,
                    //PayerFio = s.Data_services_customers.customer_fio,
                    Status = s.Status,
                    TypePayment = "Гос Пошлина",
                    PaymentMethod = string.IsNullOrEmpty(s.Uin) ? "По реквизитам" : "По УИН",
                    Summa = s.Summ,
                    DateAdd = s.DateAdd,
                    //DatePayment = s.Data_acquirings_checks.FirstOrDefault().operation_time
                }).OrderByDescending(o => o.DateAdd).ToListAsync();
            }
            catch (Exception e)
            {
                return new List<CaseServicePaymentsСompletedDto>();
            }

        }

        /// <summary>
        /// Список выполненных платежей по id услуги
        /// </summary>
        /// <param name="id">Id услуги</param>
        /// <returns></returns>
        public async Task<ServicePaymentsInfo?> GetPaymentsInfoServiceIdAsync(Guid id)
        {
            try
            {
                return await _context.DServices
                .AsNoTracking()
                .Where(w => w.Id == id)
                .Select(s => new ServicePaymentsInfo
                {
                    DServicesId = s.Id,
                    DCaseId = s.DCasesId,
                    SOfficesIdProvider = s.SOfficesIdProvider,
                    ServiceName = s.SServices.ServiceName,
                    TariffState = s.TariffState,
                    TariffMfc = s.TariffMfc
                }).FirstOrDefaultAsync();
            }
            catch (Exception e)
            {
                return null;
            }

        }

        /// <summary>
        /// Список выполненных платежей по id услуги
        /// </summary>
        /// <param name="id">Id услуги</param>
        /// <returns></returns>
        public async Task<Guid?> AddServicePaymentAsync(ServicePaymentsRequestModelAddDto request)
        {
            return null;
            try
            {
                DAcquiring model = new DAcquiring
                {
                    DCasesId = request.DCasesId,
                    DServicesId = request.DServicesId,
                    DServicesCustomersId = request.DServicesCustomersId,
                    SRecipientPaymentId = request.SRecipientPaymentId,
                    Summ = request.Summ,
                    TypePaymet = request.TypePaymet,
                    IsMirCard = request.IsMirCard,
                    Uin = request.Uin,
                    IpWindow = request.IpWindow,
                    SEmployeesId = request.SEmployeesId,
                    SOfficesId = request.SOfficesId,
                    DateAdd = DateTime.Now
                };

                await _context.DAcquirings.AddAsync(model);
                await _context.SaveChangesAsync();
                return model.Id;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        /// <summary>
        /// Список выполненных платежей по id услуги
        /// </summary>
        /// <param name="id">Id услуги</param>
        /// <returns></returns>
        public bool IsCheckServicePaymentAsync(Guid Id)
        {
            try
            {
                bool? isChecks = null;

                var dAcquiring = new DAcquiring();

                for (int i = 0; i < 10; i++)
                {
                    dAcquiring = _context.DAcquirings.Find(Id);

                    if (dAcquiring.Status == "FAILED" || dAcquiring.Status == "UNKNOWN")
                    {
                        isChecks = true;
                        break;
                    }

                    if (dAcquiring.Stage == 7 && dAcquiring.Status == "SUCCESS")
                    {
                        isChecks = false;
                        break;
                    }
                    Thread.Sleep(3000);
                }

                if (isChecks == null || isChecks == true)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            catch (Exception e)
            {
                return false;
            }

        }

        /// <summary>
        /// Список возможных СМЭВ запросов, для добавления нового запроса в услугу
        /// </summary>
        /// <param name="id">Id услуги</param>
        /// <param name="search">строка поиска</param>
        /// <param name="start">skip</param>
        /// <param name="length">take</param>
        /// <param name="smevCount">всего найдено</param>
        /// <param name="smevFilteredCount">отсортировано</param>
        /// <returns></returns>
        public List<CaseServiceSmevActiveDto> GetSmevActiveByServiceId(Guid id, string search, int start, int length, out int smevCount, out int smevFilteredCount)
        {
            var smevs = _context.DServices.Where(w => w.Id == id)
                .AsNoTracking()
                .Select(s => s.SServices.SServicesSmevRequestJoins
                    .DefaultIfEmpty()
                    .Where(w => w.SSmevRequest.IsRequestActive != null && w.IsRemove == false && w.SSmevRequest.IsRequestActive.Value)
                    .Select(ss =>
                        new CaseServiceSmevActiveDto
                        {
                            Id = ss.SSmevRequestId,
                            Name = ss.SSmevRequest.RequestName,
                            Description = ss.SSmevRequest.SSmev.SmevDescription,
                            Provider = ss.SSmevRequest.SSmev.SmevProvider,
                            Days = ss.SSmevRequest.CountDayExecution,
                            WeekTypeName = ss.SSmevRequest.SServicesWeek.TypeName,
                            FormPath = ss.SSmevRequest.SmevFormPath(),
                            ActionPath = ss.SSmevRequest.ActionPath
                        })).First();

            smevCount = smevs.Count();

            var filteredData = String.IsNullOrWhiteSpace(search)
                ? smevs
                : smevs.Search(s => s.Name.ToLower(),
                        s => s.Provider.ToLower(),
                        s => s.Id.ToString())
                    .Containing(search.Trim().ToLower().Split(" "));

            smevFilteredCount = filteredData.Count();

            return filteredData.Skip(start).Take(length).ToList();
        }

        public (List<ActiveServicesDto>, int, int) GetActiveServices(SearchCasesRequestData searchData, int start, int length)
        {
            var date = DateTime.Today;
            var employeeId = _employeeManager.GetIdAsync().Result;
            var roles = _context.SEmployeesRolesExecutors.Where(w => w.SEmployeesId == employeeId)
                .Select(s => s.SRolesExecutorId).ToList();

            var services = _context.SServices
                .AsNoTracking()
                .Where(w => !w.IsRemove
                            && w.SServicesActives.Any(a => a.IsRemove == false &&
                                                           a.DateStart <= date &&
                                                           (a.DateStop == null || date <= a.DateStop)) &&
                          (!searchData.OfficeId.Any() || !w.SOfficesId.HasValue || searchData.OfficeId.Contains(w.SOfficesId.Value)) &&
                          (!searchData.LivingSituationId.Any() || !w.SServicesLivingSituationJoins.Any() || w.SServicesLivingSituationJoins.Any(a => searchData.LivingSituationId.Contains(a.Id))) &&
                            w.SServicesRoutesStages.FirstOrDefault(f => f.IsRemove == false && f.SRoutesStageId == 1)!.SServicesRoutesStageRoles
                                .Select(s => s.SRolesExecutorId).Any(i => roles.Contains(i)) &&
                            roles.Contains(w.SServicesRoutesStages.First(f => f.SRoutesStageId == 1).SServicesRoutesStageRoles
                                .Select(s => s.SRolesExecutorId).First()) &&
                            w.SServicesCustomers.Any(a => a.SServicesCustomerTypeId == searchData.CustomresType));

            var servicesCount = services.Count();

            var filteredData = String.IsNullOrWhiteSpace(searchData.Query)
                ? searchData.OfficeId.Any() ? services : services.Where(w => w.DEmployeesServicesFavorites.Any(a => a.SEmployeesId == employeeId))
                : services.Search(s => s.ServiceName.ToLower(),
                    s => s.ServiceNameSmall.ToLower(),
                    s => s.ServiceMnemo.ToLower(),
                    s => s.Hashtag.ToLower()
                ).ContainingAll(searchData.Query.ToLower());

            var dataPage = filteredData.Skip(start).Take(length)
                .Select(s => new ActiveServicesDto
                {
                    Id = s.Id,
                    Name = s.ServiceName,
                    NameSmall = s.ServiceNameSmall,
                    ProviderName = s.SOffice.OfficeName,
                    isFavorite = s.DEmployeesServicesFavorites.Any(a => a.SEmployeesId == employeeId)
                }).AsParallel().ToList();

            var servicesFilteredCount = filteredData.Count();

            return (dataPage, servicesCount, servicesFilteredCount);
        }

        public (List<ActiveServicesDto>, int, int) GetActiveServicesForLegal(SearchCasesRequestData searchData, int start, int length)
        {
            var date = DateTime.Today;
            var employeeId = _employeeManager.GetIdAsync().Result;
            var roles = _context.SEmployeesRolesExecutors.Where(w => w.SEmployeesId == employeeId)
                .Select(s => s.SRolesExecutorId).ToList();
            var services = _context.SServices
                .AsNoTracking()
                .Where(w => !w.IsRemove
                            && w.SServicesActives.Any(a => a.IsRemove == false &&
                                                           a.DateStart <= date &&
                                                           (a.DateStop == null || date <= a.DateStop)) &&
                           (!searchData.OfficeId.Any() || !w.SOfficesId.HasValue || searchData.OfficeId.Contains(w.SOfficesId.Value)) &&
                            w.SServicesRoutesStages.FirstOrDefault(f => f.IsRemove == false && f.SRoutesStageId == 1)!.SServicesRoutesStageRoles
                                .Select(s => s.SRolesExecutorId).Any(i => roles.Contains(i)) &&
                            roles.Contains(w.SServicesRoutesStages.First(f => f.SRoutesStageId == 1).SServicesRoutesStageRoles
                                .Select(s => s.SRolesExecutorId).First()) &&
                            w.SServicesCustomers.Any(a => a.SServicesCustomerTypeId != 1912196));

            var servicesCount = services.Count();

            var filteredData = String.IsNullOrWhiteSpace(searchData.Query)
                ? services.Where(w => w.DEmployeesServicesFavorites.Any())
                : services.Search(s => s.ServiceName.ToLower(),
                    s => s.ServiceNameSmall.ToLower(),
                    s => s.ServiceMnemo.ToLower()
                ).Containing(searchData.Query.ToLower().Split(" "));

            var dataPage = filteredData.Skip(start).Take(length)
                .Select(s => new ActiveServicesDto
                {
                    Id = s.Id,
                    Name = s.ServiceName,
                    NameSmall = s.ServiceNameSmall,
                    isFavorite = s.DEmployeesServicesFavorites.Any()
                }).AsParallel().ToList();

            var servicesFilteredCount = filteredData.Count();

            return (dataPage, servicesCount, servicesFilteredCount);
        }

        /// <summary>
        /// Процедуры услуги
        /// </summary>
        /// <param name="serviceId">id услуги</param>
        /// <returns></returns>
        public List<ActiveServiceProcedureDto> GetServiceProcedures(Guid serviceId)
        {
            if (_context.SServices.Any(a => a.Id == serviceId && !a.IsRemove) == false)
                return new List<ActiveServiceProcedureDto>();

            return _context.SServicesProcedures
                .AsNoTracking()
                .Where(w => !w.IsRemove && w.SServicesId == serviceId && w.ProcedureActive)
                .Select(s => new ActiveServiceProcedureDto
                {
                    Id = s.Id,
                    Name = s.ProcedureName,
                    AdditionalFormPath = s.ExtraFormPath
                })
                .OrderBy(o => o.Name)
                .ToList();
        }

        /// <summary>
        /// Тарифы услуги
        /// </summary>
        /// <param name="procedureId">id процедуры</param>
        /// <returns></returns>
        public List<ActiveServiceTariffDto> GetServiceTariff(Guid procedureId, int? type)
        {
            if (_context.SServicesProcedures.Any(a => a.Id == procedureId && !a.IsRemove && a.ProcedureActive) == false)
                return new List<ActiveServiceTariffDto>();

            return _context.SServicesCustomerTariffs
                .AsNoTracking()
                .Where(w => !w.IsRemove && w.SServicesProcedureId == procedureId && w.SServicesCustomer.SServicesCustomerTypeId == type)
                .Select(tariff => new ActiveServiceTariffDto
                {
                    Id = tariff.Id,
                    ServiceCustomerId = tariff.SServicesCustomerId,
                    CustomerTypeId = tariff.SServicesCustomer.SServicesCustomerType.Id,
                    CustomerTypeName = tariff.SServicesCustomer.SServicesCustomerType.TypeName,
                    TariffTypeName = tariff.SServicesTariffType.TariffTypeName,
                    WeekTypeId = tariff.SServicesWeekId,
                    WeekTypeName = tariff.SServicesWeek.TypeName,
                    CountDayProcessing = tariff.CountDayProcessing,
                    CountDayExecuting = tariff.CountDayExecution,
                    CountDayReturn = tariff.CountDayReturn,
                    TariffMfc = tariff.TariffMfc,
                    TariffOiv = tariff.Tariff,
                    Comment = tariff.Commentt,
                })
                .ToList();
        }


        /// <summary>
        /// Поставщики услуги
        /// </summary>
        /// <param name="serviceId">id услуги</param>
        /// <returns></returns>
        public List<ActiveServiceProviderDto> GetServiceProvidersAll()
        {
            return _context.SOffices
                .AsNoTracking()
                .Where(w => !w.IsRemove && w.SOfficesTypeId != (int)OfficeType.Mfc && w.SOfficesTypeId != (int)OfficeType.Tosp)
                .Select(s => new ActiveServiceProviderDto
                {
                    Id = s.Id,
                    OfficeName = s.OfficeName
                })
                .ToList();
        }

        /// <summary>
        /// Создание услуги
        /// </summary>
        /// <param name="requestModel">Модель услуги</param>
        /// <returns></returns>
        public async Task<CreateCaseResponseModel> CreateCaseAsync(CreateCaseRequestModel requestModel)
        {
            try
            {
                const int serviceProcessStatusId = 0;
                const int workDays = 1;
                var date = DateTime.Today;
                var currentEmployee = await _employeeManager.GetFullInfoAsync();
                var responseModel = new CreateCaseResponseModel();
                Dictionary<string, string> search = new Dictionary<string, string>();

                var serviceCustomerTariff = await _context.SServicesCustomerTariffs
                    .Select(s => new
                    {
                        s.Id,
                        s.SServicesCustomerId,
                        s.CountDayExecution,
                        s.CountDayReturn,
                        s.CountDayProcessing,
                        s.SServicesWeekId,
                        s.SServicesCustomer.SServicesCustomerTypeId,
                        s.SServicesTariffTypeId,
                        s.Tariff,
                        s.TariffMfc
                    }).SingleOrDefaultAsync(sd => sd.Id == requestModel.TariffId);

                if (serviceCustomerTariff is null)
                    return responseModel.CreatedError("Тариф не найден."); //TODO добавить

                var newCaseId = await GenerateCaseId(currentEmployee.Office.Mnemo);

                if (string.IsNullOrEmpty(newCaseId))
                    return responseModel.CreatedError("Номер обращения не сформирован");

                var newServiceId = Guid.NewGuid();
                var newDServiceRouteStageId = Guid.NewGuid();

                var service = await _context.SServices
                    .Select(s => new
                    {
                        s.Id,
                        s.ServiceNameSmall,
                        s.IsTariffEdit,
                        s.IsReportSelect,
                        s.IsMdm,
                        s.IsPlan,
                        s.SOfficesId,
                        s.FrguServiceId,
                        s.IasMkgu,
                        s.FrguName,
                        s.SOffice.OfficeFrguProviderId,
                        s.SServicesTypeId,
                        s.SServicesInteractionId,
                        s.IsIssueAuthority,
                        ProcedureFrguTargetId = s.SServicesProcedures.Where(w => w.Id == requestModel.ProcedureId).Select(s => s.FrguTargetId).FirstOrDefault()
                    }).SingleOrDefaultAsync(sd => sd.Id == requestModel.ServiceId);

                if (service is null) return responseModel.CreatedError(ErrorDescription.ServiceNotFound);

                var countDay = serviceCustomerTariff.CountDayExecution +
                               serviceCustomerTariff.CountDayReturn +
                               serviceCustomerTariff.CountDayProcessing;

                var dataReg = date.AddDays(countDay);

                if (serviceCustomerTariff.SServicesWeekId == workDays)
                {
                    DateTime? calendar = await _context.SCalendars.Where(w => w.DateType == 1 && w.Date >= date)
                        .Select(s => s.Date).Skip(countDay).FirstOrDefaultAsync();
                    dataReg = calendar.Value;
                }

                var routeStage = await _context.SServicesRoutesStages
                    .Where(w => w.SServicesId == requestModel.ServiceId && w.SRoutesStageId == requestModel.StageId && w.IsRemove == false)
                    .Select(s => new
                    {
                        s.Id,
                        s.ParentId,
                        StageId = s.SRoutesStageId,
                        Name = s.SRoutesStage.StageName,
                        Days = s.SRoutesStage.DayExcution,
                        Comment = s.SRoutesStage.Commentt,
                        StatusId = s.SRoutesStage.SServicesStatusId,
                        IsDateFact = s.SRoutesStage.SServicesStatus.IsDatefact,
                        RolePosition = s.SServicesRoutesStageRoles.Select(ss => new
                        {
                            Id = ss.SRolesExecutorId,
                            Name = ss.SRolesExecutor.RoleName
                        }).ToList()
                    }).FirstOrDefaultAsync();

                if (routeStage is null) return responseModel.CreatedError("Пользователь не может добавить обращение");

                string additionalData = AdditionalDataFix(requestModel.AdditionalData);

                DService dService = new()
                {
                    Id = newServiceId,
                    DCasesId = newCaseId,
                    DServicesIdParent = Guid.Empty,
                    DServicesDocumentIdParent = Guid.Empty,
                    SServicesCustomerTypeId = serviceCustomerTariff.SServicesCustomerTypeId,
                    SServicesId = requestModel.ServiceId,
                    SServicesProcedureId = requestModel.ProcedureId,
                    DateFact = routeStage.IsDateFact ? DateTime.Now : null,
                    DateReg = dataReg,
                    CountDayProcessing = serviceCustomerTariff.CountDayProcessing,
                    CountDayReturn = serviceCustomerTariff.CountDayReturn,
                    CountDayExecution = serviceCustomerTariff.CountDayExecution,
                    SServicesTariffTypeId = serviceCustomerTariff.SServicesTariffTypeId,
                    TariffState = serviceCustomerTariff.Tariff,
                    TariffEdit = service.IsTariffEdit,
                    IsReportSelect = service.IsReportSelect,
                    SEmployeesIdAdd = currentEmployee.Id,
                    SOfficesIdAdd = currentEmployee.Office.Id,
                    SEmployeesJobPositionIdAdd = currentEmployee.Job.Id,
                    SServicesStatusId = routeStage.StatusId ?? serviceProcessStatusId,
                    TariffMfc = serviceCustomerTariff.TariffMfc,
                    SServicesWeekId = serviceCustomerTariff.SServicesWeekId,
                    ExtraInfo = additionalData,
                    SOfficesIdProvider = service.SOfficesId ?? Guid.Empty,
                    FrguProviderId = service.OfficeFrguProviderId,
                    FrguName = service.FrguName,
                    FrguServiceId = service.FrguServiceId,
                    FrguTargetId = service.ProcedureFrguTargetId,
                    IasMkgu = service.IasMkgu,
                    IsPlan = service.IsPlan ?? false,
                    SServicesTypeId = service.SServicesTypeId,
                    SServicesInteractionId = service.SServicesInteractionId,
                    IsIssueAuthority = service.IsIssueAuthority ?? false,
                    SEmployeesIdExecution = routeStage.IsDateFact ? currentEmployee.Id : null,
                    SOfficesIdExecution = routeStage.IsDateFact ? currentEmployee.Office.Id : null,
                    SEmployeesJobPositionIdExecution = routeStage.IsDateFact ? currentEmployee.Job.Id : null,
                    SRoutesStageIdCurrent = routeStage.StageId,
                    SEmployeesIdCurrent = currentEmployee.Id,
                    SEmployeesJobPositionIdCurrent = currentEmployee.Job.Id,
                    SOfficesIdCurrent = currentEmployee.Office.Id,
                    RoutesStageDateRegCurrent = routeStage.Days != 0 ? date.AddDays(routeStage.Days) : null,
                };

                var newCase = new DCase
                {
                    Id = newCaseId,
                    DateAdd = DateTime.Now,
                    DServicesRoutesStages = new List<DServicesRoutesStage>
                    {
                        new()
                        {
                            Id = newDServiceRouteStageId,
                            DCasesId = newCaseId,
                            DServicesId = newServiceId,
                            DServicesIdParent = Guid.Empty,
                            SRoutesStageId = routeStage.StageId,
                            DateAdd = DateTime.Now,
                            CountDayExecution = routeStage.Days,
                            DateReg = routeStage.Days != 0 ? date.AddDays(routeStage.Days) : null,
                            SEmployeesId = currentEmployee.Id,
                            SOfficesId = currentEmployee.Office.Id,
                            SEmployeesJobPositionId = currentEmployee.Job.Id,
                            EmployeeFioAdd = currentEmployee.Name,
                            SEmployeesIdAdd = currentEmployee.Id,
                            SEmployeesJobPositionIdAdd = currentEmployee.Job.Id,
                            PassAutomatically = true
                        }
                    },
                    DServicesDocuments = await _context.SServicesDocuments.Where(w =>
                            w.IsRemove == false && w.SServicesId == requestModel.ServiceId &&
                            (!w.SServicesProcedureId.HasValue || w.SServicesProcedureId.Value == requestModel.ProcedureId))
                        .Select(s => new DServicesDocument
                        {
                            DCasesId = newCaseId,
                            DServicesId = newServiceId,
                            SDocumentsId = s.SDocumentsId,
                            DocumentCount = s.DocumentCount,
                            DocumentType = s.DocumentType,
                            DocumentNeeds = s.DocumentNeeds,
                            IsCheck = false
                        }).ToListAsync(),
                    DServicesDocumentsResults = await _context.SServicesDocumentsResults
                        .Where(w => !w.IsRemove && w.SServicesId == requestModel.ServiceId &&
                        (!w.SServicesProcedureId.HasValue || w.SServicesProcedureId.Value == requestModel.ProcedureId))
                        .Select(s => new DServicesDocumentsResult
                        {
                            DCasesId = newCaseId,
                            DServicesId = newServiceId,
                            SDocumentsId = s.SDocumentsId,
                            DocumentResult = s.DocumentResult,
                            DocumentMethod = s.DocumentMethod,
                            DocumentPeriodMfc = s.DocumentPeriodMfc,
                            DocumentPeriodProvider = s.DocumentPeriodProvider
                        }).ToListAsync()
                };

                if (requestModel.Customer is not null)
                {
                    var customer = _mapper.Map<DServicesCustomer>(requestModel.Customer);
                    customer.DCasesId = newCaseId;
                    customer.CustomerType = (int)CustomerType.Applicant;
                    customer.SEmployeesId = currentEmployee.Id;
                    customer.SOfficesId = currentEmployee.Office.Id;
                    customer.SEmployeesJobPositionId = currentEmployee.Job.Id;
                    customer.DateAdd = DateTime.Now;
                    customer.CustomerAddressDetail = JsonConvert.SerializeObject(requestModel.Customer.AddressDetails);
                    newCase.DServicesCustomers.Add(customer);

                    dService.CustomerName = requestModel.Customer.Fio();
                    dService.CustomerPhone1 = requestModel.Customer.CustomerPhone1;
                    dService.CustomerPhone2 = requestModel.Customer.CustomerPhone2;
                    dService.CustomerAddress = requestModel.Customer.CustomerAddress;
                }

                if (requestModel.Representative is not null)
                {
                    var representative = _mapper.Map<DServicesCustomer>(requestModel.Representative);
                    representative.DCasesId = newCaseId;
                    representative.CustomerType = (int)CustomerType.Representative;
                    representative.SEmployeesId = currentEmployee.Id;
                    representative.SOfficesId = currentEmployee.Office.Id;
                    representative.SEmployeesJobPositionId = currentEmployee.Job.Id;
                    representative.DateAdd = DateTime.Now;
                    representative.CustomerAddressDetail = JsonConvert.SerializeObject(requestModel.Representative.AddressDetails);
                    newCase.DServicesCustomers.Add(representative);
                }

                if (requestModel.CustomerLegal is not null)
                {
                    requestModel.CustomerLegal.DCasesId = newCaseId;
                    requestModel.CustomerLegal.SEmployeesId = currentEmployee.Id;
                    requestModel.CustomerLegal.SOfficesId = currentEmployee.Office.Id;
                    requestModel.CustomerLegal.SEmployeesJobPositionId = currentEmployee.Job.Id;
                    requestModel.CustomerLegal.DateAdd = DateTime.Now;
                    newCase.DServicesCustomersLegals.Add(requestModel.CustomerLegal);

                    dService.CustomerName = requestModel.CustomerLegal.CustomerName;
                    dService.CustomerPhone1 = requestModel.CustomerLegal.CustomerPhone1;
                    dService.CustomerPhone2 = requestModel.CustomerLegal.CustomerPhone2;
                    dService.CustomerAddress = requestModel.CustomerLegal.CustomerAddress;
                }

                newCase.DServices.Add(dService);

                var office = await _context.SOffices.Where(w => w.Id == currentEmployee.Office.Id).Select(s => new { s.SendMdm, s.MdmUid }).FirstAsync();

                await _context.AddAsync(newCase);

                if (service.IsMdm is not null and true && office.SendMdm is not null and true && office.MdmUid is not null)
                {
                    var mdm = await _mdmService.MdmStartedDataServicesObjectsAsync(newServiceId, (Guid)office.MdmUid, requestModel.ProcedureId, newDServiceRouteStageId);
                    if (mdm is not null) await _context.AddRangeAsync(mdm);
                    if (requestModel.StageId == 34)
                    {
                        var mdmProcessingEnded = await _mdmService.MdmServiceProcessingEndedObjectsAsync(newServiceId, (Guid)office.MdmUid, 0, false, newDServiceRouteStageId);
                        if (mdmProcessingEnded is not null) await _context.AddRangeAsync(mdmProcessingEnded);
                    }
                }
                await _context.SaveChangesAsync();

                responseModel.SetEmployee(currentEmployee.Name)
                    .SetCaseId(newCase.Id)
                    .SetDserviceId(newServiceId)
                    .SetService(service.ServiceNameSmall)
                    .SetDataReg(dataReg)
                    .SetTariff(serviceCustomerTariff.Tariff + serviceCustomerTariff.TariffMfc)
                    .SetCustomer(requestModel.CustomerLegal != null ? requestModel.CustomerLegal.CustomerName : requestModel.Customer.Fio())
                    .CreatedSuccess();

                return responseModel;
            }
            catch (Exception e)
            {
                return new CreateCaseResponseModel().CreatedError(e.Message); //TODO изменить
            }
        }
        //TODO ИЗМЕНИТЬ 
        public async Task<CreateCaseResponseModel> CreateCaseAsync(CreateCaseLegalRequestModel requestModel)
        {
            try
            {
                const int serviceProcessStatusId = 0;
                const int workDays = 1;
                var date = DateTime.Today;
                var currentEmployee = await _employeeManager.GetFullInfoAsync();
                var responseModel = new CreateCaseResponseModel();

                var serviceCustomerTariff = await _context.SServicesCustomerTariffs
                    .Select(s => new
                    {
                        s.Id,
                        s.SServicesCustomerId,
                        s.CountDayExecution,
                        s.CountDayReturn,
                        s.CountDayProcessing,
                        s.SServicesWeekId,
                        s.SServicesCustomer.SServicesCustomerTypeId,
                        s.SServicesTariffTypeId,
                        s.Tariff,
                        s.TariffMfc
                    }).SingleOrDefaultAsync(sd => sd.Id == requestModel.TariffId);

                if (serviceCustomerTariff is null)
                    return responseModel.CreatedError("Тариф не найден."); //TODO добавить

                var newCaseId = DataUtils.GenerateCaseId(currentEmployee.Office?.Mnemo);
                var newServiceId = Guid.NewGuid();
                var newDServiceRouteStageId = Guid.NewGuid();

                var service = await _context.SServices
                    .Select(s => new
                    {
                        s.Id,
                        s.ServiceNameSmall,
                        s.IsTariffEdit,
                        s.IsReportSelect,
                        s.IsPlan,
                        s.IsMdm,
                        s.SOfficesId,
                        s.FrguServiceId,
                        s.FrguName,
                        s.SOffice.OfficeFrguProviderId,
                        ProcedureFrguTargetId = s.SServicesProcedures.Select(s => new { s.Id, s.FrguTargetId }).FirstOrDefault(f => f.Id == requestModel.ProcedureId)
                    }).SingleOrDefaultAsync(sd => sd.Id == requestModel.ServiceId);

                if (service is null) return responseModel.CreatedError(ErrorDescription.ServiceNotFound);

                var countDay = serviceCustomerTariff.CountDayExecution +
                               serviceCustomerTariff.CountDayReturn +
                               serviceCustomerTariff.CountDayProcessing;

                var dataReg = date.AddDays(countDay);

                if (serviceCustomerTariff.SServicesWeekId == workDays)
                {
                    DateTime? calendar = await _context.SCalendars.Where(w => w.DateType == 1 && w.Date >= date)
                        .Select(s => s.Date).Skip(countDay).FirstOrDefaultAsync();
                    dataReg = calendar.Value;
                }

                var serviceStages = await _context.SServicesRoutesStages
                    .Where(w => w.SServicesId == requestModel.ServiceId && w.IsRemove == false)
                    .Select(s => new
                    {
                        Id = s.Id,
                        ParentId = s.ParentId,
                        StageId = s.SRoutesStageId,
                        Name = s.SRoutesStage.StageName,
                        Days = s.SRoutesStage.DayExcution,
                        Comment = s.SRoutesStage.Commentt,
                        StatusId = s.SRoutesStage.SServicesStatusId,
                        IsDateFact = s.SRoutesStage.SServicesStatus.IsDatefact,
                        RolePosition = s.SServicesRoutesStageRoles.Select(ss => new
                        {
                            Id = ss.SRolesExecutorId,
                            Name = ss.SRolesExecutor.RoleName
                        }).ToList()
                    })
                    .ToListAsync();

                var routeStage = serviceStages.SingleOrDefault(sd => sd.ParentId == Guid.Empty);

                requestModel.Customer.DCasesId = newCaseId;
                requestModel.Customer.CustomerType = (int)CustomerType.Applicant;
                requestModel.Customer.SEmployeesId = currentEmployee.Id;
                requestModel.Customer.SOfficesId = currentEmployee.Office.Id;
                requestModel.Customer.SEmployeesJobPositionId = currentEmployee.Job.Id;
                requestModel.Customer.DateAdd = DateTime.Now;

                requestModel.CustomerLegal.DCasesId = newCaseId;
                requestModel.CustomerLegal.SEmployeesId = currentEmployee.Id;
                requestModel.CustomerLegal.SOfficesId = currentEmployee.Office.Id;
                requestModel.CustomerLegal.SEmployeesJobPositionId = currentEmployee.Job.Id;

                var newCase = new DCase
                {
                    Id = newCaseId,
                    DateAdd = DateTime.Now,
                    DServices = new List<DService>
                    {
                        new()
                        {
                            Id = newServiceId,
                            DCasesId = newCaseId,
                            SServicesProcedureId = requestModel.ProcedureId,
                            DServicesIdParent = Guid.Empty,
                            DServicesDocumentIdParent = Guid.Empty,
                            SServicesCustomerTypeId = serviceCustomerTariff.SServicesCustomerTypeId,
                            SServicesId = requestModel.ServiceId,
                            DateReg = dataReg,
                            CountDayProcessing = serviceCustomerTariff.CountDayProcessing,
                            CountDayReturn = serviceCustomerTariff.CountDayReturn,
                            CountDayExecution = serviceCustomerTariff.CountDayExecution,
                            SServicesTariffTypeId = serviceCustomerTariff.SServicesTariffTypeId,
                            TariffState = serviceCustomerTariff.Tariff,
                            TariffEdit = service.IsTariffEdit,
                            IsReportSelect = service.IsReportSelect,
                            IsPlan = service.IsPlan.HasValue ?  service.IsPlan.Value : false,
                            SEmployeesIdAdd = currentEmployee.Id,
                            SOfficesIdAdd = currentEmployee.Office.Id,
                            SEmployeesJobPositionIdAdd = currentEmployee.Job.Id,
                            SServicesStatusId = serviceProcessStatusId,
                            TariffMfc = serviceCustomerTariff.TariffMfc,
                            SServicesWeekId = serviceCustomerTariff.SServicesWeekId,
                            //RouteNext = JsonConvert.SerializeObject(serviceStages),
                            ExtraInfo = AdditionalDataFix(requestModel.AdditionalData),
                            SOfficesIdProvider = service.SOfficesId ?? Guid.Empty,
                            FrguProviderId = service.OfficeFrguProviderId,
                            FrguName = service.FrguName,
                            FrguServiceId = service.FrguServiceId,
                            FrguTargetId = service.ProcedureFrguTargetId?.FrguTargetId
                        }
                    },
                    DServicesCustomers = new List<DServicesCustomer>
                    {
                        //requestModel.Customer
                    },
                    DServicesCustomersLegals = new List<DServicesCustomersLegal>
                    {
                        requestModel.CustomerLegal
                    },
                    DServicesRoutesStages = new List<DServicesRoutesStage>
                    {
                        new()
                        {
                            DCasesId = newCaseId,
                            DServicesId = newServiceId,
                            DServicesIdParent = Guid.Empty,
                            SRoutesStageId = routeStage.StageId,
                            //SServicesRoutesStageId = routeStage.Id,
                            DateAdd = DateTime.Now,
                            CountDayExecution = routeStage.Days,
                            DateReg = routeStage.Days != 0 ? date.AddDays(routeStage.Days) : null,
                            SEmployeesId = currentEmployee.Id,
                            SOfficesId = currentEmployee.Office.Id,
                            SEmployeesJobPositionId = currentEmployee.Job.Id,
                            EmployeeFioAdd = currentEmployee.Name,
                            SEmployeesIdAdd = currentEmployee.Id,
                            SEmployeesJobPositionIdAdd = currentEmployee.Job.Id,
                            PassAutomatically = true
                        }
                    },
                    DServicesDocuments = await _context.SServicesDocuments.Where(w =>
                            w.IsRemove == false && w.SServicesId == requestModel.ServiceId &&
                            (!w.SServicesProcedureId.HasValue || w.SServicesProcedureId.Value == requestModel.ProcedureId))
                        .Select(s => new DServicesDocument
                        {
                            DCasesId = newCaseId,
                            DServicesId = newServiceId,
                            SDocumentsId = s.SDocumentsId,
                            DocumentCount = s.DocumentCount,
                            DocumentType = s.DocumentType,
                            DocumentNeeds = s.DocumentNeeds,
                            IsCheck = false
                        }).ToListAsync(),
                    DServicesDocumentsResults = await _context.SServicesDocumentsResults
                        .Where(w => !w.IsRemove && w.SServicesId == requestModel.ServiceId &&
                            (!w.SServicesProcedureId.HasValue || w.SServicesProcedureId.Value == requestModel.ProcedureId))
                        .Select(s => new DServicesDocumentsResult
                        {
                            DCasesId = newCaseId,
                            DServicesId = newServiceId,
                            SDocumentsId = s.SDocumentsId,
                            DocumentResult = s.DocumentResult,
                            DocumentMethod = s.DocumentMethod,
                            DocumentPeriodMfc = s.DocumentPeriodMfc,
                            DocumentPeriodProvider = s.DocumentPeriodProvider
                        }).ToListAsync()
                };

                var office = await _context.SOffices.Where(w => w.Id == currentEmployee.Office.Id).Select(s => new { s.SendMdm, s.MdmUid }).FirstAsync();

                await _context.AddAsync(newCase);

                if (service.IsMdm is not null and true && office.SendMdm is not null and true && office.MdmUid is not null)
                {
                    var mdm = await _mdmService.MdmStartedDataServicesObjectsAsync(newServiceId, (Guid)office.MdmUid, service.Id, newDServiceRouteStageId);
                    if (mdm is not null) await _context.AddRangeAsync(mdm);
                }

                await _context.SaveChangesAsync();

                responseModel.SetEmployee(currentEmployee.Name)
                    .SetCaseId(newCaseId)
                    .SetService(service.ServiceNameSmall)
                    .SetDataReg(dataReg)
                    .SetTariff(serviceCustomerTariff.Tariff + serviceCustomerTariff.TariffMfc)
                    .SetCustomer(requestModel.Customer.Fio())
                    .CreatedSuccess();

                return responseModel;
            }
            catch (Exception e)
            {
                return new CreateCaseResponseModel().CreatedError(e.Message); //TODO изменить
            }
        }

        private static string AdditionalDataFix(string data)
        {
            while (Regex.IsMatch(data, "{\"\\d+\":\\s*.+?}}"))
            {
                string array = Regex.Match(data, "{\"\\d+\":\\s*.+?}}").Value;
                array = Regex.Replace(array, "\"\\d+\":\\s*", "");
                array = Regex.Replace(array, "{{", "[{");
                array = Regex.Replace(array, "}}", "}]");
                data = data.Replace(Regex.Match(data, "{\"\\d+\":\\s*.+?}}").Value, array);
            }

            return data;
        }

        public async Task<bool> AddServiceStageAsync(ServiceStageSaveDto request)
        {
            try
            {
                const int statusActive = 0;
                const int statusSick = 4;
                const int stageProcessing = 2;
                const int stageOiv = 4;
                const int workDays = 1;
                var date = DateTime.Now.DateOnlyToDay();

                var employee = await _employeeManager.GetFullInfoAsync();

                var services = await _context.DServices
                    .AsSplitQuery()
                    .Where(w => request.serviceId.Contains(w.Id))
                    .Select(s => new
                    {
                        service = s,
                        s.SServices.IsMdm,
                        receivedStage = s.DServicesRoutesStages.Where(w => w.SRoutesStageId == 1).Select(s => s.Id).First(),
                        stage = s.DServicesRoutesStages.OrderByDescending(m => m.DateAdd).FirstOrDefault(),
                        nextStage = s.SServices.SServicesRoutesStages.Where(w => w.SRoutesStageId == request.stageId && w.ParentId == Guid.Empty).Select(ss => new { stage = ss, roles = ss.SServicesRoutesStageRoles.Select(s => s.SRolesExecutorId).ToList() }).First()
                    })
                    .ToListAsync();

                if (services is null or { Count: 0 }) throw new Exception();

                var office = await _context.SOffices.Where(w => w.Id == employee.Office.Id).Select(s => new { s.SendMdm, s.MdmUid }).FirstAsync();

                List<Guid> roles = new();
                List<Guid> employeesId = new();

                foreach (var fe in services)
                {
                    roles.AddRange(fe.nextStage.roles);
                    employeesId.Add(fe.stage.SEmployeesId);
                }

                roles = roles.Distinct().ToList();
                employeesId = employeesId.Distinct().ToList();

                int jobPositionalId;

                if (request.employeeId == Guid.Empty)
                {
                    var employeeFirst = await _context.SEmployees
                        .AsSplitQuery()
                        .AsNoTracking()
                        .OrderBy(o => o.DServicesRoutesStageSEmployees.Count(w =>
                            w.DateAdd.Date.CompareTo(DateTime.Now.Date) == 0 && w.DateFact == null))
                        .Where(w => employeesId.Contains(w.Id)
                                    && w.SEmployeesStatusJoins.Any(a => a.DateStart.CompareTo(DateTime.Today) <= 0 &&
                                                                        (a.DateStop == null ||
                                                                         a.DateStop.Value.CompareTo(DateTime.Today) >= 0) &&
                                                                        (a.SEmployeesStatusId == statusActive ||
                                                                         a.SEmployeesStatusId == statusSick))
                                    && w.SEmployeesExecutions.Any(a =>
                                        !a.IsRemove && a.DateStart.CompareTo(DateTime.Today) <= 0 &&
                                        (a.DateStop == null || a.DateStop.Value.CompareTo(DateTime.Today) >= 0))
                                    && (!roles.Any() ||
                                        w.SEmployeesRolesExecutors.Any(a => roles.Contains(a.SRolesExecutorId)))
                                    && w.SEmployeesOfficesJoins.Any(a =>
                                        !a.IsRemove && a.DateStart.CompareTo(DateTime.Today) <= 0 && (a.DateStop == null ||
                                            a.DateStop.Value.CompareTo(DateTime.Today) >=
                                            0) && a.SOfficesId == request.officeId)
                        )
                        .Select(s => new
                        {
                            s.Id,
                            s.SEmployeesOfficesJoins.FirstOrDefault(fd =>
                                    !fd.IsRemove && fd.DateStart.CompareTo(DateTime.Today) <= 0 &&
                                    (fd.DateStop == null || fd.DateStop.Value.CompareTo(DateTime.Today) >= 0))
                                .SEmployeesJobPositionId,
                            CurrentStage = s.DServicesRoutesStageSEmployees.Count(w => w.DateFact == null)
                        })
                        .FirstOrDefaultAsync();

                    if (employeeFirst is null)
                    {
                        var employeeSecond = await _context.SEmployeesOfficesJoins
                            .AsSplitQuery()
                            .AsNoTracking()
                            .OrderBy(o => o.SEmployees.DServicesRoutesStageSEmployees.Count(w =>
                                w.DateAdd.Date.CompareTo(DateTime.Now.Date) == 0 && w.DateFact == null))
                            .Where(w => w.SOfficesId == request.officeId && !w.IsRemove
                                && w.SEmployees.SEmployeesStatusJoins.Any(a => a.DateStart.CompareTo(DateTime.Today) <= 0 &&
                                                                               (a.DateStop == null ||
                                                                                a.DateStop.Value
                                                                                    .CompareTo(DateTime.Today) >= 0) &&
                                                                               (a.SEmployeesStatusId == statusActive ||
                                                                                a.SEmployeesStatusId == statusSick))
                                && w.SEmployees.SEmployeesExecutions.Any(a =>
                                    !a.IsRemove && a.DateStart.CompareTo(DateTime.Today) <= 0 &&
                                    (a.DateStop == null || a.DateStop.Value.CompareTo(DateTime.Today) >= 0))
                                && w.DateStart.CompareTo(DateTime.Today) <= 0
                                && (w.DateStop == null || w.DateStop.Value.CompareTo(DateTime.Today) >= 0)
                                && (!roles.Any() ||
                                    w.SEmployees.SEmployeesRolesExecutors.Any(a => roles.Contains(a.SRolesExecutorId)))
                            )
                            .Select(s => new { s.SEmployeesId, s.SEmployeesJobPositionId })
                            .FirstOrDefaultAsync();

                        if (employeeSecond is null) //return NotFound(ErrorDescription.EmployeeNotFound);
                        {
                            var currentEmployee = await _employeeManager.GetFullInfoAsync();
                            request.employeeId = currentEmployee.Id;
                            jobPositionalId = currentEmployee.Job.Id;
                        }
                        else
                        {
                            request.employeeId = employeeSecond.SEmployeesId;
                            jobPositionalId = employeeSecond.SEmployeesJobPositionId;
                        }

                    }
                    else
                    {
                        request.employeeId = employeeFirst.Id;
                        jobPositionalId = employeeFirst.SEmployeesJobPositionId;
                    }
                }

                else
                {
                    jobPositionalId = await _context.SEmployeesOfficesJoins
                        .Where(w => w.SEmployeesId == request.employeeId && w.SOfficesId == request.officeId && !w.IsRemove
                                                                 && w.DateStart.CompareTo(DateTime.Today) <= 0
                                                                 && (w.DateStop == null ||
                                                                     w.DateStop.Value.CompareTo(DateTime.Today) >= 0))
                        .Select(s => s.SEmployeesJobPositionId)
                        .FirstOrDefaultAsync();
                }

                var serviceRouteNext = await _context.SRoutesStages.Select(s => new
                {
                    StageId = s.Id,
                    Name = s.StageName,
                    Comment = s.Commentt,
                    Days = s.DayExcution,
                    StatusId = s.SServicesStatusId,
                    IsDateFact = s.SServicesStatusId.HasValue ? s.SServicesStatus.IsDatefact : false
                }).FirstAsync(f => f.StageId == request.stageId);

                foreach (var ser in services)
                {
                    ser.stage.DateFact = DateTime.Now;
                    ser.stage.TimeFact = TimeOnly.FromDateTime(DateTime.Now);
                    ser.stage.CountDayFact = DateTime.Now.Subtract(ser.stage.DateAdd).Days;

                    var countDay = serviceRouteNext.StageId switch
                    {
                        stageProcessing => await _context.SCalendars
                            .Where(w => w.DateType == 1 && w.Date >= DateTime.Now)
                            .Skip(ser.service.CountDayProcessing)
                            .Select(s => s.Date.Subtract(DateTime.Now).Days)
                            .FirstOrDefaultAsync(),
                        stageOiv when ser.service.SServicesWeekId == workDays => await _context.SCalendars
                            .Where(w => w.DateType == 1 && w.Date >= DateTime.Now)
                            .Skip(ser.service.CountDayExecution + ser.service.CountDayReturn)
                            .Select(s => s.Date.Subtract(DateTime.Now).Days)
                            .FirstOrDefaultAsync(),
                        stageOiv => ser.service.CountDayExecution + ser.service.CountDayReturn,
                        _ => serviceRouteNext.Days
                    };

                    _context.Update(ser.stage);

                    await _context.AddAsync(new DServicesRoutesStage
                    {
                        Id = Guid.NewGuid(),
                        DServicesIdParent = ser.service.DServicesIdParent,
                        DServicesId = ser.service.Id,
                        DCasesId = ser.service.DCasesId,
                        DateAdd = DateTime.Now,
                        CountDayExecution = countDay,
                        DateReg = countDay != 0 ? DateTime.Now.AddDays(countDay) : null,
                        SEmployeesId = request.employeeId,
                        SEmployeesJobPositionId = jobPositionalId,
                        SOfficesId = request.officeId,
                        PassAutomatically = request.employeeId == Guid.Empty,
                        SRoutesStageId = serviceRouteNext.StageId,
                        SEmployeesIdAdd = employee.Id,
                        EmployeeFioAdd = employee.Name,
                        SEmployeesJobPositionIdAdd = employee.Job.Id
                    });

                    ser.service.SRoutesStageIdCurrent = serviceRouteNext.StageId;
                    ser.service.RoutesStageDateRegCurrent = countDay != 0 ? DateTime.Now.AddDays(countDay) : null;
                    ser.service.SEmployeesIdCurrent = request.employeeId;
                    ser.service.SEmployeesJobPositionIdCurrent = jobPositionalId;
                    ser.service.SOfficesIdCurrent = request.officeId;

                    if (serviceRouteNext.StatusId.HasValue && ser.service.SServicesStatusId != serviceRouteNext.StatusId)
                    {
                        ser.service.SServicesStatusId = serviceRouteNext.StatusId.Value;
                        if (serviceRouteNext.IsDateFact)
                        {
                            ser.service.DateFact = DateTime.Now;
                            ser.service.SEmployeesIdExecution = employee.Id;
                            ser.service.SEmployeesJobPositionIdExecution = employee.Job.Id;
                            ser.service.SOfficesIdExecution = employee.Office.Id;
                        }

                    }
                    _context.Update(ser.service);

                    if (ser.IsMdm is not null and true && office.SendMdm is not null and true && office.MdmUid is not null)
                    {
                        if (serviceRouteNext.StageId == 7)
                        {
                            var mdm = await _mdmService.MdmServiceResultsReceivedObjectsAsync(ser.service.Id, (Guid)office.MdmUid);
                            if (mdm is not null) await _context.AddRangeAsync(mdm);
                        }
                        if (serviceRouteNext.StageId == 8)
                        {
                            var isDeadLine = ser.service.DateFact.HasValue ? ser.service.DateReg < ser.service.DateFact : ser.service.DateReg < DateTime.Now;
                            var mdm = await _mdmService.MdmServiceProcessingEndedObjectsAsync(ser.service.Id, (Guid)office.MdmUid, 0, isDeadLine, ser.receivedStage);
                            if (mdm is not null) await _context.AddRangeAsync(mdm);
                        }
                        if (serviceRouteNext.StageId == 9)
                        {
                            var isDeadLine = ser.service.DateFact.HasValue ? ser.service.DateReg < ser.service.DateFact : ser.service.DateReg < DateTime.Now;
                            var mdm = await _mdmService.MdmServiceProcessingEndedObjectsAsync(ser.service.Id, (Guid)office.MdmUid, 4, isDeadLine, ser.receivedStage);
                            if (mdm is not null) await _context.AddRangeAsync(mdm);
                        }
                        if (serviceRouteNext.StageId == 11)
                        {
                            var isDeadLine = ser.service.DateFact.HasValue ? ser.service.DateReg < ser.service.DateFact : ser.service.DateReg < DateTime.Now;
                            var mdm = await _mdmService.MdmServiceProcessingEndedObjectsAsync(ser.service.Id, (Guid)office.MdmUid, 1, isDeadLine, ser.receivedStage);
                            if (mdm is not null) await _context.AddRangeAsync(mdm);
                        }
                        if (serviceRouteNext.StageId == 14)
                        {
                            var mdm = await _mdmService.MdmServiceProcessingHoldObjectsAsync(ser.service.Id, (Guid)office.MdmUid);
                            if (mdm is not null) await _context.AddRangeAsync(mdm);
                        }
                        if (serviceRouteNext.StageId == 17)
                        {
                            var isDeadLine = ser.service.DateFact.HasValue ? ser.service.DateReg < ser.service.DateFact : ser.service.DateReg < DateTime.Now;
                            var mdm = await _mdmService.MdmServiceProcessingEndedObjectsAsync(ser.service.Id, (Guid)office.MdmUid, 5, isDeadLine, ser.receivedStage);
                            if (mdm is not null) await _context.AddRangeAsync(mdm);
                        }
                        if (serviceRouteNext.StageId == 34)
                        {
                            var isDeadLine = ser.service.DateFact.HasValue ? ser.service.DateReg < ser.service.DateFact : ser.service.DateReg < DateTime.Now;
                            var mdm = await _mdmService.MdmServiceProcessingEndedObjectsAsync(ser.service.Id, (Guid)office.MdmUid, 0, isDeadLine, ser.receivedStage);
                            if (mdm is not null) await _context.AddRangeAsync(mdm);
                        }
                    }
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public async Task<(bool, int)> AddServiceStageUnclaimedAsync(ServiceStageSaveDto request)
        {
            const int stageToIssue = 6;
            var count = 0;
            try
            {
                var employee = await _employeeManager.GetFullInfoAsync();

                var services = await _context.DServices
                    .AsSplitQuery()
                    .Where(w => request.serviceId.Contains(w.Id))
                    .Select(s => new
                    {
                        service = s,
                        stage = s.DServicesRoutesStages.OrderByDescending(m => m.DateAdd).FirstOrDefault(),

                    })
                    .ToListAsync();

                var serviceRouteNext = await _context.SRoutesStages.Select(s => new
                {
                    StageId = s.Id,
                    Name = s.StageName,
                    Comment = s.Commentt,
                    Days = s.DayExcution,
                    StatusId = s.SServicesStatusId,
                    IsDateFact = s.SServicesStatusId.HasValue ? s.SServicesStatus.IsDatefact : false
                }).FirstAsync(f => f.StageId == request.stageId);

                var jobPositionalId = await _context.SEmployeesOfficesJoins
                        .Where(w => w.SEmployeesId == request.employeeId && w.SOfficesId == request.officeId && !w.IsRemove
                                                                 && w.DateStart.CompareTo(DateTime.Today) <= 0
                                                                 && (w.DateStop == null ||
                                                                     w.DateStop.Value.CompareTo(DateTime.Today) >= 0))
                        .Select(s => s.SEmployeesJobPositionId)
                        .FirstOrDefaultAsync();

                foreach (var ser in services)
                {
                    if (ser.stage != null && ser.stage.SRoutesStageId == stageToIssue)
                    {
                        ser.stage.DateFact = DateTime.Now;
                        ser.stage.TimeFact = TimeOnly.FromDateTime(DateTime.Now);
                        ser.stage.CountDayFact = DateTime.Now.Subtract(ser.stage.DateAdd).Days;

                        var countDay = serviceRouteNext.Days;

                        _context.Update(ser.stage);

                        await _context.AddAsync(new DServicesRoutesStage
                        {
                            Id = Guid.NewGuid(),
                            DServicesIdParent = ser.service.DServicesIdParent,
                            DServicesId = ser.service.Id,
                            DCasesId = ser.service.DCasesId,
                            DateAdd = DateTime.Now,
                            CountDayExecution = countDay,
                            DateReg = countDay != 0 ? DateTime.Now.AddDays(countDay) : null,
                            SEmployeesId = request.employeeId,
                            SEmployeesJobPositionId = jobPositionalId,
                            SOfficesId = request.officeId,
                            PassAutomatically = request.employeeId == Guid.Empty,
                            SRoutesStageId = serviceRouteNext.StageId,
                            SEmployeesIdAdd = employee.Id,
                            EmployeeFioAdd = employee.Name,
                            SEmployeesJobPositionIdAdd = employee.Job.Id
                        });

                        ser.service.SRoutesStageIdCurrent = serviceRouteNext.StageId;
                        ser.service.RoutesStageDateRegCurrent = countDay != 0 ? DateTime.Now.AddDays(countDay) : null;
                        ser.service.SEmployeesIdCurrent = request.employeeId;
                        ser.service.SEmployeesJobPositionIdCurrent = jobPositionalId;
                        ser.service.SOfficesIdCurrent = request.officeId;

                        if (serviceRouteNext.StatusId.HasValue && ser.service.SServicesStatusId != serviceRouteNext.StatusId)
                        {
                            ser.service.SServicesStatusId = serviceRouteNext.StatusId.Value;
                            if (serviceRouteNext.IsDateFact)
                            {
                                ser.service.DateFact = DateTime.Now;
                                ser.service.SEmployeesIdExecution = employee.Id;
                                ser.service.SEmployeesJobPositionIdExecution = employee.Job.Id;
                                ser.service.SOfficesIdExecution = employee.Office.Id;
                            }

                        }
                        _context.Update(ser.service);

                        count++;
                    }
                }

                await _context.SaveChangesAsync();
                return (true, count);
            }
            catch (Exception e)
            {
                return (false, count);
            }
        }

        public async Task<NotesAddDto?> GetServiceNoteAsync(Guid id)
        {
            var entity = await _context.DNotes
            .Select(s => new NotesAddDto
            {
                Id = s.Id,
                DCaseId = s.DCasesId,
                DateReminder = s.DateReminder,
                NotesText = s.NoteText
            }).FirstOrDefaultAsync(f => f.Id == id);

            return entity;
        }

        public async Task<bool> AddServiceNotesAsync(NotesAddSaveDto request)
        {
            try
            {
                await _context.DNotes.AddAsync(new DNote
                {
                    DCasesId = request.DCaseId,
                    DateReminder = request.DateReminder,
                    NoteText = request.NotesText,
                    SEmployeesId = request.EmployeeId
                });

                await _context.SaveChangesAsync();

                return true;
            }
            catch
            {
                return false;
            }

        }

        public async Task<bool> EditServiceNotesAsync(NotesAddSaveDto request)
        {
            try
            {
                var entity = await _context.DNotes.FindAsync(request.Id);
                if (entity == null)
                    return false;

                entity.NoteText = request.NotesText;
                entity.DateReminder = request.DateReminder;
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public async Task<bool> DeleteServiceNotesAsync(Guid id)
        {
            try
            {
                var entity = await _context.DNotes.Where(w => w.Id == id).DeleteAsync();
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }


        public async Task<List<CaseServiceDocumentsDto>> GetDocumentsByServiceIdAsync(Guid id)
        {
            return await _context.DServicesDocuments
                .AsNoTracking()
                .Where(w => w.DServicesId == id)
                .Select(s => new CaseServiceDocumentsDto
                {
                    Id = s.Id,
                    DocumentId = s.SDocumentsId,
                    DocumentName = s.SDocuments.DocumentName,
                    DocumentNeedId = s.DocumentNeeds,
                    DocumentTypeId = s.DocumentType,
                    DocumentCopyCount = s.DocumentCount,
                    IsCheck = s.IsCheck,
                    Files = s.DServicesFiles.DefaultIfEmpty().Select(file => new FileDto
                    {
                        Id = file.Id,
                        Name = file.FileName,
                        Extension = file.FileExtention,
                        Size = file.FileSize,
                        EmployeeAdd = new EmployeeDto(file.SEmployeesId, NameSplitter.GetInitials(file.EmployeeFioAdd),
                            file.SEmployeesJobPosition.JobPositionName),
                        DateAdd = file.DateAdd
                    }).ToList()
                }).ToListAsync();
        }


        public async Task<bool> AddServiceDepartamentAsync(ServicesDepartamentDto model)
        {
            try
            {
                var entity = await _context.DServices.FindAsync(model.ServiceId);
                entity.SOfficesIdProviderDepartament = model.DepartamentId;
                _context.Update(entity);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }


        public async Task<List<CaseServiceDocumentsDto>> GetDocumentsByServiceIdAsync(string id)
        {
            return await _context.DServicesDocuments
                .AsNoTracking()
                .Where(w => w.DCasesId == id && w.SDocuments.SServicesDocuments.All(a => !a.IsRemove))
                .Select(s => new CaseServiceDocumentsDto
                {
                    Id = s.Id,
                    DocumentId = s.SDocumentsId,
                    DocumentName = s.SDocuments.DocumentName,
                    DocumentNeedId = s.DocumentNeeds,
                    DocumentTypeId = s.DocumentType,
                    DocumentCopyCount = s.DocumentCount,
                    IsCheck = s.IsCheck,
                    CountCopy = s.CountCopy ?? s.CountOriginal.GetValueOrDefault(),
                    CountOriginal = s.CountOriginal ?? s.CountOriginal.GetValueOrDefault()
                })
                .ToListAsync();
        }

        public async Task<List<CaseServiceDocumentsDto>> GetDocumentsIssuanceByServiceIdAsync(string id)
        {
            List<CaseServiceDocumentsDto> documents = new();

            documents.AddRange(await _context.DServicesDocuments
                .AsNoTracking()
                .Where(w => w.DCasesId == id && w.SDocuments.SServicesDocuments.All(a => !a.IsRemove) && w.IsCheck)
                .Select(s => new CaseServiceDocumentsDto
                {
                    Id = s.Id,
                    DocumentName = s.SDocuments.DocumentName,
                    IsCheck = s.IsIssued,
                    CountCopy = s.CountCopy ?? s.CountCopy.GetValueOrDefault(),
                    CountOriginal = s.CountOriginal ?? s.CountOriginal.GetValueOrDefault()
                })
                .ToListAsync());

            documents.AddRange(await _context.DServicesDocumentsResults
                .AsNoTracking()
                .Where(w => w.DCasesId == id && w.SDocuments.SServicesDocumentsResults.All(a => !a.IsRemove))
                .Select(s => new CaseServiceDocumentsDto
                {
                    Id = s.Id,
                    DocumentName = s.SDocuments.DocumentName,
                    IsCheck = s.IsIssued,
                    CountCopy = s.CountCopy ?? s.CountCopy.GetValueOrDefault(),
                    CountOriginal = s.CountOriginal ?? s.CountOriginal.GetValueOrDefault()
                })
                .ToListAsync());

            return documents;
        }


        public async Task<List<CaseServiceDocumentsResultsDto>> GetDocumentsResultsByServiceIdAsync(Guid id)
        {
            return await _context.DServicesDocumentsResults
                .AsNoTracking()
                .Where(w => w.DServicesId == id)
                .Select(s => new CaseServiceDocumentsResultsDto
                {
                    Id = s.Id,
                    DocumentId = s.SDocumentsId,
                    DocumentName = s.SDocuments.DocumentName,
                    Method = s.DocumentMethod,
                    PeriodMfc = s.DocumentPeriodMfc,
                    PeriodProvider = s.DocumentPeriodProvider,
                    Result = s.DocumentResult,
                    Files = s.DServicesFileResults.DefaultIfEmpty().Select(file => new FileDto
                    {
                        Id = file.Id,
                        Name = file.FileName,
                        Extension = file.FileExtention,
                        Size = file.FileSize,
                        EmployeeAdd = new EmployeeDto(file.SEmployeesId, NameSplitter.GetInitials(file.EmployeeFioAdd),
                            file.SEmployeesJobPosition.JobPositionName),
                        DateAdd = file.DateAdd
                    }).ToList()
                })
                .ToListAsync();
        }

        public async Task<bool> UploadServicesDocumentsIsCheckAsync(Guid id, bool isCheck)
        {
            try
            {
                var document = await _context.DServicesDocuments.FindAsync(id);
                if (document == null) return false;

                document.IsCheck = isCheck;

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UploadServicesDocumentsIsCheckAsync(List<DocumentsPrintDto> doc)
        {
            try
            {
                var listId = doc.Select(s => s.Id).ToList();

                var documents = await _context.DServicesDocuments.Where(w => listId.Contains(w.Id)).ToListAsync();

                var d = new DocumentsPrintDto();

                documents.ForEach(f =>
                {
                    d = doc.First(x => f.Id == x.Id);
                    f.IsCheck = d.IsChecked;
                    f.CountCopy = d.CountCopy;
                    f.CountOriginal = d.CountOriginal;
                });

                _context.UpdateRange(documents);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public async Task<bool> UploadServicesDocumentsIssuanceIsCheckAsync(List<DocumentsPrintDto> doc)
        {
            try
            {
                var listId = doc.Select(s => s.Id).ToList();

                var documents = await _context.DServicesDocuments.Where(w => listId.Contains(w.Id)).ToListAsync();

                var d = new DocumentsPrintDto();

                documents.ForEach(f =>
                {
                    d = doc.First(x => f.Id == x.Id);
                    f.IsIssued = doc.First(x => f.Id == x.Id).IsChecked;
                    f.CountCopy = d.CountCopy;
                    f.CountOriginal = d.CountOriginal;
                });

                var documentsResult = await _context.DServicesDocumentsResults.Where(w => listId.Contains(w.Id)).ToListAsync();

                documentsResult.ForEach(f =>
                {
                    d = doc.First(x => f.Id == x.Id);
                    f.IsIssued = d.IsChecked;
                    f.CountCopy = d.CountCopy;
                    f.CountOriginal = d.CountOriginal;
                });

                _context.UpdateRange(documents);
                _context.UpdateRange(documentsResult);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }


        public async Task<ServiceFileResponseModel> UploadServicesFileAsync(Guid id, DocumentFileAddType fileAddType, IFormFile file)
        {
            var response = new ServiceFileResponseModel().SetFileId(Guid.NewGuid());
            var employee = await _employeeManager.GetFullInfoAsync();
            var document = await _context.DServicesDocuments.Select(s => new
            {
                s.Id,
                s.DCasesId,
                s.SDocumentsId,
                s.DServicesId,
                s.DServices.DServicesIdParent
            }).SingleOrDefaultAsync(sd => sd.Id == id);

            var office = await _context.SOffices.Select(s => new { s.Id, ftp = s.SFtpId.HasValue ? s.SFtp : null })
                .SingleOrDefaultAsync(sd => sd.Id == employee.Office.Id);

            if (office is null || office.ftp is null)
                return response.Error("ФТП сервер не настроен").Build();

            var ftp = new FtpSettingsModel
            {
                Server = office.ftp.FtpServer,
                Login = office.ftp.FtpLogin,
                Password = office.ftp.FtpPassword
            };

            if (await _ftpService.UploadFileAsync(file,
                    $"{document.DCasesId}/{response.Id}{Path.GetExtension(file.FileName)}", ftp) == FtpStatus.Failed)
                return response.Error("Не удалось загрузить файл на ФТП").Build();

            var newFile = new DServicesFile
            {
                Id = response.Id,
                DServicesDocumentsId = document.Id,
                DCasesId = document.DCasesId,
                SFtpId = office.ftp.Id,
                FileName = Path.GetFileNameWithoutExtension(file.FileName),
                FileExtention = Path.GetExtension(file.FileName),
                FileSize = (int)file.Length,
                TypeAddFile = (int)fileAddType,
                DateAdd = DateTime.Now,
                SEmployeesId = employee.Id,
                SOfficesId = employee.Office.Id,
                SEmployeesJobPositionId = employee.Job.Id,
                EmployeeFioAdd = employee.Name
            };
            await _context.DServicesFiles.AddAsync(newFile);
            await _context.SaveChangesAsync();
            return response.Success().Build();
        }

        public async Task<ServiceFileResponseModel> UploadServicesFilesAsync(Guid id, DocumentFileAddType fileAddType, IFormFileCollection files)
        {
            var response = new ServiceFileResponseModel();
            var employee = await _employeeManager.GetFullInfoAsync();
            var document = await _context.DServicesDocuments.Select(s => new
            {
                s.Id,
                s.DCasesId,
                s.SDocumentsId,
                s.DServicesId,
                s.DServices.DServicesIdParent
            }).SingleOrDefaultAsync(sd => sd.Id == id);

            var office = await _context.SOffices.Select(s => new { s.Id, ftp = s.SFtpId.HasValue ? s.SFtp : null })
                .SingleOrDefaultAsync(sd => sd.Id == employee.Office.Id);

            if (office is null && office?.ftp is null)
                return response.Error("ФТП сервер не настроен").Build();

            var ftp = new FtpSettingsModel
            {
                Server = office.ftp.FtpServer,
                Login = office.ftp.FtpLogin,
                Password = office.ftp.FtpPassword
            };


            foreach (var file in files)
            {
                response.SetFileId(Guid.NewGuid());
                if (await _ftpService.UploadFileAsync(file,
                        $"{document.DCasesId}/{response.Id}{Path.GetExtension(file.FileName)}", ftp) == FtpStatus.Failed)
                    return response.Error("Не удалось загрузить файл на ФТП").Build();

                var newFile = new DServicesFile
                {
                    Id = response.Id,
                    DServicesDocumentsId = document.Id,
                    DCasesId = document.DCasesId,
                    SFtpId = office.ftp.Id,
                    FileName = Path.GetFileNameWithoutExtension(file.FileName),
                    FileExtention = Path.GetExtension(file.FileName),
                    FileSize = (int)file.Length,
                    TypeAddFile = (int)fileAddType,
                    DateAdd = DateTime.Now,
                    SEmployeesId = employee.Id,
                    SOfficesId = employee.Office.Id,
                    SEmployeesJobPositionId = employee.Job.Id,
                    EmployeeFioAdd = employee.Name
                };
                await _context.DServicesFiles.AddAsync(newFile);
            }

            await _context.SaveChangesAsync();
            return response.Success().Build();
        }

        public async Task<ServiceFileResponseModel> RemoveServicesFileAsync(Guid id)
        {
            var responseModel = new ServiceFileResponseModel();
            var file = await _context.DServicesFiles.FindAsync(id);
            if (file is null) return responseModel.Error(ErrorDescription.FileNotFound).Build();
            _context.DServicesFiles.Remove(file);
            await _context.SaveChangesAsync();
            return responseModel.Success().Build();
        }

        public async Task<ServiceFileResponseModel> UploadServicesFileResultAsync(Guid id, DocumentFileAddType fileAddType, IFormFile file)
        {
            var response = new ServiceFileResponseModel().SetFileId(Guid.NewGuid());
            var employee = await _employeeManager.GetFullInfoAsync();
            var document = await _context.DServicesDocumentsResults.Select(s => new
            {
                s.Id,
                s.DCasesId,
                s.SDocumentsId,
                s.DServicesId,
                s.DServices.DServicesIdParent,
            }).SingleOrDefaultAsync(sd => sd.Id == id);

            var office = await _context.SOffices.Select(s => new { s.Id, ftp = s.SFtpId.HasValue ? s.SFtp : null })
                .SingleOrDefaultAsync(sd => sd.Id == employee.Office.Id);

            if (office is null && office?.ftp is null)
                return response.Error("ФТП сервер не настроен").Build();

            var ftp = new FtpSettingsModel
            {
                Server = office.ftp.FtpServer,
                Login = office.ftp.FtpLogin,
                Password = office.ftp.FtpPassword
            };

            if (await _ftpService.UploadFileAsync(file,
                    $"{document.DCasesId}/{response.Id}{Path.GetExtension(file.FileName)}", ftp) == FtpStatus.Failed)
                return response.Error("Не удалось загрузить файл на ФТП").Build();

            var newFile = new DServicesFileResult()
            {
                Id = response.Id,
                DServicesDocumentResultId = document.Id,
                DCasesId = document.DCasesId,
                SFtpId = office.ftp.Id,
                FileName = Path.GetFileNameWithoutExtension(file.FileName),
                FileExtention = Path.GetExtension(file.FileName),
                FileSize = (int)file.Length,
                TypeAddFile = (int)fileAddType,
                DateAdd = DateTime.Now,
                SEmployeesId = employee.Id,
                SOfficesId = employee.Office.Id,
                SEmployeesJobPositionId = employee.Job.Id,
                EmployeeFioAdd = employee.Name
            };
            await _context.DServicesFileResults.AddAsync(newFile);
            await _context.SaveChangesAsync();
            return response.Success().Build();
        }

        public async Task<ServiceFileResponseModel> RemoveServicesFileResultAsync(Guid id)
        {
            var responseModel = new ServiceFileResponseModel();
            var file = await _context.DServicesFileResults.FindAsync(id);
            if (file is null) return responseModel.Error(ErrorDescription.FileNotFound).Build();
            _context.DServicesFileResults.Remove(file);
            await _context.SaveChangesAsync();
            return responseModel.Success().Build();
        }

        public async Task<FormFile> DownloadServicesFileAsync(Guid id, Models.DocumentType type)
        {
            dynamic serviceFile = type switch
            {
                Models.DocumentType.ServiceDocument => await _context.DServicesFiles.Include(i => i.SFtp).SingleAsync(s => s.Id == id),
                Models.DocumentType.ServiceDocumentResult => await _context.DServicesFileResults.Include(i => i.SFtp).SingleAsync(s => s.Id == id),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };

            var ftp = new FtpSettingsModel
            {
                Server = serviceFile.SFtp.FtpServer,
                Login = serviceFile.SFtp.FtpLogin,
                Password = serviceFile.SFtp.FtpPassword
            };

            var bytes = await _ftpService.DownloadFileAsync(
                $"{serviceFile.DCasesId}/{serviceFile.Id}{serviceFile.FileExtention}", ftp);

            if (bytes is null) return null;

            var file = new FormFile(new MemoryStream(bytes), 0, serviceFile.FileSize,
                $"{serviceFile.FileName}{serviceFile.FileExtention}",
                $"{serviceFile.FileName}{serviceFile.FileExtention}")
            {
                Headers = new HeaderDictionary(),
                ContentType = MimeTypes.GetMimeType($"{serviceFile.FileName}{serviceFile.FileExtention}")
            };
            return file;
        }

        public async Task<List<CaseCommentsDto>> GetCommentsByCaseIdAsync(string id)
        {
            var employeeId = await _employeeManager.GetIdAsync();
            await _context.SEmployeesJobPositions.LoadAsync();
            return await _context.DServicesCommentts
                .Include(i => i.SEmployees)
                .AsNoTracking()
                .Where(w => w.IsRemove == false && w.DCasesId == id)
                .Select(s => new CaseCommentsDto
                {
                    Id = s.Id,
                    Comment = s.Commentt,
                    DateAdd = s.DateAdd,
                    EmployeeAdd = new EmployeeDto(s.SEmployeesId, s.SEmployees.EmployeeName,
                        s.SEmployeesJobPosition.JobPositionName),
                    IsLeft = s.SEmployeesId != employeeId.Value
                })
                .ToListAsync();
        }

        public async Task<CaseNotificationsDto> GetNotificationsByCaseIdAsync(string id)
        {
            var employeeId = await _employeeManager.GetIdAsync();
            await _context.SEmployeesJobPositions.LoadAsync();

            CaseNotificationsDto result = new();

            result.CaseApplicantPhoneDtos.AddRange(await _context.DServicesCustomers.Where(w => w.DCasesId == id).Select(s => new CaseApplicantPhoneDto
            {
                Id = s.Id,
                ApplicantName = s.Fio(),
                Phone1 = s.CustomerPhone1,
                Phone2 = s.CustomerPhone2,
                CustomerType = (CustomerType)s.CustomerType

            }).ToListAsync());

            result.NotificationsDtos.AddRange(await _context.DServicesCustomersCalls.Where(w => w.DCasesId == id).Select(s => new NotificationsDto
            {
                Id = s.Id,
                CustomerFio = s.CustomerName,
                CustomerPhone = s.CustomerPhone,
                Period = s.TimeTalk,
                DateAdd = s.DateCall,
                EmployeeAdd = new EmployeeDto(s.SEmployeesId, s.SEmployees.EmployeeName,
                        s.SEmployeesJobPosition.JobPositionName),
                Type = 1
            }).ToListAsync());

            result.NotificationsDtos.AddRange(await _context.DServicesCustomersMessages.Where(w => w.DCasesId == id).Select(s => new NotificationsDto
            {
                Id = s.Id,
                CustomerFio = s.CustomerName,
                CustomerPhone = s.CustomerPhone,
                DateAdd = s.DateAdd,
                EmployeeAdd = new EmployeeDto(s.SEmployeesId, s.SEmployees.EmployeeName,
                        s.SEmployeesJobPosition.JobPositionName),
                TextMessage = s.TextMessage,
                //Status = s.Status,
                Type = 2
            }).ToListAsync());

            return result;
        }

        public async Task<string> AddNotificationsByCaseIdAsync(string caseid, Guid customerId, string tel)
        {
            try
            {
                var employees = await _employeeManager.GetFullInfoAsync();

                var data_services_customer_info = _context.DServicesCustomers.First(f => f.Id == customerId);
                var service = _context.DServices.Where(w => w.DCasesId == caseid && w.DServicesDocumentIdParent == Guid.Empty).Select(s => new { s.Id, s.SOfficesIdAdd, s.SOfficesIdAddNavigation.SFtpId }).First();
                string new_phone = "8" + tel.Trim(new Char[] { ' ', '+', '-' }).Remove(0, 1);
                string _testCALL = $"{{\"caseNumber\":\"{caseid}\",\"idEmployee\":\"{employees.Id}\",\"employeeFio\":\"{employees.Name}\",\"customerName\":\"{data_services_customer_info.Fio()}\",\"customerPhone\":\"{new_phone}\",\"callDuration\":\"0\",\"idService\":\"{service.Id}\",\"idFtp\":\"{service.SFtpId}\",\"idMfc\":\"{service.SOfficesIdAdd}\",\"callType\":\"{1}\",\"command\":\"" + 1 + "\"}";
                return _testCALL;
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> SaveSMSCaseAsync(CasesSMSSaveDto request, EmployeeInfo employee)
        {
            try
            {
                await _context.AddAsync(new DServicesCustomersMessage
                {
                    DCasesId = request.CasesId,
                    CustomerName = request.CustomerName,
                    CustomerPhone = request.CustomerPhone,
                    TextMessage = request.Text,
                    Status = 0,
                    SEmployeesId = employee.Id,
                    SEmployeesJobPositionId = employee.Job.Id,
                    SOfficesId = employee.Office.Id
                });
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<CaseCommentsDto> AddCommentAsync(string id, string comment, Guid[] employeesId)
        {
            var employee = await _employeeManager.GetFullInfoAsync();
            var model = new DServicesCommentt
            {
                DCasesId = id,
                SEmployeesId = employee.Id,
                SEmployeesJobPositionId = employee.Job.Id,
                SOfficesId = employee.Office.Id,
                Commentt = comment
            };
            if (employeesId.Any())
            {
                model.DServicesCommenttEmployees.AddRange(employeesId.Select(employeeId => new DServicesCommenttEmployee
                {
                    SEmployeesId = employeeId
                }));
                model.IsUnicast = true;
            }

            await _context.AddWithAudit(model, employee).SaveChangesAsync();

            return new CaseCommentsDto
            {
                Id = model.Id,
                Comment = model.Commentt,
                DateAdd = model.DateAdd,
                EmployeeAdd = new EmployeeDto(employee.Id, employee.Name, employee.Job.Name)
            };
        }

        public async Task<List<CaseAuditDto>> GetServiceAuditByCaseIdAsync(string id)
        {

            var response = new List<CaseAuditDto>();

            response.AddRange(await _context.DCasesViews
                .AsNoTracking()
                .Where(w => w.DCasesId == id)
                .Select(s => new CaseAuditDto
                {
                    DateAdd = s.DateAdd,
                    Changed = new ChangedAudit { Description = "Просмотр" },
                    Employee = new EmployeeDto(s.SEmployeesId, s.EmployeesName,
                        s.JobPositionName, s.OfficeName)
                })
                .ToListAsync());

            response.AddRange(await _context.DServicesAudits
                .AsNoTracking()
                .Where(w => w.DCasesId == id)
                .Select(s => new CaseAuditDto
                {
                    DateAdd = s.DateAdd,
                    Changed = JsonConvert.DeserializeObject<ChangedAudit>(s.Changed),
                    Employee = new EmployeeDto(s.SEmployeesId, s.SEmployees.EmployeeName,
                                                    s.SEmployeesJobPosition.JobPositionName, s.SOffices.OfficeName)
                })
                .ToListAsync());

            return response;
        }

        public async Task<(Guid ServiceId, string? FormPath)> GetAdditionalFormByCaseIdAsync(Guid id)
        {
            var service = await _context.DServices
                .Select(s => new
                {
                    s.Id,
                    s.DCasesId,
                    s.DServicesIdParent,
                    s.SServicesProcedure.ExtraFormPath
                }).FirstAsync(f => f.Id == id);
            return (service.Id, service.ExtraFormPath);
        }

        public async Task<string> GetAdditionalDataStringByServiceIdAsync(Guid id)
        {
            var service = await _context.DServices
                .Select(s => new
                {
                    s.Id,
                    s.ExtraInfo
                }).FirstAsync(f => f.Id == id);
            return service.ExtraInfo;
        }

        public async Task SaveAdditionalData(Guid id, string data, Dictionary<string, string> search)
        {
            try
            {
                var model = await _context.DServices.FindAsync(id) ?? throw new InvalidOperationException("Данные не найдены!");
                model.ExtraInfo = data;

                if (search.Any())
                {
                    if (!string.IsNullOrEmpty(model.SearchString))
                    {
                        var dict = model.SearchString.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries)
                       .Select(part => part.Split('='))
                       .ToDictionary(split => split[0], split => split[1]);

                        var newDict = search.Union(dict.Where(w => !search.ContainsKey(w.Key))).ToDictionary(k => k.Key, v => v.Value);

                        if (newDict.Any())
                        {
                            model.SearchString = string.Join("|", newDict.Select(pair => $"{pair.Key}={pair.Value}").ToArray());
                        }
                    }
                    else
                    {
                        model.SearchString = string.Join("|", search.Select(pair => $"{pair.Key}={pair.Value}").ToArray());
                    }
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception e)
            {
                var a = 5;
            }
        }

        public async Task<T?> GetAdditionalDataByServiceIdAsync<T>(Guid id)
        {
            var data = await GetAdditionalDataStringByServiceIdAsync(id);
            if (data is null)
            {
                return default(T);
            }
            return JsonConvert.DeserializeObject<T>(data, new IsoDateTimeConverter { DateTimeFormat = "dd.MM.yyyy" });
        }

        public async Task<List<CaseServiceBlank>?> GetServiceBlanksByCaseIdAsync(Guid id)
        {
            return await _context.DServices
                .Where(f => f.Id == id)
                .Select(s => s.SServices.SServicesForms.Where(w => !w.IsRemove && (!w.SServicesProcedureId.HasValue || w.SServicesProcedureId == s.SServicesProcedureId))
                                                       .Select(ss => new CaseServiceBlank
                                                       {
                                                           Id = ss.Id,
                                                           ServiceId = s.Id,
                                                           Name = ss.FileName,
                                                           Expansion = ss.FileExpansion,
                                                           Comment = ss.Commentt
                                                       }).ToList())
                .FirstOrDefaultAsync();
        }

        public async Task<CaseServiceBlankFile> GetServiceBlankByIdAsync(Guid id)
        {
            var file = await _context.SServicesForms.FindAsync(id);
            if (file is null) throw new InvalidOperationException("Бланк не найден");
            return new CaseServiceBlankFile
            {
                Id = file.Id,
                Name = file.FileName,
                Expansion = file.FileExpansion,
                Content = file.File,
                Size = file.FileSize
            };
        }

        public async Task<List<CaseServiceBlank>?> GetServiceFilesByCaseIdAsync(Guid id)
        {
            return await _context.DServices
              .Where(f => f.Id == id)
              .Select(s => s.SServices.SServicesFiles.Where(w => !w.IsRemove && (!w.SServicesProcedureId.HasValue || w.SServicesProcedureId == s.SServicesProcedureId)).Select(ss => new CaseServiceBlank
              {
                  Id = ss.Id,
                  ServiceId = s.Id,
                  Name = ss.FileName,
                  Expansion = ss.FileExpansion,
                  Comment = ss.Commentt
              }).ToList())
              .FirstOrDefaultAsync();
        }

        public async Task<CaseServiceBlankFile> GetServiceFileByIdAsync(Guid id)
        {
            var file = await _context.SServicesFiles.FindAsync(id);
            if (file is null) throw new InvalidOperationException("Файл не найден");
            return new CaseServiceBlankFile
            {
                Id = file.Id,
                Name = file.FileName,
                Expansion = file.FileExpansion,
                Content = file.File,
                Size = file.FileSize
            };
        }


        public byte[] GetBlancFastReportFileAsync(byte[] content, Guid serviceId, string connection)
        {
            RegisteredObjects.AddConnection(typeof(FastReport.Data.PostgresDataConnection));

            var rep = new WebReport();
            rep.Report.Load(new MemoryStream(content));
            rep.Report.SetParameterValue("ServiceId", serviceId.ToString());
            if (rep.Report.Dictionary.Connections.Count != 0)
            {
                rep.Report.Dictionary.Connections[0].ConnectionString = connection;
            }

            var isPrepare = rep.Report.Prepare();

            if (!isPrepare) throw new Exception("Ошибка при создание бланка");

            var pdfExport = new PDFExport
            {
                ShowProgress = false,
                Subject = "Subject",
            };
            var strm = new MemoryStream();
            rep.Report.Export(pdfExport, strm);
            rep.Report.Dispose();
            pdfExport.Dispose();
            strm.Position = 0;

            return strm.ToArray();
        }


        public async Task<byte[]?> GetPrintProcessingPersonalData(string caseId, string connection, BlankActionType type)
        {

            var service = await _context.DServices.FirstOrDefaultAsync(f => f.DCasesId == caseId && f.DServicesIdParent == Guid.Empty);

            var form = await _context.SForms.FindAsync(2);
            var content = form.File;

            RegisteredObjects.AddConnection(typeof(FastReport.Data.PostgresDataConnection));

            var rep = new WebReport();
            rep.Report.Load(new MemoryStream(content));
            rep.Report.SetParameterValue("ServiceId", service.Id.ToString());
            if (rep.Report.Dictionary.Connections.Count != 0)
            {
                rep.Report.Dictionary.Connections[0].ConnectionString = connection;
            }

            var isPrepare = rep.Report.Prepare();

            if (!isPrepare) throw new Exception("Ошибка при создание бланка");

            var strm = new MemoryStream();

            switch (type)
            {
                case BlankActionType.Pdf:
                    var pdfExport = new PDFExport();
                    rep.Report.Export(pdfExport, strm);
                    pdfExport.Dispose();
                    break;
                case BlankActionType.Word:
                    var wordExport = new Word2007Export();
                    rep.Report.Export(wordExport, strm);
                    wordExport.Dispose();
                    break;
                default: break;
            }

            rep.Report.Dispose();

            return strm.ToArray();

        }

        public async Task<byte[]?> GetPrintConsultationService(string caseId, string connection, Guid? Id, BlankActionType type)
        {
            var service = await _context.DServices.FirstOrDefaultAsync(f => f.DCasesId == caseId && f.DServicesIdParent == Guid.Empty);
            var mfcName = await _context.SOffices.Where(w => w.Id == Id).Select(s => s.OfficeName).SingleOrDefaultAsync();

            var form = await _context.SForms.FindAsync(6);
            var content = form.File;

            RegisteredObjects.AddConnection(typeof(FastReport.Data.PostgresDataConnection));

            var rep = new WebReport();
            rep.Report.Load(new MemoryStream(content));
            rep.Report.SetParameterValue("ServiceId", service.SServicesId.ToString());
            rep.Report.SetParameterValue("MfcName", mfcName);
            if (rep.Report.Dictionary.Connections.Count != 0)
            {
                rep.Report.Dictionary.Connections[0].ConnectionString = connection;
            }

            var isPrepare = rep.Report.Prepare();

            if (!isPrepare) throw new Exception("Ошибка при создание бланка");

            var strm = new MemoryStream();

            switch (type)
            {
                case BlankActionType.Pdf:
                    var pdfExport = new PDFExport();
                    rep.Report.Export(pdfExport, strm);
                    pdfExport.Dispose();
                    break;
                case BlankActionType.Word:
                    var wordExport = new Word2007Export();
                    rep.Report.Export(wordExport, strm);
                    wordExport.Dispose();
                    break;
                default: break;
            }

            rep.Report.Dispose();

            return strm.ToArray();
        }

        public async Task<byte[]?> GetPrintAcceptDocuments(string caseId, string connection, Guid? id, BlankActionType type)
        {
            try
            {
                var form = await _context.SForms.FindAsync(3);
                var content = form.File;

                var employee = await _context.SEmployees.Where(w => w.Id == id).Select(s => s.EmployeeName).SingleOrDefaultAsync();

                RegisteredObjects.AddConnection(typeof(FastReport.Data.PostgresDataConnection));

                var rep = new WebReport();
                rep.Report.Load(new MemoryStream(content));
                rep.Report.SetParameterValue("ServiceId", caseId);
                rep.Report.SetParameterValue("EmployeeName", employee);
                if (rep.Report.Dictionary.Connections.Count != 0)
                {
                    rep.Report.Dictionary.Connections[0].ConnectionString = connection;
                }

                var isPrepare = rep.Report.Prepare();

                if (!isPrepare) throw new Exception("Ошибка при создание бланка");

                var strm = new MemoryStream();

                switch (type)
                {
                    case BlankActionType.Pdf:
                        var pdfExport = new PDFExport();
                        rep.Report.Export(pdfExport, strm);
                        pdfExport.Dispose();
                        break;
                    case BlankActionType.Word:
                        var wordExport = new Word2007Export();
                        rep.Report.Export(wordExport, strm);
                        wordExport.Dispose();
                        break;
                    default: break;
                }

                rep.Report.Dispose();

                return strm.ToArray();
            }
            catch
            {
                return null;
            }
        }
        public async Task<byte[]?> GetPrintIssuanceDocuments(string caseId, string connection, string employeeName, BlankActionType type)
        {
            try
            {
                var form = await _context.SForms.FindAsync(4);
                var content = form.File;

                RegisteredObjects.AddConnection(typeof(FastReport.Data.PostgresDataConnection));

                var rep = new WebReport();
                rep.Report.Load(new MemoryStream(content));
                rep.Report.SetParameterValue("ServiceId", caseId);
                rep.Report.SetParameterValue("EmployeeFio", employeeName);
                if (rep.Report.Dictionary.Connections.Count != 0)
                {
                    rep.Report.Dictionary.Connections[0].ConnectionString = connection;
                }

                var isPrepare = rep.Report.Prepare();

                if (!isPrepare) throw new Exception("Ошибка при создание бланка");

                var strm = new MemoryStream();

                switch (type)
                {
                    case BlankActionType.Pdf:
                        var pdfExport = new PDFExport();
                        rep.Report.Export(pdfExport, strm);
                        pdfExport.Dispose();
                        break;
                    case BlankActionType.Word:
                        var wordExport = new Word2007Export();
                        rep.Report.Export(wordExport, strm);
                        wordExport.Dispose();
                        break;
                    default: break;
                }

                rep.Report.Dispose();

                return strm.ToArray();
            }
            catch
            {
                return null;
            }
        }


        public byte[] GetBlancDocxFileAsync(byte[] content, Guid serviceId)
        {
            try
            {
                Dictionary<string, string> marks = GetBlankMarks(serviceId);
                if (marks == null) throw new Exception("Ошибка при создание бланка");

                MemoryStream stream = new MemoryStream();
                stream.Write(content, 0, content.Length);
                stream.Seek(0, SeekOrigin.Begin);

                using (WordprocessingDocument document = WordprocessingDocument.Open(stream, true, new OpenSettings { AutoSave = true }))
                {
                    Body documentBody = document.MainDocumentPart.Document.Body;
                    List<Paragraph> paragraphsWithMarks = documentBody.Descendants<Paragraph>().Where(x => Regex.IsMatch(x.InnerText, @".*\[+\w+(\&?\d)?\].*")).ToList();
                    foreach (Paragraph paragraph in paragraphsWithMarks)
                    {
                        foreach (Match markMatch in Regex.Matches(paragraph.InnerText, @"\[+\w+(\&?\d)?\]", RegexOptions.Compiled))
                        {
                            string paragraphMarkValue = markMatch.Value.Trim(new[] { '[', ']' });
                            string markValueFromCollection;
                            var markMatchSplit = paragraphMarkValue.Split('&'); // разбиваем метку в бланке                            
                            if (markMatchSplit.Length < 2) // проверяем на наличие метки которую необходимо заполнить побуквенно
                            {
                                if (marks.TryGetValue(paragraphMarkValue, out markValueFromCollection))
                                {
                                    string editedParagraphText = paragraph.InnerText.Replace(markMatch.Value, markValueFromCollection);
                                    paragraph.RemoveAllChildren<Run>();
                                    paragraph.AppendChild(new Run(new Text(editedParagraphText)));
                                }
                            }
                            else
                            {
                                if (marks.TryGetValue(markMatchSplit[0], out markValueFromCollection))
                                {
                                    if (int.TryParse(markMatchSplit[1], out int apex))
                                    {
                                        var markChar = markValueFromCollection?.Length >= int.Parse(markMatchSplit[1]) ? markValueFromCollection.ToCharArray()[(int.Parse(markMatchSplit[1]) - 1)].ToString() : "";
                                        string editedParagraphText = paragraph.InnerText.Replace(markMatch.Value, markChar);
                                        paragraph.RemoveAllChildren<Run>();
                                        paragraph.AppendChild(new Run(new Text(editedParagraphText)));
                                    }

                                }
                            }
                        }
                    }
                }

                stream.Position = 0;
                return stream.ToArray();
            }
            catch (Exception ex)
            {
                throw new Exception("Ошибка при создание бланка");
            }
        }

        public Dictionary<string, string> GetBlankMarks(Guid id)
        {
            try
            {
                return _context.BlanksMarks
                    .FromSqlInterpolated($"select * from core.blank_info_select({id})")
                    .ToDictionary(keySelector: d => d.Name.Trim(new Char[] { '[', ']' }), elementSelector: d => d.Value);
            }
            catch (Exception e)
            {
                return null;
            }

        }

        //TODO переделать
        public async Task<ApplicantsDto?> GetCustomerByDocumentSerialAndNumberAsync(string serial, string number)
        {
            return await _context.DServicesCustomers.AsNoTracking().OrderByDescending(o => o.DateAdd)
                .Select(s => new ApplicantsDto
                {
                    Id = s.Id,
                    DCasesId = s.DCasesId,
                    SDocumentsIdentityId = s.SDocumentsIdentityId,
                    CustomerGender = s.CustomerGender,
                    DocumentNumber = s.DocumentNumber,
                    DocumentSerial = s.DocumentSerial,
                    DocumentBirthDate = s.DocumentBirthDate,
                    DocumentBirthPlace = s.DocumentBirthPlace,
                    DocumentIssueDate = s.DocumentIssueDate,
                    DocumentIssueBody = s.DocumentIssueBody,
                    DocumentCode = s.DocumentCode,
                    CustomerInn = s.CustomerInn,
                    CustomerPhone1 = s.CustomerPhone1,
                    CustomerPhone2 = s.CustomerPhone2,
                    CustomerEmail = s.CustomerEmail,
                    CustomerAddress = s.CustomerAddress,
                    CustomerOkato = s.CustomerOkato,
                    CustomerOktmo = s.CustomerOktmo,
                    CustomerCodeRegion = s.CustomerCodeRegion,
                    CustomerSnils = s.CustomerSnils,
                    CustomerType = s.CustomerType,
                    PollRegionSms = s.PollRegionSms,
                    PollIasMkgu = s.PollIasMkgu,
                    SAlertCustomerId = s.SAlertCustomerId,
                    FirstName = s.FirstName,
                    LastName = s.LastName,
                    SecondName = s.SecondName,
                    CustomerAddressResidence = s.CustomerAddressResidence,
                    DateTempRegistration = s.DateTempRegistration,
                    CustomerAddressTemp = s.CustomerAddressTemp,
                    addressDetails = JsonConvert.DeserializeObject<AddressDetails>(s.CustomerAddressDetail)
                })
                .FirstOrDefaultAsync(f => f.DocumentSerial == serial && f.DocumentNumber == number);
        }

        //TODO переделать
        public async Task<LegassDto?> GetLegalByInnAsync(string inn)
        {
            return await _context.DServicesCustomersLegals.AsNoTracking().OrderByDescending(o => o.DateAdd)
                .Select(s => new LegassDto
                {
                    Id = s.Id,
                    SServicesCustomerTypeId = s.SServicesCustomerTypeId,
                    CustomerInn = s.CustomerInn,
                    CustomerOkato = s.CustomerOkato,
                    CustomerOktmo = s.CustomerOktmo,
                    CustomerCodeRegion = s.CustomerCodeRegion,
                    CustomerKpp = s.CustomerKpp,
                    CustomerOgrn = s.CustomerOgrn,
                    CustomerAddress = s.CustomerAddress,
                    CustomerName = s.CustomerName,
                    CustomerAddressDetail = s.CustomerAddressDetail,
                    HeadPosition = s.HeadPosition,
                    CustomerPhone1 = s.CustomerPhone1,
                    CustomerPhone2 = s.CustomerPhone2,
                    CustomerEmail = s.CustomerEmail,
                    HeadFirstName = s.HeadFirstName,
                    HeadLastName = s.HeadLastName,
                    HeadSecondName = s.HeadSecondName
                })
                .FirstOrDefaultAsync(f => f.CustomerInn == inn);
        }


        public async Task<List<ApplicantDto>> GetCustomersByCaseNumberAsync(string caseId)
        {
            return await _context.DServicesCustomers.AsNoTracking().Where(w => w.DCasesId == caseId)
                .Select(s => new ApplicantDto
                {
                    Id = s.Id,
                    Name = s.Fio()
                })
                .ToListAsync();
        }

        //TODO переделать
        public async Task<CustomerModelDto?> GetCustomerByIdAsync(Guid id)
        {
            try
            {
                var model = await _context.DServicesCustomers.FirstOrDefaultAsync(f => f.Id == id);
                if (model is null) throw new InvalidOperationException("Данные не найдены");


                var response = _mapper.Map<CustomerModelDto>(model);
                response.AddressDetails = string.IsNullOrEmpty(model.CustomerAddressDetail) ? null : JsonConvert.DeserializeObject<AddressDetails>(model.CustomerAddressDetail);

                //var responce = new CustomerModelDto()
                //{
                //    Id = model.Id,
                //    DCasesId = model.DCasesId,
                //    SDocumentsIdentityId = model.SDocumentsIdentityId,
                //    CustomerGender = model.CustomerGender,
                //    DocumentNumber = model.DocumentNumber,
                //    DocumentSerial = model.DocumentSerial,
                //    DocumentBirthDate = model.DocumentBirthDate,
                //    DocumentBirthPlace = model.DocumentBirthPlace,
                //    DocumentIssueDate = model.DocumentIssueDate,
                //    DocumentIssueBody = model.DocumentIssueBody,
                //    DocumentCode = model.DocumentCode,
                //    CustomerInn = model.CustomerInn,
                //    CustomerPhone1 = model.CustomerPhone1,
                //    CustomerPhone2 = model.CustomerPhone2,
                //    CustomerEmail = model.CustomerEmail,
                //    CustomerAddress = model.CustomerAddress,
                //    CustomerOkato = model.CustomerOkato,
                //    CustomerOktmo = model.CustomerOktmo,
                //    CustomerCodeRegion = model.CustomerCodeRegion,
                //    CustomerSnils = model.CustomerSnils,
                //    CustomerType = model.CustomerType,
                //    PollRegionSms = model.PollRegionSms,
                //    PollIasMkgu = model.PollIasMkgu,
                //    SAlertCustomerId = model.SAlertCustomerId,
                //    SEmployeesId = model.SEmployeesId,
                //    SOfficesId = model.SOfficesId,
                //    SEmployeesJobPositionId = model.SEmployeesJobPositionId,
                //    DateAdd = model.DateAdd,
                //    CustomerAddressDetail = model.CustomerAddressDetail,
                //    FirstName = model.FirstName,
                //    LastName = model.LastName,
                //    SecondName = model.SecondName,
                //    CustomerAddressResidence = model.CustomerAddressResidence,
                //    DateTempRegistration = model.DateTempRegistration,
                //    CustomerAddressTemp = model.CustomerAddressTemp,
                //    AddressDetails = string.IsNullOrEmpty(model.CustomerAddressDetail) ? null : JsonConvert.DeserializeObject<AddressDetails>(model.CustomerAddressDetail)
                //};

                return response;
            }
            catch (Exception e)
            {
                return null;
            }

        }

        //TODO переделать
        public async Task<DServicesCustomersLegal?> GetCustomerLegalByIdAsync(Guid id)
        {
            return await _context.DServicesCustomersLegals.FirstOrDefaultAsync(f => f.Id == id);
        }

        //TODO переделать
        public async Task<CaseCustomerResponseModel> AddCustomerAsync(DServicesCustomer customer)
        {
            try
            {
                await _context.AddAsync(customer);
                await _context.SaveChangesAsync();
                return new CaseCustomerResponseModel().Success().Build();
            }
            catch (Exception e)
            {
                return new CaseCustomerResponseModel().Error("").Build();
            }
        }

        //TODO переделать
        public async Task<CaseCustomerResponseModel> UpdateCustomerAsync(CustomerModelDto customer)
        {
            var response = new CaseCustomerResponseModel();
            try
            {
                var employee = await _employeeManager.GetFullInfoAsync();

                var updatedCustomer = await _context.DServicesCustomers.FindAsync(customer.Id);
                if (updatedCustomer is null) return response.Error("Заявитель не найден.").Build();
                var dServices = await _context.DServices.FirstAsync(f => f.DCasesId == updatedCustomer.DCasesId && f.DServicesIdParent == Guid.Empty);

                updatedCustomer.FirstName = customer.FirstName;
                updatedCustomer.LastName = customer.LastName;
                updatedCustomer.SecondName = customer.SecondName;
                updatedCustomer.CustomerAddress = customer.CustomerAddress;
                updatedCustomer.CustomerAddressDetail = customer.CustomerAddressDetail;
                updatedCustomer.CustomerAddressResidence = customer.CustomerAddressResidence;
                updatedCustomer.CustomerAddressTemp = customer.CustomerAddressTemp;
                updatedCustomer.CustomerCodeRegion = customer.CustomerCodeRegion;
                updatedCustomer.CustomerEmail = customer.CustomerEmail;
                updatedCustomer.CustomerPhone1 = customer.CustomerPhone1;
                updatedCustomer.CustomerPhone2 = customer.CustomerPhone2;
                updatedCustomer.CustomerGender = customer.CustomerGender;
                updatedCustomer.CustomerInn = customer.CustomerInn;
                updatedCustomer.CustomerSnils = customer.CustomerSnils;
                updatedCustomer.DocumentIssueDate = customer.DocumentIssueDate;
                updatedCustomer.DocumentBirthDate = customer.DocumentBirthDate;
                updatedCustomer.DocumentBirthPlace = customer.DocumentBirthPlace;
                updatedCustomer.DocumentCode = customer.DocumentCode;
                updatedCustomer.DocumentIssueBody = customer.DocumentIssueBody;
                updatedCustomer.DocumentNumber = customer.DocumentNumber;
                updatedCustomer.DocumentSerial = customer.DocumentSerial;
                updatedCustomer.SDocumentsIdentityId = customer.SDocumentsIdentityId;
                updatedCustomer.CustomerAddressDetail = JsonConvert.SerializeObject(customer.AddressDetails);

                dServices.CustomerName = customer.Fio();
                dServices.CustomerAddress = customer.CustomerAddress;
                dServices.CustomerPhone1 = customer.CustomerPhone1;
                dServices.CustomerPhone2 = customer.CustomerPhone2;

                _context.UpdateWithAudit(updatedCustomer, employee);
                await _context.SaveChangesAsync();
                return response.Success().Build();
            }
            catch (Exception e)
            {
                return response.Error("").Build();
            }
        }

        //TODO переделать
        public async Task<CaseCustomerResponseModel> UpdateCustomerLegalAsync(DServicesCustomersLegal customer)
        {
            var response = new CaseCustomerResponseModel();
            try
            {
                var employee = await _employeeManager.GetFullInfoAsync();

                var updatedCustomer = await _context.DServicesCustomersLegals.FindAsync(customer.Id);
                if (updatedCustomer is null) return response.Error("Заявитель не найден.").Build();
                var dServices = await _context.DServices.FirstAsync(f => f.DCasesId == updatedCustomer.DCasesId && f.DServicesIdParent == Guid.Empty);

                updatedCustomer.HeadLastName = customer.HeadLastName;
                updatedCustomer.HeadFirstName = customer.HeadFirstName;
                updatedCustomer.HeadSecondName = customer.HeadSecondName;
                updatedCustomer.CustomerAddress = customer.CustomerAddress;
                updatedCustomer.CustomerAddressDetail = customer.CustomerAddressDetail;
                updatedCustomer.CustomerCodeRegion = customer.CustomerCodeRegion;
                updatedCustomer.CustomerEmail = customer.CustomerEmail;
                updatedCustomer.CustomerPhone1 = customer.CustomerPhone1;
                updatedCustomer.CustomerPhone2 = customer.CustomerPhone2;
                updatedCustomer.CustomerInn = customer.CustomerInn;
                updatedCustomer.CustomerOgrn = customer.CustomerOgrn;
                updatedCustomer.CustomerOkato = customer.CustomerOkato;
                updatedCustomer.CustomerOktmo = customer.CustomerOktmo;
                updatedCustomer.CustomerKpp = customer.CustomerKpp;
                updatedCustomer.HeadPosition = customer.HeadPosition;
                updatedCustomer.CustomerName = customer.CustomerName;

                dServices.CustomerName = customer.CustomerName;
                dServices.CustomerAddress = customer.CustomerAddress;
                dServices.CustomerPhone1 = customer.CustomerPhone1;
                dServices.CustomerPhone2 = customer.CustomerPhone2;

                _context.UpdateWithAudit(updatedCustomer, employee);
                await _context.SaveChangesAsync();
                return response.Success().Build();
            }
            catch (Exception e)
            {
                return response.Error("").Build();
            }
        }



    }
}