using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UserManagement;
using UserManagement.Dtos.Auth;

namespace Services.ChangeUsername;

public class ChangeUsernameService : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ChangeUsernameService(UserManager<AppUser> userManager, IHttpContextAccessor httpContextAccessor)
    {
        _userManager = userManager;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<BaseValidResponse> ChangesUsernameAsync(ChangeUsernameParamsDto @params)
    {
        var principal = _httpContextAccessor.HttpContext?.User;
        if (principal == null || !principal.Identity?.IsAuthenticated == true)
            return new BaseValidResponse { IsValid = false, Message = new[] { "No authenticated user." } };

        var user = await _userManager.GetUserAsync(principal);
        if (user == null)
            return new BaseValidResponse { IsValid = false, Message = new[] { "User not found." } };

        var result = await _userManager.SetUserNameAsync(user, @params.NewName);
        if (!result.Succeeded)
            return new BaseValidResponse { IsValid = false, Message = result.Errors.Select(e => e.Description).ToArray() };

        return new BaseValidResponse { IsValid = true };
    }
}
