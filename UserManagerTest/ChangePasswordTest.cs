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
    private readonly DefaultHttpContext _testHttpContext;

    public ChangePasswordServiceTests()
    {
        var store = new Mock<IUserStore<AppUser>>();
        _userManagerMock = new Mock<UserManager<AppUser>>(
            store.Object, null, null, null, null, null, null, null, null
        );

        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();

        // Use a shared DefaultHttpContext so tests can modify User without reconfiguring Moq setups
        _testHttpContext = new DefaultHttpContext();
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(_testHttpContext);

        // default authenticated user for all tests (can be overridden in individual tests)
        var defaultClaims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "default-user-id")
        };
        var defaultIdentity = new ClaimsIdentity(defaultClaims, "TestAuth");
        var defaultPrincipal = new ClaimsPrincipal(defaultIdentity);
        _testHttpContext.User = defaultPrincipal;

        _service = new ChangePasswordService(_userManagerMock.Object, _httpContextAccessorMock.Object);
    }

    private void SetUser(string id)
    {
        var claims = new List<Claim>
        {
            // NameIdentifier should contain the user id when using UserManager.GetUserAsync
            new Claim(ClaimTypes.NameIdentifier, id)
        };
        // mark identity as authenticated by providing an authentication type
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        // Update shared test HttpContext's User
        _testHttpContext.User = claimsPrincipal;
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
        // Arrange: simulate no authenticated user by clearing principal
        _testHttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        var dto = new ChangePasswordParamsDto();

        // Act
        var result = await _service.ChangePasswordAsync(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("No authenticated user.", result.Message[0]);
    }

    [Fact]
    public async Task ChangePasswordAsync_ReturnsError_IfUserNotFound()
    {
        // Arrange
        SetUser("nonexistent-id");

        // Ensure GetUserAsync returns null
        _userManagerMock.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
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
            Id = "1",
            Email = "user@example.com",
            UserName = "Tamas"
        };

        // Valódi jelszó beállítása
        user.PasswordHash = new PasswordHasher<AppUser>().HashPassword(user, "correctPassword");

        // Claims beállítása: use user id
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "1")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext
        {
            User = claimsPrincipal
        };
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        // Store mock: FindByIdAsync should return the user when GetUserAsync is called
        storeMock.Setup(x => x.FindByIdAsync("1", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(user);

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
        SetUser("user-id");

        _userManagerMock.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
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
