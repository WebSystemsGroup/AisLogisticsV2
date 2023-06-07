using AisLogistics.App.Data;
using AisLogistics.App.Models.DTO.Identity;
using AutoMapper;

namespace AisLogistics.App.Mapping
{
    public class IdentityProfile : Profile
    {
        public IdentityProfile()
        {
            //TSource, TDestination

            CreateMap<ApplicationUser, IdentityUserDto>();

            CreateMap<ApplicationRole, IdentityRoleDto>();

            CreateMap<IdentityUserDto, ApplicationUser>()//(MemberList.Source)
                .ForMember(dest => dest.Id, opt => opt.Condition(srs => srs.Id != null));
        }
    }
}
