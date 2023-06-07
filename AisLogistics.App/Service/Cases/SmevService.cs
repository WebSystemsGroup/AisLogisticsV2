using System.Linq;
using AisLogistics.App.Extensions;
using AisLogistics.App.Models.DTO.Services;
using AisLogistics.App.Service.MDM;
using AisLogistics.App.Utils;
using AisLogistics.App.ViewModels.Cases;
using AisLogistics.DataLayer.Concrete;
using AisLogistics.DataLayer.Entities.Models;
using AisLogistics.DataLayer.Utils;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.EntityFrameworkCore;
using SmevGate.Client.Core;
using SmevRouter;

namespace AisLogistics.App.Service
{
    public class SmevService: ISmevService
    {
        private readonly IMdmService _mdmService;
        private readonly AisLogisticsContext _context;
        private readonly EmployeeManager _employeeManager;

        public SmevService(AisLogisticsContext context, EmployeeManager employeeManager, IMdmService mdmService)
        {
            _context = context;
            _employeeManager = employeeManager;
            _mdmService = mdmService;
        }

        public string GetSmevFormById(int id)
        {
            var request = _context.SSmevRequests.Find(id);
            if (String.IsNullOrWhiteSpace(request.Path)) throw new Exception("Null");
            return request.SmevFormPath();
        }

        public CaseServiceSmevRequestDto GetSmevRequestById(Guid id)
        {
            return _context.DServicesSmevRequests
                .Select(s => new CaseServiceSmevRequestDto()
                {
                    Id = s.Id,
                    CountDayExecution = s.CountDayExecution,
                    DateAdd = s.DateAdd,
                    DateReg = s.DateReg,
                    DateRequest = s.DateFact,
                    Service = new ServiceDto(s.DServicesId, s.DServices.SServices.ServiceNameSmall,
                        s.DServices.SServices.SOffice.OfficeNameSmall, s.DServices.DateAdd),
                    Employee = new EmployeeDto(s.SEmployeesId, NameSplitter.GetInitials(s.EmployeeFioAdd),
                        s.SEmployeesJobPosition.JobPositionName)
                }).First(f => f.Id == id);
        }

        public CaseServiceSmevRequestDto GetArchiveSmevRequestById(Guid id)
        {
            return _context.AServicesSmevRequests
                .Select(s => new CaseServiceSmevRequestDto()
                {
                    Id = s.Id,
                    CountDayExecution = s.CountDayExecution,
                    DateAdd = s.DateAdd,
                    DateReg = s.DateReg.Value,
                    DateRequest = s.DateFact,
                    Service = new ServiceDto(s.AServicesId, s.AServices.SServices.ServiceNameSmall,
                        s.AServices.SServices.SOffice.OfficeNameSmall, s.AServices.DateAdd),
                    Employee = new EmployeeDto(s.SEmployeesId, NameSplitter.GetInitials(s.EmployeeFioAdd),
                        s.SEmployeesJobPosition.JobPositionName)
                }).First(f => f.Id == id);
        }

        public SmevDetailsResponseModel GetSmevResponse(Guid id)
        {
            var smevResponse = SmevClient.GetRequestResult(new SmevRequestData
            {
                DataServicesRequestSmevId = id.ToString()
            });
            var responseModel = new SmevDetailsResponseModel();

            if (smevResponse.ResponseReports is null) return responseModel.ResponseError(smevResponse.Fault?.ErrorText);

            var report = smevResponse.ResponseReports.First(s =>
                s.ReportType is ResponseReportType.Full || smevResponse.ResponseReports.Length < 2 &&
                s.ReportType is ResponseReportType.IntermediateResponse); // файл полного ответа

            responseModel.AddDocument(report.PdfDocument);
            responseModel.AddRequestInformation(GetSmevRequestById(id));

            var attachmentsReport = smevResponse.ExtractedResponseDocuments.FirstOrDefault();

            if (attachmentsReport is null) return responseModel;

            string attachments;
            if (attachmentsReport.MimeType == MimeTypeMap.GetMimeType("zip") ||
                attachmentsReport.MimeType == MimeTypeMap.GetMimeType("rar"))
            {
                Stream zipStream = new MemoryStream(attachmentsReport.FileContent) { Position = 0 };
                var pdfFromZip = ZipDecompress(zipStream).FirstOrDefault(f => f.Key.ToLower().Contains(".pdf")).Value;
                attachments = Convert.ToBase64String(pdfFromZip);
            }
            else
            {
                attachments = Convert.ToBase64String(attachmentsReport.FileContent);
            }

            return responseModel;
        }

        public SmevDetailsResponseModel GetArchiveSmevResponse(Guid id)
        {
            var smevResponse = SmevClient.GetRequestResult(new SmevRequestData
            {
      
                DataServicesRequestSmevId = id.ToString()
            });
            var responseModel = new SmevDetailsResponseModel();

            if (smevResponse.ResponseReports is null) return responseModel.ResponseError(smevResponse.Fault?.ErrorText);

            var report = smevResponse.ResponseReports.First(s =>
                s.ReportType is ResponseReportType.Full || smevResponse.ResponseReports.Length < 2 &&
                s.ReportType is ResponseReportType.IntermediateResponse); // файл полного ответа

            responseModel.AddDocument(report.PdfDocument);
            responseModel.AddRequestInformation(GetArchiveSmevRequestById(id));

            var attachmentsReport = smevResponse.ExtractedResponseDocuments.FirstOrDefault();

            if (attachmentsReport is null) return responseModel;

            string attachments;
            if (attachmentsReport.MimeType == MimeTypeMap.GetMimeType("zip") ||
                attachmentsReport.MimeType == MimeTypeMap.GetMimeType("rar"))
            {
                Stream zipStream = new MemoryStream(attachmentsReport.FileContent) { Position = 0 };
                var pdfFromZip = ZipDecompress(zipStream).FirstOrDefault(f => f.Key.ToLower().Contains(".pdf")).Value;
                attachments = Convert.ToBase64String(pdfFromZip);
            }
            else
            {
                attachments = Convert.ToBase64String(attachmentsReport.FileContent);
            }

            return responseModel;
        }

        public async Task<string> CreateNewSmevRequestAsync(Guid servicesId, int smevId, string comment, string idRef = null)
        {
            const int workDays = 1;
            var date = DateTime.Today;
            var currentEmployee = await _employeeManager.GetFullInfoAsync();

            //var service = await _context.FindAsync<DService>(servicesId);

            var service = await _context.DServices.Select(s => new {s.Id, s.DCasesId, s.SServices.IsMdm }).FirstOrDefaultAsync(f=>f.Id == servicesId); 
            if (service is null) throw new Exception("Null");

            var smev = await _context.FindAsync<SSmevRequest>(smevId);
            if (smev is null) throw new Exception("Null");

            var office = await _context.SOffices.Where(w => w.Id == currentEmployee.Office.Id).Select(s => new { s.SendMdm, s.MdmUid }).FirstAsync();

            //var dataReg =  DateTime.Now.AddDays(smev.CountDayExecution);
            var dataReg = date.AddDays(smev.CountDayExecution);

            if (smev.SServicesWeekId == workDays)
            {
                var calendar = await _context.SCalendars.Where(w => w.DateType == 1 && w.Date >= date).Select(s => new { s.Date })
                    .Skip(smev.CountDayExecution).FirstOrDefaultAsync();
                if (calendar is null) throw new Exception("Null");

                dataReg = calendar.Date;
            }

            var model = new DServicesSmevRequest
            {
                DServicesId = service.Id,
                DCasesId = service.DCasesId,
                SEmployeesId = currentEmployee.Id,
                SOfficesId = currentEmployee.Office.Id,
                EmployeeFioAdd = currentEmployee.Name,
                SEmployeesJobPositionId = currentEmployee.Job.Id,
                Commentt = comment,
                SServicesWeekId = smev.SServicesWeekId,
                SSmevRequestId = smev.Id,
                CountDayExecution = smev.CountDayExecution,
                DateReg = dataReg,
                RequestIdRef = idRef
            };

            if (service.IsMdm is not null and true && office.SendMdm is not null and true && office.MdmUid is not null)
            {
                var mdm = await _mdmService.MdmQueryObjectsAsync(service.Id, (Guid)office.MdmUid);
                if (mdm is not null) await _context.AddRangeAsync(mdm);
            }

            await _context.AddAsync(model);
            await _context.SaveChangesAsync();

            return model.Id.ToString();
        }

        #region Utils

        private static Dictionary<string, byte[]> ZipDecompress(Stream stream) => new ZipFile(stream)
            .Cast<ZipEntry>().ToDictionary(zEntry => zEntry.Name, zEntry => zEntry.ExtraData);

        #endregion
    }
}
