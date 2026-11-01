using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace auth_test.unit;

public class SecurityServiceTests
{
    [Fact]
    public async Task ForgotPasswordAsync_WhenUserDoesNotExist_ReturnsFalseAndDoesNotSendEmail()
    {
        var userManager = CreateUserManager();
        userManager
            .Setup(manager => manager.FindByEmailAsync("missing@example.com"))
            .ReturnsAsync((ApplicationUser?)null);
        var emailClient = new Mock<IEmailServiceClient>();
        var service = CreateService(userManager, emailClient);

        var result = await service.ForgotPasswordAsync("missing@example.com");

        Assert.False(result);
        emailClient.Verify(client => client.SendEmailAsync(It.IsAny<SendEmailRequest>()), Times.Never);
    }

    [Fact]
    public async Task ForgotPasswordAsync_WhenEmailSucceeds_SendsPasswordResetEmail()
    {
        var user = new ApplicationUser { Id = "user-1", Email = "user@example.com" };
        var userManager = CreateUserManager();
        userManager
            .Setup(manager => manager.FindByEmailAsync(user.Email))
            .ReturnsAsync(user);
        userManager
            .Setup(manager => manager.GeneratePasswordResetTokenAsync(user))
            .ReturnsAsync("reset token");
        var emailClient = new Mock<IEmailServiceClient>();
        SendEmailRequest? sentRequest = null;
        emailClient
            .Setup(client => client.SendEmailAsync(It.IsAny<SendEmailRequest>()))
            .Callback<SendEmailRequest>(request => sentRequest = request)
            .ReturnsAsync(true);
        var service = CreateService(userManager, emailClient);

        var result = await service.ForgotPasswordAsync(user.Email);

        Assert.True(result);
        Assert.NotNull(sentRequest);
        Assert.Equal(user.Email, sentRequest!.To);
        Assert.Equal("password_reset", sentRequest.template);
        var resetUrl = GetPayloadValue<string>(sentRequest.data, "ResetUrl");
        Assert.Equal(
            "https://yourapp.com/reset-password?userId=user-1&token=reset%20token",
            resetUrl);
    }

    [Fact]
    public async Task ForgotPasswordAsync_WhenEmailFails_ReturnsFalse()
    {
        var user = new ApplicationUser { Id = "user-1", Email = "user@example.com" };
        var userManager = CreateUserManager();
        userManager
            .Setup(manager => manager.FindByEmailAsync(user.Email))
            .ReturnsAsync(user);
        userManager
            .Setup(manager => manager.GeneratePasswordResetTokenAsync(user))
            .ReturnsAsync("token");
        var emailClient = new Mock<IEmailServiceClient>();
        emailClient
            .Setup(client => client.SendEmailAsync(It.IsAny<SendEmailRequest>()))
            .ReturnsAsync(false);
        var service = CreateService(userManager, emailClient);

        var result = await service.ForgotPasswordAsync(user.Email);

        Assert.False(result);
    }

    [Fact]
    public async Task ResetPasswordAsync_WhenUserDoesNotExist_ReturnsUserNotFound()
    {
        var userManager = CreateUserManager();
        userManager
            .Setup(manager => manager.FindByEmailAsync("missing@example.com"))
            .ReturnsAsync((ApplicationUser?)null);
        var service = CreateService(userManager, new Mock<IEmailServiceClient>());

        var result = await service.ResetPasswordAsync("missing@example.com", "token", "new-password");

        Assert.False(result.Succeeded);
        var error = Assert.Single(result.Errors);
        Assert.Equal("UserNotFound", error.Code);
        Assert.Equal("User not found", error.Description);
        userManager.Verify(
            manager => manager.ResetPasswordAsync(
                It.IsAny<ApplicationUser>(),
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task ResetPasswordAsync_WhenUserExists_DelegatesToUserManager()
    {
        var user = new ApplicationUser { Email = "user@example.com" };
        var expected = IdentityResult.Failed(new IdentityError { Code = "InvalidToken" });
        var userManager = CreateUserManager();
        userManager
            .Setup(manager => manager.FindByEmailAsync(user.Email))
            .ReturnsAsync(user);
        userManager
            .Setup(manager => manager.ResetPasswordAsync(user, "token", "new-password"))
            .ReturnsAsync(expected);
        var service = CreateService(userManager, new Mock<IEmailServiceClient>());

        var result = await service.ResetPasswordAsync(user.Email, "token", "new-password");

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task ChangePasswordAsync_WhenUserDoesNotExist_ReturnsUserNotFound()
    {
        var userManager = CreateUserManager();
        userManager
            .Setup(manager => manager.FindByIdAsync("missing-user"))
            .ReturnsAsync((ApplicationUser?)null);
        var service = CreateService(userManager, new Mock<IEmailServiceClient>());

        var result = await service.ChangePasswordAsync("missing-user", "old-password", "new-password");

        Assert.False(result.Succeeded);
        var error = Assert.Single(result.Errors);
        Assert.Equal("UserNotFound", error.Code);
    }

    [Fact]
    public async Task ChangePasswordAsync_WhenUserExists_DelegatesToUserManager()
    {
        var user = new ApplicationUser { Id = "user-1" };
        var expected = IdentityResult.Success;
        var userManager = CreateUserManager();
        userManager
            .Setup(manager => manager.FindByIdAsync(user.Id))
            .ReturnsAsync(user);
        userManager
            .Setup(manager => manager.ChangePasswordAsync(user, "old-password", "new-password"))
            .ReturnsAsync(expected);
        var service = CreateService(userManager, new Mock<IEmailServiceClient>());

        var result = await service.ChangePasswordAsync(user.Id, "old-password", "new-password");

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task VerifyEmailAsync_WhenUserDoesNotExist_ReturnsUserNotFound()
    {
        var userManager = CreateUserManager();
        userManager
            .Setup(manager => manager.FindByIdAsync("missing-user"))
            .ReturnsAsync((ApplicationUser?)null);
        var service = CreateService(userManager, new Mock<IEmailServiceClient>());

        var result = await service.VerifyEmailAsync("missing-user", "code");

        Assert.False(result.Succeeded);
        var error = Assert.Single(result.Errors);
        Assert.Equal("UserNotFound", error.Code);
    }

    [Fact]
    public async Task VerifyEmailAsync_WhenUserExists_DelegatesToUserManager()
    {
        var user = new ApplicationUser { Id = "user-1" };
        var expected = IdentityResult.Success;
        var userManager = CreateUserManager();
        userManager
            .Setup(manager => manager.FindByIdAsync(user.Id))
            .ReturnsAsync(user);
        userManager
            .Setup(manager => manager.ConfirmEmailAsync(user, "code"))
            .ReturnsAsync(expected);
        var service = CreateService(userManager, new Mock<IEmailServiceClient>());

        var result = await service.VerifyEmailAsync(user.Id, "code");

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task ResendVerificationEmailAsync_WhenUserDoesNotExist_ReturnsFalse()
    {
        var userManager = CreateUserManager();
        userManager
            .Setup(manager => manager.FindByIdAsync("missing-user"))
            .ReturnsAsync((ApplicationUser?)null);
        var emailClient = new Mock<IEmailServiceClient>();
        var service = CreateService(userManager, emailClient);

        var result = await service.ResendVerificationEmailAsync("missing-user");

        Assert.False(result);
        emailClient.Verify(client => client.SendEmailAsync(It.IsAny<SendEmailRequest>()), Times.Never);
    }

    [Fact]
    public async Task ResendVerificationEmailAsync_WhenEmailIsAlreadyConfirmed_ReturnsTrueWithoutSendingEmail()
    {
        var user = new ApplicationUser { Id = "user-1", Email = "user@example.com", EmailConfirmed = true };
        var userManager = CreateUserManager();
        userManager
            .Setup(manager => manager.FindByIdAsync(user.Id))
            .ReturnsAsync(user);
        var emailClient = new Mock<IEmailServiceClient>();
        var service = CreateService(userManager, emailClient);

        var result = await service.ResendVerificationEmailAsync(user.Id);

        Assert.True(result);
        userManager.Verify(
            manager => manager.GenerateEmailConfirmationTokenAsync(It.IsAny<ApplicationUser>()),
            Times.Never);
        emailClient.Verify(client => client.SendEmailAsync(It.IsAny<SendEmailRequest>()), Times.Never);
    }

    [Fact]
    public async Task ResendVerificationEmailAsync_WhenEmailIsNotConfirmed_SendsVerificationEmail()
    {
        var user = new ApplicationUser { Id = "user-1", Email = "user@example.com", EmailConfirmed = false };
        var userManager = CreateUserManager();
        userManager
            .Setup(manager => manager.FindByIdAsync(user.Id))
            .ReturnsAsync(user);
        userManager
            .Setup(manager => manager.GenerateEmailConfirmationTokenAsync(user))
            .ReturnsAsync("verification token");
        var emailClient = new Mock<IEmailServiceClient>();
        SendEmailRequest? sentRequest = null;
        emailClient
            .Setup(client => client.SendEmailAsync(It.IsAny<SendEmailRequest>()))
            .Callback<SendEmailRequest>(request => sentRequest = request)
            .ReturnsAsync(true);
        var service = CreateService(userManager, emailClient);

        var result = await service.ResendVerificationEmailAsync(user.Id);

        Assert.True(result);
        Assert.NotNull(sentRequest);
        Assert.Equal(user.Email, sentRequest!.To);
        Assert.Equal("email_verification", sentRequest.template);
        var verificationUrl = GetPayloadValue<string>(sentRequest.data, "VerificationUrl");
        Assert.Equal(
            "https://yourapp.com/verify-email?userId=user-1&token=verification%20token",
            verificationUrl);
    }

    private static SecurityService CreateService(
        Mock<UserManager<ApplicationUser>> userManager,
        Mock<IEmailServiceClient> emailClient) =>
        new(userManager.Object, emailClient.Object);

    private static Mock<UserManager<ApplicationUser>> CreateUserManager() =>
        new(
            Mock.Of<IUserStore<ApplicationUser>>(),
            Options.Create(new IdentityOptions()),
            Mock.Of<IPasswordHasher<ApplicationUser>>(),
            Array.Empty<IUserValidator<ApplicationUser>>(),
            Array.Empty<IPasswordValidator<ApplicationUser>>(),
            Mock.Of<ILookupNormalizer>(),
            new IdentityErrorDescriber(),
            Mock.Of<IServiceProvider>(),
            Mock.Of<ILogger<UserManager<ApplicationUser>>>());

    private static T GetPayloadValue<T>(object payload, string propertyName) =>
        (T)(payload.GetType().GetProperty(propertyName)?.GetValue(payload)
            ?? throw new InvalidOperationException($"Missing payload property '{propertyName}'."));
}