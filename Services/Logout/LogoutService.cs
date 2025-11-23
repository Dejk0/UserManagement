using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.InteropServices;
using UserManagement;

namespace Services.Logout
{
    public class LogoutService : ControllerBase
    {
        private readonly SignInManager<AppUser> _signInManager;
        public LogoutService(SignInManager<AppUser> signInManager)
        {
            _signInManager = signInManager;
        }

        public async Task<BaseValidResponse> LogoutAsync()
        {
            await _signInManager.SignOutAsync();
            return new BaseValidResponse()
            {
                IsValid = true,
                Message = ["User logged out successfully."]
            };
        }
    }
}