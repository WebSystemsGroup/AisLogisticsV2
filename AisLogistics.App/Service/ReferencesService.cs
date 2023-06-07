using AisLogistics.DataLayer.Concrete;
using AutoMapper;

namespace AisLogistics.App.Service
{
    public partial class ReferencesService : IReferencesService
    {
        private readonly AisLogisticsContext _context;
        private readonly IMapper _mapper;
        private IWebHostEnvironment _environment;

        public ReferencesService(AisLogisticsContext context, IMapper mapper, IWebHostEnvironment environment)
        {
            _context = context;
            _mapper = mapper;
            _environment = environment;
        }
    }
}
