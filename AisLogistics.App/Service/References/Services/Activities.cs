using AisLogistics.App.Models.DTO.References;
using AisLogistics.DataLayer.Common.Dto.Reference;
using AisLogistics.DataLayer.Entities.Models;
using Microsoft.EntityFrameworkCore;

namespace AisLogistics.App.Service
{
    /// <summary>
    /// Справочники - Услуги - Активность
    /// </summary>
    public partial class ReferencesService : IReferencesService
    {
        /// <summary>
        /// Поучить активности услуги
        /// </summary>
        /// <param name="serviceId">Id услуги</param>
        /// <param name="start">Брать начиная с</param>
        /// <param name="length">Кол-во элементов</param>
        public async Task<(List<ServiceActivityDto>, int, int)> GetServiceActivitiesAsync(Guid serviceId, int start, int length)
        {
            var date = DateTime.Today;

            var result = _context.SServicesActives
                .AsNoTracking()
                .Where(x => !x.IsRemove && x.SServices.Id == serviceId)
                .OrderByDescending(x => x.DateStart);

            int totalCount = result.Count();

            var dataPage = await result
                .Skip(start)
                .Take(length)
                .Select(x => new ServiceActivityDto()
                {
                    Id = x.Id,
                    SServicesId = serviceId,
                    IsServicesActive = x.DateStart<=date&&(!x.DateStop.HasValue||x.DateStop.Value>=date),
                    OfficeName = x.SOffices.OfficeName,
                    Commentt = x.Commentt,
                    DateStart = x.DateStart.ToString("dd.MM.yyyy"),
                    DateStop = x.DateStop == null ? null : x.DateStop.Value.ToString("dd.MM.yyyy"),
                    EmployeeFioAdd = x.EmployeeFioAdd,
                    DateAdd = x.DateAdd.ToString("dd.MM.yyyy")
                }).ToListAsync();

            return (dataPage, totalCount, totalCount);
        }

        /// <summary>
        /// Получить последнюю запись об активности услуги
        /// </summary>
        /// <param name="serviceId">Id услуги</param>
        public async Task<ServiceActivityModelDto> GetLastServiceActivityDtoAsync(Guid serviceId)
        {
            var query = await _context.SServicesActives
                .AsNoTracking()
                .Where(x => !x.IsRemove && x.SServicesId == serviceId)
                .OrderByDescending(x => x.DateStart)
                .FirstOrDefaultAsync();

            if (query is null)
            {
                return new ServiceActivityModelDto()
                {
                    SServicesId = serviceId,
                    DateStart = new DateTime(2000, 1, 1)
                };
            }

            var result = _mapper.Map<ServiceActivityModelDto>(query);
            result.Commentt = string.Empty;
            result.DateStart = DateTime.Compare(query.DateStart, query.DateStop ??= default(DateTime)) > 0 ? query.DateStart : (DateTime)query.DateStop;

            return result;
        }

        /// <summary>
        /// Пометить на удаление запись об активности услуги
        /// </summary>
        /// <param name="Id">Id сотрудника</param>
        /// <param name="comment">Причина удаления</param>
        public async Task RemoveServiceActivityAsync(Guid Id, string comment)
        {
            var entity = await _context.SServicesActives.FindAsync(Id);
            if (entity == null)
                throw new Exception("Запись не найдена!");

            entity.IsRemove = true;

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Добавить запись об активности услуги
        /// </summary>
        public async Task AddServiceActivityAsync(ServiceActivityModelDto model)
        {
            try
            {
                if (model.Id != Guid.Empty)
                {
                    var lastEntity = _context.SServicesActives.Find(model.Id);
                    if (lastEntity != null)
                    {
                        if (lastEntity.DateStart >= model.DateStart)
                            throw new ArgumentOutOfRangeException(nameof(model.DateStart), "Дата начала превышает максимальное значение!");

                        // не нужно - срабатывает триггер
                        //lastEntity.DateStop = model.DateStart.AddDays(-1);
                        //*******************************
                    }

                    model.Id = Guid.Empty;
                }

                List<SServicesActive> sServicesActives = new();

                if (!model.OffisesId.Any() || model.OffisesId.First() == Guid.Empty)
                {
                    var offices = await GetAllOfficesTypeMfcAndTospAsync();

                    offices.ForEach(s =>
                    {
                        var entity = _mapper.Map<SServicesActive>(model);
                        entity.SOfficesId = s.Id;
                        sServicesActives.Add(entity);
                    });

                }
                else
                {
                    model.OffisesId.ForEach(s =>
                    {
                        var entity = _mapper.Map<SServicesActive>(model);
                        entity.SOfficesId = s;
                        sServicesActives.Add(entity);
                    });
                }

                await _context.SServicesActives.AddRangeAsync(sServicesActives);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Ошибка при добавление активности");
            }
        }
    }
}
