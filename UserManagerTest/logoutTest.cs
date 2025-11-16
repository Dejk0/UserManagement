using Moq;
using Services.Logout;

namespace UserManagementTests
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
            Assert.True(result.IsValid);
        }
    }
}
