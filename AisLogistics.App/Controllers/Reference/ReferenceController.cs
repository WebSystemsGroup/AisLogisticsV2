using AisLogistics.App.Data;
using AisLogistics.App.Models.DTO.Identity;
using AisLogistics.App.Service;
using AisLogistics.DataLayer.Concrete;
using AisLogistics.DataLayer.Core.IConfiguration;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using SmartBreadcrumbs.Attributes;

namespace AisLogistics.App.Controllers.Reference
{
    [Authorize]
    public partial class ReferenceController : AbstractController
    {
        private readonly ILogger<ReferenceController> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly AisLogisticsContext _context;
        private readonly RoleManager<ApplicationRole> _roleManager;

        private readonly IReferencesService _referencesService;
        private readonly IIdentityService<IdentityUserDto, ApplicationUser, ApplicationRole, Guid> _identityService;
        private readonly IMapper _mapper;

        public ReferenceController(ILogger<ReferenceController> logger,
            IUnitOfWork unitOfWork,
            AisLogisticsContext context,
            RoleManager<ApplicationRole> roleManager,
            EmployeeManager employeeManager,
            IIdentityService<IdentityUserDto, ApplicationUser, ApplicationRole, Guid> identityService,
            IReferencesService referenceService,
            IMapper mapper) : base(employeeManager)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
            _context = context;
            _roleManager = roleManager;
            _referencesService = referenceService;
            _identityService = identityService;
            _mapper = mapper;
        }

        [Breadcrumb("Справочники", FromAction = nameof(Index), FromController = typeof(HomeController))]
        public IActionResult Index()
        {
            return RedirectToAction("Index", "Home");
        }
    }
}
