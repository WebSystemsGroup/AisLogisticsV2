using AisLogistics.DataLayer.Common.Dto.Reference;
using Microsoft.EntityFrameworkCore;

namespace AisLogistics.App.Service
{
    /// <summary>
    /// Справочники - Документы
    /// </summary>
    public partial class ReferencesService : IReferencesService
    {
        public async Task<List<DocumentDto>> GetAllDocumentsAsync()
        {
            var documents = _context.SDocuments
                .AsNoTracking()
                .Where(x => !x.IsRemove)
                .OrderBy(x => x.DocumentName);

            var result = await documents
                .Select(x => new DocumentDto()
                {
                    Id = x.Id,
                    Name = x.DocumentName
                }).ToListAsync();

            return result;
        }
    }
}
