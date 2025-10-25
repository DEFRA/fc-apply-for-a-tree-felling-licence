using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Controllers.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Forestry.Flo.Internal.Web.Tests.Controllers.FellingLicenceApplication;

public class AgentAuthorityFormControllerTests
{
    private readonly Mock<IViewAgentAuthorityFormUseCase> _viewAgentAuthorityFormUseCaseMock;
    private readonly AgentAuthorityFormController _controller;
    private readonly Guid _agentAuthorityId = Guid.NewGuid();
    private readonly Guid _agentAuthorityFormId = Guid.NewGuid();

    public AgentAuthorityFormControllerTests()
    {
        _viewAgentAuthorityFormUseCaseMock = new Mock<IViewAgentAuthorityFormUseCase>();
        _controller = new AgentAuthorityFormController(_viewAgentAuthorityFormUseCaseMock.Object);
    }

    [Fact]
    public async Task DownloadAgentAuthorityFormDocument_ReturnsRedirectToError_WhenResultIsFailure()
    {
        _viewAgentAuthorityFormUseCaseMock
            .Setup(x => x.GetAgentAuthorityFormDocumentsAsync(_agentAuthorityId, _agentAuthorityFormId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<FileContentResult>("error"));

        var result = await _controller.DownloadAgentAuthorityFormDocument(_agentAuthorityId, _agentAuthorityFormId, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirectResult.ActionName);
        Assert.Equal("Home", redirectResult.ControllerName);
    }

    [Fact]
    public async Task DownloadAgentAuthorityFormDocument_ReturnsFileResult_WhenResultIsSuccess()
    {
        var fileResult = new FileContentResult(Array.Empty<byte>(), "application/pdf");
        _viewAgentAuthorityFormUseCaseMock
            .Setup(x => x.GetAgentAuthorityFormDocumentsAsync(_agentAuthorityId, _agentAuthorityFormId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<FileContentResult>(fileResult));

        var result = await _controller.DownloadAgentAuthorityFormDocument(_agentAuthorityId, _agentAuthorityFormId, CancellationToken.None);

        Assert.Same(fileResult, result);
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenUseCaseIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new AgentAuthorityFormController(null!));
    }
}
