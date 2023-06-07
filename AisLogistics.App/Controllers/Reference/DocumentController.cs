using AisLogistics.App.Models;
using AisLogistics.App.ViewModels.ModelBuilder;
using AisLogistics.DataLayer.Entities.Models;
using DataTables.AspNet.AspNetCore;
using DataTables.AspNet.Core;
using SmartBreadcrumbs.Attributes;

namespace AisLogistics.App.Controllers.Reference
{
    public partial class ReferenceController
    {
        [Breadcrumb("Документы", FromAction = nameof(Index), FromController = typeof(ReferenceController))]
        public IActionResult Documents()
        {
            var modelBuilder = new ViewModelBuilder()
                .AddViewTitle("Документы").SetInvisibleViewTitle()
                .AddViewDescription("Справочник документов необходимых для оказания услуги")
                .AddTableMethodAction(Url.Action(nameof(GetDocumentsDataJson)))
                .AddEditMethodAction(Url.Action(nameof(DocumentChangeModal)))
                .AddRemoveMethodAction(Url.Action(nameof(DocumentRemove)));
            return View("Documents/Index", modelBuilder);
        }

        public async Task<IActionResult> GetDocumentsDataJson(IDataTablesRequest request)
        {
            var allDataCount = _context.SDocuments.Count(c => !c.IsRemove);
            var allData = _context.SDocuments.Where(w => !w.IsRemove).OrderBy(o=>o.DocumentName);

            //var data = await _unitOfWork.SDocuments.All();

            var filteredData = String.IsNullOrWhiteSpace(request.Search.Value)
                ? allData
                : allData.Where(w => !w.IsRemove & w.DocumentName.Contains(request.Search.Value));

            var dataPage = filteredData.Skip(request.Start).Take(request.Length)
                .Select(s=> new
                {
                    s.Id,
                    s.DocumentName,
                    s.DocumentDescription
                });

            var response = DataTablesResponse.Create(request, allDataCount, filteredData.Count(), dataPage);

            return new DataTablesJsonResult(response, true);
        }

        public IActionResult DocumentChangeModal(Guid? id, ActionType actionType)
        {
            var model = new ModalViewModelBuilder()
                .AddModel(actionType == ActionType.Add ? new SDocument() : _context.SDocuments.Find(id))
                .AddModalTitle((actionType == ActionType.Add ? "Добавление" : "Изменение") + " документа")
                .AddModalViewPath("~/Views/Reference/Documents/_ModalChangeDocument.cshtml")
                .AddActionType(actionType)
                .AddModalType(ModalType.LARGE);
            return ModalLayoutView(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task DocumentSaveAsync(SDocument document, ActionType actionType)
        {
            if (actionType == ActionType.Add)
            {
                await _context.SDocuments.AddAsync(document);
                await _context.SaveChangesAsync();
                ShowSuccess(MessageDescription.AddSuccess);
            }
            else
            {
                var update = await _context.SDocuments.FindAsync(document.Id);
                if (update is null)
                {
                    ShowError(MessageDescription.Error);
                    return;
                }
                update.DocumentName = document.DocumentName;
                update.DocumentDescription = document.DocumentDescription;
                update.DocumentSpecification = document.DocumentSpecification;
                await _context.SaveChangesAsync();
                ShowSuccess(MessageDescription.EditSuccess);
            }
            //if (actionType == ActionType.Add)
            //{
            //    await _documentService.AddAsync(document);
            //}
            //else
            //{
            //    await _documentService.UpdateAsync(document);
            //}
            //await  _unitOfWork.SaveChangesAsync();
            
        }

        [HttpPost]
       // [ValidateAntiForgeryToken]
        public async Task DocumentRemove(Guid id, string comment)
       {
           var update = await _context.SDocuments.FindAsync(id);
           if (update is null)
           {
               ShowError(MessageDescription.Error);
               return;
           }
           update.IsRemove = true;
           update.Commentt = comment;
           await _context.SaveChangesAsync();
           ShowSuccess(MessageDescription.RemoveSuccess);
            //await _documentService.DeleteAsync(id);
            //await _unitOfWork.SaveChangesAsync();
        }
    }
}
