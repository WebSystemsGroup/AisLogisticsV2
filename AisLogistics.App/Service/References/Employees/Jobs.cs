using AisLogistics.App.Models.DTO.References;
using AisLogistics.DataLayer.Common.Dto.Reference;
using AisLogistics.DataLayer.Entities.Models;
using DataTables.AspNet.Core;
using Microsoft.EntityFrameworkCore;

namespace AisLogistics.App.Service
{
    /// <summary>
    /// Справочники - Сотрудники - Должность
    /// </summary>
    public partial class ReferencesService : IReferencesService
    {
        /// <summary>
        /// Получить должности и офисы сотрудника
        /// </summary>
        /// <param name="request">Запрос</param>
        /// <param name="id">Id сотрудника</param>
        public (List<EmployeeOfficeDto>, int, int) GetEmployeeOffices(ref IDataTablesRequest request, Guid id)
        {
            var results = _context.SEmployeesOfficesJoins
                .AsNoTracking()
                .Where(x => !x.IsRemove && x.SEmployeesId == id)
                .Include(i => i.SOffices)
                .Include(i => i.SEmployeesJobPosition)
                .OrderByDescending(x => x.DateStart);

            int totalCount = results.Count();

            var dataPage = results
                .Skip(request.Start)
                .Take(request.Length)
                .Select(x => new EmployeeOfficeDto()
                {
                    Id = x.Id,
                    SEmployeesId = x.SEmployeesId,
                    OfficeName = x.SOffices.OfficeName,
                    JobPositionName = x.SEmployeesJobPosition.JobPositionName,
                    EmployeeIntensity = x.EmployeeIntensity,
                    EmployeeRate = x.EmployeeRate,
                    DateStart = x.DateStart.ToString("dd.MM.yyyy"),
                    DateStop = x.DateStop == null ? null : x.DateStop.Value.ToString("dd.MM.yyyy"),
                    EmployeeFioAdd = x.EmployeeFioAdd,
                    DateAdd = x.DateAdd.ToString("dd.MM.yyyy")
                }).ToList();

            return (dataPage, totalCount, totalCount);
        }

        /// <summary>
        /// Модель для редактирования должности и офиса сотрудника
        /// </summary>
        public async Task<EmployeeOfficeModelDto> GetEmployeeOfficeDtoAsync(Guid? Id)
        {
            if (Id is null)
                throw new ArgumentNullException(nameof(Id));

            var result = await _context.SEmployeesOfficesJoins.FindAsync(Id);
            if (result == null)
                throw new Exception("Данные не найдены!");

            return _mapper.Map<EmployeeOfficeModelDto>(result);
        }

        /// <summary>
        /// Получить последнюю запись об офисе и должности сотрудника
        /// </summary>
        /// <param name="employeeId">Id сотрудника</param>
        public async Task<EmployeeOfficeModelDto> GetLastEmployeesOfficeDtoAsync(Guid employeeId)
        {
            var query = await _context.SEmployeesOfficesJoins
                .AsNoTracking()
                .Where(x => !x.IsRemove && x.SEmployeesId == employeeId)
                .OrderByDescending(x => x.DateStart)
                .FirstOrDefaultAsync();

            if (query is null)
            {
                return new EmployeeOfficeModelDto()
                {
                    SEmployeesId = employeeId,
                    EmployeeRate = 1.00M,
                    DateStart = new DateTime(2000, 1, 1)
                };
            }

            var result = _mapper.Map<EmployeeOfficeModelDto>(query);
            result.DateStart = DateTime.Compare(query.DateStart, query.DateStop ??= default(DateTime)) > 0 ? query.DateStart : (DateTime)query.DateStop;

            return result;
        }

        /// <summary>
        /// Пометить на удаление запись об офисе и должности сотрудника
        /// </summary>
        /// <param name="Id">Id сотрудника</param>
        /// <param name="comment">Причина удаления</param>
        public async Task RemoveEmployeeOfficeAsync(Guid Id, string comment)
        {
            var entity = await _context.SEmployeesOfficesJoins.FindAsync(Id);
            if (entity == null)
                throw new Exception("Запись не найдена!");

            entity.IsRemove = true;

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Добавить запись об офисе и должности сотрудника
        /// </summary>
        public async Task AddEmployeeOfficeAsync(EmployeeOfficeModelDto model)
        {
            if (model.Id != Guid.Empty)
            {

                var lastEntity = _context.SEmployeesOfficesJoins.Find(model.Id);
                if (lastEntity != null)
                {
                    // не нужно - срабатывает триггер
                    //lastEntity.DateStop = model.DateStart.AddDays(-1);
                    //*******************************

                    if (lastEntity.DateStart >= model.DateStart)
                        throw new ArgumentOutOfRangeException(nameof(model.DateStart), "Дата начала превышает максимальное значение!");
                }


                model.Id = Guid.Empty;
            }

            var entity = _mapper.Map<SEmployeesOfficesJoin>(model);

            await _context.SEmployeesOfficesJoins.AddAsync(entity);
            await _context.SaveChangesAsync();
        }
    }
}
