using AisLogistics.App.Models.DTO.References;
using AisLogistics.DataLayer.Common.Dto.Reference;
using AisLogistics.DataLayer.Entities.Models;
using DataTables.AspNet.Core;
using Microsoft.EntityFrameworkCore;

namespace AisLogistics.App.Service
{
    // Справочники - Сотрудники - Активность

    public partial class ReferencesService : IReferencesService
    {
        /// <summary>
        /// Получить активности сотрудника
        /// </summary>
        /// <param name="request">Запрос</param>
        /// <param name="id">Id сотрудника</param>
        public (List<EmployeeActivityDto>, int, int) GetEmployeeActivities(ref IDataTablesRequest request, Guid id)
        {
            var results = _context.SEmployeesActives
                .AsNoTracking()
                .Where(x => !x.IsRemove && x.SEmployeesId == id)
                .OrderByDescending(x => x.DateStart);

            int totalCount = results.Count();

            var date = DateTime.Today;

            var dataPage = results
                .Skip(request.Start)
                .Take(request.Length)
                .Select(x => new EmployeeActivityDto()
                {
                    Id = x.Id,
                    SEmployeesId = x.SEmployeesId,
                    IsActive = x.DateStart<date&&(x.DateStop == null||x.DateStop>date),
                    DateStart = x.DateStart.ToString("dd.MM.yyyy"),
                    DateStop = x.DateStop == null ? null : x.DateStop.Value.ToString("dd.MM.yyyy"),
                    Commentt = x.Commentt,
                    EmployeesFioAdd = x.EmployeesFioAdd,
                    DateAdd = x.DateAdd.ToString("dd.MM.yyyy")
                }).ToList();

            return (dataPage, totalCount, totalCount);
        }

        /// <summary>
        /// Получить последнюю запись об активности сотрудника
        /// </summary>
        /// <param name="employeeId">Id сотрудника</param>
        public async Task<EmployeeActivityModelDto> GetLastEmployeeActivityDtoAsync(Guid employeeId)
        {
            var query = await _context.SEmployeesActives
                .AsNoTracking()
                .Where(x => !x.IsRemove && x.SEmployeesId == employeeId)
                .OrderByDescending(x => x.DateStart)
                .FirstOrDefaultAsync();

            if (query is null)
            {
                return new EmployeeActivityModelDto()
                {
                    SEmployeesId = employeeId,
                    DateStart = new DateTime(2000, 1, 1)
                };
            }

            var result = _mapper.Map<EmployeeActivityModelDto>(query);
            result.Commentt = string.Empty;
            result.DateStart = DateTime.Compare(query.DateStart, query.DateStop ??= default(DateTime)) > 0 ? query.DateStart : (DateTime)query.DateStop;

            return result;
        }

        /// <summary>
        /// Получить последнюю запись об активности сотрудника
        /// </summary>
        /// <param name="employeeId">Id сотрудника</param>
        public async Task<EmployeeActivityModelDto> GetEmployeeActivityDtoAsync(Guid id)
        {
            var query = await _context.SEmployeesActives.FindAsync(id);

            var result = _mapper.Map<EmployeeActivityModelDto>(query);

            return result;
        }

        public async Task<bool> GetIsEmployeeActivityAsync(Guid employeeId)
        {
            var isActive = await _context.SEmployeesActives.AnyAsync(a => !a.IsRemove && a.SEmployeesId == employeeId && a.DateStart < DateTime.Now && (a.DateStop == null || a.DateStop > DateTime.Now));
            return isActive;
        }

        /// <summary>
        /// Пометить на удаление запись об активности сотрудника
        /// </summary>
        /// <param name="Id">Id сотрудника</param>
        /// <param name="comment">Причина удаления</param>
        public async Task RemoveEmployeeActivityAsync(Guid Id, string comment)
        {
            var entity = await _context.SEmployeesActives.FindAsync(Id);
            if (entity == null)
                throw new Exception("Запись не найдена!");

            entity.IsRemove = true;

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Добавить запись об активности сотрудника
        /// </summary>
        public async Task AddEmployeeActivityAsync(EmployeeActivityModelDto model)
        {
            if (model.Id != Guid.Empty)
            {
                var lastEntity = await _context.SEmployeesActives.OrderByDescending(o=>o.DateAdd).FirstOrDefaultAsync(f=>f.SEmployeesId == model.SEmployeesId);
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

            var entity = _mapper.Map<SEmployeesActive>(model);

            await _context.SEmployeesActives.AddAsync(entity);
            await _context.SaveChangesAsync();
        }
    }
}
