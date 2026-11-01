using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Backend.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace auth_test.api.core_auth;

public class CoreAuthTests
{
    [Fact]
    public async Task Login_WhenCredentialsAreValid_RedirectsToLocalReturnUrl()
    {
        var userManager = CreateUserManager();
        var signInManager = CreateSignInManager(userManager.Object);
        signInManager
            .Setup(manager => manager.PasswordSignInAsync(
                "smurf",
                "password",
                true,
                true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);
        var controller = CreateController(userManager, signInManager);

        var result = await controller.Login(new UserLoginRequest
        {
            UserName = "smurf",
            Password = "password",
            RememberMe = true,
            ReturnUrl = "/connect/authorize?client_id=swagger-ui"
        });

        var redirect = Assert.IsType<LocalRedirectResult>(result);
        Assert.Equal("/connect/authorize?client_id=swagger-ui", redirect.Url);
    }

    [Fact]
    public async Task Login_WhenCredentialsAreInvalid_ReturnsUnauthorized()
    {
        var userManager = CreateUserManager();
        var signInManager = CreateSignInManager(userManager.Object);
        signInManager
            .Setup(manager => manager.PasswordSignInAsync(
                "smurf",
                "wrong",
                false,
                true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);
        var controller = CreateController(userManager, signInManager);

        var result = await controller.Login(new UserLoginRequest
        {
            UserName = "smurf",
            Password = "wrong",
            ReturnUrl = "/"
        });

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(
            "Invalid login attempt.",
            GetProperty<string>(unauthorized.Value!, "Message"));
    }

    [Fact]
    public async Task Login_WhenReturnUrlIsExternal_ReturnsBadRequest()
    {
        var userManager = CreateUserManager();
        var signInManager = CreateSignInManager(userManager.Object);
        var controller = CreateController(userManager, signInManager);

        var result = await controller.Login(new UserLoginRequest
        {
            UserName = "smurf",
            Password = "password",
            ReturnUrl = "https://malicious.example"
        });

        Assert.IsType<BadRequestResult>(result);
        signInManager.Verify(
            manager => manager.PasswordSignInAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<bool>()),
            Times.Never);
    }

    [Fact]
    public async Task Register_WhenCreationSucceeds_SignsUserInAndRedirects()
    {
        var userManager = CreateUserManager();
        userManager
            .Setup(manager => manager.CreateAsync(
                It.Is<ApplicationUser>(user =>
                    user.UserName == "new-user" &&
                    user.Email == "new@example.com" &&
                    user.PhoneNumber == "555-0100"),
                "password"))
            .ReturnsAsync(IdentityResult.Success);
        var signInManager = CreateSignInManager(userManager.Object);
        signInManager
            .Setup(manager => manager.SignInAsync(
                It.Is<ApplicationUser>(user =>
                    user.UserName == "new-user" &&
                    user.Email == "new@example.com"),
                false,
                null))
            .Returns(Task.CompletedTask);
        var controller = CreateController(userManager, signInManager);

        var result = await controller.Register(
            new UserRegisterRequest
            {
                UserName = "new-user",
                Email = "new@example.com",
                Password = "password",
                PhoneNumber = "555-0100"
            },
            "/connect/authorize?client_id=swagger-ui");

        var redirect = Assert.IsType<LocalRedirectResult>(result);
        Assert.Equal("/connect/authorize?client_id=swagger-ui", redirect.Url);
        signInManager.Verify(
            manager => manager.SignInAsync(
                It.IsAny<ApplicationUser>(),
                false,
                null),
            Times.Once);
    }

    [Fact]
    public async Task Register_WhenCreationFails_ReturnsBadRequest()
    {
        var userManager = CreateUserManager();
        userManager
            .Setup(manager => manager.CreateAsync(
                It.IsAny<ApplicationUser>(),
                "weak"))
            .ReturnsAsync(IdentityResult.Failed(
                new IdentityError { Code = "PasswordTooShort" }));
        var signInManager = CreateSignInManager(userManager.Object);
        var controller = CreateController(userManager, signInManager);

        var result = await controller.Register(
            new UserRegisterRequest
            {
                UserName = "new-user",
                Email = "new@example.com",
                Password = "weak"
            },
            "/");

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.IsType<IdentityResult>(badRequest.Value);
        signInManager.Verify(
            manager => manager.SignInAsync(
                It.IsAny<ApplicationUser>(),
                It.IsAny<bool>(),
                It.IsAny<string?>()),
            Times.Never);
    }

    [Fact]
    public async Task Logout_SignsUserOutAndReturnsOk()
    {
        var userManager = CreateUserManager();
        var signInManager = CreateSignInManager(userManager.Object);
        signInManager
            .Setup(manager => manager.SignOutAsync())
            .Returns(Task.CompletedTask);
        var controller = CreateController(userManager, signInManager);

        var result = await controller.Logout();

        Assert.IsType<OkResult>(result);
        signInManager.Verify(manager => manager.SignOutAsync(), Times.Once);
    }

    [Fact]
    public async Task GetCurrentUser_WhenUserExists_ReturnsProjectedUser()
    {
        var user = new ApplicationUser
        {
            Id = "user-1",
            UserName = "smurf",
            Email = "smurf@example.com"
        };
        var userManager = CreateUserManager();
        userManager
            .Setup(manager => manager.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);
        var signInManager = CreateSignInManager(userManager.Object);
        var controller = CreateController(userManager, signInManager);

        var result = await controller.GetCurrentUser();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(user.Id, GetProperty<string>(ok.Value!, "Id"));
        Assert.Equal(user.UserName, GetProperty<string>(ok.Value!, "UserName"));
        Assert.Equal(user.Email, GetProperty<string>(ok.Value!, "Email"));
    }

    [Fact]
    public async Task GetCurrentUser_WhenUserDoesNotExist_ReturnsNotFound()
    {
        var userManager = CreateUserManager();
        userManager
            .Setup(manager => manager.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((ApplicationUser?)null);
        var signInManager = CreateSignInManager(userManager.Object);
        var controller = CreateController(userManager, signInManager);

        var result = await controller.GetCurrentUser();

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public void Logout_RequiresAuthorization()
    {
        var method = typeof(CoreAuthController).GetMethod(nameof(CoreAuthController.Logout));

        Assert.NotNull(method);
        Assert.NotNull(method!.GetCustomAttributes(
            typeof(AuthorizeAttribute),
            inherit: true).SingleOrDefault());
    }

    [Fact]
    public void GetCurrentUser_RequiresAuthorization()
    {
        var method = typeof(CoreAuthController).GetMethod(
            nameof(CoreAuthController.GetCurrentUser));

        Assert.NotNull(method);
        Assert.NotNull(method!.GetCustomAttributes(
            typeof(AuthorizeAttribute),
            inherit: true).SingleOrDefault());
    }

    private static CoreAuthController CreateController(
        Mock<UserManager<ApplicationUser>> userManager,
        Mock<SignInManager<ApplicationUser>> signInManager)
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity("Identity.Application"))
        };
        var actionContext = new ActionContext(
            httpContext,
            new Microsoft.AspNetCore.Routing.RouteData(),
            new Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor());

        return new CoreAuthController(userManager.Object, signInManager.Object)
        {
            ControllerContext = new ControllerContext(actionContext),
            Url = new Microsoft.AspNetCore.Mvc.Routing.UrlHelper(actionContext)
        };
    }

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

    private static T? GetProperty<T>(object value, string propertyName) =>
        (T?)value.GetType().GetProperty(propertyName)?.GetValue(value);
}
