using AisLogistics.App.Models.DTO.References;
using AisLogistics.DataLayer.Common.Dto.Reference;
using AisLogistics.DataLayer.Entities.Models;
using Microsoft.EntityFrameworkCore;

namespace AisLogistics.App.Service
{
    /// <summary>
    /// Справочники - Услуги - Процедуры
    /// </summary>
    public partial class ReferencesService : IReferencesService
    {
        /// <summary>
        /// Поучить список процедур услуги
        /// </summary>
        /// <param name="serviceId">Id услуги</param>
        /// <param name="start">Брать начиная с</param>
        /// <param name="length">Кол-во элементов</param>
        public async Task<(List<ServicesProcedureDto>, int, int)> GetServiceProceduresAsync(Guid serviceId, int start, int length)
        {
            var result = _context.SServicesProcedures
                .AsNoTracking()
                .Where(x => !x.IsRemove && x.SServices.Id == serviceId)
                .OrderBy(x => x.DateAdd);

            int totalCount = result.Count();

            var dataPage = await result
                .Skip(start)
                .Take(length)
                .Select(x => new ServicesProcedureDto()
                {
                    Id = x.Id,
                    SServicesId = serviceId,
                    ProcedureName = x.ProcedureName,
                    ExtraFormPath = x.ExtraFormPath,
                    FrguTargetId = x.FrguTargetId,
                    EmployeesFioAdd = x.EmployeesFioAdd,
                    ProcedureActive = x.ProcedureActive,
                    DateAdd = x.DateAdd.ToString("dd.MM.yyyy")
                }).ToListAsync();

            return (dataPage, totalCount, totalCount);
        }

        /// <summary>
        /// Поучить список всех процедур услуги
        /// </summary>
        /// <param name="serviceId">Id услуги</param>
        public async Task<List<ServicesProcedureDto>> GetAllServiceProceduresAsync(Guid serviceId)
        {
            return await _context.SServicesProcedures
                .Where(x => x.SServicesId == serviceId&&!x.IsRemove)
                .OrderBy(x => x.ProcedureName)
                .Select(x => new ServicesProcedureDto()
                {
                    Id = x.Id,
                    SServicesId = serviceId,
                    ProcedureName = x.ProcedureName,
                }).ToListAsync();
        }

        /// <summary>
        /// Модель для добавления / редактирования процедуры услуги
        /// </summary>
        public async Task<ServiceProcedureModelDto> GetServiceProcedureDtoAsync(Guid? Id)
        {
            if (Id is null)
                throw new ArgumentNullException(nameof(Id));

            var result = await _context.SServicesProcedures.FindAsync(Id);
            if (result == null)
                throw new Exception("Данные не найдены!");

            return _mapper.Map<ServiceProcedureModelDto>(result);
        }

        /// <summary>
        /// Пометить на удаление процедуру услуги
        /// </summary>
        /// <param name="Id">Id записи</param>
        /// <param name="comment">Причина удаления</param>
        public async Task RemoveServiceProcedureAsync(Guid Id, string comment)
        {
            var entity = await _context.SServicesProcedures.FindAsync(Id);
            if (entity == null)
                throw new Exception("Запись не найдена!");

            entity.IsRemove = true;

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Добавить процедуру услуги
        /// </summary>
        public async Task AddServiceProcedureAsync(ServiceProcedureModelDto model)
        {
            var entity = _mapper.Map<SServicesProcedure>(model);

            await _context.SServicesProcedures.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Обновить данные процедуры услуги
        /// </summary>
        public async Task UpdateServiceProcedureAsync(ServiceProcedureModelDto model)
        {
            var entity = await _context.SServicesProcedures.FindAsync(model.Id);
            if (entity == null)
                throw new Exception("Данные не найдены!");

            _mapper.Map<ServiceProcedureModelDto, SServicesProcedure>(model, entity);

            await _context.SaveChangesAsync();
        }
    }
}
