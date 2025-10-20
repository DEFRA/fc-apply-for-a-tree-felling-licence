using AutoFixture;
using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.WoodlandOfficerReview;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Forestry.Flo.Internal.Web.Tests.Services.WoodlandOfficerReviewUseCase;

public class DesignationsUseCaseGetApplicationDesignationsTests : WoodlandOfficerReviewUseCaseTestsBase<DesignationsUseCase>
{
    [Theory, AutoData]
    public async Task ShouldReturnFailureWhenUnableToLoadApplicationSummary(Guid applicationId)
    {
        var sut = CreateSut();

        FlaRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.None);

        var result = await sut.GetApplicationDesignationsAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsFailure);

        FlaRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        
        WoodlandOwnerService.VerifyNoOtherCalls();
        ExternalUserAccountRepository.VerifyNoOtherCalls();
        InternalUserAccountService.VerifyNoOtherCalls();
        NotificationHistoryService.VerifyNoOtherCalls();

        WoodlandOfficerReviewService.VerifyNoOtherCalls();
    }

    //It is assumed that the various other permutations of outcome for FellingLicenceApplicationUseCaseBase.GetFellingLicenceDetailsAsync are already tested - see AssignToUserUseCaseTestsBase

    [Theory, AutoMoqData]
    public async Task ShouldReturnFailureWhenUnableToLoadCompartmentDesignations(
        Guid applicationId,
        FellingLicenceApplication fla,
        WoodlandOwnerModel woodlandOwner,
        UserAccount externalApplicant,
        Flo.Services.InternalUsers.Entities.UserAccount.UserAccount internalUser)
    {
        var sut = CreateSut();

        fla.LinkedPropertyProfile.ProposedFellingDetails = new List<ProposedFellingDetail>();

        FlaRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(fla));

        WoodlandOwnerService
            .Setup(x => x.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(woodlandOwner));

        ExternalUserAccountRepository
            .Setup(x => x.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(externalApplicant));

        InternalUserAccountService
            .Setup(x => x.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(internalUser));

        WoodlandOfficerReviewService
            .Setup(x => x.GetCompartmentDesignationsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<ApplicationSubmittedCompartmentDesignations>("error"));

        var result = await sut.GetApplicationDesignationsAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsFailure);

        WoodlandOfficerReviewService.Verify(x => x.GetCompartmentDesignationsAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);

        FlaRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);

    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnSuccessWithNoExistingCompartmentDesignations(
        Guid applicationId,
        FellingLicenceApplication fla,
        WoodlandOwnerModel woodlandOwner,
        UserAccount externalApplicant,
        Flo.Services.InternalUsers.Entities.UserAccount.UserAccount internalUser)
    {
        var sut = CreateSut();

        fla.LinkedPropertyProfile.ProposedFellingDetails = new List<ProposedFellingDetail>();

        var designations = new ApplicationSubmittedCompartmentDesignations
        {
            CompartmentDesignations = new List<SubmittedCompartmentDesignationsModel>()
        };

        FlaRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(fla));

        WoodlandOwnerService
            .Setup(x => x.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(woodlandOwner));

        ExternalUserAccountRepository
            .Setup(x => x.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(externalApplicant));

        InternalUserAccountService
            .Setup(x => x.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(internalUser));

        WoodlandOfficerReviewService
            .Setup(x => x.GetCompartmentDesignationsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(designations));

        var result = await sut.GetApplicationDesignationsAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(designations, result.Value.CompartmentDesignations);
        Assert.Equal(applicationId, result.Value.ApplicationId);
        Assert.NotNull(result.Value.FellingLicenceApplicationSummary);
        Assert.NotNull(result.Value.Breadcrumbs);

        WoodlandOfficerReviewService.Verify(x => x.GetCompartmentDesignationsAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);

        FlaRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);

    }


    [Theory, AutoMoqData]
    public async Task ShouldReturnSuccessWithExistingCompartmentDesignationsInCptNameOrder(
        Guid applicationId,
        FellingLicenceApplication fla,
        WoodlandOwnerModel woodlandOwner,
        UserAccount externalApplicant,
        Flo.Services.InternalUsers.Entities.UserAccount.UserAccount internalUser)
    {
        var sut = CreateSut();

        fla.LinkedPropertyProfile.ProposedFellingDetails = new List<ProposedFellingDetail>();

        var designation1 = Fixture.Build<SubmittedCompartmentDesignationsModel>()
            .With(x => x.CompartmentName, "1A")
            .Create();

        var designation2 = Fixture.Build<SubmittedCompartmentDesignationsModel>()
            .With(x => x.CompartmentName, "1B")
            .Create();

        var designation3 = Fixture.Build<SubmittedCompartmentDesignationsModel>()
            .With(x => x.CompartmentName, "2A")
            .Create();

        var designation4 = Fixture.Build<SubmittedCompartmentDesignationsModel>()
            .With(x => x.CompartmentName, "10A")
            .Create();

        var designations = new ApplicationSubmittedCompartmentDesignations
        {
            CompartmentDesignations = [designation2, designation4, designation1, designation3]
        };

        FlaRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(fla));

        WoodlandOwnerService
            .Setup(x => x.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(woodlandOwner));

        ExternalUserAccountRepository
            .Setup(x => x.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(externalApplicant));

        InternalUserAccountService
            .Setup(x => x.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(internalUser));

        WoodlandOfficerReviewService
            .Setup(x => x.GetCompartmentDesignationsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(designations));

        var result = await sut.GetApplicationDesignationsAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(designations, result.Value.CompartmentDesignations);
        Assert.Equal(designation1.Id, result.Value.CompartmentDesignations.CompartmentDesignations[0].Id);
        Assert.Equal(designation2.Id, result.Value.CompartmentDesignations.CompartmentDesignations[1].Id);
        Assert.Equal(designation3.Id, result.Value.CompartmentDesignations.CompartmentDesignations[2].Id);
        Assert.Equal(designation4.Id, result.Value.CompartmentDesignations.CompartmentDesignations[3].Id);
        Assert.Equal(applicationId, result.Value.ApplicationId);
        Assert.NotNull(result.Value.FellingLicenceApplicationSummary);
        Assert.NotNull(result.Value.Breadcrumbs);

        WoodlandOfficerReviewService.Verify(x => x.GetCompartmentDesignationsAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);

        FlaRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);

    }


    private DesignationsUseCase CreateSut()
    {
        ResetMocks();

        return new DesignationsUseCase(
            InternalUserAccountService.Object,
            ExternalUserAccountRepository.Object,
            FlaRepository.Object,
            WoodlandOwnerService.Object,
            WoodlandOfficerReviewService.Object,
            UpdateWoodlandOfficerReviewService.Object,
            MockAgentAuthorityService.Object,
            GetConfiguredFcAreas.Object,
            AuditingService.Object,
            RequestContext,
            WoodlandOfficerReviewSubStatusService.Object,
            new NullLogger<DesignationsUseCase>());
    }
}