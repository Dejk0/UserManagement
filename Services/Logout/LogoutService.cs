namespace Services.Logout
{
    public class LogoutService
    {
        public LogoutService()
        {
            
        }

        public async Task<BaseValidResponse> LogoutAsync()
        {
            return new BaseValidResponse()
            {
                IsValid = true,
                Message = ["User logged out successfully."]
            };
        }
    }
}