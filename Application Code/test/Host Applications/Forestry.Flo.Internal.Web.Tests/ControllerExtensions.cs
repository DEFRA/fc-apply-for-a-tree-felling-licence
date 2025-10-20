using Forestry.Flo.Internal.Web.Infrastructure.Display;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Tests.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Forestry.Flo.Internal.Web.Tests;

/// <summary>
/// Provides extension methods for preparing <see cref="Controller"/> instances for unit testing.
/// </summary>
public static class ControllerExtensions
{
    /// <summary>
    /// Prepares the specified <see cref="Controller"/> for unit testing by setting up its <see cref="ControllerContext"/>
    /// and <see cref="Controller.TempData"/> properties with a test user and temporary data provider.
    /// </summary>
    /// <param name="controller">The controller instance to prepare for testing.</param>
    /// <param name="localAccountId">The local account ID to use for the test user principal.</param>
    /// <param name="registerMvcServices">A boolean indicating whether to register MVC services.</param>
    /// <param name="role">The role to assign to the test user principal.</param>
    public static void PrepareControllerForTest(
        this Controller controller, 
        Guid localAccountId,
        bool registerMvcServices = false,
        AccountTypeInternal role = AccountTypeInternal.WoodlandOfficer)
    {
        controller.PrepareControllerBaseForTest(localAccountId, registerMvcServices, role);

        var tempData = new TempDataDictionary(
            new DefaultHttpContext(),
            Mock.Of<ITempDataProvider>()
        );

        controller.TempData = tempData;
    }

    /// <summary>
    /// Prepares the specified <see cref="ControllerBase"/> for unit testing by setting up its <see cref="ControllerContext"/>
    /// and <see cref="Controller.TempData"/> properties with a test user and temporary data provider.
    /// </summary>
    /// <param name="controller">The controller base instance to prepare for testing.</param>
    /// <param name="localAccountId">The local account ID to use for the test user principal.</param>
    /// <param name="registerMvcServices">A boolean indicating whether to register MVC services.</param>
    /// <param name="role">The role to assign to the test user principal.</param>
    public static void PrepareControllerBaseForTest(
        this ControllerBase controller,
        Guid localAccountId, 
        bool registerMvcServices = false,
        AccountTypeInternal role = AccountTypeInternal.WoodlandOfficer)
    {
        var serviceProvider = new ServiceCollection().BuildServiceProvider();

        if (registerMvcServices)
        {
            var authServiceMock = new Mock<IAuthenticationService>();
            authServiceMock
                .Setup(s => s.SignOutAsync(
                    It.IsAny<HttpContext>(),
                    It.IsAny<string>(),
                    It.IsAny<AuthenticationProperties>()))
                .Returns(Task.CompletedTask);

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddMvcCore();
            services.AddSingleton(authServiceMock.Object);

            serviceProvider = services.BuildServiceProvider();
        }

        var urlHelperMock = new Mock<IUrlHelper>();
        urlHelperMock
            .Setup(u => u.Action(It.IsAny<UrlActionContext>()))
            .Returns("/fake-url");

        var httpContext = new DefaultHttpContext()
        {
            Request = { Scheme = "Test" }
        };

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        urlHelperMock
            .Setup(u => u.ActionContext)
            .Returns(actionContext);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
                    localAccountId: localAccountId, 
                    accountTypeInternal: role,
                    canApprove: role is AccountTypeInternal.AccountAdministrator or AccountTypeInternal.FieldManager),
                RequestServices = serviceProvider,
            }
        };
        controller.Url = urlHelperMock.Object;
    }
}