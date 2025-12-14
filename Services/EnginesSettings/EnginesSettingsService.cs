using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UserManagement;
using UserManagement.Dtos.Auth;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Linq;

namespace Services.EnginesSettings;

public class EnginesSettingsService : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public EnginesSettingsService(UserManager<AppUser> userManager, IHttpContextAccessor httpContextAccessor)
    {
        _userManager = userManager;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<EnginsViewAccessDto> GetMotorokViewAccessAsync()
    {
        var principal = _httpContextAccessor.HttpContext?.User;
        if (principal == null || !principal.Identity?.IsAuthenticated == true)
            return new EnginsViewAccessDto() { IsValid = false };

        var user = await _userManager.GetUserAsync(principal);
        if (user == null)
            return new EnginsViewAccessDto() { IsValid = false };

        bool[]? arr = user.MotorokViewAccess;
        if (arr == null || arr.Length == 0) return new EnginsViewAccessDto() { IsValid = false };

        return new EnginsViewAccessDto() { IsValid = true, MotorokViewAccess = arr };
    }

    public async Task<EnginsViewAccessDto> SetMotorokViewAccessAsync(bool[] newAccess)
    {
        var principal = _httpContextAccessor.HttpContext?.User;
        if (principal == null || !principal.Identity?.IsAuthenticated == true)
            return new EnginsViewAccessDto() { IsValid = false };

        var user = await _userManager.GetUserAsync(principal);
        if (user == null)
            return new EnginsViewAccessDto() { IsValid = false };

        // Current access (may be null)
        var current = user.MotorokViewAccess ?? Array.Empty<bool>();

        // Calculate how many positions actually change between current and newAccess
        int maxLen = Math.Max(current.Length, newAccess?.Length ?? 0);
        int changeCount = 0;
        for (int i = 0; i < maxLen; i++)
        {
            bool cur = i < current.Length ? current[i] : false;
            bool next = (newAccess != null && i < newAccess.Length) ? newAccess[i] : false;
            if (cur != next) changeCount++;
        }

        // If nothing changes, return current (no token cost)
        if (changeCount == 0)
        {
            return new EnginsViewAccessDto() { IsValid = true, MotorokViewAccess = current };
        }

        // If user doesn't have enough tokens to cover all changes, return unchanged
        if (user.Tokens < changeCount)
        {
            return new EnginsViewAccessDto() { IsValid = false, MotorokViewAccess = current };
        }

        // Apply the update and deduct tokens equal to the number of changes
        user.MotorokViewAccess = newAccess;
        user.Tokens -= changeCount;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            // If update failed, return unchanged access
            return new EnginsViewAccessDto() { IsValid = false, MotorokViewAccess = current };
        }

        return new EnginsViewAccessDto() { IsValid = true, MotorokViewAccess = newAccess };
    }
}
