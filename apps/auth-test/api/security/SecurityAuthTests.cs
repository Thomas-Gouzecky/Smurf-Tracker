using System.Security.Claims;
using Backend.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace auth_test.api.security;

public class SecurityAuthTests
{
    [Fact]
    public async Task ForgotPassword_ReturnsUniformOkResponseAndDelegatesEmail()
    {
        var service = new Mock<ISecurityService>();
        service
            .Setup(security => security.ForgotPasswordAsync("user@example.com"))
            .ReturnsAsync(false);
        var controller = new SecurityController(service.Object);

        var result = await controller.ForgotPassword(new ForgotPasswordRequest
        {
            Email = "user@example.com"
        });

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(
            "If an account with that email exists, a password reset link has been sent.",
            GetProperty<string>(ok.Value!, "Message"));
        service.Verify(security => security.ForgotPasswordAsync("user@example.com"), Times.Once);
    }

    [Fact]
    public async Task ResetPassword_WhenServiceSucceeds_ReturnsOk()
    {
        var service = new Mock<ISecurityService>();
        service
            .Setup(security => security.ResetPasswordAsync("user@example.com", "reset-code", "new-password"))
            .ReturnsAsync(IdentityResult.Success);
        var controller = new SecurityController(service.Object);

        var result = await controller.ResetPassword(new ResetPasswordRequest
        {
            Email = "user@example.com",
            ResetCode = "reset-code",
            NewPassword = "new-password"
        });

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Password has been reset successfully.", GetProperty<string>(ok.Value!, "Message"));
        service.Verify(
            security => security.ResetPasswordAsync("user@example.com", "reset-code", "new-password"),
            Times.Once);
    }

    [Fact]
    public async Task ResetPassword_WhenServiceFails_ReturnsBadRequestWithErrors()
    {
        var errors = new[] { new IdentityError { Code = "InvalidToken", Description = "Invalid token" } };
        var service = new Mock<ISecurityService>();
        service
            .Setup(security => security.ResetPasswordAsync("user@example.com", "reset-code", "new-password"))
            .ReturnsAsync(IdentityResult.Failed(errors));
        var controller = new SecurityController(service.Object);

        var result = await controller.ResetPassword(new ResetPasswordRequest
        {
            Email = "user@example.com",
            ResetCode = "reset-code",
            NewPassword = "new-password"
        });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Failed to reset password.", GetProperty<string>(badRequest.Value!, "Message"));
        var actualErrors = Assert.Single(GetProperty<IEnumerable<IdentityError>>(badRequest.Value!, "Errors"));
        Assert.Equal(errors[0].Code, actualErrors.Code);
        Assert.Equal(errors[0].Description, actualErrors.Description);
    }

    [Fact]
    public async Task ChangePassword_WhenClaimIsMissing_ReturnsUnauthorizedWithoutCallingService()
    {
        var service = new Mock<ISecurityService>();
        var controller = CreateController(service, new ClaimsPrincipal(new ClaimsIdentity()));

        var result = await controller.ChangePassword(new ChangePasswordRequest
        {
            CurrentPassword = "old-password",
            NewPassword = "new-password"
        });

        Assert.IsType<UnauthorizedResult>(result);
        service.Verify(
            security => security.ChangePasswordAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task ChangePassword_WhenServiceSucceeds_ReturnsOkWithUserId()
    {
        var service = new Mock<ISecurityService>();
        service
            .Setup(security => security.ChangePasswordAsync("user-1", "old-password", "new-password"))
            .ReturnsAsync(IdentityResult.Success);
        var controller = CreateController(service, User("user-1"));

        var result = await controller.ChangePassword(new ChangePasswordRequest
        {
            CurrentPassword = "old-password",
            NewPassword = "new-password"
        });

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Password has been changed successfully.", GetProperty<string>(ok.Value!, "Message"));
        service.Verify(
            security => security.ChangePasswordAsync("user-1", "old-password", "new-password"),
            Times.Once);
    }

    [Fact]
    public async Task ChangePassword_WhenServiceFails_ReturnsBadRequestWithErrors()
    {
        var service = new Mock<ISecurityService>();
        service
            .Setup(security => security.ChangePasswordAsync("user-1", "old-password", "new-password"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "PasswordMismatch" }));
        var controller = CreateController(service, User("user-1"));

        var result = await controller.ChangePassword(new ChangePasswordRequest
        {
            CurrentPassword = "old-password",
            NewPassword = "new-password"
        });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Failed to change password.", GetProperty<string>(badRequest.Value!, "Message"));
    }

    [Fact]
    public async Task VerifyEmail_WhenParametersAreMissing_ReturnsBadRequestWithoutCallingService()
    {
        var service = new Mock<ISecurityService>();
        var controller = CreateController(service, new ClaimsPrincipal(new ClaimsIdentity()));

        var result = await controller.VerifyEmail(string.Empty, string.Empty);

        Assert.IsType<BadRequestObjectResult>(result);
        service.Verify(
            security => security.VerifyEmailAsync(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task VerifyEmail_WhenServiceSucceeds_ReturnsOkWithUserId()
    {
        var service = new Mock<ISecurityService>();
        service
            .Setup(security => security.VerifyEmailAsync("user-1", "code"))
            .ReturnsAsync(IdentityResult.Success);
        var controller = CreateController(service, User("user-1"));

        var result = await controller.VerifyEmail("user-1", "code");

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Email has been verified successfully.", GetProperty<string>(ok.Value!, "Message"));
        service.Verify(security => security.VerifyEmailAsync("user-1", "code"), Times.Once);
    }

    [Fact]
    public async Task VerifyEmail_ForwardsEncodedTokenWithoutAuthentication()
    {
        const string token = "token+with/slash==";
        var service = new Mock<ISecurityService>();
        service
            .Setup(security => security.VerifyEmailAsync("user-1", token))
            .ReturnsAsync(IdentityResult.Success);
        var controller = CreateController(
            service,
            new ClaimsPrincipal(new ClaimsIdentity()));

        var result = await controller.VerifyEmail("user-1", token);

        Assert.IsType<OkObjectResult>(result);
        service.Verify(security => security.VerifyEmailAsync("user-1", token), Times.Once);
    }

    [Fact]
    public async Task VerifyEmail_WhenServiceFails_ReturnsBadRequestWithErrors()
    {
        var service = new Mock<ISecurityService>();
        service
            .Setup(security => security.VerifyEmailAsync("user-1", "code"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "InvalidCode" }));
        var controller = CreateController(service, User("user-1"));

        var result = await controller.VerifyEmail("user-1", "code");

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Failed to verify email.", GetProperty<string>(badRequest.Value!, "Message"));
    }

    [Fact]
    public async Task SendVerificationEmail_WhenClaimIsMissing_ReturnsUnauthorizedWithoutCallingService()
    {
        var service = new Mock<ISecurityService>();
        var controller = CreateController(service, new ClaimsPrincipal(new ClaimsIdentity()));

        var result = await controller.SendVerificationEmail();

        Assert.IsType<UnauthorizedResult>(result);
        service.Verify(
            security => security.SendVerificationEmailAsync(It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task SendVerificationEmail_WhenServiceSucceeds_ReturnsOk()
    {
        var service = new Mock<ISecurityService>();
        service
            .Setup(security => security.SendVerificationEmailAsync("user-1"))
            .ReturnsAsync(IdentityResult.Success);
        var controller = CreateController(service, User("user-1"));

        var result = await controller.SendVerificationEmail();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.True(((IdentityResult)ok.Value!).Succeeded);
        service.Verify(security => security.SendVerificationEmailAsync("user-1"), Times.Once);
    }

    [Fact]
    public async Task SendVerificationEmail_WhenServiceFails_ReturnsBadRequest()
    {
        var service = new Mock<ISecurityService>();
        service
            .Setup(security => security.SendVerificationEmailAsync("user-1"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError
            {
                Code = "EmailSendFailed"
            }));
        var controller = CreateController(service, User("user-1"));

        var result = await controller.SendVerificationEmail();

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.False(((IdentityResult)badRequest.Value!).Succeeded);
    }

    [Fact]
    public void ProtectedEndpointsRequireAuthorization()
    {
        foreach (var methodName in new[] { nameof(SecurityController.ChangePassword), nameof(SecurityController.SendVerificationEmail) })
        {
            var method = typeof(SecurityController).GetMethod(methodName);

            Assert.NotNull(method);
            Assert.NotNull(method!.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true).SingleOrDefault());
        }
    }

    [Fact]
    public void PublicEndpointsAllowAnonymous()
    {
        foreach (var methodName in new[] { nameof(SecurityController.ForgotPassword), nameof(SecurityController.ResetPassword), nameof(SecurityController.VerifyEmail) })
        {
            var method = typeof(SecurityController).GetMethod(methodName);

            Assert.NotNull(method);
            Assert.NotNull(method!.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true).SingleOrDefault());
        }
    }

    private static SecurityController CreateController(
        Mock<ISecurityService> service,
        ClaimsPrincipal user) =>
        new(service.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            }
        };

    private static ClaimsPrincipal User(string userId) =>
        new(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, userId) },
            "Bearer"));

    private static T GetProperty<T>(object value, string propertyName) =>
        (T)(value.GetType().GetProperty(propertyName)?.GetValue(value)
            ?? throw new InvalidOperationException($"Missing property '{propertyName}'."));
}