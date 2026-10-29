using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace auth_test;

public class AuthServiceTests
{
    [Fact]
    public async Task LoginAsync_WhenUserDoesNotExist_ReturnsUserNotFound()
    {
        var userManager = CreateUserManager();
        userManager
            .Setup(manager => manager.FindByEmailAsync("missing@example.com"))
            .ReturnsAsync((ApplicationUser?)null);
        var signInManager = CreateSignInManager(userManager.Object);
        var jwtService = new Mock<IJwtService>();
        var service = CreateService(signInManager, userManager, jwtService);

        var result = await service.LoginAsync(new LoginRequest
        {
            Email = "missing@example.com",
            Password = "password"
        });

        Assert.False(result.IsSuccess);
        var error = Assert.Single(result.Errors!);
        Assert.Equal("UserNotFound", error.Code);
        Assert.Equal("username", error.InputField);
        signInManager.Verify(
            manager => manager.CheckPasswordSignInAsync(
                It.IsAny<ApplicationUser>(),
                It.IsAny<string>(),
                It.IsAny<bool>()),
            Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WhenPasswordIsInvalid_ReturnsInvalidCredentials()
    {
        var user = new ApplicationUser { Id = "user-1", Email = "user@example.com" };
        var userManager = CreateUserManager();
        userManager
            .Setup(manager => manager.FindByEmailAsync(user.Email))
            .ReturnsAsync(user);
        var signInManager = CreateSignInManager(userManager.Object);
        signInManager
            .Setup(manager => manager.CheckPasswordSignInAsync(user, "wrong", true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);
        var jwtService = new Mock<IJwtService>();
        var service = CreateService(signInManager, userManager, jwtService);

        var result = await service.LoginAsync(new LoginRequest
        {
            Email = user.Email,
            Password = "wrong"
        });

        Assert.False(result.IsSuccess);
        var error = Assert.Single(result.Errors!);
        Assert.Equal("InvalidCredentials", error.Code);
        Assert.Equal("password", error.InputField);
        jwtService.Verify(jwt => jwt.CreateAccessToken(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WhenUserIsLockedOut_ReturnsUserLockedOut()
    {
        var user = new ApplicationUser { Email = "locked@example.com" };
        var userManager = CreateUserManager();
        userManager
            .Setup(manager => manager.FindByEmailAsync(user.Email))
            .ReturnsAsync(user);
        var signInManager = CreateSignInManager(userManager.Object);
        signInManager
            .Setup(manager => manager.CheckPasswordSignInAsync(user, "password", true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.LockedOut);
        var service = CreateService(signInManager, userManager, new Mock<IJwtService>());

        var result = await service.LoginAsync(new LoginRequest
        {
            Email = user.Email,
            Password = "password"
        });

        Assert.False(result.IsSuccess);
        var error = Assert.Single(result.Errors!);
        Assert.Equal("UserLockedOut", error.Code);
        Assert.Equal("username", error.InputField);
    }

    [Fact]
    public async Task LoginAsync_WhenCredentialsAreValid_ReturnsAccessToken()
    {
        var user = new ApplicationUser { Id = "user-1", Email = "user@example.com" };
        var userManager = CreateUserManager();
        userManager
            .Setup(manager => manager.FindByEmailAsync(user.Email))
            .ReturnsAsync(user);
        var signInManager = CreateSignInManager(userManager.Object);
        signInManager
            .Setup(manager => manager.CheckPasswordSignInAsync(user, "password", true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);
        var jwtService = new Mock<IJwtService>();
        jwtService.Setup(jwt => jwt.CreateAccessToken(user)).Returns("access-token");
        var service = CreateService(signInManager, userManager, jwtService);

        var result = await service.LoginAsync(new LoginRequest
        {
            Email = user.Email,
            Password = "password"
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("access-token", result.AccessToken);
        Assert.Equal("Bearer", result.TokenType);
        Assert.Equal(3600, result.ExpiresIn);
    }

    [Fact]
    public async Task RegisterAsync_WhenCreationSucceeds_ReturnsAccessTokenAndSignsUserIn()
    {
        var userManager = CreateUserManager();
        userManager
            .Setup(manager => manager.CreateAsync(
                It.Is<ApplicationUser>(user =>
                    user.Email == "new@example.com" &&
                    user.UserName == "new-user" &&
                    user.PhoneNumber == "555-0100"),
                "password"))
            .ReturnsAsync(IdentityResult.Success);
        var signInManager = CreateSignInManager(userManager.Object);
        signInManager
            .Setup(manager => manager.SignInAsync(
                It.Is<ApplicationUser>(user =>
                    user.Email == "new@example.com" &&
                    user.UserName == "new-user" &&
                    user.PhoneNumber == "555-0100"),
                false,
                null))
            .Returns(Task.CompletedTask);
        var jwtService = new Mock<IJwtService>();
        jwtService
            .Setup(jwt => jwt.CreateAccessToken(It.Is<ApplicationUser>(user =>
                user.Email == "new@example.com" &&
                user.UserName == "new-user" &&
                user.PhoneNumber == "555-0100")))
            .Returns("registration-access-token");
        var service = CreateService(
            signInManager,
            userManager,
            jwtService);

        var result = await service.RegisterAsync(new UserRegisterRequest
        {
            UserName = "new-user",
            Email = "new@example.com",
            Password = "password",
            PhoneNumber = "555-0100"
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("registration-access-token", result.AccessToken);
        Assert.Equal("Bearer", result.TokenType);
        Assert.Equal(3600, result.ExpiresIn);
        Assert.Empty(result.Errors!);
        signInManager.Verify(manager => manager.SignInAsync(
            It.Is<ApplicationUser>(user =>
                user.Email == "new@example.com" &&
                user.UserName == "new-user" &&
                user.PhoneNumber == "555-0100"),
            false,
            null), Times.Once);
        jwtService.Verify(jwt => jwt.CreateAccessToken(It.Is<ApplicationUser>(user =>
            user.Email == "new@example.com" &&
            user.UserName == "new-user" &&
            user.PhoneNumber == "555-0100")), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WhenCreationFails_MapsErrorsToInputFields()
    {
        var userManager = CreateUserManager();
        userManager
            .Setup(manager => manager.CreateAsync(It.IsAny<ApplicationUser>(), "weak"))
            .ReturnsAsync(IdentityResult.Failed(
                new IdentityError { Code = "PasswordTooShort", Description = "Password is too short." },
                new IdentityError { Code = "DuplicateUserName", Description = "Username is already taken." },
                new IdentityError { Code = "Unknown", Description = "Registration failed." }));
        var service = CreateService(
            CreateSignInManager(userManager.Object),
            userManager,
            new Mock<IJwtService>());

        var result = await service.RegisterAsync(new UserRegisterRequest
        {
            UserName = "existing-user",
            Email = "existing@example.com",
            Password = "weak",
            PhoneNumber = "555-0101"
        });

        Assert.False(result.IsSuccess);
        Assert.Equal(
            new[] { "password", "username", "general" },
            result.Errors!.Select(error => error.InputField));
        Assert.Contains("Password is too short.", result.ErrorMessage);
        Assert.Contains("Username is already taken.", result.ErrorMessage);
    }

    [Fact]
    public async Task LogoutAsync_ReturnsSuccess()
    {
        var service = CreateService(
            CreateSignInManager(CreateUserManager().Object),
            CreateUserManager(),
            new Mock<IJwtService>());

        var result = await service.LogoutAsync();

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Errors!);
    }

    [Fact]
    public async Task GetCurrentUserAsync_DelegatesToUserManager()
    {
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity("test"));
        var user = new ApplicationUser { Id = "user-1" };
        var userManager = CreateUserManager();
        userManager
            .Setup(manager => manager.GetUserAsync(claimsPrincipal))
            .ReturnsAsync(user);
        var service = CreateService(
            CreateSignInManager(userManager.Object),
            userManager,
            new Mock<IJwtService>());

        var result = await service.GetCurrentUserAsync(claimsPrincipal);

        Assert.Same(user, result);
        userManager.Verify(manager => manager.GetUserAsync(claimsPrincipal), Times.Once);
    }

    private static AuthService CreateService(
        Mock<SignInManager<ApplicationUser>> signInManager,
        Mock<UserManager<ApplicationUser>> userManager,
        Mock<IJwtService> jwtService) =>
        new(signInManager.Object, userManager.Object, jwtService.Object);

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

    private static Mock<SignInManager<ApplicationUser>> CreateSignInManager(
        UserManager<ApplicationUser> userManager) =>
        new(
            userManager,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<ApplicationUser>>(),
            Options.Create(new IdentityOptions()),
            Mock.Of<ILogger<SignInManager<ApplicationUser>>>(),
            Mock.Of<IAuthenticationSchemeProvider>(),
            Mock.Of<IUserConfirmation<ApplicationUser>>());
}
