using AisLogistics.App.Models.DTO.References;
using AisLogistics.DataLayer.Common.Dto.Reference;
using AisLogistics.DataLayer.Entities.Models;
using DataTables.AspNet.Core;
using Microsoft.EntityFrameworkCore;

namespace AisLogistics.App.Service
{
    // Справочники - Услуги - Способы обращений

    public partial class ReferencesService : IReferencesService
    {
        /// <summary>
        /// Поучить список способов обращений услуги
        /// </summary>
        /// <param name="request">Запрос</param>
        /// <param name="serviceId">Id услуги</param>
        /// <param name="wt">Тип обращения</param>
        public (List<ServiceWayGetDto>, int, int) GetServiceWaysGet(IDataTablesRequest request, Guid serviceId, int wt)
        {
            var results = _context.SServicesWayGetJoins
                .AsNoTracking()
                .Where(x => !x.IsRemove && x.SServices.Id == serviceId && x.WayType == wt)
                .Include(i => i.SServicesWayGet)
                .OrderBy(x => x.DateAdd);

            int totalCount = results.Count();

            var dataPage = results
                .Skip(request.Start)
                .Take(request.Length)
                .Select(x => new ServiceWayGetDto()
                {
                    Id = x.Id,
                    SServicesId = serviceId,
                    SServicesWayGetId = x.SServicesWayGetId,
                    WayType = x.WayType,
                    NameWay = x.SServicesWayGet.NameWay
                }).ToList();

            return (dataPage, totalCount, totalCount);
        }

        /// <summary>
        /// Поучить список способов обращений услуги
        /// </summary>
        /// <param name="searchPredicate">Условие поиска</param>
        /// <param name="serviceId">Id услуги</param>
        /// <param name="start">Брать начиная с</param>
        /// <param name="length">Кол-во элементов</param>
        public (List<ServiceWayGetDto>, int, int) GetServiceWaysGet(Func<SServicesWayGetJoin, bool> searchPredicate, Guid serviceId, int start, int length)
        {
            var result = _context.SServicesWayGetJoins
                .AsNoTracking()
                .Where(x => !x.IsRemove && x.SServices.Id == serviceId)
                .Include(i => i.SServicesWayGet)
                .OrderBy(x => x.DateAdd);

            int totalCount = result.Count();

            var filteredResult = searchPredicate is null ?
                result :
                result.Where(searchPredicate);

            int filteredCount = filteredResult.Count();

            var dataPage = filteredResult
                .Skip(start)
                .Take(length)
                .Select(x => new ServiceWayGetDto()
                {
                    Id = x.Id,
                    SServicesId = serviceId,
                    SServicesWayGetId = x.SServicesWayGetId,
                    WayType = x.WayType,
                    NameWay = x.SServicesWayGet.NameWay
                }).ToList();

            return (dataPage, totalCount, filteredCount);
        }

        /// <summary>
        /// Поучить список всех способов обращений услуги
        /// </summary>
        public List<ServiceWayGetDto> GetAllServiceWaysGet(Guid serviceId, int wayType)
        {
            var result = _context.SServicesWayGetJoins
                .AsNoTracking()
                .Where(x => !x.IsRemove && x.SServices.Id == serviceId && x.WayType == wayType)
                .Include(i => i.SServicesWayGet)
                .OrderBy(x => x.DateAdd);

            return result
                .Select(x => new ServiceWayGetDto()
                {
                    Id = x.Id,
                    SServicesId = serviceId,
                    SServicesWayGetId = x.SServicesWayGetId,
                    WayType = x.WayType,
                    NameWay = x.SServicesWayGet.NameWay
                }).ToList();
        }

        /// <summary>
        /// Модель для добавления / редактирования способа получения услуги
        /// </summary>
        public async Task<ServiceWayGetModelDto> GetServiceWayGetAsync(Guid? Id)
        {
            if (Id is null)
                throw new ArgumentNullException(nameof(Id));

            var result = await _context.SServicesWayGetJoins
                .FirstOrDefaultAsync(x => x.Id == Id);
            if (result == null)
                throw new Exception("Данные не найдены!");

            return _mapper.Map<ServiceWayGetModelDto>(result);
        }

        /// <summary>
        /// Пометить на удаление способ получения услуги
        /// </summary>
        /// <param name="Id">Id записи</param>
        /// <param name="comment">Причина удаления</param>
        public async Task RemoveServiceWayGetAsync(Guid Id, string comment)
        {
            var entity = await _context.SServicesWayGetJoins.FindAsync(Id);
            if (entity == null)
                throw new Exception("Запись не найдена!");

            entity.IsRemove = true;

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Добавить способ получения услуги
        /// </summary>
        public async Task AddServiceWayGetAsync(ServiceWayGetModelDto model)
        {
            var entity = _mapper.Map<SServicesWayGetJoin>(model);

            await _context.SServicesWayGetJoins.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Обновить способ получения услуги
        /// </summary>
        public async Task UpdateServiceWayGetAsync(ServiceWayGetModelDto model)
        {
            var entity = await _context.SServicesWayGetJoins.FindAsync(model.Id);
            if (entity == null)
                throw new Exception("Данные не найдены!");

            _mapper.Map<ServiceWayGetModelDto, SServicesWayGetJoin>(model, entity);

            await _context.SaveChangesAsync();
        }
    }
}
