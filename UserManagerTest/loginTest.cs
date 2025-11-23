
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Services.Login;
using System.Security.Claims;
using UserManagement;
using UserManagement.Dtos.Auth;

namespace UserManagementTests;

public class LoginServiceTests
{

    private readonly Mock<UserManager<AppUser>> _userManagerMock;
    private readonly SignInManager<AppUser> _signInManager; // nem Mock<SignInManager>
    private readonly Mock<IConfiguration> _configMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly LoginService _loginService;

    public LoginServiceTests()
    {
        // mock a user store-hoz (szükséges a UserManager mockhoz)
        var store = new Mock<IUserStore<AppUser>>();

        // CREATE a Mock<UserManager<TUser>> with the long constructor signature
        _userManagerMock = new Mock<UserManager<AppUser>>(
            store.Object,
            null, // IOptions<IdentityOptions>
            null, // IPasswordHasher<TUser>
            null, // IEnumerable<IUserValidator<TUser>>
            null, // IEnumerable<IPasswordValidator<TUser>>
            null, // ILookupNormalizer
            null, // IdentityErrorDescriber
            null, // IServiceProvider
            null  // ILogger<UserManager<TUser>>
        );

        // SignInManager: ne mock-old, hozz létre valódi példányt, de a függőségeit mock-olhatod
        var contextAccessor = new Mock<IHttpContextAccessor>();
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<AppUser>>();
        _signInManager = new SignInManager<AppUser>(
            _userManagerMock.Object,
            contextAccessor.Object,
            claimsFactory.Object,
            null, // IOptions<IdentityOptions>
            null, // ILogger<SignInManager<TUser>>
            null, // IAuthenticationSchemeProvider
            null  // IUserConfirmation<TUser>
        );

        _configMock = new Mock<IConfiguration>();
        _configMock.Setup(c => c["Jwt:Key"]).Returns("this_is_a_super_secret_key_123456");
        _configMock.Setup(c => c["Jwt:Issuer"]).Returns("TestIssuer");
        _configMock.Setup(c => c["Jwt:Audience"]).Returns("TestAudience");

        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();

        // inject mocks/objektumok a tesztelendő service-be
        _loginService = new LoginService(_userManagerMock.Object,
                                        _httpContextAccessorMock.Object,
                                        _signInManager);
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

    [Fact]
    public async Task LoadUserTestSessionReturnName()
    {
        // Arrange
        var userName = "TestUser";
        var user = new AppUser { Id = "123", UserName = userName, Email = "test@example.com" };

        // ClaimsPrincipal mock
        var claims = new List<Claim> { new Claim(ClaimTypes.Name, userName) };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        // UserManager mock: GetUserAsync visszaadja a user-t
        _userManagerMock.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                        .ReturnsAsync(user);

        // Service példányosítása
        var loginService = new LoginService(
            _userManagerMock.Object,
            _httpContextAccessorMock.Object,
            _signInManager
        );

        // HttpContext és ControllerContext mockolása
        var httpContext = new DefaultHttpContext { User = claimsPrincipal };
        loginService.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Act
        var result = await loginService.LoadUser();

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(userName, result.UserName);
    }


    [Fact]
    public async Task LoadUserTestReturnFalse()
    {
        // Service példányosítása
        var loginService = new LoginService(
            _userManagerMock.Object,
            _httpContextAccessorMock.Object,
            _signInManager
        );

        // Act
        var result = await loginService.LoadUser();

        // Assert
        Assert.False(result.IsValid);
    }
}
