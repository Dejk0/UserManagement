using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;
using UserManagement;
using UserManagementServices.ConfirmEmail;

namespace UserManagementTests
{
    public class ConfirmEmailServiceTests
    {
        private readonly Mock<UserManager<AppUser>> _userManagerMock;
        private readonly ConfirmEmailService _service;


        public ConfirmEmailServiceTests()
        {
            var userStoreMock = new Mock<IUserStore<AppUser>>();
            _userManagerMock = new Mock<UserManager<AppUser>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null
            );

            _service = new ConfirmEmailService(_userManagerMock.Object);
        }

        [Fact]
        public async Task ConfirmEmailAsync_ReturnsBadRequest_WhenUserIdOrCodeIsNull()
        {
            // Act
            var result = await _service.ConfirmEmailAsync(null, null);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Missing parameters.", badRequest.Value);
        }

        [Fact]
        public async Task ConfirmEmailAsync_ReturnsNotFound_WhenUserNotFound()
        {
            // Arrange
            _userManagerMock.Setup(x => x.FindByIdAsync("fake-user")).ReturnsAsync((AppUser)null);

            // Act
            var result = await _service.ConfirmEmailAsync("fake-user", "encodedCode");

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("User not found.", notFound.Value);
        }

        [Fact]
        public async Task ConfirmEmailAsync_ReturnsOk_WhenEmailConfirmedSuccessfully()
        {
            // Arrange
            var user = new AppUser();
            var originalCode = "abc123";
            var encodedCode = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(originalCode));

            _userManagerMock.Setup(x => x.FindByIdAsync("user-id")).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.ConfirmEmailAsync(user, originalCode))
                            .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _service.ConfirmEmailAsync("user-id", encodedCode);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Email confirmed.", okResult.Value);
        }

        [Fact]
        public async Task ConfirmEmailAsync_ReturnsBadRequest_WhenConfirmationFails()
        {
            // Arrange
            var user = new AppUser();
            var originalCode = "abc123";
            var encodedCode = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(originalCode));

            _userManagerMock.Setup(x => x.FindByIdAsync("user-id")).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.ConfirmEmailAsync(user, originalCode))
                            .ReturnsAsync(IdentityResult.Failed());

            // Act
            var result = await _service.ConfirmEmailAsync("user-id", encodedCode);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Email confirmation failed.", badRequest.Value);
        }
    }
}
