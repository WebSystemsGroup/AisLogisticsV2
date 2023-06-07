using AisLogistics.DataLayer.Common.Dto.Case;
using AisLogistics.DataLayer.Common.Dto.Reference;
using AisLogistics.DataLayer.Common.Dto.Systems;
using AisLogistics.DataLayer.Entities.Models;
using AutoMapper;

namespace AisLogistics.DataLayer.Common.Mapper
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            //TSource, TDestination

            CreateMap<SDocument, DocumentDto>()
                .ForMember(d => d.Id, opt => opt.MapFrom(s => s.Id))
                .ForMember(d => d.Name, opt => opt.MapFrom(s => s.DocumentName))
                .ReverseMap();

            CreateMap<SOffice, OfficeModelDto>()
                .ForMember(dest => dest.FtpServerName, opt => opt.MapFrom(src=> $"{src.SFtp.FtpServer} {src.SFtp.FtpLogin}"))
                .ForMember(dest => dest.OfficesTypeName, opt => opt.MapFrom(src => src.SOfficesType.TypeName));

            CreateMap<OfficeModelDto, SOffice>()
                .ForMember(dest => dest.EmployeesFioAdd, opt => opt.Condition(src => !string.IsNullOrEmpty(src.EmployeesFioAdd)));

            CreateMap<SOfficesAcquiring,SOfficeAcquiringDto>()
               .ReverseMap();

            CreateMap<SRolesExecutor, RoleExecutorModelDto>()
                .ReverseMap();
            //CreateMap<RoleExecutorModelDto, SRolesExecutor>()
            //    .ForMember(dest => dest.EmployeesFioAdd, opt => opt.Condition(src => !string.IsNullOrEmpty(src.EmployeesFioAdd)));

            CreateMap<SCalendar, CalendarDateModelDto>()
                .ReverseMap();

            CreateMap<SServicesFile, ServiceFileModelDto>();
            CreateMap<ServiceFileModelDto, SServicesFile>()
                .ForMember(dest => dest.FileSize, opt => opt.Condition(src => src.FileSize != 0))
                .ForMember(dest => dest.FileName, opt => opt.Condition(src => !string.IsNullOrEmpty(src.FileName)))
                .ForMember(dest => dest.FileExpansion, opt => opt.Condition(src => !string.IsNullOrEmpty(src.FileExpansion)))
                .ForMember(dest => dest.EmployeesFioAdd, opt => opt.Condition(src => !string.IsNullOrEmpty(src.EmployeesFioAdd)));

            CreateMap<SServicesForm, ServiceBlancModelDto>();
            CreateMap<ServiceBlancModelDto, SServicesForm>()
                .ForMember(dest => dest.FileSize, opt => opt.Condition(src => src.FileSize != 0))
                .ForMember(dest => dest.FileName, opt => opt.Condition(src => !string.IsNullOrEmpty(src.FileName)))
                .ForMember(dest => dest.FileExpansion, opt => opt.Condition(src => !string.IsNullOrEmpty(src.FileExpansion)))
                .ForMember(dest => dest.EmployeesFioAdd, opt => opt.Condition(src => !string.IsNullOrEmpty(src.EmployeesFioAdd)));

            CreateMap<SServicesFile, ReferencesServicesFileModelDto>();
            CreateMap<ReferencesServicesFileModelDto, SServicesFile>()
                .ForMember(dest => dest.FileSize, opt => opt.Condition(src => src.FileSize != 0))
                .ForMember(dest => dest.FileName, opt => opt.Condition(src => !string.IsNullOrEmpty(src.FileName)))
                .ForMember(dest => dest.FileExpansion, opt => opt.Condition(src => !string.IsNullOrEmpty(src.FileExpansion)))
                .ForMember(dest => dest.EmployeesFioAdd, opt => opt.Condition(src => !string.IsNullOrEmpty(src.EmployeesFioAdd)));

            CreateMap<SService, ServiceModelDto>()
                .ForMember(dest => dest.ServiceLivingSituation, opt => opt.MapFrom(src => src.SServicesLivingSituationJoins))
                .ForMember(dest => dest.OfficeName, opt => opt.MapFrom(src => src.SOffice.OfficeName))
                .ForMember(dest => dest.SServicesInteractionName, opt => opt.MapFrom(src => src.SServicesInteraction.InteractionName))
                .ForMember(dest => dest.SServicesTypeName, opt => opt.MapFrom(src => src.SServicesType.TypeName));

            CreateMap<ServiceModelDto, SService>()
                .ForMember(dest => dest.EmployeesFioAdd, opt => opt.Condition(src => !string.IsNullOrEmpty(src.EmployeesFioAdd)));

            CreateMap<ServiceLivingSituationDto, SServicesLivingSituationJoin>();
            CreateMap<SServicesLivingSituationJoin, ServiceLivingSituationDto>()
                .ForMember(dest => dest.EmployeesFioAdd, opt => opt.Condition(src => !string.IsNullOrEmpty(src.EmployeesFioAdd)))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.SServicesLivingSituations.SituationName));

            CreateMap<SServicesCustomer, ServiceCustomerModelDto>();
            CreateMap<ServiceCustomerModelDto, SServicesCustomer>()
                .ForMember(dest => dest.EmployeesFioAdd, opt => opt.Condition(src => !string.IsNullOrEmpty(src.EmployeesFioAdd)));

            CreateMap<SServicesDocument, ServiceDocumentModelDto>();
            CreateMap<ServiceDocumentModelDto, SServicesDocument>()
                .ForMember(dest => dest.EmployeesFioAdd, opt => opt.Condition(src => !string.IsNullOrEmpty(src.EmployeesFioAdd)));

            CreateMap<SServicesDocumentsResult, ServiceDocumentResultModelDto>();
            CreateMap<ServiceDocumentResultModelDto, SServicesDocumentsResult>()
                .ForMember(dest => dest.EmployeesFioAdd, opt => opt.Condition(src => !string.IsNullOrEmpty(src.EmployeesFioAdd)));

            CreateMap<SServicesSmevRequestJoin, ServiceSmevRequestModelDto>();
            CreateMap<ServiceSmevRequestModelDto, SServicesSmevRequestJoin>()
                .ForMember(dest => dest.EmployeesFioAdd, opt => opt.Condition(src => !string.IsNullOrEmpty(src.EmployeesFioAdd)));

            CreateMap<SServicesCustomerTariff, ServiceCustomerTariffModelDto>();
            CreateMap<ServiceCustomerTariffModelDto, SServicesCustomerTariff>()
                .ForMember(dest => dest.EmployeeFioAdd, opt => opt.Condition(src => !string.IsNullOrEmpty(src.EmployeeFioAdd)));
/*
            CreateMap<SServicesFileJoin, ServiceFileModelDto>();
            CreateMap<ServiceFileModelDto, SServicesFileJoin>()
                .ForMember(dest => dest.EmployeesFioAdd, opt => opt.Condition(src => !string.IsNullOrEmpty(src.EmployeesFioAdd)));*/

            CreateMap<SServicesWayGetJoin, ServiceWayGetModelDto>();
            CreateMap<ServiceWayGetModelDto, SServicesWayGetJoin>()
                .ForMember(dest => dest.EmployeesFioAdd, opt => opt.Condition(src => !string.IsNullOrEmpty(src.EmployeesFioAdd)));

            CreateMap<SServicesReason, ServiceReasonModelDto>();
            CreateMap<ServiceReasonModelDto, SServicesReason>()
                .ForMember(dest => dest.EmployeesFioAdd, opt => opt.Condition(src => !string.IsNullOrEmpty(src.EmployeesFioAdd)));

            CreateMap<SServicesTypeQualityJoin, ServiceQualityTypeModelDto>();
            CreateMap<ServiceQualityTypeModelDto, SServicesTypeQualityJoin>()
                .ForMember(dest => dest.EmployeesFioAdd, opt => opt.Condition(src => !string.IsNullOrEmpty(src.EmployeesFioAdd)));

            CreateMap<SServicesProcedure, ServiceProcedureModelDto>();
            CreateMap<ServiceProcedureModelDto, SServicesProcedure>()
                .ForMember(dest => dest.EmployeesFioAdd, opt => opt.Condition(src => !string.IsNullOrEmpty(src.EmployeesFioAdd)));

            CreateMap<SServicesRoutesStageRole, ServicesStageRoleModelDto>();
            CreateMap<ServicesStageRoleModelDto, SServicesRoutesStageRole>()
                .ForMember(dest => dest.EmployeesFioAdd, opt => opt.Condition(src => !string.IsNullOrEmpty(src.EmployeesFioAdd)));

            CreateMap<SServicesActive, ServiceActivityModelDto>()
                .ReverseMap();

            CreateMap<SSmev, SmevServiceModelDto>()
                .ReverseMap();

            CreateMap<SSmevRequest, SmevRequestModelDto>()
                .ReverseMap();

            CreateMap<SEmployee, EmployeeModelDto>();
            CreateMap<EmployeeModelDto, SEmployee>()
                .ForMember(dest => dest.EmployeesFioAdd, opt => opt.Condition(src => !string.IsNullOrEmpty(src.EmployeesFioAdd)));

            CreateMap<EmployeeAddModelDto, EmployeeModelDto>();

            CreateMap<SEmployeesOfficesJoin, EmployeeOfficeModelDto>();
            CreateMap<EmployeeOfficeModelDto, SEmployeesOfficesJoin>()
                .ForMember(dest => dest.EmployeeFioAdd, opt => opt.Condition(src => !string.IsNullOrEmpty(src.EmployeeFioAdd)));

            CreateMap<SEmployeesStatusJoin, EmployeeStatusModelDto>()
                .ReverseMap();
            //CreateMap<EmployeeStatusModelDto, SEmployeesStatusJoin>()
            //    .ForMember(dest => dest.EmployeesFioAdd, opt => opt.Condition(src => !string.IsNullOrEmpty(src.EmployeesFioAdd)));

            CreateMap<SEmployeesExecution, EmployeeExecutionModelDto>()
                .ReverseMap();
            //CreateMap<EmployeeExecutionModelDto, SEmployeesExecution>()
            //    .ForMember(dest => dest.EmployeesFioAdd, opt => opt.Condition(src => !string.IsNullOrEmpty(src.EmployeesFioAdd)));

            CreateMap<SEmployeesActive, EmployeeActivityModelDto>()
                .ReverseMap();

            CreateMap<SEmployeesRolesExecutor, EmployeeRoleExecutorModelDto>()
                .ReverseMap();

            CreateMap<DInformation, InformationModelDto>();
            CreateMap<InformationModelDto, DInformation>()
                .ForMember(dest => dest.EmployeesFioAdd, opt => opt.Condition(src => !string.IsNullOrEmpty(src.EmployeesFioAdd)))
                .ForMember(dest => dest.EmployeesJobPositionAdd, opt => opt.Condition(src => !string.IsNullOrEmpty(src.EmployeesJobPositionAdd)));

            CreateMap<SFtp, FtpModelDto>()
                .ReverseMap();

            CreateMap<SServicesLivingSituation, LivingSituationModelDto>()
               .ReverseMap();

            CreateMap<SStateTask, StateTaskDto>()
                .ReverseMap();

            CreateMap<DIasMkguInfomatAnswer, IasMkguInfomatAnswerDto>()
                .ReverseMap();

            CreateMap<DIasMkguInfomatAnswerCommentt, IasMkguInfomatAnswerCommenttDto>()
                .ReverseMap();

            CreateMap<SServicesRoutesStage, ServiceStageModelDto>();
            CreateMap<ServiceStageModelDto, SServicesRoutesStage>()
                .ForMember(dest=> dest.ParentId, opt => opt.MapFrom(src => src.ParentId))
                .ForMember(dest => dest.SServicesId, opt => opt.MapFrom(src => src.SServicesId))
                 .ForMember(dest => dest.SRoutesStageId, opt => opt.MapFrom(src => src.SRoutesStageId))
                .ForMember(dest => dest.EmployeesFioAdd, opt => opt.Condition(src => !string.IsNullOrEmpty(src.EmployeesFioAdd)));

            CreateMap<DServicesCustomer, CustomerModelDto>();
            CreateMap<CustomerModelDto, DServicesCustomer>()
                .ForMember(dest => dest.DCasesId, opt => opt.Condition(src => !string.IsNullOrEmpty(src.DCasesId)))
                .ForMember(dest => dest.FirstName, opt => opt.Condition(src => !string.IsNullOrEmpty(src.FirstName)))
                .ForMember(dest => dest.LastName, opt => opt.Condition(src => !string.IsNullOrEmpty(src.LastName)));

        }
    }
}