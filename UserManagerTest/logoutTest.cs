using Moq;
using Services.Logout;

namespace BabyNameMatcher.Tests.Serviceses.Auth
{
    public class LogoutServiceTests
    {

        private readonly LogoutService _logoutService;

        public LogoutServiceTests()
        {
            _logoutService = new LogoutService();
        }

        [Fact]
        public async Task LogoutAsync_ReturnsOk_AndLogsInformation()
        {
            // Arrange

            // Act
            var result = await _logoutService.LogoutAsync();

            // Assert
            Assert.Equal(result.IsValid, true);
        }
    }
}
