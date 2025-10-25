using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Tests.Common;
using Moq;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services;

public partial class GetFellingLicenceApplicationForInternalUsersServiceTests
{
    private readonly Mock<IFellingLicenceApplicationInternalRepository> _fellingLicenceApplicationRepository = new();
    private readonly Mock<IClock> _clock = new();

    private DateTime _currentTime;

    [Theory, AutoMoqData]
    public async Task GetApplicationAssignedUsers_WhenApplicationNotFoundInRepoToCheckAccess(Guid applicationId, UserAccessModel userAccessModel)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository
            .Setup(x => x.CheckUserCanAccessApplicationAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<bool>("error"));

        var result =
            await sut.GetApplicationAssignedUsers(applicationId, userAccessModel, CancellationToken.None);

        Assert.True(result.IsFailure);

        _fellingLicenceApplicationRepository
            .Verify(x => x.CheckUserCanAccessApplicationAsync(applicationId, userAccessModel, It.IsAny<CancellationToken>()),
                Times.Once);

        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task GetApplicationAssignedUsers_WhenUserDoesNotHaveAccess(Guid applicationId, UserAccessModel userAccessModel)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository
            .Setup(x => x.CheckUserCanAccessApplicationAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(false));

        var result =
            await sut.GetApplicationAssignedUsers(applicationId, userAccessModel, CancellationToken.None);

        Assert.True(result.IsFailure);

        _fellingLicenceApplicationRepository
            .Verify(x => x.CheckUserCanAccessApplicationAsync(applicationId, userAccessModel, It.IsAny<CancellationToken>()),
                Times.Once);

        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task GetApplicationAssignedUsers_WithNoRetrievedUsers(Guid applicationId, UserAccessModel userAccessModel)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository
            .Setup(x => x.CheckUserCanAccessApplicationAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));
        _fellingLicenceApplicationRepository
            .Setup(x => x.GetAssigneeHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<AssigneeHistory>());

        var result =
            await sut.GetApplicationAssignedUsers(applicationId, userAccessModel, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);

        _fellingLicenceApplicationRepository
            .Verify(x => x.CheckUserCanAccessApplicationAsync(applicationId, userAccessModel, It.IsAny<CancellationToken>()),
                Times.Once);
        _fellingLicenceApplicationRepository
            .Verify(x => x.GetAssigneeHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()),
                Times.Once);

        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task GetApplicationAssignedUsers_WithRetrievedUsers(Guid applicationId, UserAccessModel userAccessModel, List<AssigneeHistory> users)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository
            .Setup(x => x.CheckUserCanAccessApplicationAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));
        _fellingLicenceApplicationRepository
            .Setup(x => x.GetAssigneeHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

        var result =
            await sut.GetApplicationAssignedUsers(applicationId, userAccessModel, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(users.Count, result.Value.Count);

        users.ForEach(e => Assert.Contains(result.Value, a => a.Id == e.Id
            && a.ApplicationId == e.FellingLicenceApplicationId
            && a.AssignedUserId == e.AssignedUserId
            && a.TimestampAssigned == e.TimestampAssigned
            && a.TimestampUnassigned == e.TimestampUnassigned
            && a.Role == e.Role));

        _fellingLicenceApplicationRepository
            .Verify(x => x.CheckUserCanAccessApplicationAsync(applicationId, userAccessModel, It.IsAny<CancellationToken>()),
                Times.Once);
        _fellingLicenceApplicationRepository
            .Verify(x => x.GetAssigneeHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()),
                Times.Once);

        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task GetApplicationStatusHistory_WhenApplicationNotFoundInRepoToCheckAccess(Guid applicationId, UserAccessModel userAccessModel)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository
            .Setup(x => x.CheckUserCanAccessApplicationAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<bool>("error"));

        var result =
            await sut.GetApplicationStatusHistory(applicationId, userAccessModel, CancellationToken.None);

        Assert.True(result.IsFailure);

        _fellingLicenceApplicationRepository
            .Verify(x => x.CheckUserCanAccessApplicationAsync(applicationId, userAccessModel, It.IsAny<CancellationToken>()),
                Times.Once);

        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task GetApplicationStatusHistory_WhenUserDoesNotHaveAccess(Guid applicationId, UserAccessModel userAccessModel)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository
            .Setup(x => x.CheckUserCanAccessApplicationAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(false));

        var result =
            await sut.GetApplicationStatusHistory(applicationId, userAccessModel, CancellationToken.None);

        Assert.True(result.IsFailure);

        _fellingLicenceApplicationRepository
            .Verify(x => x.CheckUserCanAccessApplicationAsync(applicationId, userAccessModel, It.IsAny<CancellationToken>()),
                Times.Once);

        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task GetApplicationStatusHistory_WithNoStatusHistory(Guid applicationId, UserAccessModel userAccessModel)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository
            .Setup(x => x.CheckUserCanAccessApplicationAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));
        _fellingLicenceApplicationRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<StatusHistory>());

        var result =
            await sut.GetApplicationStatusHistory(applicationId, userAccessModel, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);

        _fellingLicenceApplicationRepository
            .Verify(x => x.CheckUserCanAccessApplicationAsync(applicationId, userAccessModel, It.IsAny<CancellationToken>()),
                Times.Once);
        _fellingLicenceApplicationRepository
            .Verify(x => x.GetStatusHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()),
                Times.Once);

        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task GetApplicationStatusHistory_WithRetrievedHistory(Guid applicationId, UserAccessModel userAccessModel, List<StatusHistory> statuses)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository
            .Setup(x => x.CheckUserCanAccessApplicationAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));
        _fellingLicenceApplicationRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(statuses);

        var result =
            await sut.GetApplicationStatusHistory(applicationId, userAccessModel, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(statuses.Count, result.Value.Count);

        statuses.ForEach(e => Assert.Contains(result.Value, a => a.Created == e.Created
                                                              && a.Status == e.Status
                                                              && a.CreatedById == e.CreatedById));

        _fellingLicenceApplicationRepository
            .Verify(x => x.CheckUserCanAccessApplicationAsync(applicationId, userAccessModel, It.IsAny<CancellationToken>()),
                Times.Once);
        _fellingLicenceApplicationRepository
            .Verify(x => x.GetStatusHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()),
                Times.Once);

        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task ReturnsListOfPublicRegisterPeriodEndModels_ForDecisionPublicRegister(
        List<FellingLicenceApplication> fellingLicenceApplications)
    {
        var sut = CreateSut();

        foreach (var assigneeHistory in fellingLicenceApplications.SelectMany(application => application.AssigneeHistories))
        {
            assigneeHistory.TimestampUnassigned = null;
            assigneeHistory.Role = AssignedUserRole.FieldManager;
        }
        
        _fellingLicenceApplicationRepository.Setup(s =>
                s.GetFinalisedApplicationsWithExpiredDecisionPublicRegisterPeriodsAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(fellingLicenceApplications);

        var result = await sut.RetrieveFinalisedApplicationsHavingExpiredOnTheDecisionPublicRegisterAsync(CancellationToken.None);

        // verify all applications have been retrieved

        Assert.Equal(fellingLicenceApplications.Count, result.Count);

        foreach (var application in fellingLicenceApplications)
        {
            var periodEndModel = result.FirstOrDefault(x => x.ApplicationReference == application.ApplicationReference);
            Assert.NotNull(periodEndModel);
            Assert.Equal(application.PublicRegister, periodEndModel!.PublicRegister);
            Assert.Equal(application.AssigneeHistories.Count(x => x.Role == AssignedUserRole.FieldManager),
                periodEndModel.AssignedUserIds!.Count);
        }

        _fellingLicenceApplicationRepository.Verify(v => v.GetFinalisedApplicationsWithExpiredDecisionPublicRegisterPeriodsAsync(_currentTime,CancellationToken.None), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ReturnsListOfPublicRegisterPeriodEndModels_ForConsultationPublicRegister(
        List<FellingLicenceApplication> fellingLicenceApplications)
    {
        var sut = CreateSut();

        foreach (var assigneeHistory in fellingLicenceApplications.SelectMany(application => application.AssigneeHistories))
        {
            assigneeHistory.TimestampUnassigned = null;
            assigneeHistory.Role = AssignedUserRole.FieldManager;
        }

        _fellingLicenceApplicationRepository.Setup(s =>
                s.GetApplicationsWithExpiredConsultationPublicRegisterPeriodsAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(fellingLicenceApplications);

        var result = await sut.RetrieveApplicationsHavingExpiredOnTheConsultationPublicRegisterAsync(CancellationToken.None);

        // verify all applications have been retrieved

        Assert.Equal(fellingLicenceApplications.Count, result.Count);

        foreach (var application in fellingLicenceApplications)
        {
            var periodEndModel = result.FirstOrDefault(x => x.ApplicationReference == application.ApplicationReference);
            Assert.NotNull(periodEndModel);
            Assert.Equal(application.PublicRegister, periodEndModel!.PublicRegister);
            Assert.Equal(application.AssigneeHistories.Count(x => x.Role == AssignedUserRole.FieldManager), periodEndModel.AssignedUserIds!.Count);
        }

        _fellingLicenceApplicationRepository.Verify(v => v.GetApplicationsWithExpiredConsultationPublicRegisterPeriodsAsync(_currentTime, CancellationToken.None), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ReturnsListOfPublicRegisterPeriodEndModelsWillFindApproversOnly_ForDecisionPublicRegister(
        List<FellingLicenceApplication> fellingLicenceApplications)
    {
        var invalidRoles = new List<AssignedUserRole>
        {
            AssignedUserRole.Applicant,
            AssignedUserRole.AdminOfficer,
            AssignedUserRole.Author,
            AssignedUserRole.WoodlandOfficer
        };

        foreach (var assignedUserRole in invalidRoles)
        {
            var sut = CreateSut();

            foreach (var assigneeHistory in fellingLicenceApplications.SelectMany(application => application.AssigneeHistories))
            {
                assigneeHistory.TimestampUnassigned = null;
                assigneeHistory.Role = assignedUserRole;
            }
            
            _fellingLicenceApplicationRepository.Setup(s =>
                    s.GetFinalisedApplicationsWithExpiredDecisionPublicRegisterPeriodsAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(fellingLicenceApplications);

            var result = await sut.RetrieveFinalisedApplicationsHavingExpiredOnTheDecisionPublicRegisterAsync(CancellationToken.None);

            // verify all applications have been retrieved

            Assert.Equal(fellingLicenceApplications.Count, result.Count);

            foreach (var application in fellingLicenceApplications)
            {
                var periodEndModel = result.FirstOrDefault(x => x.ApplicationReference == application.ApplicationReference);
                Assert.NotNull(periodEndModel);
                Assert.Equal(application.PublicRegister, periodEndModel!.PublicRegister);
                Assert.Empty(periodEndModel.AssignedUserIds!);
            }

            _fellingLicenceApplicationRepository.Verify(v => v.GetFinalisedApplicationsWithExpiredDecisionPublicRegisterPeriodsAsync(_currentTime, CancellationToken.None), Times.Once);
        }
    }

    [Fact]
    public async Task ReturnsEmptyListOfPublicRegisterPeriodEndModels_ForDecisionPublicRegister()
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository.Setup(s =>
                s.GetFinalisedApplicationsWithExpiredDecisionPublicRegisterPeriodsAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FellingLicenceApplication>());

        var result = await sut.RetrieveFinalisedApplicationsHavingExpiredOnTheDecisionPublicRegisterAsync(CancellationToken.None);

        Assert.Empty(result);

        _fellingLicenceApplicationRepository.Verify(v => v.GetFinalisedApplicationsWithExpiredDecisionPublicRegisterPeriodsAsync(_currentTime, CancellationToken.None), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task RetrievePublicRegisterForRemoval_WhenApplicationHasNone(
        FellingLicenceApplication fellingLicenceApplication)
    {
        var sut = CreateSut();

        fellingLicenceApplication.PublicRegister = null;

        _fellingLicenceApplicationRepository.Setup(s =>
                s.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(fellingLicenceApplication);

        var result = await sut.RetrievePublicRegisterForRemoval(fellingLicenceApplication.Id, CancellationToken.None);

        // verify all applications have been retrieved

        Assert.False(result.HasValue);

    }

    [Theory, AutoMoqData]
    public async Task RetrievePublicRegisterForRemoval_WhenApplicationHasOne(
        FellingLicenceApplication fellingLicenceApplication)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository.Setup(s =>
                s.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(fellingLicenceApplication);

        var result = await sut.RetrievePublicRegisterForRemoval(fellingLicenceApplication.Id, CancellationToken.None);

        // verify all applications have been retrieved

        Assert.True(result.HasValue);

        Assert.Equal(fellingLicenceApplication.PublicRegister, result.Value.PublicRegister);
        Assert.Equal(fellingLicenceApplication.ApplicationReference, result.Value.ApplicationReference);
        Assert.Equal(fellingLicenceApplication.SubmittedFlaPropertyDetail?.Name, result.Value.PropertyName);
        Assert.Equal(fellingLicenceApplication.AdministrativeRegion, result.Value.AdminHubName);
        Assert.Equivalent(fellingLicenceApplication.AssigneeHistories
            .Where(x => x.TimestampUnassigned is null).Select(y => y.AssignedUserId).ToList(), result.Value.AssignedUserIds);
    }

    [Theory, AutoMoqData]
    public async Task GetSubmittedFlaPropertyCompartmentsByApplicationIdTest(
        Guid applicationId,
        List<SubmittedFlaPropertyCompartment> compartments)
    {
        var sut = CreateSut();
        var expectedResult = Result.Success(compartments);

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetSubmittedFlaPropertyCompartmentsByApplicationIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var result = await sut.GetSubmittedFlaPropertyCompartmentsByApplicationIdAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(expectedResult.Value, result.Value);

        _fellingLicenceApplicationRepository
            .Verify(x => x.GetSubmittedFlaPropertyCompartmentsByApplicationIdAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task GetEnvironmentalImpactAssessmentTest(
        Guid applicationId,
        EnvironmentalImpactAssessment entity)
    {
        var sut = CreateSut();
        var expectedResult = Result.Success(entity);

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetEnvironmentalImpactAssessmentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var result = await sut.GetEnvironmentalImpactAssessmentAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(expectedResult.Value, result.Value);

        _fellingLicenceApplicationRepository
            .Verify(x => x.GetEnvironmentalImpactAssessmentAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
    }

    private GetFellingLicenceApplicationForInternalUsersService CreateSut()
    {
        _fellingLicenceApplicationRepository.Reset();
        _currentTime = DateTime.UtcNow;

        _clock.Setup(x => x.GetCurrentInstant()).Returns(Instant.FromDateTimeUtc(_currentTime));

        return new GetFellingLicenceApplicationForInternalUsersService(
            _fellingLicenceApplicationRepository.Object,
            _clock.Object);
    }
}