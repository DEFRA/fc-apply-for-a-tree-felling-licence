using CSharpFunctionalExtensions;
using Forestry.Flo.HostApplicationsCommon.Infrastructure;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.Api;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using VoluntaryWithdrawalNotificationOptions = Forestry.Flo.Internal.Web.Infrastructure.VoluntaryWithdrawalNotificationOptions;

namespace Forestry.Flo.Internal.Web.Tests.Services;

public class AutomaticWithdrawalNotificationUseCaseTests
{
    private readonly Mock<IWithdrawalNotificationService> _withdrawalNotificationServiceMock = new();
    private readonly Mock<IOptions<VoluntaryWithdrawalNotificationOptions>> _settingsMock = new();
    private readonly Mock<IWithdrawApplicationInternalUseCase> _mockWithdrawUseCase = new();
    private readonly TimeSpan _threshold = TimeSpan.FromDays(15);
    private readonly InternalUserSiteOptions _options = new InternalUserSiteOptions { BaseUrl = "https://internal-application-base-url/" };

    [Theory, AutoMoqData]
    public async Task WhenUnableToRetrieveApplicationsToWithdraw(
        string error)
    {
        var sut = CreateSut();

        _withdrawalNotificationServiceMock
            .Setup(x => x.GetApplicationsAfterThresholdForWithdrawalAsync(It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<IList<VoluntaryWithdrawalNotificationModel>>(error));

        await sut.ProcessApplicationsAsync(CancellationToken.None);

        _withdrawalNotificationServiceMock
            .Verify(x => x.GetApplicationsAfterThresholdForWithdrawalAsync(_threshold, It.IsAny<CancellationToken>()), Times.Once);
        _withdrawalNotificationServiceMock.VerifyNoOtherCalls();

        _mockWithdrawUseCase.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task WhenNoApplicationsToWithdraw()
    {
        var sut = CreateSut();

        _withdrawalNotificationServiceMock
            .Setup(x => x.GetApplicationsAfterThresholdForWithdrawalAsync(It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IList<VoluntaryWithdrawalNotificationModel>>(new List<VoluntaryWithdrawalNotificationModel>()));

        await sut.ProcessApplicationsAsync(CancellationToken.None);

        _withdrawalNotificationServiceMock
            .Verify(x => x.GetApplicationsAfterThresholdForWithdrawalAsync(_threshold, It.IsAny<CancellationToken>()), Times.Once);
        _withdrawalNotificationServiceMock.VerifyNoOtherCalls();

        _mockWithdrawUseCase.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenApplicationsToWithdraw(
        List<VoluntaryWithdrawalNotificationModel> applicationsToWithdraw)
    {
        var sut = CreateSut();

        _withdrawalNotificationServiceMock
            .Setup(x => x.GetApplicationsAfterThresholdForWithdrawalAsync(It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IList<VoluntaryWithdrawalNotificationModel>>(applicationsToWithdraw));

        await sut.ProcessApplicationsAsync(CancellationToken.None);

        _withdrawalNotificationServiceMock
            .Verify(x => x.GetApplicationsAfterThresholdForWithdrawalAsync(_threshold, It.IsAny<CancellationToken>()), Times.Once);
        _withdrawalNotificationServiceMock.VerifyNoOtherCalls();

        foreach (var application in applicationsToWithdraw)
        {
            var expectedLinkToApplication = $"{_options.BaseUrl}FellingLicenceApplication/ApplicationSummary/{application.ApplicationId}";
            _mockWithdrawUseCase.Verify(x => x.WithdrawApplicationAsync(
                application.ApplicationId,
                WithdrawalReason.ExceededResubmitDeadline,
                expectedLinkToApplication,
                It.IsAny<CancellationToken>()), Times.Once);
        }
        _mockWithdrawUseCase.VerifyNoOtherCalls();
    }

    private AutomaticWithdrawalNotificationUseCase CreateSut()
    {
        _withdrawalNotificationServiceMock.Reset();
        _settingsMock.Setup(x => x.Value)
            .Returns(new VoluntaryWithdrawalNotificationOptions { ThresholdAutomaticWithdrawal = _threshold });
        _mockWithdrawUseCase.Reset();

        return new AutomaticWithdrawalNotificationUseCase(
            _withdrawalNotificationServiceMock.Object,
            _settingsMock.Object,
            new OptionsWrapper<InternalUserSiteOptions>(_options),
            new NullLogger<AutomaticWithdrawalNotificationUseCase>(),
            _mockWithdrawUseCase.Object);
    }
}
