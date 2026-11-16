using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Net;

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

        Assert.Equal(false, result);
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
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
        var service = CreateService(userManager, emailClient);

        var result = await service.ForgotPasswordAsync(user.Email);

        Assert.Equal(HttpStatusCode.OK, Assert.IsType<HttpResponseMessage>(result).StatusCode);
        Assert.NotNull(sentRequest);
        Assert.Equal(user.Email, sentRequest!.To);
        Assert.Equal("password_reset", sentRequest.Template);
        var resetUrl = GetPayloadValue<string>(sentRequest.Data, "ResetLink");
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
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest));
        var service = CreateService(userManager, emailClient);

        var result = await service.ForgotPasswordAsync(user.Email);

        Assert.Equal(HttpStatusCode.BadRequest, Assert.IsType<HttpResponseMessage>(result).StatusCode);
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
    public async Task SendVerificationEmailAsync_WhenUserDoesNotExist_ReturnsUserNotFound()
    {
        var userManager = CreateUserManager();
        userManager
            .Setup(manager => manager.FindByIdAsync("missing-user"))
            .ReturnsAsync((ApplicationUser?)null);
        var emailClient = new Mock<IEmailServiceClient>();
        var service = CreateService(userManager, emailClient);

        var result = await service.SendVerificationEmailAsync("missing-user");

        Assert.False(result.Succeeded);
        emailClient.Verify(client => client.SendEmailAsync(It.IsAny<SendEmailRequest>()), Times.Never);
    }

    [Fact]
    public async Task SendVerificationEmailAsync_WhenEmailIsAlreadyConfirmed_ReturnsSuccessWithoutSendingEmail()
    {
        var user = new ApplicationUser { Id = "user-1", Email = "user@example.com", EmailConfirmed = true };
        var userManager = CreateUserManager();
        userManager
            .Setup(manager => manager.FindByIdAsync(user.Id))
            .ReturnsAsync(user);
        var emailClient = new Mock<IEmailServiceClient>();
        var service = CreateService(userManager, emailClient);

        var result = await service.SendVerificationEmailAsync(user.Id);

        Assert.True(result.Succeeded);
        userManager.Verify(
            manager => manager.GenerateEmailConfirmationTokenAsync(It.IsAny<ApplicationUser>()),
            Times.Never);
        emailClient.Verify(client => client.SendEmailAsync(It.IsAny<SendEmailRequest>()), Times.Never);
    }

    [Fact]
    public async Task SendVerificationEmailAsync_WhenEmailIsNotConfirmed_SendsVerificationEmail()
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
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
        var service = CreateService(userManager, emailClient);

        var result = await service.SendVerificationEmailAsync(user.Id);

        Assert.True(result.Succeeded);
        Assert.NotNull(sentRequest);
        Assert.Equal(user.Email, sentRequest!.To);
        Assert.Equal("account_verification", sentRequest.Template);
        var verificationUrl = GetPayloadValue<string>(sentRequest.Data, "VerificationLink");
        Assert.Equal(
            "https://yourapp.com/verify-email?userId=user-1&token=verification%20token",
            verificationUrl);
    }

    [Fact]
    public async Task SendVerificationEmailAsync_WhenEmailServiceFails_ReturnsEmailSendFailed()
    {
        var user = new ApplicationUser
        {
            Id = "user-1",
            Email = "user@example.com",
            UserName = "user"
        };
        var userManager = CreateUserManager();
        userManager
            .Setup(manager => manager.FindByIdAsync(user.Id))
            .ReturnsAsync(user);
        userManager
            .Setup(manager => manager.GenerateEmailConfirmationTokenAsync(user))
            .ReturnsAsync("verification token");
        var emailClient = new Mock<IEmailServiceClient>();
        emailClient
            .Setup(client => client.SendEmailAsync(It.IsAny<SendEmailRequest>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadGateway));
        var service = CreateService(userManager, emailClient);

        var result = await service.SendVerificationEmailAsync(user.Id);

        Assert.False(result.Succeeded);
        var error = Assert.Single(result.Errors);
        Assert.Equal("EmailSendFailed", error.Code);
    }

    [Fact]
    public async Task SendVerificationEmailAsync_EncodesTokenAndHandlesBaseUrlWithoutTrailingSlash()
    {
        var user = new ApplicationUser
        {
            Id = "user-1",
            Email = "user@example.com",
            UserName = "user"
        };
        var userManager = CreateUserManager();
        userManager
            .Setup(manager => manager.FindByIdAsync(user.Id))
            .ReturnsAsync(user);
        userManager
            .Setup(manager => manager.GenerateEmailConfirmationTokenAsync(user))
            .ReturnsAsync("token+with/slash==");
        var emailClient = new Mock<IEmailServiceClient>();
        SendEmailRequest? sentRequest = null;
        emailClient
            .Setup(client => client.SendEmailAsync(It.IsAny<SendEmailRequest>()))
            .Callback<SendEmailRequest>(request => sentRequest = request)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
        var service = new SecurityService(
            userManager.Object,
            emailClient.Object,
            Options.Create(new ApplicationOptions
            {
                BaseUrl = "https://yourapp.com"
            }));

        var result = await service.SendVerificationEmailAsync(user.Id);

        Assert.True(result.Succeeded);
        Assert.Equal(
            "https://yourapp.com/verify-email?userId=user-1&token=token%2Bwith%2Fslash%3D%3D",
            GetPayloadValue<string>(sentRequest!.Data, "VerificationLink"));
    }

    private static SecurityService CreateService(
        Mock<UserManager<ApplicationUser>> userManager,
        Mock<IEmailServiceClient> emailClient) =>
        new(
            userManager.Object,
            emailClient.Object,
            Options.Create(new ApplicationOptions
            {
                BaseUrl = "https://yourapp.com/"
            }));

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