using AisLogistics.App.Models.DTO.References;
using AisLogistics.DataLayer.Common.Dto.Reference;
using AisLogistics.DataLayer.Entities.Models;
using DataTables.AspNet.Core;
using Microsoft.EntityFrameworkCore;

namespace AisLogistics.App.Service
{
    /// <summary>
    /// Справочники - Сотрудники - Статус
    /// </summary>
    public partial class ReferencesService : IReferencesService
    {
        /// <summary>
        /// Получить статусы сотрудника
        /// </summary>
        /// <param name="request">Запрос</param>
        /// <param name="id">Id сотрудника</param>
        public (List<EmployeeStatusDto>, int, int) GetEmployeeStatuses(ref IDataTablesRequest request, Guid id)
        {
            var results = _context.SEmployeesStatusJoins
                .AsNoTracking()
                .Where(x => !x.IsRemove && x.SEmployeesId == id)
                .Include(i => i.SEmployeesStatus)
                .OrderByDescending(x => x.DateStart);

            int totalCount = results.Count();

            var dataPage = results
                .Skip(request.Start)
                .Take(request.Length)
                .Select(x => new EmployeeStatusDto()
                {
                    Id = x.Id,
                    SEmployeesId = x.SEmployeesId,
                    StatusName = x.SEmployeesStatus.StatusName,
                    DateStart = x.DateStart.ToString("dd.MM.yyyy"),
                    DateStop = x.DateStop == null ? null : x.DateStop.Value.ToString("dd.MM.yyyy"),
                    Commentt = x.Commentt,
                    EmployeesFioAdd = x.EmployeesFioAdd,
                    DateAdd = x.DateAdd.ToString("dd.MM.yyyy")
                }).ToList();

            return (dataPage, totalCount, totalCount);
        }

        /// <summary>
        /// Получить последнюю запись о статусе сотрудника
        /// </summary>
        /// <param name="employeeId">Id сотрудника</param>
        public async Task<EmployeeStatusModelDto> GetLastEmployeeStatusDtoAsync(Guid employeeId)
        {
            var query = await _context.SEmployeesStatusJoins
                .AsNoTracking()
                .Where(x => !x.IsRemove && x.SEmployeesId == employeeId)
                .OrderByDescending(x => x.DateStart)
                .FirstOrDefaultAsync();

            if (query is null)
            {
                return new EmployeeStatusModelDto()
                {
                    SEmployeesId = employeeId,
                    DateStart = new DateTime(2000, 1, 1)
                };
            }

            var result = _mapper.Map<EmployeeStatusModelDto>(query);
            result.Commentt = string.Empty;
            result.DateStart = DateTime.Compare(query.DateStart, query.DateStop ??= default(DateTime)) > 0 ? query.DateStart : (DateTime)query.DateStop;

            return result;
        }

        /// <summary>
        /// Пометить на удаление запись о статусе сотрудника
        /// </summary>
        /// <param name="Id">Id сотрудника</param>
        /// <param name="comment">Причина удаления</param>
        public async Task RemoveEmployeeStatusAsync(Guid Id, string comment)
        {
            var entity = await _context.SEmployeesStatusJoins.FindAsync(Id);
            if (entity == null)
                throw new Exception("Запись не найдена!");

            entity.IsRemove = true;

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Добавить запись об офисе и должности сотрудника
        /// </summary>
        public async Task AddEmployeeStatusAsync(EmployeeStatusModelDto model)
        {
            if (model.Id != Guid.Empty)
            {
                var lastEntity = _context.SEmployeesStatusJoins.Find(model.Id);
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

            var entity = _mapper.Map<SEmployeesStatusJoin>(model);

            await _context.SEmployeesStatusJoins.AddAsync(entity);
            await _context.SaveChangesAsync();
        }
    }
}
