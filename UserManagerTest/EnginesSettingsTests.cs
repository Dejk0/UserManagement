using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;
using Services.EnginesSettings;
using System.Security.Claims;
using System.Text.Json;
using UserManagement;
using Xunit;

namespace UserManagementTests
{
    public class EnginesSettingsTests
    {
        private readonly Mock<UserManager<AppUser>> _userManagerMock;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly EnginesSettingsService _service;

        public EnginesSettingsTests()
        {
            var store = new Mock<IUserStore<AppUser>>();
            _userManagerMock = new Mock<UserManager<AppUser>>(store.Object, null, null, null, null, null, null, null, null);
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();

            // default authenticated principal
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "user-id") };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);
            var httpContext = new DefaultHttpContext { User = principal };
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

            _service = new EnginesSettingsService(_userManagerMock.Object, _httpContextAccessorMock.Object);
        }

        [Fact]
        public async Task GetMotorokViewAccessAsync_ReturnsInvalid_WhenNoAuthenticatedUser()
        {
            // Arrange: no authenticated user
            var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) };
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

            // Act
            var result = await _service.GetMotorokViewAccessAsync();

            // Assert
            Assert.False(result.IsValid);
            Assert.Null(result.MotorokViewAccess);
        }

        [Fact]
        public async Task GetMotorokViewAccessAsync_ReturnsInvalid_WhenUserNotFound()
        {
            // Arrange
            _userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((AppUser)null);

            // Act
            var result = await _service.GetMotorokViewAccessAsync();

            // Assert
            Assert.False(result.IsValid);
            Assert.Null(result.MotorokViewAccess);
        }

        [Fact]
        public async Task GetMotorokViewAccessAsync_ReturnsInvalid_WhenColumnIsNullOrEmpty()
        {
            // Arrange
            var user = new AppUser { Id = "user-id", MotorokViewAccess = null };
            _userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

            var users = new List<AppUser> { user }.AsQueryable();
            _userManagerMock.Setup(u => u.Users).Returns(users);

            // Act
            var result = await _service.GetMotorokViewAccessAsync();

            // Assert
            Assert.False(result.IsValid);
            Assert.Null(result.MotorokViewAccess);
        }

        [Fact]
        public async Task GetMotorokViewAccessAsync_ReturnsArray_WhenJsonPresent()
        {
            // Arrange
            var arr = new bool[] { true, false, true };
            var json = JsonSerializer.Serialize(arr);
            var user = new AppUser { Id = "user-id", MotorokViewAccess = arr };
            _userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

            var users = new List<AppUser> { user }.AsQueryable();
            _userManagerMock.Setup(u => u.Users).Returns(users);

            // Act
            var result = await _service.GetMotorokViewAccessAsync();

            // Assert
            Assert.True(result.IsValid);
            Assert.NotNull(result.MotorokViewAccess);
            Assert.Equal(3, result.MotorokViewAccess.Length);
            Assert.True(result.MotorokViewAccess[0]);
            Assert.False(result.MotorokViewAccess[1]);
            Assert.True(result.MotorokViewAccess[2]);
        }

        [Fact]
        public async Task GetMotorokViewAccessAsync_ReturnsInvalid_OnInvalidJson()
        {
            // Arrange
            var user = new AppUser { Id = "user-id", MotorokViewAccess = null };
            _userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

            var users = new List<AppUser> { user }.AsQueryable();
            _userManagerMock.Setup(u => u.Users).Returns(users);

            // Act
            var result = await _service.GetMotorokViewAccessAsync();

            // Assert
            Assert.False(result.IsValid);
            Assert.Null(result.MotorokViewAccess);
        }

        // ----------------- SetMotorokViewAccessAsync tests -----------------

        [Fact]
        public async Task SetMotorokViewAccessAsync_ReturnsInvalid_WhenNoAuthenticatedUser()
        {
            // Arrange: no authenticated user
            var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) };
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

            // Act
            var result = await _service.SetMotorokViewAccessAsync(new bool[] { true });

            // Assert
            Assert.False(result.IsValid);
            Assert.Null(result.MotorokViewAccess);
        }

        [Fact]
        public async Task SetMotorokViewAccessAsync_ReturnsInvalid_WhenUserNotFound()
        {
            // Arrange
            _userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((AppUser)null);

            // Act
            var result = await _service.SetMotorokViewAccessAsync(new bool[] { true });

            // Assert
            Assert.False(result.IsValid);
            Assert.Null(result.MotorokViewAccess);
        }

        [Fact]
        public async Task SetMotorokViewAccessAsync_ReturnsUnchanged_WhenInsufficientTokens()
        {
            // Arrange: user has 0 tokens, one change required
            var user = new AppUser { Id = "user-id", MotorokViewAccess = new bool[] { false, false }, Tokens = 0 };
            _userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

            var newAccess = new bool[] { true, false }; // changeCount = 1

            // Act
            var result = await _service.SetMotorokViewAccessAsync(newAccess);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(user.MotorokViewAccess, result.MotorokViewAccess);
            _userManagerMock.Verify(u => u.UpdateAsync(It.IsAny<AppUser>()), Times.Never);
        }

        [Fact]
        public async Task SetMotorokViewAccessAsync_NoChanges_ReturnsSuccessAndNoUpdate()
        {
            // Arrange
            var current = new bool[] { true, false, true };
            var user = new AppUser { Id = "user-id", MotorokViewAccess = current, Tokens = 5 };
            _userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

            // Act
            var result = await _service.SetMotorokViewAccessAsync(current);

            // Assert
            Assert.True(result.IsValid);
            Assert.Equal(current, result.MotorokViewAccess);
            _userManagerMock.Verify(u => u.UpdateAsync(It.IsAny<AppUser>()), Times.Never);
            Assert.Equal(5, user.Tokens);
        }

        [Fact]
        public async Task SetMotorokViewAccessAsync_Succeeds_WhenEnoughTokens()
        {
            // Arrange
            var current = new bool[] { false, false, false };
            var user = new AppUser { Id = "user-id", MotorokViewAccess = current, Tokens = 2 };
            _userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
            _userManagerMock.Setup(u => u.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            var newAccess = new bool[] { true, false, true }; // changeCount = 2

            // Act
            var result = await _service.SetMotorokViewAccessAsync(newAccess);

            // Assert
            Assert.True(result.IsValid);
            Assert.Equal(newAccess, result.MotorokViewAccess);
            _userManagerMock.Verify(u => u.UpdateAsync(user), Times.Once);
            Assert.Equal(0, user.Tokens);
        }

        [Fact]
        public async Task SetMotorokViewAccessAsync_ReturnsUnchanged_WhenUpdateFails()
        {
            // Arrange
            var current = new bool[] { false, false };
            var user = new AppUser { Id = "user-id", MotorokViewAccess = current, Tokens = 5 };
            _userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
            _userManagerMock.Setup(u => u.UpdateAsync(user)).ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "db" }));

            var newAccess = new bool[] { true, false }; // changeCount = 1

            // Act
            var result = await _service.SetMotorokViewAccessAsync(newAccess);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(current, result.MotorokViewAccess);
            _userManagerMock.Verify(u => u.UpdateAsync(user), Times.Once);
            Assert.Equal(4, user.Tokens);
        }
    }
}
