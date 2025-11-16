using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Services.ChangePassword;
using System.Security.Claims;
using UserManagement;
using UserManagement.Dtos.Auth;

namespace UserManagementTests;

public class ChangePasswordServiceTests
{
    private readonly Mock<UserManager<AppUser>> _userManagerMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly ChangePasswordService _service;

    public ChangePasswordServiceTests()
    {
        var store = new Mock<IUserStore<AppUser>>();
        _userManagerMock = new Mock<UserManager<AppUser>>(
            store.Object, null, null, null, null, null, null, null, null
        );

        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();

        _service = new ChangePasswordService(_userManagerMock.Object, _httpContextAccessorMock.Object);
    }

    private void SetUser(string email)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, email)
        };
        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext
        {
            User = claimsPrincipal
        };

        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
    }

    private (UserManager<AppUser>, Mock<IUserPasswordStore<AppUser>>) CreateRealUserManager()
    {
        // 1. Mock létrehozása
        var storeMock = new Mock<IUserPasswordStore<AppUser>>();

        // 2. Kiegészítő interfész hozzáadása még Object elérése ELŐTT!
        storeMock.As<IUserEmailStore<AppUser>>();

        // 3. Csak ezután példányosítsuk a UserManager-t
        var userManager = new UserManager<AppUser>(
            storeMock.Object, // 👈 csak itt nyúlunk hozzá először!
            new Mock<IOptions<IdentityOptions>>().Object,
            new PasswordHasher<AppUser>(),
            new List<IUserValidator<AppUser>> { new UserValidator<AppUser>() },
            new List<IPasswordValidator<AppUser>> { new PasswordValidator<AppUser>() },
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null,
            new Mock<ILogger<UserManager<AppUser>>>().Object
        );

        return (userManager, storeMock);
    }

    [Fact]
    public async Task ChangePasswordAsync_ReturnsUnauthorized_IfNoUserClaim()
    {
        // Arrange
        var httpContext = new DefaultHttpContext(); // no user
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        var dto = new ChangePasswordParamsDto();

        // Act
        var result = await _service.ChangePasswordAsync(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("No user claim found.", result.Message[0]);
    }

    [Fact]
    public async Task ChangePasswordAsync_ReturnsError_IfUserNotFound()
    {
        // Arrange
        SetUser("test@example.com");

        _userManagerMock.Setup(x => x.FindByEmailAsync("test@example.com"))
                        .ReturnsAsync((AppUser)null);

        var dto = new ChangePasswordParamsDto();

        // Act
        var result = await _service.ChangePasswordAsync(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("User not found.", result.Message[0]);
    }

    [Fact]
    public async Task ChangePasswordAsync_ReturnsError_IfChangeFails()
    {
        // Arrange
        var (userManager, storeMock) = CreateRealUserManager();
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();

        var user = new AppUser
        {
            Email = "user@example.com",
            UserName = "Tamas"
        };

        // Valódi jelszó beállítása
        user.PasswordHash = new PasswordHasher<AppUser>().HashPassword(user, "correctPassword");

        // Claims beállítása
        var claims = new List<Claim>
{
    new Claim(ClaimTypes.NameIdentifier, "user@example.com")
};
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext
        {
            User = claimsPrincipal
        };
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        // Store mockok
        storeMock.As<IUserEmailStore<AppUser>>()
.Setup(x => x.FindByEmailAsync("USER@EXAMPLE.COM", It.IsAny<CancellationToken>()))
.ReturnsAsync(user);

        storeMock.As<IUserEmailStore<AppUser>>()
            .Setup(x => x.GetEmailAsync(user, It.IsAny<CancellationToken>()))
            .ReturnsAsync("user@example.com");

        storeMock.As<IUserEmailStore<AppUser>>()
            .Setup(x => x.GetNormalizedEmailAsync(user, It.IsAny<CancellationToken>()))
            .ReturnsAsync("USER@EXAMPLE.COM");

        // Password-related metódusok
        storeMock.Setup(x => x.SetPasswordHashAsync(user, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

        storeMock.Setup(x => x.UpdateAsync(user, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(IdentityResult.Success);


        var service = new ChangePasswordService(userManager, httpContextAccessorMock.Object);

        var dto = new ChangePasswordParamsDto
        {
            CurrentPassword = "wrongPassword", // nem egyezik
            NewPassword = "NewValid123!"
        };

        // Act
        var result = await service.ChangePasswordAsync(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Incorrect password", result.Message[0], StringComparison.OrdinalIgnoreCase);

    }



    [Fact]
    public async Task ChangePasswordAsync_ReturnsSuccess_IfSuccessful()
    {
        // Arrange
        var user = new AppUser();
        SetUser("user@example.com");

        _userManagerMock.Setup(x => x.FindByEmailAsync("user@example.com"))
                        .ReturnsAsync(user);

        _userManagerMock.Setup(x => x.ChangePasswordAsync(user, "old", "new"))
                        .ReturnsAsync(IdentityResult.Success);

        var dto = new ChangePasswordParamsDto
        {
            CurrentPassword = "old",
            NewPassword = "new"
        };

        // Act
        var result = await _service.ChangePasswordAsync(dto);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(null, result.Message);
    }
}
