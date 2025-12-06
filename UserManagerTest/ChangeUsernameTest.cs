using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;
using Services.ChangeUsername;
using System.Security.Claims;
using UserManagement;
using UserManagement.Dtos.Auth;
using Xunit;

namespace UserManagementTests;

public class ChangeUsernameServiceTests
{
    private readonly Mock<UserManager<AppUser>> _userManagerMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly ChangeUsernameService _service;

    public ChangeUsernameServiceTests()
    {
        var store = new Mock<IUserStore<AppUser>>();
        _userManagerMock = new Mock<UserManager<AppUser>>(
            store.Object, null, null, null, null, null, null, null, null
        );

        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();

        _service = new ChangeUsernameService(_userManagerMock.Object, _httpContextAccessorMock.Object);
    }

    private void SetUserClaim(string email)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, email)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
    }

    [Fact]
    public async Task ChangesUsernameAsync_ReturnsUnauthorized_WhenNoUser()
    {
        // Arrange: no authenticated user
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(new DefaultHttpContext());

        var dto = new ChangeUsernameParamsDto { NewName = "newname" };

        // Act
        var result = await _service.ChangesUsernameAsync(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("No authenticated user.", result.Message[0]);
    }

    [Fact]
    public async Task ChangesUsernameAsync_ReturnsError_WhenUserNotFound()
    {
        // Arrange
        SetUserClaim("user@example.com");
        _userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((AppUser)null);

        var dto = new ChangeUsernameParamsDto { NewName = "newname" };

        // Act
        var result = await _service.ChangesUsernameAsync(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("User not found.", result.Message[0]);
    }

    [Fact]
    public async Task ChangesUsernameAsync_ReturnsError_WhenSetUserNameFails()
    {
        // Arrange
        SetUserClaim("user@example.com");
        var user = new AppUser { Email = "user@example.com", UserName = "oldname" };
        _userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

        var identityError = new IdentityError { Description = "Some error" };
        _userManagerMock.Setup(u => u.SetUserNameAsync(user, "newname")).ReturnsAsync(IdentityResult.Failed(identityError));

        var dto = new ChangeUsernameParamsDto { NewName = "newname" };

        // Act
        var result = await _service.ChangesUsernameAsync(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Some error", result.Message[0]);
    }

    [Fact]
    public async Task ChangesUsernameAsync_ReturnsSuccess_WhenSetUserNameSucceeds()
    {
        // Arrange
        SetUserClaim("user@example.com");
        var user = new AppUser { Email = "user@example.com", UserName = "oldname" };
        _userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        _userManagerMock.Setup(u => u.SetUserNameAsync(user, "newname")).ReturnsAsync(IdentityResult.Success);

        var dto = new ChangeUsernameParamsDto { NewName = "newname" };

        // Act
        var result = await _service.ChangesUsernameAsync(dto);

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.Message);
    }
}
