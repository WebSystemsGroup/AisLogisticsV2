using AisLogistics.App.Extensions;
using AisLogistics.App.Models;
using AisLogistics.App.Models.DTO.Information;
using AisLogistics.DataLayer.Concrete;
using AisLogistics.DataLayer.Entities.Models;
using DataTables.AspNet.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NUglify.Helpers;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using Z.EntityFramework.Plus;
using static AisLogistics.App.Models.AdditionalForms.PfrEightToSeventeenModel;

namespace AisLogistics.App.Service
{
    public class InformationManager
    {
        private readonly AisLogisticsContext _context;
        private readonly EmployeeManager _employeeManager;

        public InformationManager(AisLogisticsContext context, EmployeeManager employeeManager)
        {
            _context = context;
            _employeeManager = employeeManager;
        }

        public async Task<List<EmployeeInformationDto>> GetInformationListAsync(int count)
        {
            var userId = await _employeeManager.GetIdAsync();
            var information = await _context.DInformations.AsNoTracking()
                .Select(s => new EmployeeInformationDto
                {
                    Id = s.Id,
                    Type = s.SInformationType.TypeName,
                    Topic = s.InformationTopic,
                    Text = s.InformationText,
                    DateAdd = s.DateAdd,
                    DateStop = s.DateStop,
                    DateStart = s.DateStart,
                    IsRemove = s.IsRemove,
                    EmployeeFio = s.EmployeesFioAdd,
                    EmployeeJob = s.EmployeesJobPositionAdd,
                    IsAttachment = s.DInformationFiles.Any(),
                    ForMe = s.DInformationRecipients.Any(a => a.SEmployeesId == userId)
                }).OrderBy(o => o.DateAdd)
                .Where(w => w.IsRemove == false && w.ForMe && w.DateStart <= DateTime.Today &&
                            (w.DateStop.HasValue == false || w.DateStop >= DateTime.Now)).ToListAsync();
            return information;
        }

        public async Task<InformationDetails?> GetInformationDetailssAsync(Guid id)
        {
            return await _context.DInformations.AsNoTracking()
                .Select(s => new InformationDetails
                {
                    Id = s.Id,
                    Type = s.SInformationType.TypeName,
                    Topic = s.InformationTopic,
                    Text = s.InformationText,
                    DateAdd = s.DateAdd,
                    EmployeeFio = s.EmployeesFioAdd,
                    EmployeeJob = s.EmployeesJobPositionAdd,
                    Files = s.DInformationFiles.Select(ss=> new FileDto {Id=ss.Id, FileName = ss.FileName, FileExtensions = ss.FileExtention, FileSize = ss.FileSize}).ToList()
                })
                .FirstOrDefaultAsync(f=>f.Id==id);
        }

        public async Task<EmployeeWarningDto> GetWarningRousteStageListAsync()
        {
            try
            {
                var userId = await _employeeManager.GetIdAsync();

                var date = DateTime.Now;

                var response = new EmployeeWarningDto();

                var list = await _context.DServicesRoutesStages
                    .AsNoTracking()
                    .Where(w => w.SEmployeesId == userId &&
                    !w.DateFact.HasValue &&
                    w.DateReg.HasValue &&
                    w.DateReg <= date.AddDays(3))
                    .Select(s => new
                    {
                        s.DCasesId,
                        s.DServices.SServices.ServiceName,
                        CustomerName = s.DCases.DServicesCustomersLegals.Count == 0 ? s.DCases.DServicesCustomers.Where(w => w.CustomerType == (int)CustomerType.Applicant).Select(ss => ss.Fio()).First() :
                                                                              s.DCases.DServicesCustomers.Where(w => w.CustomerType == (int)CustomerType.Representative).Select(ss => ss.Fio()).First(),
                        s.SRoutesStage.StageName,

                        days = s.DateReg.Value.Date.Subtract(date.Date).Days
                    }).ToListAsync();

                response.Expired.AddRange(list.Where(w => w.days < 0).Select(s => new StageInfo { ServiceName = s.ServiceName, CaseId = s.DCasesId, StageName = s.StageName, ApplicantName = s.CustomerName }).ToList());
                response.Lastday.AddRange(list.Where(w => w.days == 1||w.days==0).Select(s => new StageInfo { ServiceName = s.ServiceName, CaseId = s.DCasesId, StageName = s.StageName, ApplicantName = s.CustomerName }).ToList());
                response.LessThreeDays.AddRange(list.Where(w => w.days > 1 && w.days <= 3).Select(s => new StageInfo { ServiceName = s.ServiceName, CaseId = s.DCasesId, StageName = s.StageName, ApplicantName = s.CustomerName }).ToList());

                return response;
            }
            catch (Exception ex)
            {
                return new EmployeeWarningDto();
            }
        }


        public async Task<List<EmployeeRatingDto>> GetEmployeeRatingAsync(IDataTablesRequest? request, bool isAll)
        {
            List<EmployeeRatingDto> response = new List<EmployeeRatingDto>();
            try
            {
                var date = DateTime.Now.AddDays(-1);
                int i = 1;
                List<int> indicators = new List<int>() {1,25,29 };

                var data = _context.DIndicatorsValues
                     .Where(w => w.Month == date.Month &&
                              w.Year == date.Year &&
                              indicators.Contains(w.SIndicatorsId))
                    .Join(_context.SEmployees, indicators => indicators.SEmployeesId, b => b.Id, (indicators, b) => new { indicators.SEmployeesId, indicators.SOfficesId, indicators.IndicatorValue, b.EmployeeName })
                    .Join(_context.SOffices, c => c.SOfficesId, d => d.Id, (c, d) => new { c.SEmployeesId, c.SOfficesId, c.IndicatorValue, c.EmployeeName, d.OfficeName })
                    .Select(s => new { s.SEmployeesId, s.EmployeeName, s.OfficeName, s.SOfficesId, s.IndicatorValue })
                    .GroupBy(g => g.SEmployeesId)
                    .Select(s => new EmployeeRatingDto
                    {
                        EmployeeId = s.Key.Value,
                        EmployeeName = s.First().EmployeeName,
                        OfficessName = s.First().OfficeName,
                        OfficessId = s.First().SOfficesId.Value,
                        EmployeePoint = (int)s.Sum(s => s.IndicatorValue),
                    })
                    .OrderByDescending(o => o.EmployeePoint);
                    

                if (isAll == false)
                {
                   response = await data.Take(10).ToListAsync();
                }
                else
                {
                    response = await data.ToListAsync();
                }

                response.ForEach(f => f.PositionId = i++);

                if(request!=null)
                {
                    Guid? office = null;
                    var MfcIdColumn = request?.Columns?.Where(w => w.Name == "mfc").Select(s => s.Search.Value).FirstOrDefault();
                    if (MfcIdColumn != null) office = new Guid(MfcIdColumn);

                    if(office != null)
                    {
                        response.RemoveAll(r=>r.OfficessId != office.Value);
                    }

                    if (string.IsNullOrEmpty(request.Search.Value)==false)   response.RemoveAll(x => !x.EmployeeName.Contains(request.Search.Value, StringComparison.OrdinalIgnoreCase));
                }

                return response;
            }
            catch (Exception ex)
            {
                return response;
            }
        }


        public async Task<List<EmployeeNotesDto>> GetNotesListAsync()
        {
            try
            {
                var userId = await _employeeManager.GetIdAsync();
                string? applicantName = null;
                List<EmployeeNotesDto> notesEmployee = new();
                var notes = await _context.DNotes.Where(w => w.SEmployeesId == userId).OrderByDescending(o => o.DateAdd).ToListAsync();
                var isView = false;

                if (notes.Any())
                {
                    foreach (var note in notes)
                    {
                        if (note.DCasesId is not null)
                        {
                            applicantName = await _context.DCases.Where(w => w.Id == note.DCasesId)
                                .Select(s => s.DServicesCustomersLegals.Count != 0 ? s.DServicesCustomersLegals.First().CustomerName : s.DServicesCustomers.First(f => f.CustomerType == (int)CustomerType.Applicant).Fio())
                                .FirstOrDefaultAsync();
                        }
                        notesEmployee.Add(new EmployeeNotesDto
                        {
                            Id = note.Id,
                            ApplicantName = applicantName ?? string.Empty,
                            DateAdd = note.DateAdd,
                            DateReminder = note.DateReminder,
                            DCasesId = note.DCasesId ?? string.Empty,
                            NoteText = note.NoteText,
                            IsViewed = note.IsViewed
                        });

                        if (!note.DateReminder.HasValue || note.DateReminder.Value != DateTime.Now) continue;
                        note.IsViewed = true;
                        isView = true;
                        _context.Update(note);
                    }

                    if(isView) await _context.SaveChangesAsync();
                }
                return notesEmployee;
            }
            catch (Exception ex)
            {
                return new List<EmployeeNotesDto>();
            }
        }
    }
}
