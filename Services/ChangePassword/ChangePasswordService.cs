using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UserManagement;
using UserManagement.Dtos.Auth;

namespace Services.ChangePassword;

public class ChangePasswordService : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ChangePasswordService(UserManager<AppUser> userManager, IHttpContextAccessor httpContextAccessor)
    {
        _userManager = userManager;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<BaseValidResponse> ChangePasswordAsync(ChangePasswordParamsDto @params)
    {
        var email = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (email == null)
            return new BaseValidResponse()
            {
                Message = ["No user claim found."],
                IsValid = false
            }
        ;

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
            return new BaseValidResponse()
            {
                Message = ["User not found."],
                IsValid = false
            };

        var result = await _userManager.ChangePasswordAsync(user, @params.CurrentPassword, @params.NewPassword);

        if (!result.Succeeded)
        {
            string[] errors = result.Errors.Select(e => e.Description).ToArray();

            return new BaseValidResponse()
            {
                Message = errors,
                IsValid = false
            };
        }

        return new BaseValidResponse()
        {
            IsValid = true
        };
    }
}
