using Forestry.Flo.External.Web.Infrastructure.Display;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;

namespace Forestry.Flo.External.Web.Tests.Infrastructure.Display
{
    public class UrlHelperExtensionsTests
    {
        private readonly Mock<IUrlHelper> _urlHelper;

        public UrlHelperExtensionsTests()
        {
            _urlHelper = new Mock<IUrlHelper>();
        }

        private  IUrlHelper SetupUrlHelper(string? hostUrl = null)
        {
            var httpContext = new DefaultHttpContext
            {
                Request =
                {
                    Host = HostString.FromUriComponent(new Uri(hostUrl ?? "https://test.com")),
                    Scheme = "https"
                }
            };
            _urlHelper.SetupGet(h => h.ActionContext)
                .Returns(new ActionContext(httpContext, new RouteData(), new ActionDescriptor()));
            return _urlHelper.Object;
        }


        [Fact]
        public void ShouldReturnAbsolutePathToResource_GivenRelativePath()
        {
            //Arrange
            const string path = "test-path";
            _urlHelper.Setup(h => h.Content(path)).Returns(path);
            
            const string host = "https://test.com";
            var sut = SetupUrlHelper(host);
           
            //Act
            var result = sut.AbsoluteContent(path);
            
            //Assert
            _urlHelper.VerifyAll();
            Assert.Equal($"{host}/{path}", result);
        }
        
        [Fact]
        public void ShouldReturnAbsolutePath_GivenControllerAction()
        {
            //Arrange
            const string action = "Privacy";
            const string controller = "Home";
            const string expectedUrl = "https://test.com/home/privacy";
            
            _urlHelper.Setup(h =>
                h.Action(It.Is<UrlActionContext>(c => 
                    c.Action == action && c.Controller == controller)))
                .Returns(expectedUrl);
            
            var sut = SetupUrlHelper();
            
            //Act
            var result1 = sut.AbsoluteAction(action, controller);
            
            //Assert
            _urlHelper.VerifyAll();
            Assert.Equal(expectedUrl, result1);
        }
    }
}