
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Services.Login;
using System.Security.Claims;
using UserManagement;
using UserManagement.Dtos;
using UserManagement.Dtos.Auth;

namespace UserManagementTests;

public class LoginServiceTests
{
    private readonly Mock<UserManager<AppUser>> _userManagerMock;
    private readonly Mock<SignInManager<AppUser>> _singinManagerMock;
    private readonly Mock<IConfiguration> _configMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly LoginService _loginService;

    public LoginServiceTests()
    {
        var store = new Mock<IUserStore<AppUser>>();
        _userManagerMock = new Mock<UserManager<AppUser>>(store.Object, null, null, null, null, null, null, null, null);
        _singinManagerMock = new Mock<SignInManager<AppUser>>(store.Object, null, null, null, null, null, null, null, null);

        _configMock = new Mock<IConfiguration>();
        _configMock.Setup(c => c["Jwt:Key"]).Returns("this_is_a_super_secret_key_123456");
        _configMock.Setup(c => c["Jwt:Issuer"]).Returns("TestIssuer");
        _configMock.Setup(c => c["Jwt:Audience"]).Returns("TestAudience");

        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();

        _loginService = new LoginService(_userManagerMock.Object, _httpContextAccessorMock.Object, _singinManagerMock.Object);
    }


    [Fact]
    public async Task LoginAsync_JWT_WithValidCredentials_ReturnsToken()
    {
        // Arrange
        var user = new AppUser { Id = "123", Email = "test@example.com", UserName = "TestUser" };
        var loginDto = new LoginParamsDto { Email = user.Email, Password = "password123" };

        _userManagerMock.Setup(m => m.FindByEmailAsync(loginDto.Email))
                        .ReturnsAsync(user);
        _userManagerMock.Setup(m => m.CheckPasswordAsync(user, loginDto.Password))
                        .ReturnsAsync(true);
        DataContext.AuthType = Authentication.Type.Jwt;

            // Act
        var result = await _loginService.LoginAsync(loginDto);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(result.Token));

    }

    [Fact]
    public async Task LoginAsync_WithInvalidCredentials_ReturnsEmptyToken()
    {
        // Arrange
        var loginDto = new LoginParamsDto { Email = "wrong@example.com", Password = "wrongpass" };
        _userManagerMock.Setup(m => m.FindByEmailAsync(loginDto.Email))
                        .ReturnsAsync((AppUser)null);

        // Act
        var result = await _loginService.LoginAsync(loginDto);

        // Assert
        Assert.Equal("", result.Token);
    }


    [Fact]
    public async Task SendingNewToken_WhenEmailClaimIsMissing_ReturnsEmptyToken()
    {
        // Arrange: nincs bejelentkezett felhasználó
        var context = new DefaultHttpContext();
        _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(context);

        // Act
        var result = await _loginService.SendingNewToken();

        // Assert
        Assert.Equal("", result.Token);
    }

    [Fact]
    public async Task SendingNewToken_WhenUserNotFound_ReturnsEmptyToken()
    {
        // Arrange: van claim, de nem található a felhasználó
        var email = "notfound@example.com";
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, email) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var context = new DefaultHttpContext
        {
            User = principal
        };
        _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(context);

        _userManagerMock.Setup(m => m.FindByEmailAsync(email))
                        .ReturnsAsync((AppUser)null);

        // Act
        var result = await _loginService.SendingNewToken();

        // Assert
        Assert.Equal("", result.Token);
    }

    [Fact]
    public async Task SendingNewToken_WhenUserExists_ReturnsValidToken()
    {
        // Arrange
        var email = "user@example.com";
        var user = new AppUser
        {
            Id = "123",
            Email = email,
            UserName = "TestUser"
        };

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, email) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var context = new DefaultHttpContext
        {
            User = principal
        };
        _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(context);

        _userManagerMock.Setup(m => m.FindByEmailAsync(email))
                        .ReturnsAsync(user);
        DataContext.AuthType = Authentication.Type.Jwt;

        // Act
        var result = await _loginService.SendingNewToken();

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(result.Token));
    }
}

