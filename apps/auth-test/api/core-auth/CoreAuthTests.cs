using System.Security.Claims;
using Backend.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace auth_test.api.core_auth;

public class CoreAuthTests
{
    [Fact]
    public async Task Login_WhenServiceSucceeds_ReturnsOkWithResult()
    {
        var request = new LoginRequest { Email = "user@example.com", Password = "password" };
        var expected = AuthResult.Success("access-token");
        var authService = new Mock<IAuthService>();
        authService.Setup(service => service.LoginAsync(request)).ReturnsAsync(expected);
        var controller = new CoreAuthController(authService.Object);

        var result = await controller.Login(request);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(expected, ok.Value);
        authService.Verify(service => service.LoginAsync(request), Times.Once);
    }

    [Fact]
    public async Task Login_WhenServiceFails_ReturnsUnauthorizedWithResult()
    {
        var request = new LoginRequest { Email = "user@example.com", Password = "wrong" };
        var expected = AuthResult.Failure("Invalid credentials");
        var authService = new Mock<IAuthService>();
        authService.Setup(service => service.LoginAsync(request)).ReturnsAsync(expected);
        var controller = new CoreAuthController(authService.Object);

        var result = await controller.Login(request);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Same(expected, unauthorized.Value);
    }

    [Fact]
    public async Task Register_WhenServiceSucceeds_ReturnsOkWithResult()
    {
        var request = new UserRegisterRequest
        {
            UserName = "new-user",
            Email = "new@example.com",
            Password = "password",
            PhoneNumber = "555-0100"
        };
        var expected = AuthResult.Success();
        var authService = new Mock<IAuthService>();
        authService.Setup(service => service.RegisterAsync(request)).ReturnsAsync(expected);
        var controller = new CoreAuthController(authService.Object);

        var result = await controller.Register(request);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(expected, ok.Value);
        authService.Verify(service => service.RegisterAsync(request), Times.Once);
    }

    [Fact]
    public async Task Register_WhenServiceFails_ReturnsBadRequestWithResult()
    {
        var request = new UserRegisterRequest
        {
            UserName = "existing-user",
            Email = "existing@example.com",
            Password = "password",
            PhoneNumber = "555-0101"
        };
        var expected = AuthResult.Failure("Email is already registered");
        var authService = new Mock<IAuthService>();
        authService.Setup(service => service.RegisterAsync(request)).ReturnsAsync(expected);
        var controller = new CoreAuthController(authService.Object);

        var result = await controller.Register(request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Same(expected, badRequest.Value);
    }

    [Fact]
    public async Task Logout_WhenServiceSucceeds_ReturnsOkWithResult()
    {
        var expected = AuthResult.Success();
        var authService = new Mock<IAuthService>();
        authService.Setup(service => service.LogoutAsync()).ReturnsAsync(expected);
        var controller = new CoreAuthController(authService.Object);

        var result = await controller.Logout();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(expected, ok.Value);
        authService.Verify(service => service.LogoutAsync(), Times.Once);
    }

    [Fact]
    public async Task Logout_WhenServiceFails_ReturnsBadRequestWithResult()
    {
        var expected = AuthResult.Failure("Logout failed");
        var authService = new Mock<IAuthService>();
        authService.Setup(service => service.LogoutAsync()).ReturnsAsync(expected);
        var controller = new CoreAuthController(authService.Object);

        var result = await controller.Logout();

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Same(expected, badRequest.Value);
    }

    [Fact]
    public async Task GetCurrentUser_WhenUserExists_ReturnsProjectedUser()
    {
        var claims = new ClaimsPrincipal(new ClaimsIdentity("Bearer"));
        var user = new ApplicationUser
        {
            Id = "user-1",
            UserName = "smurf",
            Email = "smurf@example.com"
        };
        var authService = new Mock<IAuthService>();
        authService.Setup(service => service.GetCurrentUserAsync(claims)).ReturnsAsync(user);
        var controller = CreateController(authService, claims);

        var result = await controller.GetCurrentUser();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
        var value = ok.Value!;
        Assert.Equal(user.Id, GetProperty<string>(value, "Id"));
        Assert.Equal(user.UserName, GetProperty<string>(value, "UserName"));
        Assert.Equal(user.Email, GetProperty<string>(value, "Email"));
        authService.Verify(service => service.GetCurrentUserAsync(claims), Times.Once);
    }

    [Fact]
    public async Task GetCurrentUser_WhenUserDoesNotExist_ReturnsNotFound()
    {
        var claims = new ClaimsPrincipal(new ClaimsIdentity("Bearer"));
        var authService = new Mock<IAuthService>();
        authService.Setup(service => service.GetCurrentUserAsync(claims))
            .ReturnsAsync((ApplicationUser?)null);
        var controller = CreateController(authService, claims);

        var result = await controller.GetCurrentUser();

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public void Logout_RequiresAuthorization()
    {
        var method = typeof(CoreAuthController).GetMethod(nameof(CoreAuthController.Logout));

        Assert.NotNull(method);
        Assert.NotNull(method!.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true).SingleOrDefault());
    }

    [Fact]
    public void GetCurrentUser_RequiresAuthorization()
    {
        var method = typeof(CoreAuthController).GetMethod(nameof(CoreAuthController.GetCurrentUser));

        Assert.NotNull(method);
        Assert.NotNull(method!.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true).SingleOrDefault());
    }

    private static CoreAuthController CreateController(
        Mock<IAuthService> authService,
        ClaimsPrincipal user)
    {
        var controller = new CoreAuthController(authService.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = user
                }
            }
        };

        return controller;
    }

    private static T? GetProperty<T>(object value, string propertyName) =>
        (T?)value.GetType().GetProperty(propertyName)?.GetValue(value);
}