using AisLogistics.App.Extensions;
using AisLogistics.App.Models;
using AisLogistics.App.Models.DTO.References;
using AisLogistics.App.Models.DTO.Services;
using AisLogistics.DataLayer.Common.Dto.Reference;
using AisLogistics.DataLayer.Concrete;
using AisLogistics.DataLayer.Entities.Models;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Globalization;

namespace AisLogistics.App.Service
{
    public partial class ReferencesService : IReferencesService
    {
        /// <summary>
        /// Все этапы
        /// </summary>
        public async Task<List<StageDto>> GetAllRoutesStagesAsync()
        {
            return await _context.SRoutesStages
                .AsNoTracking()
                .OrderBy(x => x.StageName)
                .Select(x => new StageDto(x.Id, x.StageName))
                .ToListAsync();
        }

        public async Task<List<StatusDto>> GetAllStatuseAsync()
        {
            return await _context.SServicesStatuses
              .AsNoTracking()
              .OrderBy(x => x.Id)
              .Select(x => new StatusDto(x.Id, x.StatusName))
              .ToListAsync();
        }
    }
}
