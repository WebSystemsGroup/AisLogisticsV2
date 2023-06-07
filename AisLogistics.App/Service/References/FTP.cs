using AisLogistics.App.Models.DTO.References;
using AisLogistics.DataLayer.Common.Dto.Reference;
using AisLogistics.DataLayer.Entities.Models;
using DataTables.AspNet.Core;
using Microsoft.EntityFrameworkCore;

namespace AisLogistics.App.Service
{
    // Справочники - FTP

    /// <summary>
    /// Получить список "FTP"
    /// </summary>
    public partial class ReferencesService : IReferencesService
    {
        public (List<FTPDto>, int, int) GetFTP(IDataTablesRequest request)
        {
            var results = _context.SFtps
                .AsNoTracking()
                .Where(x => !x.IsRemove);

            int totalCount = results.Count();

            var dataPage = results
                .Skip(request.Start)
                .Take(request.Length)
                .Select(x => new FTPDto()
                {
                    Id = x.Id,
                    FtpServer = x.FtpServer,
                    FtpFolder = x.FtpFolder,
                    FtpLogin = x.FtpLogin,

                }).ToList();

            return (dataPage, totalCount, totalCount);
        }

        public async Task<List<FtpModelDto>> GetFTPAllAsync()
        {
            return await _context.SFtps
                .AsNoTracking()
                .Where(x => !x.IsRemove)
                .Select(s => new FtpModelDto
                {
                    Id=s.Id,
                    FtpServer = s.FtpServer,
                    FtpFolder = s.FtpFolder,
                    FtpLogin = s.FtpLogin
                }).ToListAsync();
        }

        /// <summary>
        /// Модель для редактирования FTP
        /// </summary>
        public async Task<FtpModelDto> GetFTPDtoAsync(Guid? Id)
        {
            try 
            {
                if (Id is null)
                    throw new ArgumentNullException(nameof(Id));

                var result = await _context.SFtps.FindAsync(Id);
                if (result == null)
                    throw new InvalidOperationException("Данные не найдены!");

                return _mapper.Map<FtpModelDto>(result);
            }
            
            catch(Exception ex)
            {
                var a = ex;

                return null;
            }
        }

        public async Task AddFTPAsync(FtpModelDto model)
        {
            var entity = _mapper.Map<SFtp>(model);

            await _context.SFtps.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateFTPAsync(FtpModelDto model)
        {
            var entity = await _context.SFtps.FindAsync(model.Id);
            if (entity == null)
                throw new Exception("Данные не найдены!");

            _mapper.Map<FtpModelDto, SFtp>(model, entity);

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Пометить на удаление (удалить?) FTP
        /// </summary>
        /// <param name="Id">ID записи</param>
        /// <param name="comment">Причина удаления</param>
        public async Task RemoveFTPAsync(Guid Id, string comment)
        {
            var entity = await _context.SFtps.FindAsync(Id);
            if (entity == null)
                throw new Exception("Запись не найдена!");

            entity.IsRemove = true;

            await _context.SaveChangesAsync();
        }
    }
}
