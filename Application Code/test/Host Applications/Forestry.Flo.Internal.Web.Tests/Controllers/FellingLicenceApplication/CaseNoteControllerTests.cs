using AutoFixture;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Controllers.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Forestry.Flo.Internal.Web.Tests.Controllers.FellingLicenceApplication;

public class CaseNoteControllerTests
{
    private readonly CaseNoteController _controller = new();
    private readonly Mock<IAmendCaseNotes> _amendCaseNotesMock = new();
    private readonly Mock<IUrlHelper> _mockUrlHelper = new();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Fixture _fixture = new();

    [Fact]
    public async Task AddCaseNote_RedirectsToError_WhenResultIsFailure()
    {
        var model = _fixture.Create<AddCaseNoteModel>();

        _amendCaseNotesMock
            .Setup(x => x.AddCaseNoteAsync(It.IsAny<AddCaseNoteRecord>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<bool>("error"));

        _controller.PrepareControllerForTest(_userId, role: AccountTypeInternal.WoodlandOfficer);

        var result = await _controller.AddCaseNote(model, _amendCaseNotesMock.Object, _mockUrlHelper.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirectResult.ActionName);
        Assert.Equal("Home", redirectResult.ControllerName);
    }

    [Fact]
    public async Task AddCaseNote_RedirectsToReturnUrl_WhenSuccess()
    {
        var model = _fixture.Create<AddCaseNoteModel>();

        _controller.PrepareControllerForTest(Guid.NewGuid(), role: AccountTypeInternal.WoodlandOfficer);

        _mockUrlHelper.Setup(x => x.IsLocalUrl(It.IsAny<string?>())).Returns(true);

        _amendCaseNotesMock
            .Setup(x => x.AddCaseNoteAsync(It.IsAny<AddCaseNoteRecord>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));

        var result = await _controller.AddCaseNote(model, _amendCaseNotesMock.Object, _mockUrlHelper.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Equal(model.ReturnUrl, redirectResult.Url);
    }

    [Fact]
    public async Task AddCaseNote_RedirectsToReturnUrl_WhenSuccessButInvalidReturnUrl()
    {
        var model = _fixture.Create<AddCaseNoteModel>();

        _controller.PrepareControllerForTest(Guid.NewGuid(), role: AccountTypeInternal.WoodlandOfficer);

        _mockUrlHelper.Setup(x => x.IsLocalUrl(It.IsAny<string?>())).Returns(false);

        _amendCaseNotesMock
            .Setup(x => x.AddCaseNoteAsync(It.IsAny<AddCaseNoteRecord>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));

        var result = await _controller.AddCaseNote(model, _amendCaseNotesMock.Object, _mockUrlHelper.Object, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ApplicationSummary", redirectResult.ActionName);
        Assert.Equal("FellingLicenceApplication", redirectResult.ControllerName);
    }
}