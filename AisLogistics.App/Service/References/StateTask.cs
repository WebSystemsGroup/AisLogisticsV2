using AisLogistics.App.Models.DTO.References;
using AisLogistics.DataLayer.Common.Dto.Reference;
using AisLogistics.DataLayer.Entities.Models;
using DataTables.AspNet.Core;
using Microsoft.EntityFrameworkCore;

namespace AisLogistics.App.Service
{
    // Справочники - Гос задние

    /// <summary>
    /// Получить список Гос задание
    /// </summary>
    public partial class ReferencesService : IReferencesService
    {
        public async Task<(List<StateTasksDto>, int, int)> GetStateTask(IDataTablesRequest request)
        {
            var results = _context.SStateTasks
                .AsNoTracking();
               
            int totalCount = results.Count();

            var dataPage = await results
                .Skip(request.Start)
                .Take(request.Length)
                .Select(x => new StateTasksDto()
                {
                  Id = x.Id,
                  CountService = x.CountService,
                  YearTask = x.YearTask,
                  EmployeeFioAdd = x.EmployeesFioAdd,
                  DateAdd = x.DateAdd.ToShortDateString(),
                }).ToListAsync();

            return (dataPage, totalCount, totalCount);
        }

        /// <summary>
        /// Модель для редактирования FTP
        /// </summary>
        public async Task<StateTaskDto> GetStateTaskDtoAsync(Guid? Id)
        {
            try
            {
                if (Id is null)
                    throw new ArgumentNullException(nameof(Id));

                var result = await _context.SStateTasks.FindAsync(Id);
                if (result == null)
                    throw new InvalidOperationException("Данные не найдены!");

                return _mapper.Map<StateTaskDto>(result);
            }

            catch (Exception ex)
            {
                var a = ex;

                return null;
            }
        }

        public async Task AddStateTaskAsync(StateTaskDto model, string employeeFioAdd)
        {

            var entity = _mapper.Map<SStateTask>(model);

            entity.EmployeesFioAdd = employeeFioAdd;
            entity.DateAdd = DateTime.Now;
            await _context.SStateTasks.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateStateTaskAsync(StateTaskDto model)
        {
            var entity = await _context.SStateTasks.FindAsync(model.Id);
            if (entity == null)
                throw new Exception("Данные не найдены!");

            _mapper.Map<StateTaskDto, SStateTask>(model, entity);

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Пометить на удаление (удалить?) Гос задание
        /// </summary>
        /// <param name="Id">ID записи</param>
        /// <param name="comment">Причина удаления</param>
        public async Task RemoveStateTaskAsync(Guid Id, string comment)
        {
            try
            {
                var entity = await _context.SStateTasks.FindAsync(Id);
                if (entity == null)
                    throw new Exception("Запись не найдена!");

                _context.Remove(entity);

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                var a = ex.Message;
            }
        }
    }
}
