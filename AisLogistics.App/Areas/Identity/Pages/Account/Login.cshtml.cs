using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AisLogistics.App.Data;
using AisLogistics.App.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using NuGet.Protocol.Core.Types;

namespace AisLogistics.App.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly EmployeeManager _employeeManager;
        private readonly IReferencesService _referencesService;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(UserManager<ApplicationUser> userManager, 
            SignInManager<ApplicationUser> signInManager,
            EmployeeManager employeeManager,
            IReferencesService referencesService,
            ILogger<LoginModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _employeeManager = employeeManager;
            _referencesService = referencesService;
            _logger = logger;
        }
        
        [BindProperty]
        public InputModel Input { get; set; }
        
        public IList<AuthenticationScheme> ExternalLogins { get; set; }
        
        public string ReturnUrl { get; set; }
        
        [TempData]
        public string ErrorMessage { get; set; }
        
        public class InputModel
        {
            [Required]
            public string Username { get; set; }
            
            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }
            
            public Guid? OfficeId { get; set; }

            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnGetTosp(string username)
        {
            var user = await _userManager.FindByNameAsync(username);
            if (user is null) return new JsonResult("");

            var Employeeoffice = await _referencesService.GetActiveEmployeeOfficeDtoAsync(user.Id);
            if (Employeeoffice is null) return new JsonResult("");

            var offices = await _referencesService.GetAllOfficesTypeMfcAndTospAsync();
            if (offices is null) return new JsonResult("");


            return new JsonResult(offices.Select(s => new
            {
                id = s.Id,
                text = s.OfficeName,
                selected = s.Id == Employeeoffice.Id
            }));
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ReturnUrl = returnUrl;

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (!ModelState.IsValid) return Page();
            var user = await _userManager.FindByNameAsync(Input.Username);
            if (user is null)
            {
                ModelState.AddModelError(string.Empty, "Неверная попытка входа.");
                return Page();
            }

            var result = await _signInManager.PasswordSignInAsync(user, Input.Password, Input.RememberMe,
                lockoutOnFailure: false);
            if (result.Succeeded)
            {
                var claims = await _userManager.GetClaimsAsync(user);
                var claim = claims.FirstOrDefault(f => f.Type == "OfficeId");
                if (claim is not null)
                    await _userManager.RemoveClaimAsync(user, claim);
                if (Input.OfficeId.HasValue)
                    await _userManager.AddClaimAsync(user, new Claim("OfficeId", Input.OfficeId.ToString()));
                _logger.LogInformation("User logged in.");

                await _employeeManager.AddSEmployeesAuthorizationLog(user.Id, Input.OfficeId.Value);
             
                return LocalRedirect(returnUrl);
            }

            if (result.RequiresTwoFactor)
            {
                return RedirectToPage("./LoginWith2fa",
                    new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("Учетная запись пользователя заблокирована.");
                return RedirectToPage("./Lockout");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Неверная попытка входа.");
                return Page();
            }

        }
    }
}
