using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using FluentEmail.Core;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Tests.Common;
using Moq;

namespace Forestry.Flo.Internal.Web.Tests.Services.ExternalConsulteeReview;

public partial class ExternalConsulteeInviteUseCaseTests
{
    [Theory, AutoData]
    public async Task WhenApplicationNotFoundForNewConsulteeInviteViewModel(Guid applicationId)
    {
        var sut = CreateSut();

        _internalUserContextFlaRepository.Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.None);

        var (isSuccess, error, _) = await sut.GetConsulteeInvitesIndexViewModelAsync(applicationId, CancellationToken.None);

        Assert.False(isSuccess);

        _internalUserContextFlaRepository
            .Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _internalUserContextFlaRepository.VerifyNoOtherCalls();

        _woodlandOwnerService.VerifyNoOtherCalls();
        _mockAgentAuthorityService.VerifyNoOtherCalls();
        _externalUserAccountService.VerifyNoOtherCalls();
        _internalUserAccountService.VerifyNoOtherCalls();
    }

    // ExtractApplicationSummaryAsync failure scenarios assumed to be tested elsewhere

    [Theory, AutoMoqData]
    public async Task WhenApplicationHasNoSupportingDocumentsOrPublicRegister(
        Guid applicationId,
        FellingLicenceApplication application,
        WoodlandOwnerModel woodlandOwner)
    {
        application.Documents = [];
        application.PublicRegister = null;
        application.AssigneeHistories = [];
        application.LinkedPropertyProfile.ProposedFellingDetails = [];


        var sut = CreateSut();

        _internalUserContextFlaRepository
            .Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));

        _woodlandOwnerService
            .Setup(r => r.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(woodlandOwner));

        _mockAgentAuthorityService
            .Setup(x => x.GetAgencyForWoodlandOwnerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<AgencyModel>.None);

        var (isSuccess, error, model) = await sut.GetNewExternalConsulteeInviteViewModelAsync(applicationId, CancellationToken.None);

        Assert.True(isSuccess);

        Assert.Equal(applicationId, model.ApplicationId);
        Assert.Null(model.ExemptFromConsultationPublicRegister);
        Assert.False(model.PublicRegisterAlreadyCompleted);
        Assert.Empty(model.ConsulteeDocuments);
        Assert.Empty(model.SelectedDocumentIds);

        _internalUserContextFlaRepository
            .Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _internalUserContextFlaRepository.VerifyNoOtherCalls();

        _woodlandOwnerService.Verify(x => x.RetrieveWoodlandOwnerByIdAsync(application.WoodlandOwnerId, It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()), Times.Once());
        _woodlandOwnerService.VerifyNoOtherCalls();

        _mockAgentAuthorityService.Verify(x => x.GetAgencyForWoodlandOwnerAsync(application.WoodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once);
        _mockAgentAuthorityService.VerifyNoOtherCalls();

        _externalUserAccountService.VerifyNoOtherCalls();
        _internalUserAccountService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenApplicationHasNoSupportingDocumentsForConsulteeOrPublicRegister(
        Guid applicationId,
        FellingLicenceApplication application,
        WoodlandOwnerModel woodlandOwner)
    {
        application.Documents.ForEach(x =>
        {
            x.DeletionTimestamp = null;
            x.VisibleToConsultee = false;
        });
        application.PublicRegister = null;
        application.AssigneeHistories = [];
        application.LinkedPropertyProfile.ProposedFellingDetails = [];


        var sut = CreateSut();

        _internalUserContextFlaRepository
            .Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));

        _woodlandOwnerService
            .Setup(r => r.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(woodlandOwner));

        _mockAgentAuthorityService
            .Setup(x => x.GetAgencyForWoodlandOwnerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<AgencyModel>.None);

        var (isSuccess, error, model) = await sut.GetNewExternalConsulteeInviteViewModelAsync(applicationId, CancellationToken.None);

        Assert.True(isSuccess);

        Assert.Equal(applicationId, model.ApplicationId);
        Assert.Null(model.ExemptFromConsultationPublicRegister);
        Assert.False(model.PublicRegisterAlreadyCompleted);
        Assert.Empty(model.ConsulteeDocuments);
        Assert.Empty(model.SelectedDocumentIds);

        _internalUserContextFlaRepository
            .Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _internalUserContextFlaRepository.VerifyNoOtherCalls();

        _woodlandOwnerService.Verify(x => x.RetrieveWoodlandOwnerByIdAsync(application.WoodlandOwnerId, It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()), Times.Once());
        _woodlandOwnerService.VerifyNoOtherCalls();

        _mockAgentAuthorityService.Verify(x => x.GetAgencyForWoodlandOwnerAsync(application.WoodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once);
        _mockAgentAuthorityService.VerifyNoOtherCalls();

        _externalUserAccountService.VerifyNoOtherCalls();
        _internalUserAccountService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenApplicationHasNoSupportingDocumentsNotDeletedOrPublicRegister(
        Guid applicationId,
        FellingLicenceApplication application,
        WoodlandOwnerModel woodlandOwner)
    {
        application.Documents.ForEach(x =>
        {
            x.VisibleToConsultee = true;
            x.DeletionTimestamp = DateTime.UtcNow.AddDays(-1);
        });
        application.PublicRegister = null;
        application.AssigneeHistories = [];
        application.LinkedPropertyProfile.ProposedFellingDetails = [];


        var sut = CreateSut();

        _internalUserContextFlaRepository
            .Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));

        _woodlandOwnerService
            .Setup(r => r.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(woodlandOwner));

        _mockAgentAuthorityService
            .Setup(x => x.GetAgencyForWoodlandOwnerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<AgencyModel>.None);

        var (isSuccess, error, model) = await sut.GetNewExternalConsulteeInviteViewModelAsync(applicationId, CancellationToken.None);

        Assert.True(isSuccess);

        Assert.Equal(applicationId, model.ApplicationId);
        Assert.Null(model.ExemptFromConsultationPublicRegister);
        Assert.False(model.PublicRegisterAlreadyCompleted);
        Assert.Empty(model.ConsulteeDocuments);
        Assert.Empty(model.SelectedDocumentIds);

        _internalUserContextFlaRepository
            .Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _internalUserContextFlaRepository.VerifyNoOtherCalls();

        _woodlandOwnerService.Verify(x => x.RetrieveWoodlandOwnerByIdAsync(application.WoodlandOwnerId, It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()), Times.Once());
        _woodlandOwnerService.VerifyNoOtherCalls();

        _mockAgentAuthorityService.Verify(x => x.GetAgencyForWoodlandOwnerAsync(application.WoodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once);
        _mockAgentAuthorityService.VerifyNoOtherCalls();

        _externalUserAccountService.VerifyNoOtherCalls();
        _internalUserAccountService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenApplicationHasSupportingDocumentsButNoPublicRegister(
        Guid applicationId,
        FellingLicenceApplication application,
        WoodlandOwnerModel woodlandOwner)
    {
        application.Documents.ForEach(x =>
        {
            x.VisibleToConsultee = true;
            x.DeletionTimestamp = null;
        });
        application.PublicRegister = null;
        application.AssigneeHistories = [];
        application.LinkedPropertyProfile.ProposedFellingDetails = [];

        var expectedDocuments = ModelMapping.ToDocumentModelList(application.Documents)
            .OrderByDescending(x => x.CreatedTimestamp).ToList();

        var sut = CreateSut();

        _internalUserContextFlaRepository
            .Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));

        _woodlandOwnerService
            .Setup(r => r.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(woodlandOwner));

        _mockAgentAuthorityService
            .Setup(x => x.GetAgencyForWoodlandOwnerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<AgencyModel>.None);

        var (isSuccess, error, model) = await sut.GetNewExternalConsulteeInviteViewModelAsync(applicationId, CancellationToken.None);

        Assert.True(isSuccess);

        Assert.Equal(applicationId, model.ApplicationId);
        Assert.Null(model.ExemptFromConsultationPublicRegister);
        Assert.False(model.PublicRegisterAlreadyCompleted);
        Assert.Equivalent(expectedDocuments, model.ConsulteeDocuments);
        Assert.Equivalent(expectedDocuments.Select(x => (Guid?)x.Id), model.SelectedDocumentIds);

        _internalUserContextFlaRepository
            .Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _internalUserContextFlaRepository.VerifyNoOtherCalls();

        _woodlandOwnerService.Verify(x => x.RetrieveWoodlandOwnerByIdAsync(application.WoodlandOwnerId, It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()), Times.Once());
        _woodlandOwnerService.VerifyNoOtherCalls();

        _mockAgentAuthorityService.Verify(x => x.GetAgencyForWoodlandOwnerAsync(application.WoodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once);
        _mockAgentAuthorityService.VerifyNoOtherCalls();

        _externalUserAccountService.VerifyNoOtherCalls();
        _internalUserAccountService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenApplicationHasSupportingDocumentsAndIsExemptFromPublicRegister(
        Guid applicationId,
        FellingLicenceApplication application,
        WoodlandOwnerModel woodlandOwner)
    {
        application.Documents.ForEach(x =>
        {
            x.VisibleToConsultee = true;
            x.DeletionTimestamp = null;
        });
        application.PublicRegister.WoodlandOfficerSetAsExemptFromConsultationPublicRegister = true;
        application.PublicRegister.ConsultationPublicRegisterPublicationTimestamp = null;
        application.AssigneeHistories = [];
        application.LinkedPropertyProfile.ProposedFellingDetails = [];

        var expectedDocuments = ModelMapping.ToDocumentModelList(application.Documents)
            .OrderByDescending(x => x.CreatedTimestamp).ToList();

        var sut = CreateSut();

        _internalUserContextFlaRepository
            .Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));

        _woodlandOwnerService
            .Setup(r => r.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(woodlandOwner));

        _mockAgentAuthorityService
            .Setup(x => x.GetAgencyForWoodlandOwnerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<AgencyModel>.None);

        var (isSuccess, error, model) = await sut.GetNewExternalConsulteeInviteViewModelAsync(applicationId, CancellationToken.None);

        Assert.True(isSuccess);

        Assert.Equal(applicationId, model.ApplicationId);
        Assert.True(model.ExemptFromConsultationPublicRegister);
        Assert.True(model.PublicRegisterAlreadyCompleted);
        Assert.Equivalent(expectedDocuments, model.ConsulteeDocuments);
        Assert.Equivalent(expectedDocuments.Select(x => (Guid?)x.Id), model.SelectedDocumentIds);

        _internalUserContextFlaRepository
            .Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _internalUserContextFlaRepository.VerifyNoOtherCalls();

        _woodlandOwnerService.Verify(x => x.RetrieveWoodlandOwnerByIdAsync(application.WoodlandOwnerId, It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()), Times.Once());
        _woodlandOwnerService.VerifyNoOtherCalls();

        _mockAgentAuthorityService.Verify(x => x.GetAgencyForWoodlandOwnerAsync(application.WoodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once);
        _mockAgentAuthorityService.VerifyNoOtherCalls();

        _externalUserAccountService.VerifyNoOtherCalls();
        _internalUserAccountService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenApplicationHasSupportingDocumentsAndIsOnPublicRegister(
        Guid applicationId,
        FellingLicenceApplication application,
        WoodlandOwnerModel woodlandOwner)
    {
        application.Documents.ForEach(x =>
        {
            x.VisibleToConsultee = true;
            x.DeletionTimestamp = null;
        });
        application.PublicRegister.WoodlandOfficerSetAsExemptFromConsultationPublicRegister = false;
        application.PublicRegister.ConsultationPublicRegisterPublicationTimestamp = DateTime.UtcNow.AddDays(-1);
        application.AssigneeHistories = [];
        application.LinkedPropertyProfile.ProposedFellingDetails = [];

        var expectedDocuments = ModelMapping.ToDocumentModelList(application.Documents)
            .OrderByDescending(x => x.CreatedTimestamp).ToList();

        var sut = CreateSut();

        _internalUserContextFlaRepository
            .Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));

        _woodlandOwnerService
            .Setup(r => r.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(woodlandOwner));

        _mockAgentAuthorityService
            .Setup(x => x.GetAgencyForWoodlandOwnerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<AgencyModel>.None);

        var (isSuccess, error, model) = await sut.GetNewExternalConsulteeInviteViewModelAsync(applicationId, CancellationToken.None);

        Assert.True(isSuccess);

        Assert.Equal(applicationId, model.ApplicationId);
        Assert.False(model.ExemptFromConsultationPublicRegister);
        Assert.True(model.PublicRegisterAlreadyCompleted);
        Assert.Equivalent(expectedDocuments, model.ConsulteeDocuments);
        Assert.Equivalent(expectedDocuments.Select(x => (Guid?)x.Id), model.SelectedDocumentIds);

        _internalUserContextFlaRepository
            .Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _internalUserContextFlaRepository.VerifyNoOtherCalls();

        _woodlandOwnerService.Verify(x => x.RetrieveWoodlandOwnerByIdAsync(application.WoodlandOwnerId, It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()), Times.Once());
        _woodlandOwnerService.VerifyNoOtherCalls();

        _mockAgentAuthorityService.Verify(x => x.GetAgencyForWoodlandOwnerAsync(application.WoodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once);
        _mockAgentAuthorityService.VerifyNoOtherCalls();

        _externalUserAccountService.VerifyNoOtherCalls();
        _internalUserAccountService.VerifyNoOtherCalls();
    }
}