using CSharpFunctionalExtensions;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Forestry.Flo.Services.FellingLicenceApplications.Models.AdminOfficerReview;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services;

public class GetAdminOfficerReviewServiceTests
{
    private Mock<IFellingLicenceApplicationInternalRepository> _mockRepository = new();

    [Theory, AutoMoqData]
    public async Task WhenNoAdminOfficerReviewFoundInRepository(Guid applicationId, bool isAgentApplication)
    {
        var sut = CreateSut();

        _mockRepository
            .Setup(x => x.GetAdminOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<Entities.AdminOfficerReview>.None);

        var result = await sut.GetAdminOfficerReviewStatusAsync(applicationId, isAgentApplication, true, false, false, CancellationToken.None);

        AssertIsDefault(result, isAgentApplication);

        _mockRepository.Verify(x => x.GetAdminOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(false, null, null, InternalReviewStepStatus.Completed)]
    [InlineData(false, false, null, InternalReviewStepStatus.Completed)]
    [InlineData(false, true, false, InternalReviewStepStatus.Completed)]
    [InlineData(true, null, null, InternalReviewStepStatus.NotStarted)]
    [InlineData(true, false, null, InternalReviewStepStatus.InProgress)]
    [InlineData(true, true, false, InternalReviewStepStatus.Failed)]
    [InlineData(true, true, true, InternalReviewStepStatus.Completed)]
    public async Task WhenAdminOfficerReviewFoundInRepository_ReturnsExpectedAgentAuthorityStatus(
        bool isAgencyApplication,
        bool? agentAuthorityComplete,
        bool? agentAuthorityPassed,
        InternalReviewStepStatus expectedAgentAuthorityTaskListState)
    {
        var applicationId = Guid.NewGuid();
        var review = new Entities.AdminOfficerReview
        {
            AgentAuthorityFormChecked = agentAuthorityComplete,
            AgentAuthorityCheckPassed = agentAuthorityPassed
        };

        var sut = CreateSut();

        _mockRepository
            .Setup(x => x.GetAdminOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(review.AsMaybe);

        var result = await sut.GetAdminOfficerReviewStatusAsync(applicationId, isAgencyApplication, true, false, false, CancellationToken.None);
        
        Assert.Equal(expectedAgentAuthorityTaskListState, result.AdminOfficerReviewTaskListStates.AgentAuthorityFormStepStatus);

        _mockRepository.Verify(x => x.GetAdminOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(null, null, InternalReviewStepStatus.NotStarted)]
    [InlineData(false, null, InternalReviewStepStatus.InProgress)]
    [InlineData(true, false, InternalReviewStepStatus.Failed)]
    [InlineData(true, true, InternalReviewStepStatus.Completed)]
    public async Task WhenAdminOfficerReviewFoundInRepository_ReturnsExpectedMappingStatus(
        bool? mappingCheckComplete,
        bool? mappingCheckPassed,
        InternalReviewStepStatus expectedMappingTaskListState)
    {
        var applicationId = Guid.NewGuid();
        var review = new Entities.AdminOfficerReview
        {
            MappingChecked = mappingCheckComplete,
            MappingCheckPassed = mappingCheckPassed
        };

        var sut = CreateSut();

        _mockRepository
            .Setup(x => x.GetAdminOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(review.AsMaybe);

        var result = await sut.GetAdminOfficerReviewStatusAsync(applicationId, It.IsAny<bool>(), true, false, false, CancellationToken.None);

        Assert.Equal(expectedMappingTaskListState, result.AdminOfficerReviewTaskListStates.MappingCheckStepStatus);

        _mockRepository.Verify(x => x.GetAdminOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(false, InternalReviewStepStatus.NotStarted)]
    [InlineData(true, InternalReviewStepStatus.Completed)]
    public async Task WhenWoodlandOfficerAssigned_ReturnsRelevantStatus(
        bool isAssigned,
        InternalReviewStepStatus expectedMappingTaskListState)
    {
        var applicationId = Guid.NewGuid();
        var review = new Entities.AdminOfficerReview
        {
            MappingChecked = true,
            MappingCheckPassed = true
        };

        var sut = CreateSut();

        _mockRepository
            .Setup(x => x.GetAdminOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(review.AsMaybe);

        var result = await sut.GetAdminOfficerReviewStatusAsync(applicationId, It.IsAny<bool>(), true, isAssigned, false, CancellationToken.None);

        _mockRepository.Verify(x => x.GetAdminOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(expectedMappingTaskListState, result.AdminOfficerReviewTaskListStates.AssignWoodlandOfficerStatus);
    }

    [Fact]
    public async Task WhenAoReviewNotStartedAtAll_ConstraintsCheckCannotBeStarted()
    {
        var applicationId = Guid.NewGuid();

        var sut = CreateSut();

        _mockRepository
            .Setup(x => x.GetAdminOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<Entities.AdminOfficerReview>.None);

        var result = await sut.GetAdminOfficerReviewStatusAsync(applicationId, true, true, false, false, CancellationToken.None);

        Assert.Equal(InternalReviewStepStatus.CannotStartYet, result.AdminOfficerReviewTaskListStates.ConstraintsCheckStepStatus);

        _mockRepository.Verify(x => x.GetAdminOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, CombinatorialData]
    public async Task WhenAgentApplicationAndMappingOrAafCheckNotComplete_ConstraintsCheckCannotBeStarted(bool mappingComplete, bool aafComplete)
    {
        if (mappingComplete && aafComplete)
        {
            return;
        }

        var applicationId = Guid.NewGuid();
        var review = new Entities.AdminOfficerReview
        {
            MappingCheckPassed = mappingComplete,
            AgentAuthorityCheckPassed = aafComplete,
        };

        var sut = CreateSut();

        _mockRepository
            .Setup(x => x.GetAdminOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(review.AsMaybe);

        var result = await sut.GetAdminOfficerReviewStatusAsync(applicationId, true, true, false, false, CancellationToken.None);

        Assert.Equal(InternalReviewStepStatus.CannotStartYet, result.AdminOfficerReviewTaskListStates.ConstraintsCheckStepStatus);

        _mockRepository.Verify(x => x.GetAdminOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, CombinatorialData]
    public async Task WhenWoodlandOwnerApplicationAndMappingNotComplete_ConstraintsCheckCannotBeStarted(bool aafComplete)
    {
        var applicationId = Guid.NewGuid();
        var review = new Entities.AdminOfficerReview
        {
            MappingCheckPassed = false,
            AgentAuthorityCheckPassed = aafComplete,
        };

        var sut = CreateSut();

        _mockRepository
            .Setup(x => x.GetAdminOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(review.AsMaybe);

        var result = await sut.GetAdminOfficerReviewStatusAsync(applicationId, false, true, false, false, CancellationToken.None);

        Assert.Equal(InternalReviewStepStatus.CannotStartYet, result.AdminOfficerReviewTaskListStates.ConstraintsCheckStepStatus);

        _mockRepository.Verify(x => x.GetAdminOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    private GetAdminOfficerReviewService CreateSut()
    {
        _mockRepository.Reset();

        return new GetAdminOfficerReviewService(_mockRepository.Object, new NullLogger<GetAdminOfficerReviewService>());
    }

    private void AssertIsDefault(AdminOfficerReviewStatusModel value, bool isAgentApplication)
    {
        Assert.Equal(isAgentApplication, value.AdminOfficerReviewTaskListStates.AgentApplication);
        Assert.Equal(isAgentApplication ? InternalReviewStepStatus.NotStarted : InternalReviewStepStatus.Completed, value.AdminOfficerReviewTaskListStates.AgentAuthorityFormStepStatus);
        Assert.Equal(InternalReviewStepStatus.NotStarted, value.AdminOfficerReviewTaskListStates.MappingCheckStepStatus);
        Assert.Equal(InternalReviewStepStatus.CannotStartYet, value.AdminOfficerReviewTaskListStates.ConstraintsCheckStepStatus);
    }
}