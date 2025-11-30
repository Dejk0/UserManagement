using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.ComponentModel.DataAnnotations;
using System.Text;
using UserManagement;
using UserManagement.Dtos.Auth;

namespace Services.Registration
{
    public class RegisterService : ControllerBase
    {
        private readonly SignInManager<AppUser> _signInManager;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IUserStore<AppUser> _userStore;
        private readonly IUserEmailStore<AppUser> _emailStore;
        //private readonly IEmailService _emailService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public RegisterService(
            UserManager<AppUser> userManager,
            IUserStore<AppUser> userStore,
            SignInManager<AppUser> signInManager,
            //IEmailService emailSender,
            IHttpContextAccessor httpContextAccessor,
            DataContext context
            )
        {
            _userManager = userManager;
            _userStore = userStore;
            _signInManager = signInManager;
            //_emailService = emailSender;
            _httpContextAccessor = httpContextAccessor;
            _emailStore = GetEmailStore();
        }


        private AppUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<AppUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(AppUser)}'. " +
                    $"Ensure that '{nameof(AppUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
            }
        }
        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }
        }

        public async Task<RegistrationResultDto> RegisterAsync(RegistrationParamDto @params)
        {
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            if (ModelState.IsValid)
            {
                if (@params.Password != @params.ConfirmPassword)
                {
                    return new RegistrationResultDto
                    {
                        Success = false,
                        Error = "Passwords do not match."
                    };
                }

                if (await _userManager.FindByEmailAsync(@params.Email) != null)
                {
                    return new RegistrationResultDto
                    {
                        Success = false,
                        Error = "This email is already taken."
                    };
                }

                var user = CreateUser();
                await _userStore.SetUserNameAsync(user, @params.UserName, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, @params.Email, CancellationToken.None);
                var result = await _userManager.CreateAsync(user, @params.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Guest");
                    var userId = await _userManager.GetUserIdAsync(user);
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var request = _httpContextAccessor.HttpContext?.Request;
                    var callbackUrl = $"{request?.Scheme}://{request?.Host}/api/Auth/confirm-email?userId={Uri.EscapeDataString(userId)}&code={Uri.EscapeDataString(code)}";

                    //await _emailService.Send(@params.Email, await _appLocalizer.TAsync(2102, "Confirm your email."),
                    //    await _appLocalizer.TAsync(2092, "Hi!") + "\n\n " +
                    //    await _appLocalizer.TAsync(2103, "Please confirm your account by click here.") + " " +
                    //      $" <a href='{callbackUrl}'>link</a>.");

                    if (_userManager.Options.SignIn.RequireConfirmedAccount)
                    {
                        return new RegistrationResultDto() { Success = true, CallbackUrl = callbackUrl };
                    }
                    else
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return new RegistrationResultDto()
                        {
                            Success = true
                        };
                    }
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                    return new RegistrationResultDto()
                    {
                        Success = false,
                        Error = error.Description
                    };
                }
            }


            return new RegistrationResultDto()
            {
                Success = false,
                Error = "An unexpected error has occurred."
            }; ;
        }

        private IUserEmailStore<AppUser> GetEmailStore()
        {
            //if (!_userManager.SupportsUserEmail)
            //{
            //    throw new NotSupportedException("The default UI requires a user store with email support.");
            //}
            return (IUserEmailStore<AppUser>)_userStore;
        }
    }
}
