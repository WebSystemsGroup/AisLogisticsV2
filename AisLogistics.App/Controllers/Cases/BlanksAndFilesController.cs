using AisLogistics.App.Models.DTO.Services;
using AisLogistics.App.ViewModels.ModelBuilder;
using AisLogistics.DataLayer.Utils;

namespace AisLogistics.App.Controllers.Cases;

public partial class CasesController
{
    public async Task<IActionResult> BlanksListModal(Guid id)
    {
        var blanks = await _caseService.GetServiceBlanksByCaseIdAsync(id) ?? new List<CaseServiceBlank>();

        var modelBuilder = new ViewModelBuilder()
               .AddViewTitle("Бланки")
               .AddModel(blanks);
        return PartialView("Details/Modals/_ModalBlanksList", modelBuilder);

    }

    public async Task<IActionResult> FilesListModal(Guid id)
    {
        var blanks = await _caseService.GetServiceFilesByCaseIdAsync(id) ?? new List<CaseServiceBlank>(); ;

        var modelBuilder = new ViewModelBuilder()
               .AddViewTitle("Файлы")
               .AddModel(blanks);
        return PartialView("Details/Modals/_ModalFilesList", modelBuilder);
    }

    //TODO полностью переделать, временно 
    public async Task<IActionResult> DownloadBlank(Guid id, Guid? serviceId)
    {
        try
        {
            var file = await _caseService.GetServiceBlankByIdAsync(id);
            if (file is null) return NotFound();

            if (file.Expansion.ToLower() == ".frx")
            {
                var connection = _configuration.GetConnectionString("DefaultConnection");
                var content = _caseService.GetBlancFastReportFileAsync(file.Content, (Guid)serviceId, connection);
                return PdfLayoutView(content);
            }
            else if (file.Expansion.ToLower() == ".docx")
            {

                var content = _caseService.GetBlancDocxFileAsync(file.Content, (Guid)serviceId);
                return File(content, MimeTypeMap.GetMimeType(file.Expansion), $"{file.Name}.docx");
            }
            else if (file.Expansion.ToLower() == ".doc")
            {

                var content = _caseService.GetBlancDocxFileAsync(file.Content, (Guid)serviceId);
                return File(content, MimeTypeMap.GetMimeType(file.Expansion), $"{file.Name}.doc");
            }
            else if (file.Expansion.ToLower() == ".pdf")
            {
                return PdfLayoutView(file.Content);
            }
            else
            {
                return File(file.Content, MimeTypeMap.GetMimeType(file.Expansion), $"{file.Name}.{file.Expansion}");
            }

            ShowError("Блак не найден");
            return NotFound();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            ShowError(e.Message);
            return NotFound();
        }
    }

    public async Task<IActionResult> DownloadFile(Guid id, Guid? serviceId)
    {
        try
        {
            var file = await _caseService.GetServiceFileByIdAsync(id);
            if (file is null)
            {
                ShowError("Файл не найден");
                return NotFound();
            }

            if (file.Expansion == ".pdf" && file.Size < 1500000)
            {
                return PdfLayoutView(file.Content);
            }
            else
            {
                return File(file.Content, MimeTypeMap.GetMimeType(file.Expansion), $"{file.Name}.{file.Expansion}");
            }
        }
        catch (Exception e)
        {
            ShowError(e.Message);
            return NotFound();
        }
    }
}