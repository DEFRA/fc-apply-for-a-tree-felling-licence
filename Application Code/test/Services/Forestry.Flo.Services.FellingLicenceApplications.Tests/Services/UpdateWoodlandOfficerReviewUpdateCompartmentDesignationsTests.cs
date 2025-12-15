using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;
using Forestry.Flo.Tests.Common;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services;

public class UpdateWoodlandOfficerReviewUpdateCompartmentDesignationsTests : UpdateWoodlandOfficerReviewServiceTestsBase
{
    [Theory, AutoMoqData]
    public async Task ReturnsFailureWhenRepositoryThrows(
        Guid applicationId,
        Guid userId,
        SubmittedCompartmentDesignationsModel designations)
    {
        var sut = CreateSut();

        FellingLicenceApplicationRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("done gone wrong"));

        var result = await sut.UpdateCompartmentDesignationsAsync(applicationId, userId, designations, CancellationToken.None);

        Assert.True(result.IsFailure);

        FellingLicenceApplicationRepository.Verify(x => x.GetStatusHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.VerifyNoOtherCalls();
        UnitOfWork.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task ReturnsFailureWhenNotInCorrectState(
        Guid applicationId,
        Guid userId,
        SubmittedCompartmentDesignationsModel designations)
    {
        var sut = CreateSut();

        FellingLicenceApplicationRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StatusHistory>(0));

        var result = await sut.UpdateCompartmentDesignationsAsync(applicationId, userId, designations, CancellationToken.None);

        Assert.True(result.IsFailure);

        FellingLicenceApplicationRepository.Verify(x => x.GetStatusHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.VerifyNoOtherCalls();
        UnitOfWork.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task ReturnsFailureWhenNoAssignedWoForApplication(
        Guid applicationId,
        Guid userId,
        SubmittedCompartmentDesignationsModel designations)
    {
        var sut = CreateSut();

        FellingLicenceApplicationRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StatusHistory> { new StatusHistory { Created = DateTime.UtcNow, Status = FellingLicenceStatus.WoodlandOfficerReview } });
        FellingLicenceApplicationRepository
            .Setup(x => x.GetAssigneeHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssigneeHistory>(0));

        var result = await sut.UpdateCompartmentDesignationsAsync(applicationId, userId, designations, CancellationToken.None);

        Assert.True(result.IsFailure);

        FellingLicenceApplicationRepository.Verify(x => x.GetStatusHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetAssigneeHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.VerifyNoOtherCalls();
        UnitOfWork.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task ReturnsFailureWhenDifferentAssignedWoForApplication(
        Guid applicationId,
        Guid userId,
        SubmittedCompartmentDesignationsModel designations)
    {
        var sut = CreateSut();

        FellingLicenceApplicationRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StatusHistory> { new StatusHistory { Created = DateTime.UtcNow, Status = FellingLicenceStatus.WoodlandOfficerReview } });
        FellingLicenceApplicationRepository
            .Setup(x => x.GetAssigneeHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssigneeHistory> { new AssigneeHistory { Role = AssignedUserRole.WoodlandOfficer, AssignedUserId = Guid.NewGuid() } });

        var result = await sut.UpdateCompartmentDesignationsAsync(applicationId, userId, designations, CancellationToken.None);

        Assert.True(result.IsFailure);

        FellingLicenceApplicationRepository.Verify(x => x.GetStatusHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetAssigneeHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.VerifyNoOtherCalls();
        UnitOfWork.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task ReturnsFailureWhenUnableToLoadCompartments(
        Guid applicationId,
        Guid userId,
        SubmittedCompartmentDesignationsModel designations,
        WoodlandOfficerReview woodlandOfficerReview)
    {
        var sut = CreateSut();

        FellingLicenceApplicationRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StatusHistory> { new StatusHistory { Created = DateTime.UtcNow, Status = FellingLicenceStatus.WoodlandOfficerReview } });
        FellingLicenceApplicationRepository
            .Setup(x => x.GetAssigneeHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssigneeHistory> { new AssigneeHistory { Role = AssignedUserRole.WoodlandOfficer, AssignedUserId = userId } });
        FellingLicenceApplicationRepository
            .Setup(x => x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(woodlandOfficerReview));
        FellingLicenceApplicationRepository
            .Setup(x => x.GetSubmittedFlaPropertyCompartmentsByApplicationIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<List<SubmittedFlaPropertyCompartment>>("error"));

        var result = await sut.UpdateCompartmentDesignationsAsync(applicationId, userId, designations, CancellationToken.None);

        Assert.True(result.IsFailure);

        FellingLicenceApplicationRepository.Verify(x => x.GetStatusHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetAssigneeHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetSubmittedFlaPropertyCompartmentsByApplicationIdAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.VerifyNoOtherCalls();
        UnitOfWork.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task ReturnsFailureWhenModelCompartmentIdNotFoundAmongstApplicationCompartments(
        Guid applicationId,
        Guid userId,
        SubmittedCompartmentDesignationsModel designations,
        WoodlandOfficerReview woodlandOfficerReview,
        List<SubmittedFlaPropertyCompartment> compartments)
    {
        var sut = CreateSut();

        FellingLicenceApplicationRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StatusHistory> { new StatusHistory { Created = DateTime.UtcNow, Status = FellingLicenceStatus.WoodlandOfficerReview } });
        FellingLicenceApplicationRepository
            .Setup(x => x.GetAssigneeHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssigneeHistory> { new AssigneeHistory { Role = AssignedUserRole.WoodlandOfficer, AssignedUserId = userId } });
        FellingLicenceApplicationRepository
            .Setup(x => x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(woodlandOfficerReview));
        FellingLicenceApplicationRepository
            .Setup(x => x.GetSubmittedFlaPropertyCompartmentsByApplicationIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(compartments));

        var result = await sut.UpdateCompartmentDesignationsAsync(applicationId, userId, designations, CancellationToken.None);

        Assert.True(result.IsFailure);

        FellingLicenceApplicationRepository.Verify(x => x.GetStatusHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetAssigneeHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetSubmittedFlaPropertyCompartmentsByApplicationIdAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.VerifyNoOtherCalls();
        UnitOfWork.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenNoExistingWoReviewAndNoExistingDesignations(
        Guid applicationId,
        Guid userId,
        SubmittedCompartmentDesignationsModel designations,
        List<SubmittedFlaPropertyCompartment> compartments)
    {
        var sut = CreateSut();

        designations.SubmittedFlaCompartmentId = compartments[0].Id;
        compartments[0].SubmittedCompartmentDesignations = null;

        FellingLicenceApplicationRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StatusHistory> { new StatusHistory { Created = DateTime.UtcNow, Status = FellingLicenceStatus.WoodlandOfficerReview } });
        FellingLicenceApplicationRepository
            .Setup(x => x.GetAssigneeHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssigneeHistory> { new AssigneeHistory { Role = AssignedUserRole.WoodlandOfficer, AssignedUserId = userId } });
        FellingLicenceApplicationRepository
            .Setup(x => x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.None);
        FellingLicenceApplicationRepository
            .Setup(x => x.GetSubmittedFlaPropertyCompartmentsByApplicationIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(compartments));

        var result = await sut.UpdateCompartmentDesignationsAsync(applicationId, userId, designations, CancellationToken.None);

        Assert.True(result.IsSuccess);

        FellingLicenceApplicationRepository.Verify(x => x.GetStatusHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetAssigneeHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.AddWoodlandOfficerReviewAsync(It.IsAny<WoodlandOfficerReview>(), It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetSubmittedFlaPropertyCompartmentsByApplicationIdAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);

        UnitOfWork.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);

        FellingLicenceApplicationRepository.VerifyNoOtherCalls();
        UnitOfWork.VerifyNoOtherCalls();

        Assert.NotNull(compartments[0].SubmittedCompartmentDesignations);
        Assert.Equal(compartments[0].Id, compartments[0].SubmittedCompartmentDesignations.SubmittedFlaPropertyCompartmentId);
        Assert.Equal(designations.Sssi, compartments[0].SubmittedCompartmentDesignations.Sssi);
        Assert.Equal(designations.Sacs, compartments[0].SubmittedCompartmentDesignations.Sacs);
        Assert.Equal(designations.Spa, compartments[0].SubmittedCompartmentDesignations.Spa);
        Assert.Equal(designations.Ramsar, compartments[0].SubmittedCompartmentDesignations.Ramsar);
        Assert.Equal(designations.Sbi, compartments[0].SubmittedCompartmentDesignations.Sbi);
        Assert.Equal(designations.Other, compartments[0].SubmittedCompartmentDesignations.Other);
        if (designations.Other)
        {
            Assert.Equal(designations.OtherDesignationDetails, compartments[0].SubmittedCompartmentDesignations.OtherDesignationDetails);
        }
        else
        {
            Assert.Null(compartments[0].SubmittedCompartmentDesignations.OtherDesignationDetails);
        }
        Assert.Equal(designations.None, compartments[0].SubmittedCompartmentDesignations.None);
        Assert.Equal(designations.Paws, compartments[0].SubmittedCompartmentDesignations.Paws);
        if (designations.Paws)
        {
            Assert.Equal(designations.ProportionBeforeFelling, compartments[0].SubmittedCompartmentDesignations.ProportionBeforeFelling);
            Assert.Equal(designations.ProportionAfterFelling, compartments[0].SubmittedCompartmentDesignations.ProportionAfterFelling);
        }
        else
        {
            Assert.Null(compartments[0].SubmittedCompartmentDesignations.ProportionBeforeFelling);
            Assert.Null(compartments[0].SubmittedCompartmentDesignations.ProportionAfterFelling);
        }
        Assert.True(compartments[0].SubmittedCompartmentDesignations.HasBeenReviewed);

    }

    [Theory, AutoMoqData]
    public async Task WhenUpdatingExistingData(
        Guid applicationId,
        Guid userId,
        SubmittedCompartmentDesignationsModel designations,
        List<SubmittedFlaPropertyCompartment> compartments,
        WoodlandOfficerReview woodlandOfficerReview)
    {
        var sut = CreateSut();

        designations.SubmittedFlaCompartmentId = compartments[0].Id;

        FellingLicenceApplicationRepository
            .Setup(x => x.GetStatusHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StatusHistory> { new StatusHistory { Created = DateTime.UtcNow, Status = FellingLicenceStatus.WoodlandOfficerReview } });
        FellingLicenceApplicationRepository
            .Setup(x => x.GetAssigneeHistoryForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssigneeHistory> { new AssigneeHistory { Role = AssignedUserRole.WoodlandOfficer, AssignedUserId = userId } });
        FellingLicenceApplicationRepository
            .Setup(x => x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(woodlandOfficerReview));
        FellingLicenceApplicationRepository
            .Setup(x => x.GetSubmittedFlaPropertyCompartmentsByApplicationIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(compartments));

        var result = await sut.UpdateCompartmentDesignationsAsync(applicationId, userId, designations, CancellationToken.None);

        Assert.True(result.IsSuccess);

        FellingLicenceApplicationRepository.Verify(x => x.GetStatusHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetAssigneeHistoryForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        FellingLicenceApplicationRepository.Verify(x => x.GetSubmittedFlaPropertyCompartmentsByApplicationIdAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);

        UnitOfWork.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);

        FellingLicenceApplicationRepository.VerifyNoOtherCalls();
        UnitOfWork.VerifyNoOtherCalls();

        Assert.NotNull(compartments[0].SubmittedCompartmentDesignations);
        Assert.Equal(compartments[0].Id, compartments[0].SubmittedCompartmentDesignations.SubmittedFlaPropertyCompartmentId);
        Assert.Equal(designations.Sssi, compartments[0].SubmittedCompartmentDesignations.Sssi);
        Assert.Equal(designations.Sacs, compartments[0].SubmittedCompartmentDesignations.Sacs);
        Assert.Equal(designations.Spa, compartments[0].SubmittedCompartmentDesignations.Spa);
        Assert.Equal(designations.Ramsar, compartments[0].SubmittedCompartmentDesignations.Ramsar);
        Assert.Equal(designations.Sbi, compartments[0].SubmittedCompartmentDesignations.Sbi);
        Assert.Equal(designations.Other, compartments[0].SubmittedCompartmentDesignations.Other);
        if (designations.Other)
        {
            Assert.Equal(designations.OtherDesignationDetails, compartments[0].SubmittedCompartmentDesignations.OtherDesignationDetails);
        }
        else
        {
            Assert.Null(compartments[0].SubmittedCompartmentDesignations.OtherDesignationDetails);
        }
        Assert.Equal(designations.None, compartments[0].SubmittedCompartmentDesignations.None);

        if (designations.Paws)
        {
            Assert.Equal(designations.ProportionBeforeFelling, compartments[0].SubmittedCompartmentDesignations.ProportionBeforeFelling);
            Assert.Equal(designations.ProportionAfterFelling, compartments[0].SubmittedCompartmentDesignations.ProportionAfterFelling);
        }
        else
        {
            Assert.Null(compartments[0].SubmittedCompartmentDesignations.ProportionBeforeFelling);
            Assert.Null(compartments[0].SubmittedCompartmentDesignations.ProportionAfterFelling);
        }
        Assert.True(compartments[0].SubmittedCompartmentDesignations.HasBeenReviewed);

    }
}
