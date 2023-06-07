using AisLogistics.App.Models;
using AisLogistics.App.Service;
using AisLogistics.App.Settings;
using AisLogistics.App.ViewModels.Cases;
using AisLogistics.App.ViewModels.ModelBuilder;
using GarService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using SmevGate.Client.Core;
using SmevRouter;
using System.ServiceModel;
using AisLogistics.DataLayer.Concrete;
using AisLogistics.DataLayer.Utils;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using System.Xml;
using System.Net;
using NuGet.Protocol.Plugins;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace AisLogistics.App.Controllers.Smev
{
    [Authorize]
    [ServiceContract]
    public partial class SmevBaseController : AbstractController
    {
        protected readonly ISmevService _smevService;
        protected readonly ICaseService _caseService;
        protected readonly IGarService _garService;

        public SmevBaseController(ICaseService caseService, ISmevService smevService, IGarService garService,
            IOptions<SmevSettings> smevOptions)
        {
            _caseService = caseService;
            _smevService = smevService;
            _garService = garService;
            SmevClient.Init(smevOptions.Value.SmevConnection, smevOptions.Value.AuthCode);
        }

        protected SmevBaseController(ICaseService caseService, EmployeeManager employeeManager,
            IOptions<SmevSettings> smevOptions) : base(employeeManager)
        {
            _caseService = caseService;
            SmevClient.Init(smevOptions.Value.SmevConnection, smevOptions.Value.AuthCode);
        }


        public IActionResult? GetSmevDetailsModal(Guid id)
        {
            var response = _smevService.GetSmevResponse(id);
            if (response.ResponseStatus.IsError)
            {
                ShowError(response.ResponseStatus.Message ?? "Произошла неизвестная ошибка");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    response.ResponseStatus.Message ?? "Произошла неизвестная ошибка");
            }

            var modelBuilder = new ModalViewModelBuilder()
                .AddModalTitle("Информация о СМЭВ запросе")
                .AddModalType(ModalType.FULL)
                .AddModel(response)
                .HideSubmitButton()
                .AddModalViewPath("~/Views/Cases/Details/Modals/_ModalSmevDetails.cshtml");
            return ModalLayoutView(modelBuilder);
        }

        public IActionResult? GetArchiveSmevDetailsModal(Guid id)
        {
            var response = _smevService.GetArchiveSmevResponse(id);
            if (response.ResponseStatus.IsError)
            {
                ShowError(response.ResponseStatus.Message ?? "Произошла неизвестная ошибка");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    response.ResponseStatus.Message ?? "Произошла неизвестная ошибка");
            }

            var modelBuilder = new ModalViewModelBuilder()
                .AddModalTitle("Информация о СМЭВ запросе")
                .AddModalType(ModalType.FULL)
                .AddModel(response)
                .HideSubmitButton()
                .AddModalViewPath("~/Views/Archive/Details/Modals/_ModalSmevDetails.cshtml");
            return ModalLayoutView(modelBuilder);
        }

        public async Task<IActionResult> DownloadFile(Guid id, DocumentType type)
        {
            var file = await _caseService.DownloadServicesFileAsync(id, type);
            if (file is null) return NotFound();
            return File(file.OpenReadStream(), file.ContentType, file.FileName);
        }

        /// <summary>
        /// Получение справочника
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public JsonResult SmevGetDictionary(DictionaryType type)
        {
            try
            {
                return Json(SmevClient.GetDictionary(type));
            }
            catch (Exception e)
            {
                return Json(new { error = e.Message });
            }
        }

        public IActionResult GetSmevForm(SmevCreateResponseModel model)
        {
            model.AddApplicants(_caseService.GetApplicantsByServiceId(model.ServiceId));
            return PartialView(_smevService.GetSmevFormById(model.SmevId), model);
        }

        #region Поиск адресов в КЛАДР

        /// <summary>
        /// Поиск адреса в КЛАДР
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public JsonResult KladrSearchAddress(string request)
        {
            try
            {
                return Json(SmevClient.SearchKladr(request));
            }
            catch (Exception e)
            {
                return Json(new { error = e.Message });
            }
        }

        #endregion


        #region Поиск адресов в ГАР

        /// <summary>
        /// Поиск адреса в ГАР
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public JsonResult GarSearchAddressExtended(GarFullTextSearchRequestData request)
        {
            try
            {
                return Json(_garService.SearchAddressExtended(request));
            }
            catch (Exception e)
            {
                return Json(new { error = e.Message });
            }
        }

        #endregion

        #region Attachments

        /// <summary>
        /// Запрос скачивание XML-логов запроса
        /// </summary>
        /// <param name="id">Индетификатор СМЭВ запроса услуги</param>>
        /// <returns></returns>
        public async Task<IActionResult> Smev_SaveXml(Guid id)
        {
            SmevRequestData request = new() { DataServicesRequestSmevId = Convert.ToString(id) };
            var logTypes = SmevClient.GetXmlLogType(request);
            MemoryStream ms = new();
            ZipOutputStream zipStream = new(ms) { UseZip64 = UseZip64.Off };
            zipStream.SetLevel(9);
            logTypes.ForEach(fe =>
            {
                ZipEntry newEntry = new($"{fe}.xml") { DateTime = DateTime.Now, IsUnicodeText = true };
                zipStream.PutNextEntry(newEntry);
                XmlDocument xml = new();
                xml.LoadXml(SmevClient.GetXml(request, fe));
                Stream stream = new MemoryStream();
                xml.Save(stream);
                stream.Position = 0;
                StreamUtils.Copy(stream, zipStream, new byte[4096]);
                stream.Dispose();
                zipStream.CloseEntry();
                zipStream.IsStreamOwner = false;
            });

            await zipStream.DisposeAsync();

            return File(ms.ToArray(), MimeTypeMap.GetMimeType(".zip"), $"{id}.zip");
        }

        /// <summary>
        /// Запрос скачивание документов прикрепленных к запросу
        /// </summary>
        /// <param name="id">Индетификатор СМЭВ запроса услуги</param>>
        /// <returns></returns>
        public async Task<IActionResult> Smev_SaveRequestAttachments(Guid id)
        {
            SmevRequestData request = new() { DataServicesRequestSmevId = Convert.ToString(id) }; // запрос
            var documents = SmevClient.GetRequestAttachments(request);
            List<string> name = new();
            MemoryStream ms = new();
            ZipOutputStream zipStream = new(ms) { UseZip64 = UseZip64.Off };
            zipStream.SetLevel(9);
            documents.ForEach(fe =>
            {
                var filename = fe.FileName;
                if (name.Contains(filename)) filename = $"copy_{filename}";
                ZipEntry newEntry = new($"{filename}") { DateTime = DateTime.Now, IsUnicodeText = true };
                zipStream.PutNextEntry(newEntry);
                Stream stream = new MemoryStream(fe.FileContent);
                StreamUtils.Copy(stream, zipStream, new byte[4096]);
                stream.Dispose();
                zipStream.CloseEntry();
                zipStream.IsStreamOwner = false;
                name.Add(filename);
            });

            await zipStream.DisposeAsync();

            return File(ms.ToArray(), MimeTypeMap.GetMimeType(".zip"), "RequestAttachments.zip");
        }

        /// <summary>
        /// Запрос скачивание вложений прикрепленных к ответу
        /// </summary>
        /// <param name="id">Индетификатор СМЭВ запроса услуги</param>>
        /// <returns></returns>
        public async Task<IActionResult> Smev_SaveResponseAttachments(Guid id)
        {
            SmevRequestData request = new() { DataServicesRequestSmevId = Convert.ToString(id) }; // запрос
            var documents = SmevClient.GetResponseAttachments(request);
            List<string> name = new();
            MemoryStream ms = new();
            ZipOutputStream zipStream = new(ms) { UseZip64 = UseZip64.Off };
            zipStream.SetLevel(9);
            documents.ForEach(fe =>
            {
                var filename = fe.FileName;
                if (name.Contains(filename)) filename = $"copy_{filename}";
                ZipEntry newEntry = new($"{filename}") { DateTime = DateTime.Now, IsUnicodeText = true };
                zipStream.PutNextEntry(newEntry);
                Stream stream = new MemoryStream(fe.FileContent);
                StreamUtils.Copy(stream, zipStream, new byte[4096]);
                stream.Dispose();
                zipStream.CloseEntry();
                zipStream.IsStreamOwner = false;
                name.Add(filename);
            });

            var documentsAdditioanlForms = SmevClient.GetAdditioanlForms(request);

            documentsAdditioanlForms.ForEach(fe =>
            {
                var filename = fe.Name;
                if (name.Contains(filename)) filename = $"copy_{filename}";
                ZipEntry newEntry = new($"{filename}.pdf") { DateTime = DateTime.Now, IsUnicodeText = true };
                zipStream.PutNextEntry(newEntry);
                Stream stream = new MemoryStream(fe.PdfDocument);
                StreamUtils.Copy(stream, zipStream, new byte[4096]);
                stream.Dispose();
                zipStream.CloseEntry();
                zipStream.IsStreamOwner = false;
                name.Add(filename);
            });

            await zipStream.DisposeAsync();

            return File(ms.ToArray(), MimeTypeMap.GetMimeType(".zip"), "RequestAttachments.zip");
        }

        /// <summary>
        /// Запрос скачивание дополнительных форм прикрепленных к ответу
        /// </summary>
        /// <param name="id">Индетификатор СМЭВ запроса услуги</param>>
        /// <returns></returns>
        public async Task<IActionResult> Smev_SaveResponseAttachmentsForms(Guid id)
        {
            SmevRequestData request = new() { DataServicesRequestSmevId = Convert.ToString(id) }; // запрос
            var documents = SmevClient.GetAdditioanlForms(request);
            List<string> name = new();
            MemoryStream ms = new();
            ZipOutputStream zipStream = new(ms) { UseZip64 = UseZip64.Off };
            zipStream.SetLevel(9);
            documents.ForEach(fe =>
            {
                var filename = fe.Name;
                if (name.Contains(filename)) filename = $"copy_{filename}";
                ZipEntry newEntry = new($"{filename}.pdf") { DateTime = DateTime.Now, IsUnicodeText = true };
                zipStream.PutNextEntry(newEntry);
                Stream stream = new MemoryStream(fe.PdfDocument);
                StreamUtils.Copy(stream, zipStream, new byte[4096]);
                stream.Dispose();
                zipStream.CloseEntry();
                zipStream.IsStreamOwner = false;
                name.Add(filename);
            });

            await zipStream.DisposeAsync();

            return File(ms.ToArray(), MimeTypeMap.GetMimeType(".zip"), "ResponseAttachmentsDopForms.zip");
        }

        #endregion

        #region Utils

        [NonAction]
        protected ActionResult<string> SmevResponse<T>(T response) where T : SmevResponseData
        {
            if (response.ResponseReports is null || response.ResponseReports.Length == 0) 
                return BadRequest(response.Fault?.ErrorText);

            return response.ResponseReports
                .Where(w => w.ReportType == ResponseReportType.Full)
                .Select(s => "data:application/pdf;base64," + Convert.ToBase64String(s.PdfDocument))
                .First();
        }

        #endregion
    }
}