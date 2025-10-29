using AutoFixture;
using Forestry.Flo.Internal.Web.Middleware;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Infrastructure;
using Forestry.Flo.Services.InternalUsers;
using Forestry.Flo.Services.InternalUsers.Configuration;
using Forestry.Flo.Services.InternalUsers.Entities.UserAccount;
using Forestry.Flo.Tests.Common;
using GovUk.OneLogin.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Security.Claims;
using AuthenticationOptions = Forestry.Flo.Services.Common.Infrastructure.AuthenticationOptions;
using UserAccount = Forestry.Flo.Services.InternalUsers.Entities.UserAccount.UserAccount;

namespace Forestry.Flo.Internal.Web.Tests.Middleware;

public class UserAccountValidationMiddlewareTests
{
    private readonly InternalUsersContext _internalUsersContext = TestInternalUserDatabaseFactory.CreateDefaultTestContext();
    private readonly Fixture _fixture = new();


    private UserAccountValidationMiddleware CreateMiddleware(
        PermittedRegisteredUserOptions? permittedOptions = null,
        AuthenticationOptions? authOptions = null,
        ILogger<UserAccountValidationMiddleware>? logger = null)
    {
        var dbContextFactoryMock = new Mock<IDbContextFactory<InternalUsersContext>>();
        dbContextFactoryMock.Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => _internalUsersContext);

        permittedOptions ??= new PermittedRegisteredUserOptions
        {
            PermittedEmailDomainsForRegisteredUser = [ "test.com" ]
        };
        authOptions ??= new AuthenticationOptions { Provider = AuthenticationProvider.Azure };
        logger ??= Mock.Of<ILogger<UserAccountValidationMiddleware>>();

        return new UserAccountValidationMiddleware(
            dbContextFactoryMock.Object,
            Options.Create(permittedOptions),
            Options.Create(authOptions),
            logger
        );
    }

    private static ClaimsPrincipal CreatePrincipal(
        bool isAuthenticated = true,
        string? email = "user@test.com",
        string? nameIdentifier = "id",
        AuthenticationProvider provider = AuthenticationProvider.Azure)
    {
        var claims = new List<Claim>();
        if (isAuthenticated)
        {
            if (provider == AuthenticationProvider.OneLogin)
            {
                claims.Add(new Claim(OneLoginPrincipalClaimTypes.EmailAddress, email ?? ""));
                claims.Add(new Claim(OneLoginPrincipalClaimTypes.NameIdentifier, nameIdentifier ?? ""));
            }
            else
            {
                claims.Add(new Claim(FloClaimTypes.Email, email ?? ""));
                claims.Add(new Claim(ClaimTypes.NameIdentifier, nameIdentifier ?? ""));
            }
        }
        var identity = new ClaimsIdentity(claims, isAuthenticated ? "Test" : null);
        return new ClaimsPrincipal(identity);
    }

    private static DefaultHttpContext CreateHttpContext(
        string path = "/",
        ClaimsPrincipal? user = null)
    {
        var context = new DefaultHttpContext
        {
            Request =
            {
                Path = path,
                Scheme = "https",
                Host = new HostString("localhost")
            },
            User = user ?? new ClaimsPrincipal(new ClaimsIdentity())
        };
        return context;
    }

    [Fact]
    public async Task InvokeAsync_DoesNotRunOnErrorPage()
    {
        var middleware = CreateMiddleware();
        var context = CreateHttpContext("/Home/Error");
        var called = false;
        await middleware.InvokeAsync(context, _ => { called = true; return Task.CompletedTask; });
        Assert.True(called);
    }


    [Fact]
    public async Task InvokeAsync_DoesNotRunOnLogoutPage()
    {
        var middleware = CreateMiddleware();
        var context = CreateHttpContext("/Home/Logout");
        var called = false;
        await middleware.InvokeAsync(context, _ => { called = true; return Task.CompletedTask; });
        Assert.True(called);
    }

    [Fact]
    public async Task InvokeAsync_SkipsIfNotAuthenticated()
    {
        var middleware = CreateMiddleware();
        var context = CreateHttpContext("/", new ClaimsPrincipal(new ClaimsIdentity()));
        var called = false;
        await middleware.InvokeAsync(context, _ => { called = true; return Task.CompletedTask; });
        Assert.True(called);
    }

    [Fact]
    public async Task InvokeAsync_BlocksIfEmailDomainNotPermitted()
    {
        var options = new PermittedRegisteredUserOptions { PermittedEmailDomainsForRegisteredUser = ["allowed.com"] };
        var middleware = CreateMiddleware(permittedOptions: options);
        var context = CreateHttpContext("/", CreatePrincipal(email: "user@notallowed.com"));
        var called = false;
        await middleware.InvokeAsync(context, _ => { called = true; return Task.CompletedTask; });
        Assert.Equal("/Home/AccountError", context.Response.Headers["Location"]);
        Assert.False(called);
    }

    [Fact]
    public async Task InvokeAsync_AllowsIfEmailDomainNotPermittedButOnErrorUrl()
    {
        var options = new PermittedRegisteredUserOptions { PermittedEmailDomainsForRegisteredUser = [ "allowed.com" ] };
        var middleware = CreateMiddleware(permittedOptions: options);
        var context = CreateHttpContext("/Home/AccountError", CreatePrincipal(email: "user@notallowed.com"));
        var called = false;
        await middleware.InvokeAsync(context, _ => { called = true; return Task.CompletedTask; });
        Assert.True(called);
    }

    [Fact]
    public async Task InvokeAsync_ClearsCookiesAndSignsOutIfUserAccountNotFound_Azure()
    {
        var context = CreateHttpContext("/", CreatePrincipal());
        var authOptions = new AuthenticationOptions { Provider = AuthenticationProvider.Azure };
        var middleware = CreateMiddleware(authOptions: authOptions);

        var signOutCalled = new List<string>();
        context.RequestServices = new ServiceCollection()
            .AddSingleton(MockAuthService(signOutCalled))
            .BuildServiceProvider();

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        Assert.Contains("SignIn", signOutCalled);
        Assert.Contains("SignUp", signOutCalled);
        Assert.Equal("\"cookies\", \"storage\", \"cache\"", context.Response.Headers["Clear-Site-Data"]);
    }

    [Fact]
    public async Task InvokeAsync_ClearsCookiesAndSignsOutIfUserAccountNotFound_OneLogin()
    {
        var context = CreateHttpContext("/", CreatePrincipal(provider: AuthenticationProvider.OneLogin));
        var authOptions = new AuthenticationOptions { Provider = AuthenticationProvider.OneLogin };
        var middleware = CreateMiddleware(authOptions: authOptions);

        var signOutCalled = new List<string>();
        context.RequestServices = new ServiceCollection()
            .AddSingleton(MockAuthService(signOutCalled))
            .BuildServiceProvider();

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        Assert.Contains(CookieAuthenticationDefaults.AuthenticationScheme, signOutCalled);
        Assert.Contains(OneLoginDefaults.AuthenticationScheme, signOutCalled);
        Assert.Equal("\"cookies\", \"storage\", \"cache\"", context.Response.Headers["Clear-Site-Data"]);
    }

    [Fact]
    public async Task InvokeAsync_RedirectsIfUserAccountInvalid()
    {
        var userAccount = _fixture.Build<UserAccount>()
            .With(x => x.IdentityProviderId, "id")
            .With(x => x.Status, Status.Closed)
            .Create();

        _internalUsersContext.UserAccounts.Add(userAccount);
        await _internalUsersContext.SaveChangesAsync();

        var context = CreateHttpContext("/", CreatePrincipal());
        var middleware = CreateMiddleware();

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        Assert.Equal("/Home/AccountError", context.Response.Headers["Location"]);
    }

    [Fact]
    public async Task InvokeAsync_AllowsIfUserAccountInvalidButOnErrorUrl()
    {
        var userAccount = _fixture.Build<UserAccount>()
            .With(x => x.IdentityProviderId, "id")
            .With(x => x.Status, Status.Closed)
            .Create();
        _internalUsersContext.UserAccounts.Add(userAccount);
        await _internalUsersContext.SaveChangesAsync();

        var context = CreateHttpContext("/Home/AccountError", CreatePrincipal());
        var middleware = CreateMiddleware();

        var called = false;
        await middleware.InvokeAsync(context, _ => { called = true; return Task.CompletedTask; });

        Assert.True(called);
    }

    [Fact]
    public async Task InvokeAsync_RedirectsIfUserRegistrationDetailsIncomplete()
    {
        var userAccount = _fixture.Build<UserAccount>()
            .With(x => x.IdentityProviderId, "id")
            .With(x => x.Status, Status.Confirmed)
            .With(x => x.FirstName, "")
            .With(x => x.LastName, "")
            .Create();
        _internalUsersContext.UserAccounts.Add(userAccount);
        await _internalUsersContext.SaveChangesAsync();

        var context = CreateHttpContext("/", CreatePrincipal());
        var middleware = CreateMiddleware();

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        Assert.Equal("/Account/RegisterAccountDetails", context.Response.Headers["Location"]);
    }

    [Fact]
    public async Task InvokeAsync_AllowsIfUserRegistrationDetailsIncompleteButOnRegisterAccountDetailsUrl()
    {
        var userAccount = _fixture.Build<UserAccount>()
            .With(x => x.IdentityProviderId, "id")
            .With(x => x.Status, Status.Confirmed)
            .With(x => x.FirstName, "")
            .With(x => x.LastName, "")
            .Create();
        _internalUsersContext.UserAccounts.Add(userAccount);
        await _internalUsersContext.SaveChangesAsync();

        var context = CreateHttpContext("/Account/RegisterAccountDetails", CreatePrincipal());
        var middleware = CreateMiddleware();

        var called = false;
        await middleware.InvokeAsync(context, _ => { called = true; return Task.CompletedTask; });

        Assert.True(called);
    }

    [Fact]
    public async Task InvokeAsync_RedirectsIfUserAccountNotConfirmed()
    {
        var userAccount = _fixture.Build<UserAccount>()
            .With(x => x.IdentityProviderId, "id")
            .With(x => x.Status, Status.Requested)
            .With(x => x.FirstName, "A")
            .With(x => x.LastName, "B")
            .Create();

        _internalUsersContext.UserAccounts.Add(userAccount);
        await _internalUsersContext.SaveChangesAsync();

        var context = CreateHttpContext("/", CreatePrincipal());
        var middleware = CreateMiddleware();

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        Assert.Equal("/Account/UserAccountAwaitingConfirmation", context.Response.Headers["Location"]);
    }

    [Fact]
    public async Task InvokeAsync_AllowsIfUserAccountNotConfirmedButOnAwaitingConfirmationUrl()
    {
        var userAccount = _fixture.Build<UserAccount>()
            .With(x => x.IdentityProviderId, "id")
            .With(x => x.Status, Status.Requested)
            .With(x => x.FirstName, "A")
            .With(x => x.LastName, "B")
            .Create();
        _internalUsersContext.UserAccounts.Add(userAccount);
        await _internalUsersContext.SaveChangesAsync();

        var context = CreateHttpContext("/Account/UserAccountAwaitingConfirmation", CreatePrincipal());
        var middleware = CreateMiddleware();

        var called = false;
        await middleware.InvokeAsync(context, _ => { called = true; return Task.CompletedTask; });

        Assert.True(called);
    }

    [Fact]
    public async Task InvokeAsync_AllowsIfUserAccountValidAndConfirmed()
    {
        var userAccount = _fixture.Build<UserAccount>()
            .With(x => x.IdentityProviderId, "id")
            .With(x => x.Status, Status.Confirmed)
            .With(x => x.FirstName, "A")
            .With(x => x.LastName, "B")
            .Create();
        _internalUsersContext.UserAccounts.Add(userAccount);
        await _internalUsersContext.SaveChangesAsync();
        var context = CreateHttpContext("/", CreatePrincipal());
        var middleware = CreateMiddleware();

        var called = false;
        await middleware.InvokeAsync(context, _ => { called = true; return Task.CompletedTask; });

        Assert.True(called);
    }

    [Fact]
    public async Task InvokeAsync_LogsAndRedirectsOnException()
    {
        var loggerMock = new Mock<ILogger<UserAccountValidationMiddleware>>();
        var context = CreateHttpContext("/", CreatePrincipal());
        var middleware = CreateMiddleware(logger: loggerMock.Object);

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        Assert.Equal("/Home/Error", context.Response.Headers["Location"]);
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error validating user account")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private static IAuthenticationService MockAuthService(List<string> signOutCalled)
    {
        var mock = new Mock<IAuthenticationService>();
        mock.Setup(x => x.SignOutAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<AuthenticationProperties>()))
            .Returns<HttpContext, string, AuthenticationProperties>((_, scheme, _) =>
            {
                signOutCalled.Add(scheme);
                return Task.CompletedTask;
            });
        return mock.Object;
    }
}