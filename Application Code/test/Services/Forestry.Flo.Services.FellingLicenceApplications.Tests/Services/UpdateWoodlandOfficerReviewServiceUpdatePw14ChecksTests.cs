using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;
using Moq;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services;

public class UpdateWoodlandOfficerReviewServiceUpdatePw14ChecksTests : UpdateWoodlandOfficerReviewServiceTestsBase
{
    [Theory, AutoData]
    public async Task ReturnsFailureWhenRepositoryThrows(
        Guid applicationId,
        Guid userId,
        Pw14ChecksModel model)
    {
        var sut = CreateSut();

        FellingLicenceApplicationRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("done gone wrong"));

        var result = await sut.UpdatePw14ChecksAsync(applicationId, model, userId, CancellationToken.None);

        Assert.True(result.IsFailure);

        FellingLicenceApplicationRepository.Verify(x => x.GetStatusHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.VerifyNoOtherCalls();
        UnitOfWork.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task ReturnsFailureWhenNotInCorrectState(
        Guid applicationId,
        Guid userId,
        Pw14ChecksModel model)
    {
        var sut = CreateSut();

        FellingLicenceApplicationRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StatusHistory>(0));

        var result = await sut.UpdatePw14ChecksAsync(applicationId, model, userId, CancellationToken.None);

        Assert.True(result.IsFailure);

        FellingLicenceApplicationRepository.Verify(x => x.GetStatusHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.VerifyNoOtherCalls();
        UnitOfWork.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task ReturnsFailureWhenNoAssignedWoForApplication(
        Guid applicationId,
        Guid userId,
        Pw14ChecksModel model)
    {
        var sut = CreateSut();

        FellingLicenceApplicationRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StatusHistory>{new StatusHistory{Created = DateTime.UtcNow, Status = FellingLicenceStatus.WoodlandOfficerReview}});
        FellingLicenceApplicationRepository
            .Setup(x => x.GetAssigneeHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssigneeHistory>(0));

        var result = await sut.UpdatePw14ChecksAsync(applicationId, model, userId, CancellationToken.None);

        Assert.True(result.IsFailure);

        FellingLicenceApplicationRepository.Verify(x => x.GetStatusHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetAssigneeHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.VerifyNoOtherCalls();
        UnitOfWork.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task ReturnsFailureWhenDifferentAssignedWoForApplication(
        Guid applicationId,
        Guid userId,
        Pw14ChecksModel model)
    {
        var sut = CreateSut();

        FellingLicenceApplicationRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StatusHistory> { new StatusHistory { Created = DateTime.UtcNow, Status = FellingLicenceStatus.WoodlandOfficerReview } });
        FellingLicenceApplicationRepository
            .Setup(x => x.GetAssigneeHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssigneeHistory>{new AssigneeHistory{Role = AssignedUserRole.WoodlandOfficer, AssignedUserId = Guid.NewGuid()}});

        var result = await sut.UpdatePw14ChecksAsync(applicationId, model, userId, CancellationToken.None);

        Assert.True(result.IsFailure);

        FellingLicenceApplicationRepository.Verify(x => x.GetStatusHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetAssigneeHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.VerifyNoOtherCalls();
        UnitOfWork.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task CanSaveUpdateWhenNoWoodlandOfficerReviewEntityExists(
        Guid applicationId,
        Guid userId,
        Pw14ChecksModel model)
    {
        var sut = CreateSut();

        FellingLicenceApplicationRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StatusHistory> { new StatusHistory { Created = DateTime.UtcNow, Status = FellingLicenceStatus.WoodlandOfficerReview } });
        FellingLicenceApplicationRepository
            .Setup(x => x.GetAssigneeHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssigneeHistory> { new AssigneeHistory { Role = AssignedUserRole.WoodlandOfficer, AssignedUserId = userId } });
        FellingLicenceApplicationRepository.Setup(x => x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.None);

        var result = await sut.UpdatePw14ChecksAsync(applicationId, model, userId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        FellingLicenceApplicationRepository.Verify(x => x.GetStatusHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetAssigneeHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(
            x => x.AddWoodlandOfficerReviewAsync(It.Is<WoodlandOfficerReview>(x =>
                x.FellingLicenceApplicationId == applicationId
                && x.LastUpdatedById == userId
                && x.LastUpdatedDate == Now.ToDateTimeUtc()
                && x.LandInformationSearchChecked == model.LandInformationSearchChecked
                && x.AreProposalsUkfsCompliant == model.AreProposalsUkfsCompliant
                && x.TpoOrCaDeclared == model.TpoOrCaDeclared
                && x.IsApplicationValid == model.IsApplicationValid
                && x.EiaThresholdExceeded == model.EiaThresholdExceeded
                && x.EiaTrackerCompleted == model.EiaTrackerCompleted
                && x.EiaChecklistDone == model.EiaChecklistDone
                && x.LocalAuthorityConsulted == model.LocalAuthorityConsulted
                && x.InterestDeclared == model.InterestDeclared
                && x.InterestDeclarationCompleted == model.InterestDeclarationCompleted
                && x.ComplianceRecommendationsEnacted == model.ComplianceRecommendationsEnacted
                && x.MapAccuracyConfirmed == model.MapAccuracyConfirmed
                && x.EpsLicenceConsidered == model.EpsLicenceConsidered
                && x.Stage1HabitatRegulationsAssessmentRequired == model.Stage1HabitatRegulationsAssessmentRequired
                && x.Pw14ChecksComplete == model.Pw14ChecksComplete
            ), It.IsAny<CancellationToken>()), Times.Once);
        UnitOfWork.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);

        FellingLicenceApplicationRepository.VerifyNoOtherCalls();
        UnitOfWork.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task CanSaveUpdateWhenWoodlandOfficerReviewEntityExists(
        Guid applicationId,
        Guid userId,
        Pw14ChecksModel model)
    {
        var sut = CreateSut();

        var existingReview = new WoodlandOfficerReview
        {
            FellingLicenceApplicationId = applicationId
        };

        FellingLicenceApplicationRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StatusHistory> { new StatusHistory { Created = DateTime.UtcNow, Status = FellingLicenceStatus.WoodlandOfficerReview } });
        FellingLicenceApplicationRepository
            .Setup(x => x.GetAssigneeHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssigneeHistory> { new AssigneeHistory { Role = AssignedUserRole.WoodlandOfficer, AssignedUserId = userId } });
        FellingLicenceApplicationRepository.Setup(x => x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.From(existingReview));

        var result = await sut.UpdatePw14ChecksAsync(applicationId, model, userId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        FellingLicenceApplicationRepository.Verify(x => x.GetStatusHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetAssigneeHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        UnitOfWork.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);

        FellingLicenceApplicationRepository.VerifyNoOtherCalls();
        UnitOfWork.VerifyNoOtherCalls();

        Assert.Equal(model.LandInformationSearchChecked, existingReview.LandInformationSearchChecked);
        Assert.Equal(model.AreProposalsUkfsCompliant, existingReview.AreProposalsUkfsCompliant);
        Assert.Equal(model.TpoOrCaDeclared, existingReview.TpoOrCaDeclared);
        Assert.Equal(model.IsApplicationValid, existingReview.IsApplicationValid);
        Assert.Equal(model.EiaThresholdExceeded, existingReview.EiaThresholdExceeded);
        Assert.Equal(model.EiaTrackerCompleted, existingReview.EiaTrackerCompleted);
        Assert.Equal(model.EiaChecklistDone, existingReview.EiaChecklistDone);
        Assert.Equal(model.LocalAuthorityConsulted, existingReview.LocalAuthorityConsulted);
        Assert.Equal(model.InterestDeclared, existingReview.InterestDeclared);
        Assert.Equal(model.InterestDeclarationCompleted, existingReview.InterestDeclarationCompleted);
        Assert.Equal(model.ComplianceRecommendationsEnacted, existingReview.ComplianceRecommendationsEnacted);
        Assert.Equal(model.MapAccuracyConfirmed, existingReview.MapAccuracyConfirmed);
        Assert.Equal(model.EpsLicenceConsidered, existingReview.EpsLicenceConsidered);
        Assert.Equal(model.Stage1HabitatRegulationsAssessmentRequired, existingReview.Stage1HabitatRegulationsAssessmentRequired);
        Assert.Equal(model.Pw14ChecksComplete, existingReview.Pw14ChecksComplete);
    }

    [Fact]
    public void Pw14ChecksComplete_ReturnsFalse_WhenAnyRequiredFieldIsNullOrFalse()
    {
        var model = new Pw14ChecksModel
        {
            LandInformationSearchChecked = true,
            AreProposalsUkfsCompliant = true,
            IsApplicationValid = true,
            TpoOrCaDeclared = false,
            InterestDeclared = false,
            EiaThresholdExceeded = false,
            MapAccuracyConfirmed = true,
            EpsLicenceConsidered = true,
            Stage1HabitatRegulationsAssessmentRequired = true
        };

        // All required fields are set for the "false" branches of the section checks
        Assert.True(model.Pw14ChecksComplete);

        // Now test each required field being null or false
        model.LandInformationSearchChecked = null;
        Assert.False(model.Pw14ChecksComplete);
        model.LandInformationSearchChecked = true;

        model.AreProposalsUkfsCompliant = null;
        Assert.False(model.Pw14ChecksComplete);
        model.AreProposalsUkfsCompliant = true;

        model.IsApplicationValid = null;
        Assert.False(model.Pw14ChecksComplete);
        model.IsApplicationValid = true;

        model.MapAccuracyConfirmed = null;
        Assert.False(model.Pw14ChecksComplete);
        model.MapAccuracyConfirmed = true;

        model.EpsLicenceConsidered = null;
        Assert.False(model.Pw14ChecksComplete);
        model.EpsLicenceConsidered = true;

        model.Stage1HabitatRegulationsAssessmentRequired = null;
        Assert.False(model.Pw14ChecksComplete);
        model.Stage1HabitatRegulationsAssessmentRequired = true;
    }

    [Fact]
    public void Pw14ChecksComplete_ReturnsFalse_WhenSectionChecksAreNotComplete()
    {
        var model = new Pw14ChecksModel
        {
            LandInformationSearchChecked = true,
            AreProposalsUkfsCompliant = true,
            IsApplicationValid = true,
            TpoOrCaDeclared = true,
            LocalAuthorityConsulted = null,
            InterestDeclared = true,
            InterestDeclarationCompleted = null,
            ComplianceRecommendationsEnacted = null,
            EiaThresholdExceeded = true,
            EiaTrackerCompleted = null,
            EiaChecklistDone = null,
            MapAccuracyConfirmed = true,
            EpsLicenceConsidered = true,
            Stage1HabitatRegulationsAssessmentRequired = true
        };

        Assert.False(model.Pw14ChecksComplete);

        // Complete TPO/CPA section
        model.LocalAuthorityConsulted = true;
        Assert.False(model.Pw14ChecksComplete);

        // Complete Interest Declaration section
        model.InterestDeclarationCompleted = true;
        model.ComplianceRecommendationsEnacted = true;
        Assert.False(model.Pw14ChecksComplete);

        // Complete EIA section
        model.EiaTrackerCompleted = true;
        model.EiaChecklistDone = true;
        Assert.True(model.Pw14ChecksComplete);
    }

    [Fact]
    public void Pw14ChecksComplete_ReturnsTrue_WhenAllSectionsAndFieldsAreValid()
    {
        var model = new Pw14ChecksModel
        {
            LandInformationSearchChecked = true,
            AreProposalsUkfsCompliant = true,
            IsApplicationValid = true,
            TpoOrCaDeclared = true,
            LocalAuthorityConsulted = true,
            InterestDeclared = true,
            InterestDeclarationCompleted = true,
            ComplianceRecommendationsEnacted = true,
            EiaThresholdExceeded = true,
            EiaTrackerCompleted = true,
            EiaChecklistDone = true,
            MapAccuracyConfirmed = true,
            EpsLicenceConsidered = true,
            Stage1HabitatRegulationsAssessmentRequired = false
        };

        Assert.True(model.Pw14ChecksComplete);
    }

    [Fact]
    public void Pw14ChecksComplete_ReturnsTrue_WhenAllSectionsAreIncorrect()
    {
        var model = new Pw14ChecksModel
        {
            LandInformationSearchChecked = false,
            AreProposalsUkfsCompliant = false,
            IsApplicationValid = false,
            TpoOrCaDeclared = true,
            LocalAuthorityConsulted = false,
            InterestDeclared = true,
            InterestDeclarationCompleted = false,
            ComplianceRecommendationsEnacted = false,
            EiaThresholdExceeded = true,
            EiaTrackerCompleted = false,
            EiaChecklistDone = false,
            MapAccuracyConfirmed = false,
            EpsLicenceConsidered = false,
            Stage1HabitatRegulationsAssessmentRequired = false
        };

        Assert.True(model.Pw14ChecksComplete);
    }
}