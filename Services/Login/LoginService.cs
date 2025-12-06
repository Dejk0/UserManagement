using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System.Security.Claims;
using System.Text;
using UserManagement;
using UserManagement.Dtos.Auth;
using UserManagementServices.JWT;

namespace Services.Login
{
    public class LoginService : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LoginService(UserManager<AppUser> userManager, IHttpContextAccessor httpContextAccessor, SignInManager<AppUser> signInManager)
        {
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
            _signInManager = signInManager;
        }

        public async Task<TokenResponse> LoginAsync([FromBody] LoginParamsDto loginDto)
        {
            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            if (user == null)
            {
                return new TokenResponse { Token = "", Message = ["Incorrect email or password."] };
            }
            var isPasswordValid = await _userManager.CheckPasswordAsync(user, loginDto.Password);

            if (!isPasswordValid)
            {
                return new TokenResponse { Token = "", Message = ["Incorrect email or password."] };
            }

            if (user != null && isPasswordValid)
            {
                if (DataContext.AuthType == Authentication.Type.Jwt)
                {
                    string token = JWT.GenerateJwtToken(user);
                    return new TokenResponse { Token = token };
                }
                else
                {
                    await _signInManager.SignInAsync(user, isPasswordValid);
                    return new TokenResponse { Token = "", IsValid = true };
                }

            }

            return new TokenResponse { Token = "", Message = ["An unexpected error has occurred."] };
        }

        public async Task<TokenResponse> SendingNewToken()
        {
            var email = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (email == null)
            {
                return new TokenResponse { Token = "", IsValid = false };
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return new TokenResponse { Token = "", IsValid = false };
            }

            if (DataContext.AuthType == Authentication.Type.Jwt)
            {
                string token = JWT.GenerateJwtToken(user);
                return new TokenResponse { Token = token, IsValid = true };
            }

            return new TokenResponse { IsValid = true, Message = ["Missing authType"] };
        }

        public async Task<LoadUserResultDto> LoadUser()
        {
            var result = new LoadUserResultDto() { IsAuthenticated = false };
            var httpUser = _httpContextAccessor?.HttpContext?.User;
            if (httpUser != null)
            {
                var user = await _userManager.GetUserAsync(httpUser);
                if (user != null)
                {
                    result.IsAuthenticated = true;
                    if (httpUser.Identity != null && user.UserName != null)
                    {
                        result.Name = user.UserName;
                    }

                    var roles = await _userManager.GetRolesAsync(user) ?? new List<string>();
                    result.Roles = roles.Count > 0 ? roles.ToArray() : Array.Empty<string>();
                }
            }           
            return result;
        }

        public class TokenResponse : BaseValidResponse
        {
            public string? Token { get; set; }
        }
    }
}
