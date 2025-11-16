using BabyNameMatcher.Serviceses.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using UserManagement;
using UserManagement.Dtos.Auth;

namespace UserManagementTests
{
    public class RegisterServiceTests
    {
        private readonly Mock<UserManager<AppUser>> _userManagerMock;
        private readonly Mock<SignInManager<AppUser>> _signInManagerMock;
        private readonly Mock<IUserEmailStore<AppUser>> _userEmailStoreMock;
        private readonly Mock<ILogger<RegisterService>> _loggerMock;
        //private readonly Mock<IEmailService> _emailSenderMock;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly RegisterService _service;

        public RegisterServiceTests()
        {
            var identityOptions = new IdentityOptions
            {
                SignIn = { RequireConfirmedAccount = true }
            };
            var optionsMock = new Mock<IOptions<IdentityOptions>>();
            optionsMock.Setup(o => o.Value).Returns(identityOptions);

            _userEmailStoreMock = new Mock<IUserEmailStore<AppUser>>();
            _userManagerMock = new Mock<UserManager<AppUser>>(
                _userEmailStoreMock.Object, optionsMock.Object, null, null, null, null, null, null, null
            );

            var userPrincipalFactory = new Mock<IUserClaimsPrincipalFactory<AppUser>>();
            _signInManagerMock = new Mock<SignInManager<AppUser>>(
                _userManagerMock.Object,
                new Mock<IHttpContextAccessor>().Object,
                userPrincipalFactory.Object,
                null, null, null, null
            );

            _loggerMock = new Mock<ILogger<RegisterService>>();
            //_emailSenderMock = new Mock<IEmailService>();
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Scheme = "https";
            httpContext.Request.Host = new HostString("localhost");

            _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);

            _service = new RegisterService(
                _userManagerMock.Object,
                _userEmailStoreMock.Object,
                _signInManagerMock.Object,
                _httpContextAccessorMock.Object,
                null
            );
        }

        [Fact]
        public async Task RegisterAsync_WithValidInput_ReturnsSuccessWithCallbackUrl()
        {
            // Arrange
            var email = "test@example.com";
            var password = "Test1234!";

            _service.Input = new RegisterService.InputModel
            {
                Email = email,
                Password = password,
                ConfirmPassword = password
            };

            var dto = new RegistrationParamDto { Email = email, Password = password };

            _signInManagerMock.Setup(s => s.GetExternalAuthenticationSchemesAsync())
                .ReturnsAsync(new List<AuthenticationScheme>());

            _userEmailStoreMock.Setup(s => s.SetUserNameAsync(It.IsAny<AppUser>(), email, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _userEmailStoreMock.Setup(s => s.SetEmailAsync(It.IsAny<AppUser>(), email, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _userManagerMock.Setup(u => u.CreateAsync(It.IsAny<AppUser>(), password))
                .ReturnsAsync(IdentityResult.Success);

            _userManagerMock.Setup(u => u.GetUserIdAsync(It.IsAny<AppUser>()))
                .ReturnsAsync("test-user-id");

            _userManagerMock.Setup(u => u.GenerateEmailConfirmationTokenAsync(It.IsAny<AppUser>()))
                .ReturnsAsync("confirmation-code");

            // Act
            var result = await _service.RegisterAsync(dto);

            // Assert
            Assert.True(result.Success);
            Assert.StartsWith("https://localhost", result.CallbackUrl);
        }

        [Fact]
        public async Task RegisterAsync_WhenCreateFails_ReturnsErrorDescription()
        {
            // Arrange
            var email = "invalid@example.com";
            var password = "123";

            _service.Input = new RegisterService.InputModel
            {
                Email = email,
                Password = password,
                ConfirmPassword = password
            };

            var dto = new RegistrationParamDto { Email = email, Password = password };

            _signInManagerMock.Setup(s => s.GetExternalAuthenticationSchemesAsync())
                .ReturnsAsync(new List<AuthenticationScheme>());

            _userEmailStoreMock.Setup(s => s.SetUserNameAsync(It.IsAny<AppUser>(), email, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _userEmailStoreMock.Setup(s => s.SetEmailAsync(It.IsAny<AppUser>(), email, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var identityError = new IdentityError { Description = "Invalid password" };
            _userManagerMock.Setup(u => u.CreateAsync(It.IsAny<AppUser>(), password))
                .ReturnsAsync(IdentityResult.Failed(identityError));
            _userManagerMock.Setup(u => u.SupportsUserEmail).Returns(false);

            // Act
            var result = await _service.RegisterAsync(dto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Invalid password", result.Error);
        }
    }
}
