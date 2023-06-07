using AisLogistics.App.Extensions;
using AisLogistics.App.Models.DTO.References;
using AisLogistics.DataLayer.Common.Dto.Reference;
using AisLogistics.DataLayer.Entities.Models;
using Clave.Expressionify;
using DataTables.AspNet.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Linq;
using System.Linq.Expressions;

namespace AisLogistics.App.Service
{
    /// <summary>
    /// Справочники - Сотрудники
    /// </summary>
    public partial class ReferencesService : IReferencesService
    {
        public async Task<EmployeeModelDto> GetEmployeeDtoAsync(Guid? Id)
        {
            if (Id is null)
                throw new ArgumentNullException(nameof(Id));

            var result = await _context.SEmployees.FindAsync(Id);
            if (result == null)
                throw new Exception("Данные не найдены!");

            return _mapper.Map<EmployeeModelDto>(result);
        }

        public async Task<List<EmployeeModelDto>> GetEmployeesDtoAsync(List<Guid> Id)
        {
            return await _context.SEmployees
                .Where(w => Id.Any(a => a == w.Id))
                .Select(s=> new EmployeeModelDto { Id = s.Id, EmployeeName = s.EmployeeName })
                .ToListAsync();
        }

        public (List<EmployeesOfficesJoinDto>, int, int) GetReferencesEmployees(IDataTablesRequest request)
        {
            var orderColumns = request.Columns.Where(x => x.Sort != null);
            var date = DateTime.Today;

            //var orderInfo = request.Columns.FirstOrDefault(x => x.Sort != null);
            //Expression<Func<SEmployee, string>> orderByExpr = orderInfo is null
            //    ? x => x.EmployeeName
            //    : x => x.EmployeeName;

            var results = _context.SEmployees
                .AsNoTracking()
                .Where(x => !x.IsRemove)
                .GroupJoin(
                    inner: _context.SEmployeesOfficesJoins
                        .AsNoTracking()
                        .Where(x => !(x.IsRemove))
                        .Include(i => i.SOffices)
                        .Include(i => i.SEmployeesJobPosition),
                    outerKeySelector: employee => employee.Id,
                    innerKeySelector: office => office.SEmployees.Id,
                    resultSelector: (employee, eoj) => new
                    {
                        employee = employee,
                        eoj = eoj
                    })
                .SelectMany(x => x.eoj.DefaultIfEmpty(), (x, y) => new { x.employee, OfficeJoins = y })
                .Where(x => x.OfficeJoins.DateStop == null)
                .GroupJoin(
                    inner: _context.SEmployeesStatusJoins
                        .AsNoTracking()
                        .Where(x => !x.IsRemove)
                        .Include(i => i.SEmployeesStatus)
                        .Where(x => x.DateStop == null),
                    outerKeySelector: x => x.employee.Id,
                    innerKeySelector: y => y.SEmployees.Id,
                    resultSelector: (employee, esj) => new
                    {
                        employee = employee,
                        esj = esj
                    })
                .SelectMany(x => x.esj.DefaultIfEmpty(), (x, y) => new EmployeesOfficesJoinDto()
                {
                    SEmployeesId = x.employee.employee.Id,
                    EmployeeName = x.employee.employee.EmployeeName,
                    SOfficesId = x.employee.OfficeJoins == null ? Guid.Empty : x.employee.OfficeJoins.SOfficesId,
                    OfficeName = x.employee.OfficeJoins == null ? null : x.employee.OfficeJoins.SOffices.OfficeName,
                    SEmployeesJobPositionId = x.employee.OfficeJoins == null ? default : x.employee.OfficeJoins.SEmployeesJobPositionId,
                    JobPositionName = x.employee.OfficeJoins == null ? default : x.employee.OfficeJoins.SEmployeesJobPosition.JobPositionName,
                    StatusName = y == null ? default : y.SEmployeesStatus.StatusName,
                    IsExecution = _context.SEmployeesExecutions
                                  .Any(xx => !xx.IsRemove && xx.SEmployeesId == x.employee.employee.Id && xx.DateStop == null),
                    IsActive = _context.SEmployeesActives
                                  .Any(xx => !xx.IsRemove && xx.SEmployeesId == x.employee.employee.Id && xx.DateStart<date&&(xx.DateStop == null||xx.DateStop>date))
                               
                })
                .OrderByColums(orderColumns);

            int totalCount = results.Count();

            //Expression<Func<EmployeesOfficesJoinDto, bool>>? searchExpr =
            //    string.IsNullOrEmpty(request.Search.Value)
            //    ? null
            //    : x => x.EmployeeName.Contains(request.Search.Value, StringComparison.OrdinalIgnoreCase);

            var filteredResult = string.IsNullOrEmpty(request.Search.Value)
                ? results
                : results.ToList().Where(x => x.EmployeeName.Contains(request.Search.Value, StringComparison.OrdinalIgnoreCase));

            int filteredCount = filteredResult.Count();

            var dataPage = filteredResult
                .Skip(request.Start)
                .Take(request.Length)
                .ToList();

            return (dataPage, totalCount, filteredCount);
        }

        public (List<EmployeesOfficesJoinDto> Employees, int TotalCount, int FilteredCount) GetReferencesEmployees(Func<EmployeesOfficesJoinDto, bool> searchPredicate, int? start = null, int? length = null)
        {
            var currDate = DateTime.Now;

            //https://docs.microsoft.com/ru-ru/dotnet/csharp/linq/perform-left-outer-joins

            //var employees = _context.SEmployees
            //    .AsNoTracking()
            //    .Where(x => !x.IsRemove)
            //    .OrderBy(x => x.EmployeeName);

            //var offices = _context.SEmployeesOfficesJoins
            //    .AsNoTracking()
            //    .Where(x => !x.IsRemove)
            //    .Include(i => i.SOffices)
            //    .Include(i => i.SEmployeesJobPosition);

            var results = _context.SEmployees
                .AsNoTracking()
                .Where(x => !x.IsRemove)
                .OrderBy(x => x.EmployeeName)
                .GroupJoin(
                    inner: _context.SEmployeesOfficesJoins
                        .AsNoTracking()
                        .Where(x => !(x.IsRemove))
                        .Include(i => i.SOffices)
                        .Include(i => i.SEmployeesJobPosition),
                    outerKeySelector: employee => employee.Id,
                    innerKeySelector: office => office.SEmployees.Id,
                    resultSelector: (employee, eoj) => new
                    {
                        employee = employee,
                        eoj = eoj
                    })
                .SelectMany(x => x.eoj.DefaultIfEmpty(), (x, y) => new { x.employee, OfficeJoins = y })
                .Where(x => x.OfficeJoins.DateStop == null)
                .GroupJoin(
                    inner: _context.SEmployeesStatusJoins
                        .AsNoTracking()
                        .Where(x => !x.IsRemove)
                        .Include(i => i.SEmployeesStatus)
                        .Where(x => x.DateStop == null),
                    outerKeySelector: x => x.employee.Id,
                    innerKeySelector: y => y.SEmployees.Id,
                    resultSelector: (employee, esj) => new
                    {
                        employee = employee,
                        esj = esj
                    })
                .SelectMany(x => x.esj.DefaultIfEmpty(), (x, y) => new EmployeesOfficesJoinDto()
                {
                    SEmployeesId = x.employee.employee.Id,
                    EmployeeName = x.employee.employee.EmployeeName,
                    SOfficesId = x.employee.OfficeJoins == null ? Guid.Empty : x.employee.OfficeJoins.SOfficesId,
                    OfficeName = x.employee.OfficeJoins == null ? null : x.employee.OfficeJoins.SOffices.OfficeName,
                    SEmployeesJobPositionId = x.employee.OfficeJoins == null ? default : x.employee.OfficeJoins.SEmployeesJobPositionId,
                    JobPositionName = x.employee.OfficeJoins == null ? default : x.employee.OfficeJoins.SEmployeesJobPosition.JobPositionName,
                    StatusName = y == null ? default : y.SEmployeesStatus.StatusName,
                    IsExecution = _context.SEmployeesExecutions
                                  .Any(xx => !xx.IsRemove && xx.SEmployeesId == x.employee.employee.Id && xx.DateStop == null),
                    IsActive = _context.SEmployeesActives
                                  .Any(xx => !xx.IsRemove && xx.SEmployeesId == x.employee.employee.Id && xx.DateStart<currDate&&(xx.DateStop == null||xx.DateStop>currDate))
                                  
                });

            int totalCount = results.Count();

            var filteredResult = searchPredicate is null ?
                results :
                results.ToList().Where(searchPredicate);

            int filteredCount = filteredResult.Count();

            var dataPage = filteredResult;
            if (start.HasValue) dataPage = dataPage.Skip(start.Value);
            if (length.HasValue) dataPage = dataPage.Take(length.Value);

            return (dataPage.ToList(), totalCount, filteredCount);
        }

        public (List<EmployeesOfficesJoinDto> Employees, int TotalCount, int FilteredCount) GetReferencesEmployees(int? start = null, int? length = null)
        {
            var currDate = DateTime.Now;

            //https://docs.microsoft.com/ru-ru/dotnet/csharp/linq/perform-left-outer-joins

            var results = _context.SEmployees
                .AsNoTracking()
                .Where(x => !(x.IsRemove))
                .OrderBy(x => x.EmployeeName)
                .GroupJoin(
                    inner: _context.SEmployeesOfficesJoins
                        .AsNoTracking()
                        .Where(x => !(x.IsRemove))
                        .Include(i => i.SOffices)
                        .Include(i => i.SEmployeesJobPosition),
                    outerKeySelector: employee => employee.Id,
                    innerKeySelector: office => office.SEmployees.Id,
                    resultSelector: (employee, eoj) => new
                    {
                        employee = employee,
                        eoj = eoj
                    })
                .SelectMany(x => x.eoj.DefaultIfEmpty(), (x, y) => new { x.employee, OfficeJoins = y })
                .Where(x => x.OfficeJoins.DateStop == null)
                .GroupJoin(
                    inner: _context.SEmployeesStatusJoins
                        .AsNoTracking()
                        .Where(x => !x.IsRemove)
                        .Include(i => i.SEmployeesStatus)
                        .Where(x => x.DateStop == null),
                    outerKeySelector: x => x.employee.Id,
                    innerKeySelector: y => y.SEmployees.Id,
                    resultSelector: (employee, esj) => new
                    {
                        employee = employee,
                        esj = esj
                    })
                .SelectMany(x => x.esj.DefaultIfEmpty(), (x, y) => new EmployeesOfficesJoinDto()
                {
                    SEmployeesId = x.employee.employee.Id,
                    EmployeeName = x.employee.employee.EmployeeName,
                    SOfficesId = x.employee.OfficeJoins == null ? Guid.Empty : x.employee.OfficeJoins.SOfficesId,
                    OfficeName = x.employee.OfficeJoins == null ? null : x.employee.OfficeJoins.SOffices.OfficeNameSmall,
                    SEmployeesJobPositionId = x.employee.OfficeJoins == null
                        ? default
                        : x.employee.OfficeJoins.SEmployeesJobPositionId,
                    JobPositionName = x.employee.OfficeJoins == null
                        ? default
                        : x.employee.OfficeJoins.SEmployeesJobPosition.JobPositionName,
                    StatusName = y == null ? default : y.SEmployeesStatus.StatusName,
                    IsExecution = _context.SEmployeesExecutions
                        .Any(xx => !xx.IsRemove && xx.SEmployeesId == x.employee.employee.Id && xx.DateStop == null),
                    IsActive = _context.SEmployeesActives
                        .Any(xx => !xx.IsRemove && xx.SEmployeesId == x.employee.employee.Id && xx.DateStart < currDate && (xx.DateStop == null || xx.DateStop > currDate)) 
                });

            //var results = from employee in employees
            //              join office in offices on employee equals office.SEmployees into eoj
            //              join status in statuses on employee.Id equals status.SEmployees.Id into esj
            //              from substatus in esj.DefaultIfEmpty()
            //              from suboffice in eoj.DefaultIfEmpty()
            //              where substatus.DateStop == null && suboffice.DateStop == null
            //              select new EmployeesOfficesJoinDto()
            //              {
            //                  Id = suboffice == null ? Guid.Empty : suboffice.Id,
            //                  SEmployeesId = employee.Id,
            //                  EmployeeName = employee.EmployeeName,
            //                  SOfficesId = suboffice == null ? Guid.Empty : suboffice.SOfficesId,
            //                  OfficeName = suboffice == null ? null : suboffice.SOffices.OfficeName,
            //                  SEmployeesJobPositionId = suboffice == null ? default : suboffice.SEmployeesJobPositionId,
            //                  JobPositionName = suboffice == null ? default : suboffice.SEmployeesJobPosition.JobPositionName,
            //                  //StatusName = _context.SEmployeesStatusJoins
            //                  //    .Where(xx => !xx.IsRemove && xx.SEmployeesId == employee.Id && xx.DateStart <= currDate && (xx.DateStop == null || currDate >= xx.DateStop))
            //                  //    .Include(i => i.SEmployeesStatus)
            //                  //    .Select(xx => xx.SEmployeesStatus.StatusName)
            //                  //    .FirstOrDefault(),
            //                  StatusName = substatus == null ? default : substatus.SEmployeesStatus.StatusName,
            //                  IsExecution = _context.SEmployeesExecutions
            //                      .Where(xx => !xx.IsRemove && xx.SEmployeesId == employee.Id && xx.DateStart <= currDate && (xx.DateStop == null || currDate >= xx.DateStop))
            //                      .Select(xx => xx.IsExecution)
            //                      .FirstOrDefault(),
            //                  IsActive = _context.SEmployeesActives
            //                      .Where(xx => !xx.IsRemove && xx.SEmployeesId == employee.Id)
            //                      .Select(xx => xx.IsActive)
            //                      .FirstOrDefault()
            //              };

            int totalCount = results.Count();

            var dataPage = results;
            if (start.HasValue) dataPage = dataPage.Skip(start.Value);
            if (length.HasValue) dataPage = dataPage.Take(length.Value);

            return (dataPage.ToList(), totalCount, totalCount);
        }

        //public (List<EmployeesOfficesJoinDto>, int, int) GetEmployeesOfficesJoins(Func<SEmployeesOfficesJoin, bool> searchPredicate, int start, int length)
        //{
        //    var currDate = DateTime.Now;

        //    var results = _context.SEmployeesOfficesJoins
        //        .AsNoTracking()
        //        .Where(x => !x.IsRemove && x.DateStart <= currDate && (x.DateStop == null || currDate >= x.DateStop))
        //        .Include(i => i.SEmployees)
        //        .Include(i => i.SOffices)
        //        .Include(i => i.SEmployeesJobPosition)
        //        .OrderBy(x => x.SEmployees.EmployeeName);

        //    int totalCount = results.Count();

        //    var filteredResult = searchPredicate is null ?
        //        results :
        //        results.Where(searchPredicate);

        //    int filteredCount = filteredResult.Count();

        //    var dataPage = filteredResult
        //        .Skip(start)
        //        .Take(length)
        //        .Select(x => new EmployeesOfficesJoinDto()
        //        {
        //            Id = x.Id,
        //            SEmployeesId = x.SEmployeesId,
        //            EmployeeName = x.SEmployees.EmployeeName,
        //            SOfficesId = x.SOfficesId,
        //            OfficeName = x.SOffices.OfficeName,
        //            SEmployeesJobPositionId = x.SEmployeesJobPositionId,
        //            JobPositionName = x.SEmployeesJobPosition.JobPositionName,
        //            StatusName = _context.SEmployeesStatusJoins
        //                .Where(xx => !xx.IsRemove && xx.SEmployeesId == x.SEmployeesId && xx.DateStart <= currDate && (xx.DateStop == null || currDate >= xx.DateStop))
        //                .Include(i => i.SEmployeesStatus)
        //                .Select(xx => xx.SEmployeesStatus.StatusName)
        //                .FirstOrDefault(),
        //            IsExecution = _context.SEmployeesExecutions
        //                .Where(xx => !xx.IsRemove && xx.SEmployeesId == x.SEmployeesId && xx.DateStart <= currDate && (xx.DateStop == null || currDate >= xx.DateStop))
        //                .Select(xx => xx.IsExecution)
        //                .FirstOrDefault(),
        //            IsActive = _context.SEmployeesActives
        //                .Where(xx => !xx.IsRemove && xx.SEmployeesId == x.SEmployeesId)
        //                .Select(xx => xx.IsActive)
        //                .FirstOrDefault()
        //        }).ToList();

        //    return (dataPage, totalCount, filteredCount);
        //}

        //public (List<EmployeesOfficesJoinDto>, int, int) GetEmployeesOfficesJoins(int start, int length)
        //{
        //    var currDate = DateTime.Now;
        //    //var res = _context.SEmployees
        //    //    .AsNoTracking()
        //    //    .Where(x => !x.IsRemove)
        //    //    .Include(i => i.SEmployeesOfficesJoins)
        //    //        .ThenInclude(i => i.SOffices)
        //    //    .Include(i => i.SEmployeesOfficesJoins)
        //    //        .ThenInclude(i => i.SEmployeesJobPosition);

        //    var results = _context.SEmployeesOfficesJoins
        //        .AsNoTracking()
        //        .Where(x => !x.IsRemove && x.DateStart <= currDate && (x.DateStop == null || currDate >= x.DateStop))
        //        .Include(i => i.SOffices)
        //        .Include(i => i.SEmployeesJobPosition)
        //        .OrderBy(x => x.SEmployees.EmployeeName);

        //    int totalCount = results.Count();

        //    var dataPage = results
        //        .Skip(start)
        //        .Take(length)
        //        .Select(x => new EmployeesOfficesJoinDto()
        //        {
        //            Id = x.Id,
        //            SEmployeesId = x.SEmployeesId,
        //            EmployeeName = x.SEmployees.EmployeeName,
        //            SOfficesId = x.SOfficesId,
        //            OfficeName = x.SOffices.OfficeName,
        //            SEmployeesJobPositionId = x.SEmployeesJobPositionId,
        //            JobPositionName = x.SEmployeesJobPosition.JobPositionName,
        //            StatusName = _context.SEmployeesStatusJoins
        //                .Where(xx => !xx.IsRemove && xx.SEmployeesId == x.SEmployeesId && xx.DateStart <= currDate && (xx.DateStop == null || currDate >= xx.DateStop))
        //                .Include(i => i.SEmployeesStatus)
        //                .Select(xx => xx.SEmployeesStatus.StatusName)
        //                .FirstOrDefault(),
        //            IsExecution = _context.SEmployeesExecutions
        //                .Where(xx => !xx.IsRemove && xx.SEmployeesId == x.SEmployeesId && xx.DateStart <= currDate && (xx.DateStop == null || currDate >= xx.DateStop))
        //                .Select(xx => xx.IsExecution)
        //                .FirstOrDefault(),
        //            IsActive = _context.SEmployeesActives
        //                .Where(xx => !xx.IsRemove && xx.SEmployeesId == x.SEmployeesId)
        //                .Select(xx => xx.IsActive)
        //                .FirstOrDefault()
        //        }).ToList();

        //    return (dataPage, totalCount, totalCount);
        //}

        /// <summary>
        /// Добавить сотрудника
        /// </summary>
        public async Task<Guid> AddEmployeeAsync(EmployeeModelDto model)
        {
            var entity = _mapper.Map<SEmployee>(model);

            await _context.SEmployees.AddAsync(entity);
            await _context.SaveChangesAsync();

            return entity.Id;
        }

        /// <summary>
        /// Добавить пользователя и связанные с ним данные
        /// </summary>
        public async Task<Guid> AddEmployeeWithLinkedDataAsync(EmployeeAddModelDto model)
        {
            var newEmployeeId = default(Guid);

            using (IDbContextTransaction transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var newEmployee = _mapper.Map<EmployeeModelDto>(model);
                    newEmployee.SEmployeesIdAdd = model.SEmployeesIdAdd;
                    newEmployee.EmployeesFioAdd = model.EmployeesFioAdd;
                    newEmployee.AspNetUserId = model.AspNetUserId;

                    newEmployeeId = await AddEmployeeAsync(newEmployee);

                    // добавить Роль исполнителя в таблицу s_employees_roles_executor
                    if (model.SRolesExecutorId != Guid.Empty)
                    {
                        // лишний mapping
                        //await AddEmployeeExecutorRoleAsync(new EmployeeRoleExecutorModelDto()
                        //{
                        //    SEmployeesId = newEmployeeId,
                        //    SRolesExecutorId = model.SRolesExecutorId,
                        //    EmployeesFioAdd = model.EmployeesFioAdd
                        //});
                        await _context.SEmployeesRolesExecutors.AddAsync(new SEmployeesRolesExecutor()
                        {
                            SEmployeesId = newEmployeeId,
                            SRolesExecutorId = model.SRolesExecutorId,
                            EmployeesFioAdd = model.EmployeesFioAdd
                        });
                    }

                    // добавить Офис Должность Интенсивность Ставка в таблицу s_employees_offices_join
                    // лишний mapping
                    //await AddEmployeeOfficeAsync(new EmployeeOfficeModelDto()
                    //{
                    //    SEmployeesId = newEmployeeId,
                    //    DateStart = model.DateStart,
                    //    EmployeeIntensity = model.EmployeeIntensity,
                    //    EmployeeRate = model.EmployeeRate,
                    //    SEmployeesJobPositionId = model.SEmployeesJobPositionId,
                    //    SOfficesId = model.SOfficesId,
                    //    EmployeeFioAdd = model.EmployeesFioAdd
                    //});
                    await _context.SEmployeesOfficesJoins.AddAsync(new SEmployeesOfficesJoin()
                    {
                        SEmployeesId = newEmployeeId,
                        DateStart = model.DateStart,
                        EmployeeIntensity = model.EmployeeIntensity,
                        EmployeeRate = model.EmployeeRate,
                        SEmployeesJobPositionId = model.SEmployeesJobPositionId,
                        SOfficesId = model.SOfficesId,
                        EmployeeFioAdd = model.EmployeesFioAdd
                    });

                    // добавить Статус в таблицу s_employees_status_join
                    // лишний mapping
                    //await AddEmployeeStatusAsync(new EmployeeStatusModelDto()
                    //{
                    //    SEmployeesId = newEmployeeId,
                    //    DateStart = model.DateStart,
                    //    SEmployeesStatusId = model.SEmployeesStatusId,
                    //    EmployeesFioAdd = model.EmployeesFioAdd
                    //});
                    await _context.SEmployeesStatusJoins.AddAsync(new SEmployeesStatusJoin()
                    {
                        SEmployeesId = newEmployeeId,
                        DateStart = model.DateStart,
                        SEmployeesStatusId = model.SEmployeesStatusId,
                        EmployeesFioAdd = model.EmployeesFioAdd
                    });

                    // добавить Исполнение в таблицу s_employees_execution
                    // лишний mapping
                    //await AddEmployeeExecutionAsync(new EmployeeExecutionModelDto()
                    //{
                    //    SEmployeesId = newEmployeeId,
                    //    DateStart = model.DateStart,
                    //    IsExecution = model.IsExecution,
                    //    EmployeesFioAdd = model.EmployeesFioAdd
                    //});
                    if (model.IsExecution)
                    {
                        await _context.SEmployeesExecutions.AddAsync(new SEmployeesExecution()
                        {
                            SEmployeesId = newEmployeeId,
                            DateStart = model.DateStart,
                            EmployeesFioAdd = model.EmployeesFioAdd
                        });
                    }

                    // добавить Активность в таблицу s_employees_active
                    // лишний mapping
                    //await AddEmployeeActivityAsync(new EmployeeActivityModelDto()
                    //{
                    //    SEmployeesId = newEmployeeId,
                    //    IsActive = model.IsActive,
                    //    DateStart = model.DateStart,
                    //    EmployeesFioAdd = model.EmployeesFioAdd
                    //});
                    if (model.IsActive)
                    {
                        await _context.SEmployeesActives.AddAsync(new SEmployeesActive()
                        {
                            SEmployeesId = newEmployeeId,
                            DateStart = model.DateStart,
                            EmployeesFioAdd = model.EmployeesFioAdd
                        });
                    }

                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw new InvalidOperationException(ex.Message);
                }
            }

            return newEmployeeId;
        }

        /// <summary>
        /// Обновить основные данные пользователя
        /// </summary>
        public async Task UpdateEmployeeAsync(EmployeeModelDto model)
        {
            var entity = await _context.SEmployees.FindAsync(model.Id);
            if (entity == null)
                throw new Exception("Данные не найдены!");

            _mapper.Map<EmployeeModelDto, SEmployee>(model, entity);

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Пометить на удаление сотрудника
        /// </summary>
        /// <param name="Id">Id сотрудника</param>
        /// <param name="comment">Причина удаления</param>
        public async Task RemoveEmployee(Guid Id, string comment)
        {
            var entity = await _context.SEmployees.FindAsync(Id);
            if (entity == null)
                throw new Exception("Запись не найдена!");

            entity.IsRemove = true;

            await _context.SaveChangesAsync();
        }

     
        public async Task<Guid?> GetEmployeeAspNetUserId(Guid Id) 
        {
            return await _context.SEmployees.Where(w => w.Id == Id).Select(s => s.AspNetUserId).FirstOrDefaultAsync();
        }
    }
}
