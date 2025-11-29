using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using Services.Logout;
using UserManagement;

namespace UserManagementTests
{
    public class LogoutServiceTests
    {
        private readonly Mock<SignInManager<AppUser>> _signInManagerMock;
        private readonly LogoutService _logoutService;

        public LogoutServiceTests()
        {
            // Mock UserStore
            var userStore = new Mock<IUserStore<AppUser>>();

            // Mock UserManager
            var userManagerMock = new Mock<UserManager<AppUser>>(
                userStore.Object,
                null,
                new PasswordHasher<AppUser>(),
                new IUserValidator<AppUser>[0],
                new IPasswordValidator<AppUser>[0],
                null,
                null,
                null,
                new Mock<ILogger<UserManager<AppUser>>>().Object
            );

            // Mocked HttpContextAccessor
            var contextAccessor = new Mock<IHttpContextAccessor>();
            contextAccessor.Setup(a => a.HttpContext).Returns(new DefaultHttpContext());

            // Claims principal factory
            var claimsFactory = new Mock<IUserClaimsPrincipalFactory<AppUser>>();

            // Now we can mock the SignInManager
            _signInManagerMock = new Mock<SignInManager<AppUser>>(
                userManagerMock.Object,
                contextAccessor.Object,
                claimsFactory.Object,
                null,
                null,
                null,
                null
            );

            // Service under test
            _logoutService = new LogoutService(_signInManagerMock.Object);
        }

        [Fact]
        public async Task LogoutAsync_CallsSignOut_AndReturnsValidResponse()
        {
            // Arrange
            _signInManagerMock.Setup(s => s.SignOutAsync())
                              .Returns(Task.CompletedTask)
                              .Verifiable();

            // Act
            var result = await _logoutService.LogoutAsync();

            // Assert
            Assert.True(result.IsValid);
            Assert.Contains("User logged out successfully.", result.Message);

            _signInManagerMock.Verify(s => s.SignOutAsync(), Times.Once);
        }
    }
}
