using AisLogistics.App.Models;
using AisLogistics.App.Models.DTO.References;
using AisLogistics.DataLayer.Common.Dto.Reference;
using AisLogistics.DataLayer.Entities.Models;
using Clave.Expressionify;
using DataTables.AspNet.Core;
using Microsoft.EntityFrameworkCore;
using NinjaNye.SearchExtensions;

namespace AisLogistics.App.Service
{
    // Справочники - Офисы
    public partial class ReferencesService : IReferencesService
    {
        /// <summary>
        /// Поучить список офисов
        /// </summary>
        /// <param name="request">Запрос</param>
        public (List<OfficeParentsDto>, int, int) GetOffices(IDataTablesRequest request)
        {
            int? officeType = null;
            var officeTypeColumn = request?.Columns?.Where(w => w.Name == "officeType").Select(s => s.Search.Value).FirstOrDefault();

            if (officeTypeColumn != null)
                officeType = int.Parse(officeTypeColumn);

            Guid? officeParents = null;
            var oofficeParentsColumn = request?.Columns?.Where(w => w.Name == "parentOffice").Select(s => s.Search.Value).FirstOrDefault();

            if (oofficeParentsColumn != null)
                officeParents = new Guid(oofficeParentsColumn);

            var results = _context.SOffices
                .AsNoTracking()
                .AsSplitQuery()
                .Expressionify()
                .Where(x => !x.IsRemove &&
                (!officeType.HasValue || x.SOfficesTypeId == officeType) &&
                (!officeParents.HasValue || x.ParentId == officeParents));

            int totalCount = results.Count();
            var filteredResult = string.IsNullOrEmpty(request.Search.Value)
               ? results
               : results.Search(s => s.OfficeName.ToLower(),
                                s => s.OfficeNameSmall.ToLower())
               .Containing(request.Search.Value.ToLower().Split(""));

            int filteredCount = filteredResult.Count();

            var dataPage = filteredResult
                .OrderBy(x => x.OfficeName)
                .Skip(request.Start)
                .Take(request.Length)
                .Select(x => new OfficeParentsDto()
                {
                    Id = x.Id,
                    OfficeName = x.OfficeName,
                    ParentOfficeId = x.ParentId,
                }).ToList();

            dataPage?.ForEach(x => {
                if (x.ParentOfficeId is not null && x.ParentOfficeId != Guid.Empty)
                {
                    x.ParentOfficeName = _context.SOffices.AsNoTracking().Where(w => w.Id == x.ParentOfficeId).Select(s => s.OfficeName).First();
                }
            });

            return (dataPage, totalCount, filteredCount);
        }

        /// <summary>
        /// Модель для добавления / редактирования офиса
        /// </summary>
        public async Task<List<OfficeParentsDto>> GetOfficesParentAllAsync()
        {
            var data = await _context.SOffices.Where(w => !w.IsRemove).Select(s => new OfficeParentsDto { Id = s.Id, OfficeName = s.OfficeName, ParentOfficeId = s.ParentId }).ToListAsync();

            data?.ForEach(x =>
            {
                x.CountChild = data.Count(c => c.ParentOfficeId == x.Id);
            });

            return data;
        }

        /// <summary>
        /// Модель для добавления / редактирования офиса
        /// </summary>
        public async Task<OfficeModelDto> GetOfficeDtoAsync(Guid? Id)
        {
            if (Id is null)
                throw new ArgumentNullException(nameof(Id));

            var result = await _context.SOffices
                .Include(i => i.SOfficesType)
                .Include(i => i.SFtp)
                .FirstOrDefaultAsync(f => f.Id == Id);
            if (result == null)
                throw new InvalidOperationException("Данные не найдены!");

            var model = _mapper.Map<OfficeModelDto>(result);

            if (result.ParentId is not null && result.ParentId != Guid.Empty)
            {
                model.OffcesParentName = await _context.SOffices.Where(w => w.Id == result.ParentId).Select(s => s.OfficeName).FirstAsync();
            }

            return model;
        }

        /// <summary>
        /// Список офисов при авторизации
        /// </summary>
        public async Task<List<OfficeDto>> GetActiveEmployeeOfficesDtoAsync(Guid id)
        {
            var date = DateTime.Today;
            return await _context.SEmployeesOfficesJoins
                .AsNoTracking()
                .Where(w => !w.IsRemove && w.SEmployees.AspNetUserId == id &&
                w.DateStart <= date && (w.DateStop == null || date >= w.DateStop))
                .Select(s => new OfficeDto { Id = s.SOfficesId, OfficeName = s.SOffices.OfficeName })
                .ToListAsync();
        }

        /// <summary>
        /// Список офисов при авторизации
        /// </summary>
        public async Task<OfficeDto?> GetActiveEmployeeOfficeDtoAsync(Guid id)
        {
            var date = DateTime.Today;
            return await _context.SEmployeesOfficesJoins
                .AsNoTracking()
                .Where(w => !w.IsRemove && w.SEmployees.AspNetUserId == id &&
                w.DateStart <= date && (w.DateStop == null || date <= w.DateStop))
                .Select(s => new OfficeDto { Id = s.SOfficesId, OfficeName = s.SOffices.OfficeName })
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Список Тосп для Офисов
        /// </summary>
        public async Task<List<OfficeDto>> GetTospDtoAsync(Guid id)
        {
            return await _context.SOffices
                .AsNoTracking()
                .Where(x => !x.IsRemove && x.ParentId == id && x.SOfficesTypeId == (int)OfficeType.Tosp)
                .Select(x => new OfficeDto
                {
                    Id = x.Id,
                    OfficeName = x.OfficeName
                }).ToListAsync();
        }

        /// <summary>
        /// Список Тосп для Офисов
        /// </summary>
        public async Task<List<OfficeDto>> GetServiceProvidersDepartement(List<Guid> id)
        {
            try
            {
                return await _context.SOffices
                    .AsNoTracking()
                    .Where(x => !x.IsRemove && (id.Any(a => a == x.Id) || (x.ParentId.HasValue && id.Any(a => a == x.ParentId.Value))))
                    .Select(x => new OfficeDto
                    {
                        Id = x.Id,
                        OfficeName = x.OfficeName
                    }).ToListAsync();
            }
            catch (Exception ex)
            {
                return new List<OfficeDto>();
            }
        }

        public async Task<Dictionary<string, string>> GetOfficeTypesAsync()
        {
            return await _context.SOfficesTypes
                .ToDictionaryAsync(x => x.Id.ToString(), x => x.TypeName);
        }

        /// <summary>
        /// Добавить офис
        /// </summary>
        public async Task AddOfficeAsync(OfficeModelDto model)
        {
            try
            {
                var entity = _mapper.Map<SOffice>(model);

                await _context.SOffices.AddAsync(entity);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                var a = ex.Message;
            }
        }

        /// <summary>
        /// Обновить основные данные  офиса
        /// </summary>
        /// <param name="model"></param>
        public async Task UpdateOfficeAsync(OfficeModelDto model)
        {
            var entity = await _context.SOffices.FindAsync(model.Id);
            if (entity == null)
                throw new InvalidOperationException("Данные не найдены!");

            _mapper.Map<OfficeModelDto, SOffice>(model, entity);

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Пометить на удаление офис
        /// </summary>
        /// <param name="Id">Id офиса</param>
        /// <param name="comment">Причина удаления</param>
        public async Task RemoveOfficeAsync(Guid Id, string comment)
        {
            var entity = await _context.SOffices.FindAsync(Id);
            if (entity == null)
                throw new InvalidOperationException("Запись не найдена!");

            entity.IsRemove = true;
            entity.Commentt = comment;

            await _context.SaveChangesAsync();
        }

        public async Task<List<OfficeDto>> GetEmployeesForMfc(List<Guid> employeeId) 
        {
            var date = DateTime.Today;
            var employee = await _context.SEmployeesOfficesJoins.Where(w => employeeId.Contains(w.SOfficesId) &&
                        !w.IsRemove && w.DateStart < date && (!w.DateStop.HasValue || w.DateStop > date))
                .AsNoTracking()
                .Select(s => new OfficeDto
                {
                    Id = s.SEmployeesId,
                    OfficeName = s.SEmployees.EmployeeName
                })
                .ToListAsync();

            return employee;
        }
    }
}
