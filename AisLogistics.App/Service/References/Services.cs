using AisLogistics.App.Models.DTO.References;
using AisLogistics.App.Models.DTO.Template;
using AisLogistics.DataLayer.Common.Dto.Reference;
using AisLogistics.DataLayer.Entities.Models;
using DataTables.AspNet.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NinjaNye.SearchExtensions;
using NUglify.Helpers;

namespace AisLogistics.App.Service
{
    // Справочники - Услуги

    public partial class ReferencesService : IReferencesService
    {
        /// <summary>
        /// Поучить список услуг
        /// </summary>
        /// <param name="request">Запрос</param>
        public async Task<(List<ServicesDto>, int, int)> GetServices(IDataTablesRequest request)
        {
            Guid? providers = null;
            var providesTypeColumn = request?.Columns?.Where(w => w.Name == "providers").Select(s => s.Search.Value).FirstOrDefault();

            if (providesTypeColumn != null)
                providers = new Guid(providesTypeColumn);

            int? types = null;
            var typesTypeColumn = request?.Columns?.Where(w => w.Name == "types").Select(s => s.Search.Value).FirstOrDefault();

            if (typesTypeColumn != null)
                types = int.Parse(typesTypeColumn);

            string? hashtags = null;
            var hashtagsTypeColumn = request?.Columns?.Where(w => w.Name == "hashtags").Select(s => s.Search.Value).FirstOrDefault();

            if (hashtagsTypeColumn != null)
                hashtags = hashtagsTypeColumn;

            var results = _context.SServices
                .AsNoTracking()
                //.Include(i => i.SOffice)
                .Where(x => !x.IsRemove &&
                (!providers.HasValue || x.SOfficesId == providers) &&
                (!types.HasValue || x.SServicesTypeId == types));

            if (hashtags is not null)
            {
                results = results.Search(s => s.Hashtag.ToLower()).Containing(hashtags.Split(" "));
            }

            int totalCount = results.Count();

            var filteredResult = string.IsNullOrEmpty(request.Search.Value)
               ? results
               : results.Search(s => s.ServiceName.ToLower(),
                                s => s.ServiceNameSmall.ToLower())
               .ContainingAll(request.Search.Value.ToLower());

            int filteredCount = filteredResult.Count();

            var dataPage = await filteredResult
                .OrderBy(x => x.ServiceName)
                .Skip(request.Start)
                .Take(request.Length)
                .Select(x => new ServicesDto()
                {
                    Id = x.Id,
                    ServiceName = x.ServiceNameSmall,
                    OfficeName = x.SOffice.OfficeName
                }).ToListAsync();

            return (dataPage, totalCount, filteredCount);
        }

        /// <summary>
        /// Модель для добавления / редактирования услуги
        /// </summary>
        public async Task<ServiceModelDto> GetServiceDtoAsync(Guid? Id)
        {
            try
            {
                if (Id is null)
                    throw new ArgumentNullException(nameof(Id));

                var result = await _context.SServices
                    .Include(i => i.SOffice)
                    .Include(i => i.SServicesType)
                    .Include(i => i.SServicesInteraction)
                    .Include(i => i.SServicesLivingSituationJoins)
                        .ThenInclude(i => i.SServicesLivingSituations)
                    .FirstOrDefaultAsync(f => f.Id == Id);
                if (result == null)
                    throw new Exception("Данные не найдены!");

                var model = _mapper.Map<ServiceModelDto>(result);

                model.LivingSituationId = result.SServicesLivingSituationJoins.Select(s => s.SServicesLivingSituationsId).ToList();

                if (result.Hashtag is not null)
                    model.HashtagId = result.Hashtag.Split('#').ToList();

                return model;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// Поучить список услуг
        /// </summary>
        public async Task<List<ServicesDto>> GetServicesAll()
        {
            return await _context.SServices
                 .AsNoTracking()
                 .Where(x => !x.IsRemove)
                 .OrderBy(x => x.ServiceName)
                 .Select(x => new ServicesDto()
                 {
                     Id = x.Id,
                     ServiceName = x.ServiceName
                 }).ToListAsync();
        }

        /// <summary>
        /// Поучить список услуг
        /// </summary>
        public async Task<List<ServicesDto>> GetServicesAll(List<Guid> id)
        {
            return await _context.SServices
                 .AsNoTracking()
                 .Where(x => !x.IsRemove && id.Any(a=>a==x.Id))
                 .OrderBy(x => x.ServiceName)
                 .Select(x => new ServicesDto()
                 {
                     Id = x.Id,
                     ServiceName = x.ServiceName
                 }).ToListAsync();
        }

        /// <summary>
        /// Поучить список услуг для поставщика
        /// </summary>
        public async Task<List<ServicesDto>> GetServicesForProviders(List<Guid> provides)
        {
            if (provides.Any())
            {
                return await _context.SServices
                    .AsNoTracking()
                    .Where(x => !x.IsRemove && provides.Contains((Guid)x.SOfficesId))
                    .OrderBy(x => x.ServiceName)
                     .Select(x => new ServicesDto()
                     {
                         Id = x.Id,
                         ServiceName = x.ServiceName
                     }).ToListAsync();
            }
            else
            {
                return await GetServicesAll();
            }
        }

        /// <summary>
        /// Добавить услугу
        /// </summary>
        public async Task AddServiceAsync(ServiceModelDto model)
        {
            model.Hashtag = string.Join("", model.HashtagId.Select(s => "#" + s));

            var entity = _mapper.Map<SService>(model);

            entity.SServicesLivingSituationJoins = model.LivingSituationId.Select(s => new SServicesLivingSituationJoin { SServicesLivingSituationsId = s, EmployeesFioAdd = model.EmployeesFioAdd }).ToList();

            entity.SServicesRoutesStages.Add(new SServicesRoutesStage
            {
                SRoutesStageId = 1,
                ParentId = Guid.Empty,
                EmployeesFioAdd = model.EmployeesFioAdd
            });

            await _context.SServices.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Обновить основные данные услуги
        /// </summary>
        public async Task UpdateServiceAsync(ServiceModelDto model)
        {
            try
            {
                var entity = await _context.SServices.FindAsync(model.Id);
                if (entity == null)
                    throw new Exception("Данные не найдены!");


                model.Hashtag = string.Join("", model.HashtagId.Select(s => "#" + s));

                _mapper.Map(model, entity);


                var livingSituayion = await _context.SServicesLivingSituationJoins.Where(w => w.SServicesId == model.Id).Select(s => s.SServicesLivingSituationsId).ToListAsync();

                entity.SServicesLivingSituationJoins = model.LivingSituationId.Where(w => !livingSituayion.Contains(w)).Select(s => new SServicesLivingSituationJoin { SServicesLivingSituationsId = s, EmployeesFioAdd = entity.EmployeesFioAdd }).ToList();

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Запись не добавлена!");
            }
        }

        /// <summary>
        /// Пометить на удаление услугу
        /// </summary>
        /// <param name="Id">Id услуги</param>
        /// <param name="comment">Причина удаления</param>
        public async Task RemoveServiceAsync(Guid Id, string comment)
        {
            var entity = await _context.SServices.FindAsync(Id);
            if (entity == null)
                throw new Exception("Запись не найдена!");

            entity.IsRemove = true;
            entity.Commentt = comment;

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Копирование услуги
        /// </summary>
        /// <param name="Id">Id услуги</param>
        public async Task CopyServiceAsync(Guid Id, string EmployeeFio)
        {
            IDbContextTransaction transaction = null;
            try
            {
                var entity = await _context.SServices.AsNoTracking().FirstOrDefaultAsync(f => f.Id == Id);

                if (entity == null)
                    throw new Exception("Запись не найдена!");

                var newId = Guid.NewGuid();

                var sServicesLivingSituationJoins = await _context.SServicesLivingSituationJoins
                    .AsNoTracking()
                    .Where(w => !w.IsRemove && w.SServicesId == entity.Id)
                    .Select(s => new SServicesLivingSituationJoin
                    {
                        SServicesId = newId,
                        SServicesLivingSituationsId = s.SServicesLivingSituationsId,
                        IsRemove = s.IsRemove,
                        Commentt = s.Commentt,
                        DateAdd = DateTime.Now,
                        EmployeesFioAdd = EmployeeFio,
                    }).ToListAsync();

                var sServicesProcedures = await _context.SServicesProcedures
                    .AsNoTracking()
                    .Where(w => !w.IsRemove && w.SServicesId == entity.Id)
                    .Select(s => new SServicesProcedure
                    {
                        SServicesId = newId,
                        ProcedureName = s.ProcedureName,
                        ProcedureActive = s.ProcedureActive,
                        ExtraFormPath = s.ExtraFormPath,
                        FrguTargetId = s.FrguTargetId,
                        IsRemove = s.IsRemove,
                        DateAdd = DateTime.Now,
                        EmployeesFioAdd = EmployeeFio,
                    }).ToListAsync();

                var sServicesCustomers = await _context.SServicesCustomers
                    .AsNoTracking()
                    .Where(w => !w.IsRemove && w.SServicesId == entity.Id)
                    .Select(s => new SServicesCustomer
                    {
                        SServicesId = newId,
                        SServicesCustomerTypeId = s.SServicesCustomerTypeId,
                        RepresentativeList = s.RepresentativeList,
                        RepresentativeDocument = s.RepresentativeDocument,
                        RepresentativeSpecification = s.RepresentativeSpecification,
                        Representative = s.Representative,
                        IsRemove = s.IsRemove,
                        DateAdd = DateTime.Now,
                        EmployeesFioAdd = EmployeeFio,
                        SServicesCustomerTariffs = s.SServicesCustomerTariffs.Where(w => !w.IsRemove).Select(ss => new SServicesCustomerTariff
                        {
                            SServicesProcedureId = ss.SServicesProcedureId,
                            SServicesTariffTypeId = ss.SServicesTariffTypeId,
                            SServicesWeekId = ss.SServicesWeekId,
                            CountDayProcessing = ss.CountDayProcessing,
                            CountDayExecution = ss.CountDayExecution,
                            CountDayReturn = ss.CountDayReturn,
                            Tariff = ss.Tariff,
                            TariffMfc = ss.TariffMfc,
                            Commentt = ss.Commentt,
                            IsRemove = ss.IsRemove,
                            DateAdd = DateTime.Now,
                            EmployeeFioAdd = EmployeeFio,
                        }).ToList()
                    }).ToListAsync();

                var sServicesDocuments = await _context.SServicesDocuments
                    .AsNoTracking()
                    .Where(w => !w.IsRemove && w.SServicesId == entity.Id)
                    .Select(s => new SServicesDocument
                    {
                        SServicesId = newId,
                        SServicesProcedureId = s.SServicesProcedureId,
                        SDocumentsId = s.SDocumentsId,
                        DocumentNeeds = s.DocumentNeeds,
                        DocumentType = s.DocumentType,
                        DocumentCount = s.DocumentCount,
                        IsRemove = s.IsRemove,
                        DateAdd = DateTime.Now,
                        EmployeesFioAdd = EmployeeFio,
                    }).ToListAsync();

                var sServicesDocumentsResults = await _context.SServicesDocumentsResults
                    .AsNoTracking()
                    .Where(w => !w.IsRemove && w.SServicesId == entity.Id)
                    .Select(s => new SServicesDocumentsResult
                    {
                        SServicesId = newId,
                        SServicesProcedureId = s.SServicesProcedureId,
                        SDocumentsId = s.SDocumentsId,
                        DocumentResult = s.DocumentResult,
                        DocumentMethod = s.DocumentMethod,
                        DocumentPeriodProvider = s.DocumentPeriodProvider,
                        DocumentPeriodMfc = s.DocumentPeriodMfc,
                        IsRemove = s.IsRemove,
                        DateAdd = DateTime.Now,
                        EmployeesFioAdd = EmployeeFio
                    }).ToListAsync();

                var sServicesSmevRequestJoins = await _context.SServicesSmevRequestJoins
                    .AsNoTracking()
                    .Where(w => !w.IsRemove && w.SServicesId == entity.Id)
                    .Select(s => new SServicesSmevRequestJoin
                    {
                        SServicesId = newId,
                        SSmevRequestId = s.SSmevRequestId,
                        IsRemove = s.IsRemove,
                        DateAdd = DateTime.Now,
                        EmployeesFioAdd = EmployeeFio
                    }).ToListAsync();

                var sServicesRoutesStages = await _context.SServicesRoutesStages
                    .AsNoTracking()
                    .Where(w => !w.IsRemove && w.SServicesId == entity.Id)
                    .Include(i => i.SServicesRoutesStageRoles.Where(w => !w.IsRemove))
                    .Select(s => new SServicesRoutesStage
                    {
                        Id = s.Id,
                        SServicesId = newId,
                        ParentId = s.ParentId,
                        SRoutesStageId = s.SRoutesStageId,
                        IsRemove = s.IsRemove,
                        Commentt = s.Commentt,
                        DateAdd = DateTime.Now,
                        EmployeesFioAdd = EmployeeFio,
                        SServicesRoutesStageRoles = s.SServicesRoutesStageRoles.Where(w => !w.IsRemove).Select(ss => new SServicesRoutesStageRole
                        {
                            SRolesExecutorId = ss.SRolesExecutorId,
                            IsRemove = ss.IsRemove,
                            DateAdd = DateTime.Now,
                            EmployeesFioAdd = EmployeeFio
                        }).ToList()
                    }).ToListAsync();

                sServicesRoutesStages.ForEach(f =>
                {
                    var sServicesRoutesStageId = Guid.NewGuid();

                    sServicesRoutesStages.Where(w => w.ParentId == f.Id).ForEach(ff =>
                    {
                        ff.ParentId = sServicesRoutesStageId;
                    });

                    f.Id = sServicesRoutesStageId;
                });

                var sServicesForms = await _context.SServicesForms
                    .AsNoTracking()
                    .Where(w => !w.IsRemove && w.SServicesId == entity.Id)
                    .Select(s => new SServicesForm
                    {
                        SServicesId = newId,
                        SServicesProcedureId = s.SServicesProcedureId,
                        FileName = s.FileName,
                        FileExpansion = s.FileExpansion,
                        FileSize = s.FileSize,
                        IsRemove = s.IsRemove,
                        Commentt = s.Commentt,
                        DateAdd = DateTime.Now,
                        EmployeesFioAdd = EmployeeFio
                    }).ToListAsync();


                var sServicesFiles = await _context.SServicesFiles
                    .AsNoTracking()
                    .Where(w => !w.IsRemove && w.SServicesId == entity.Id)
                    .Select(s => new SServicesFile
                    {
                        SServicesId = newId,
                        SServicesProcedureId = s.SServicesProcedureId,
                        FileName = s.FileName,
                        FileExpansion = s.FileExpansion,
                        FileSize = s.FileSize,
                        IsRemove = s.IsRemove,
                        Commentt = s.Commentt,
                        DateAdd = DateTime.Now,
                        EmployeesFioAdd = EmployeeFio
                    }).ToListAsync();

                var sServicesWayGetJoins = await _context.SServicesWayGetJoins
                    .AsNoTracking()
                    .Where(w => !w.IsRemove && w.SServicesId == entity.Id)
                    .Select(s => new SServicesWayGetJoin
                    {
                        SServicesId = newId,
                        SServicesWayGetId = s.SServicesWayGetId,
                        WayType = s.WayType,
                        IsRemove = s.IsRemove,
                        DateAdd = DateTime.Now,
                        EmployeesFioAdd = EmployeeFio
                    }).ToListAsync();

                var sServicesReasons = await _context.SServicesReasons
                    .AsNoTracking()
                    .Where(w => !w.IsRemove && w.SServicesId == entity.Id)
                    .Select(s => new SServicesReason
                    {
                        SServicesId = newId,
                        CountDay = s.CountDay,
                        SServicesWeekId = s.SServicesWeekId,
                        ReasonText = s.ReasonText,
                        ReasonType = s.ReasonType,
                        LegalAct = s.LegalAct,
                        IsRemove = s.IsRemove,
                        Commentt = s.Commentt,
                        DateAdd = DateTime.Now,
                        EmployeesFioAdd = EmployeeFio
                    }).ToListAsync();

                var sServicesTypeQualityJoins = await _context.SServicesTypeQualityJoins
                    .AsNoTracking()
                    .Where(w => !w.IsRemove && w.SServicesId == entity.Id)
                    .Select(s => new SServicesTypeQualityJoin
                    {
                        SServicesId = newId,
                        SServicesTypeQualityId = s.SServicesTypeQualityId,
                        IsRemove = s.IsRemove,
                        DateAdd = DateTime.Now,
                        EmployeesFioAdd = EmployeeFio
                    }).ToListAsync();


                var sServicesActives = await _context.SServicesActives
                    .AsNoTracking()
                    .Where(w => !w.IsRemove && w.SServicesId == entity.Id)
                    .Select(s => new SServicesActive
                    {
                        SServicesId = newId,
                        SOfficesId = s.SOfficesId,
                        DateStart = s.DateStart,
                        DateStop = s.DateStop,
                        IsRemove = s.IsRemove,
                        Commentt = s.Commentt,
                        DateAdd = DateTime.Now,
                        EmployeeFioAdd = EmployeeFio
                    }).ToListAsync();

                entity.Id = newId;
                entity.ServiceName = "(Копия) " + entity.ServiceName;
                entity.ServiceNameSmall = "(Копия) " + entity.ServiceNameSmall;
                entity.EmployeesFioAdd = EmployeeFio;
                entity.DateAdd = DateTime.Now;

                transaction = await _context.Database.BeginTransactionAsync();

                await _context.AddAsync(entity);

                if (sServicesLivingSituationJoins.Any()) await _context.AddRangeAsync(sServicesLivingSituationJoins);
                if (sServicesProcedures.Any()) await _context.AddRangeAsync(sServicesProcedures);
                if (sServicesCustomers.Any()) await _context.AddRangeAsync(sServicesCustomers);
                if (sServicesDocuments.Any()) await _context.AddRangeAsync(sServicesDocuments);
                if (sServicesDocumentsResults.Any()) await _context.AddRangeAsync(sServicesDocumentsResults);
                if (sServicesSmevRequestJoins.Any()) await _context.AddRangeAsync(sServicesSmevRequestJoins);
                if (sServicesRoutesStages.Any()) await _context.AddRangeAsync(sServicesRoutesStages);
                if (sServicesForms.Any()) await _context.AddRangeAsync(sServicesForms);
                if (sServicesFiles.Any()) await _context.AddRangeAsync(sServicesFiles);
                if (sServicesWayGetJoins.Any()) await _context.AddRangeAsync(sServicesWayGetJoins);
                if (sServicesReasons.Any()) await _context.AddRangeAsync(sServicesReasons);
                if (sServicesTypeQualityJoins.Any()) await _context.AddRangeAsync(sServicesTypeQualityJoins);
                if (sServicesActives.Any()) await _context.AddRangeAsync(sServicesActives);

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                var a = ex.Message;
                if (transaction != null) await transaction.RollbackAsync();
                throw new Exception(MessageDescription.Error);
            }
        }

        /// <summary>
        /// Реестр типов услуг
        /// </summary>
        public async Task<List<SelectListDto<int>>> GetServiceTypesAll()
        {
            return await _context.SServicesTypes
                .AsNoTracking()
                .Select(s => new SelectListDto<int>(s.Id, s.TypeName))
                .ToListAsync();
        }

        /// <summary>
        /// Реестр способа взаимодействия
        /// </summary>
        public async Task<List<SelectListDto<int>>> GetServiceInteractionsAll()
        {
            return await _context.SServicesInteractions
                .AsNoTracking()
                .Select(s => new SelectListDto<int>(s.Id, s.InteractionName))
                .ToListAsync();
        }

        /// <summary>
        /// Реестр жизненых ситуаций
        /// </summary>
        public async Task<List<SelectListDto<Guid>>> GetServiceLivingSituationAll()
        {
            return await _context.SServicesLivingSituations
                .AsNoTracking()
                .Select(s => new SelectListDto<Guid>(s.Id, s.SituationName))
                .ToListAsync();
        }

        /// <summary>
        /// Реестр Хэштегов
        /// </summary>
        public async Task<List<SelectListDto<int>>> GetServiceHashtagAll()
        {
            return await _context.SHashtags
                .AsNoTracking()
                .Select(s => new SelectListDto<int>(s.Id, s.HashtagName))
                .ToListAsync();
        }
    }
}
