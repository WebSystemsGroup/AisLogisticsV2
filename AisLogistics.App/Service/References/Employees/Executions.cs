using AisLogistics.App.Models.DTO.References;
using AisLogistics.DataLayer.Common.Dto.Reference;
using AisLogistics.DataLayer.Entities.Models;
using DataTables.AspNet.Core;
using Microsoft.EntityFrameworkCore;

namespace AisLogistics.App.Service
{
    /// <summary>
    /// Справочники - Сотрудники - Исполнение
    /// </summary>
    public partial class ReferencesService : IReferencesService
    {
        /// <summary>
        /// Получить исполнения сотрудника
        /// </summary>
        /// <param name="request">Запрос</param>
        /// <param name="id">Id сотрудника</param>
        public (List<EmployeeExecutionDto>, int, int) GetEmployeeExecutions(ref IDataTablesRequest request, Guid id)
        {
            var results = _context.SEmployeesExecutions
                .AsNoTracking()
                .Where(x => !x.IsRemove && x.SEmployeesId == id)
                .OrderByDescending(x => x.DateStart);

            int totalCount = results.Count();

            var dataPage = results
                .Skip(request.Start)
                .Take(request.Length)
                .Select(x => new EmployeeExecutionDto()
                {
                    Id = x.Id,
                    SEmployeesId = x.SEmployeesId,
                    IsExecution = x.DateStart <= DateTime.Today && (x.DateStop==null||x.DateStop>=DateTime.Today),
                    DateStart = x.DateStart.ToString("dd.MM.yyyy"),
                    DateStop = x.DateStop == null ? null : x.DateStop.Value.ToString("dd.MM.yyyy"),
                    Commentt = x.Commentt,
                    EmployeesFioAdd = x.EmployeesFioAdd,
                    DateAdd = x.DateAdd.ToString("dd.MM.yyyy")
                }).ToList();

            return (dataPage, totalCount, totalCount);
        }

        /// <summary>
        /// Получить последнюю запись об исполнении сотрудника
        /// </summary>
        /// <param name="employeeId">Id сотрудника</param>
        public async Task<EmployeeExecutionModelDto> GetLastEmployeeExectionDtoAsync(Guid employeeId)
        {
            var query = await _context.SEmployeesExecutions
                .AsNoTracking()
                .Where(x => !x.IsRemove && x.SEmployeesId == employeeId)
                .OrderByDescending(x => x.DateStart)
                .FirstOrDefaultAsync();

            if (query is null)
            {
                return new EmployeeExecutionModelDto()
                {
                    SEmployeesId = employeeId,
                    DateStart = new DateTime(2000, 1, 1)
                };
            }

            var result = _mapper.Map<EmployeeExecutionModelDto>(query);
            result.Commentt = string.Empty;
            result.DateStart = DateTime.Compare(query.DateStart, query.DateStop ??= default(DateTime)) > 0 ? query.DateStart : (DateTime)query.DateStop;

            return result;
        }


        /// <summary>
        /// Получить последнюю запись об активности сотрудника
        /// </summary>
        /// <param name="employeeId">Id сотрудника</param>
        public async Task<EmployeeExecutionModelDto> GetEmployeeExectionDtoAsync(Guid id)
        {
            var query = await _context.SEmployeesExecutions.FindAsync(id);

            var result = _mapper.Map<EmployeeExecutionModelDto>(query);

            return result;
        }

        public async Task<bool> GetIsEmployeeExectionAsync(Guid employeeId)
        {
            var isActive = await _context.SEmployeesExecutions.AnyAsync(a => !a.IsRemove && a.SEmployeesId == employeeId && a.DateStart < DateTime.Now && (a.DateStop == null || a.DateStop > DateTime.Now));
            return isActive;
        }

        /// <summary>
        /// Пометить на удаление запись об исполнении сотрудника
        /// </summary>
        /// <param name="Id">Id сотрудника</param>
        /// <param name="comment">Причина удаления</param>
        public async Task RemoveEmployeeExecutionAsync(Guid Id, string comment)
        {
            var entity = await _context.SEmployeesExecutions.FindAsync(Id);
            if (entity == null)
                throw new Exception("Запись не найдена!");

            entity.IsRemove = true;

            await _context.SaveChangesAsync();

            //using (IDbContextTransaction transaction = _context.Database.BeginTransaction())
            //{
            //    try
            //    {
            //        entity.IsRemove = true;

            //        // DateStop = null у предыдущей записи
            //        var lastEntity = _context.SEmployeesExecutions
            //            .Where(x => x.SEmployeesId == entity.SEmployeesId && x.DateStart < entity.DateStart && !x.IsRemove)
            //            .OrderByDescending(x => x.DateStart)
            //            .FirstOrDefault();
            //        if (lastEntity != null)
            //            lastEntity.DateStop = null;

            //        await _context.SaveChangesAsync();

            //        await transaction.CommitAsync();
            //    }
            //    catch (Exception ex)
            //    {
            //        await transaction.RollbackAsync();
            //        throw new Exception(ex.Message);
            //    }
            //}
        }

        /// <summary>
        /// Добавить запись об исполнении сотрудника
        /// </summary>
        public async Task AddEmployeeExecutionAsync(EmployeeExecutionModelDto model)
        {
            if (model.Id != Guid.Empty)
            {

                var lastEntity = _context.SEmployeesExecutions.Find(model.Id);
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

            var entity = _mapper.Map<SEmployeesExecution>(model);

            await _context.SEmployeesExecutions.AddAsync(entity);
            await _context.SaveChangesAsync();
        }
    }
}
