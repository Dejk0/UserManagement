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
        var principal = _httpContextAccessor.HttpContext?.User;
        if (principal == null || !principal.Identity?.IsAuthenticated == true)
            return new BaseValidResponse { IsValid = false, Message = new[] { "No authenticated user." } };

        var user = await _userManager.GetUserAsync(principal);
        if (user == null)
            return new BaseValidResponse { IsValid = false, Message = new[] { "User not found." } };

        if (string.IsNullOrWhiteSpace(@params.NewPassword))
            return new BaseValidResponse { IsValid = false, Message = new[] { "New password cannot be empty." } };

        if (await _userManager.CheckPasswordAsync(user, @params.NewPassword))
        {
            return new BaseValidResponse
            {
                IsValid = false,
                Message = new[] { "the new password cannot be as the old." }
            };
        }

        var result = await _userManager.ChangePasswordAsync(user, @params.CurrentPassword, @params.NewPassword);
        if (!result.Succeeded)
            return new BaseValidResponse { IsValid = false, Message = result.Errors.Select(e => e.Description).ToArray() };

        return new BaseValidResponse { IsValid = true };
    }
    
    public async Task<BaseValidResponse> ChangePasswordNoAuthAsync(ChangePasswordParamsDto @params)
    {
        var principal = _httpContextAccessor.HttpContext?.User;
        if (principal == null || !principal.Identity?.IsAuthenticated == true)
            return new BaseValidResponse { IsValid = false, Message = new[] { "No authenticated user." } };

        var user = await _userManager.GetUserAsync(principal);
        if (user == null)
            return new BaseValidResponse { IsValid = false, Message = new[] { "User not found." } };

        if (string.IsNullOrWhiteSpace(@params.NewPassword))
            return new BaseValidResponse { IsValid = false, Message = new[] { "New password cannot be empty." } };

        if (await _userManager.CheckPasswordAsync(user, @params.NewPassword))
        {
            return new BaseValidResponse
            {
                IsValid = false,
                Message = new[] { "the new password cannot be as the old." }
            };
        }

        var result = await _userManager.ChangePasswordAsync(user, @params.CurrentPassword, @params.NewPassword);
        if (!result.Succeeded)
            return new BaseValidResponse { IsValid = false, Message = result.Errors.Select(e => e.Description).ToArray() };

        return new BaseValidResponse { IsValid = true };
    }
    public async Task<BaseValidResponse> SandingChangePasswordEmail(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            return new BaseValidResponse { IsValid = false, Message = new[] { "User not found." } };
        }
        user.ChangeingPassword = true;
        await _userManager.UpdateAsync(user);


        return new BaseValidResponse { IsValid = true, Message = new[] { $"deaktamas1994.hu/changepassword?id={user.Id}" } };
    }
}
