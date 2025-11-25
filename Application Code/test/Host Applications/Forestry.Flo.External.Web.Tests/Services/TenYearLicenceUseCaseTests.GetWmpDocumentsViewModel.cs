using AutoFixture;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.PropertyProfiles.Entities;
using Forestry.Flo.Tests.Common;

namespace Forestry.Flo.External.Web.Tests.Services;

public partial class TenYearLicenceUseCaseTests
{
    [Theory, AutoMoqData]
    public async Task GetWmpDocumentsViewModelWhenCannotRetrieveUserAccess(
        Guid applicationId,
        Guid userId,
        bool returnToApplicationSummary,
        bool fromDataImport,
        string error)
    {
        var sut = CreateSut();

        _retrieveUserAccountsMock
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserAccessModel>(error));

        var user = GetExternalApplicant(userId);

        var result = await sut.GetWmpDocumentsViewModel(
            user,
            applicationId,
            returnToApplicationSummary,
            fromDataImport,
            CancellationToken.None);

        Assert.True(result.IsFailure);

        _retrieveUserAccountsMock.Verify(x => x.RetrieveUserAccessAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveUserAccountsMock.VerifyNoOtherCalls();

        _retrieveWoodlandOwnersMock.VerifyNoOtherCalls();
        _retrieveFellingLicenceApplicationMock.VerifyNoOtherCalls();
        _retrievePropertyProfilesMock.VerifyNoOtherCalls();
        _retrieveCompartmentsMock.VerifyNoOtherCalls();
        _retrieveAgentAuthorityMock.VerifyNoOtherCalls();
        _updateFellingLicenceApplicationMock.VerifyNoOtherCalls();
        _auditMock.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task GetWmpDocumentsViewModelWhenCannotRetrieveApplication(
        Guid applicationId,
        Guid userId,
        bool returnToApplicationSummary,
        bool fromDataImport,
        UserAccessModel userAccess,
        string error)
    {
        var sut = CreateSut();

        _retrieveUserAccountsMock
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(userAccess));

        _retrieveFellingLicenceApplicationMock
            .Setup(x => x.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<FellingLicenceApplication>(error));

        var user = GetExternalApplicant(userId);

        var result = await sut.GetWmpDocumentsViewModel(
            user,
            applicationId,
            returnToApplicationSummary,
            fromDataImport,
            CancellationToken.None);

        Assert.True(result.IsFailure);

        _retrieveUserAccountsMock.Verify(x => x.RetrieveUserAccessAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveUserAccountsMock.VerifyNoOtherCalls();

        _retrieveWoodlandOwnersMock.VerifyNoOtherCalls();

        _retrieveFellingLicenceApplicationMock.Verify(x => x.GetApplicationByIdAsync(applicationId, userAccess, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveFellingLicenceApplicationMock.VerifyNoOtherCalls();

        _retrievePropertyProfilesMock.VerifyNoOtherCalls();
        _retrieveCompartmentsMock.VerifyNoOtherCalls();
        _retrieveAgentAuthorityMock.VerifyNoOtherCalls();
        _updateFellingLicenceApplicationMock.VerifyNoOtherCalls();
        _auditMock.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task GetWmpDocumentsViewModelWhenCannotCalculateApplicationSummary(
        Guid applicationId,
        Guid userId,
        bool returnToApplicationSummary,
        bool fromDataImport,
        UserAccessModel userAccess,
        FellingLicenceApplication application,
        string error)
    {
        var status = _fixture.Build<StatusHistory>()
            .With(x => x.Status, FellingLicenceStatus.Draft)
            .Without(x => x.FellingLicenceApplication)
            .Create();

        application.StatusHistories = [status];

        var sut = CreateSut();

        _retrieveUserAccountsMock
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(userAccess));

        _retrieveFellingLicenceApplicationMock
            .Setup(x => x.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(application));

        _retrievePropertyProfilesMock
            .Setup(x => x.GetPropertyByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<PropertyProfile>(error));

        var user = GetExternalApplicant(userId);

        var result = await sut.GetWmpDocumentsViewModel(
            user,
            applicationId,
            returnToApplicationSummary,
            fromDataImport,
            CancellationToken.None);

        Assert.True(result.IsFailure);

        _retrieveUserAccountsMock.Verify(x => x.RetrieveUserAccessAsync(userId, It.IsAny<CancellationToken>()), Times.Exactly(3));
        _retrieveUserAccountsMock.VerifyNoOtherCalls();

        _retrieveWoodlandOwnersMock.VerifyNoOtherCalls();

        _retrieveFellingLicenceApplicationMock.Verify(x => x.GetApplicationByIdAsync(applicationId, userAccess, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveFellingLicenceApplicationMock.VerifyNoOtherCalls();

        _retrievePropertyProfilesMock.Verify(x => x.GetPropertyByIdAsync(application.LinkedPropertyProfile!.PropertyProfileId, userAccess, It.IsAny<CancellationToken>()), Times.Once);
        _retrievePropertyProfilesMock.VerifyNoOtherCalls();
        _retrieveCompartmentsMock.VerifyNoOtherCalls();
        _retrieveAgentAuthorityMock.VerifyNoOtherCalls();
        _updateFellingLicenceApplicationMock.VerifyNoOtherCalls();
        _auditMock.VerifyNoOtherCalls();
    }

    // error scenarios in ApplicationUseCaseCommon.GetApplicationSummaryAsync covered in TenYearLicenceUseCaseTests.cs

    [Theory, AutoMoqData]
    public async Task GetWmpDocumentsViewModelWhenSuccessful(
        Guid applicationId,
        Guid userId,
        bool returnToApplicationSummary,
        bool fromDataImport,
        UserAccessModel userAccess,
        FellingLicenceApplication application,
        PropertyProfile propertyProfile,
        WoodlandOwnerModel woodlandOwner,
        AgencyModel agency)
    {
        var document = _fixture.Build<Document>()
            .Without(x => x.FellingLicenceApplication)
            .Without(x => x.DeletionTimestamp)
            .With(x => x.Purpose, DocumentPurpose.WmpDocument)
            .Create();
        var notWmpDocument = _fixture.Build<Document>()
            .Without(x => x.FellingLicenceApplication)
            .Without(x => x.DeletionTimestamp)
            .With(x => x.Purpose, DocumentPurpose.Attachment)
            .Create();
        var deletedDocument = _fixture.Build<Document>()
            .Without(x => x.FellingLicenceApplication)
            .With(x => x.DeletionTimestamp, DateTime.UtcNow.AddDays(-1))
            .With(x => x.Purpose, DocumentPurpose.WmpDocument)
            .Create();

        application.Documents = [document, notWmpDocument, deletedDocument];

        var status = _fixture.Build<StatusHistory>()
            .With(x => x.Status, FellingLicenceStatus.Draft)
            .Without(x => x.FellingLicenceApplication)
            .Create();

        application.StatusHistories = [status];

        var sut = CreateSut();

        _retrieveUserAccountsMock
            .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(userAccess));

        _retrieveFellingLicenceApplicationMock
            .Setup(x => x.GetApplicationByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(application));

        _retrievePropertyProfilesMock
            .Setup(x => x.GetPropertyByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(propertyProfile));

        _retrieveWoodlandOwnersMock
            .Setup(x => x.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(woodlandOwner));

        _retrieveAgentAuthorityMock
            .Setup(x => x.GetAgencyForWoodlandOwnerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<AgencyModel>.From(agency));

        var user = GetExternalApplicant(userId);

        var result = await sut.GetWmpDocumentsViewModel(
            user,
            applicationId,
            returnToApplicationSummary,
            fromDataImport,
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var actual = result.Value;
        Assert.NotNull(actual);

        Assert.Equal(applicationId, actual.ApplicationId);
        Assert.Equal(returnToApplicationSummary, actual.ReturnToApplicationSummary);
        Assert.Equal(fromDataImport, actual.FromDataImport);
        Assert.Equal(FellingLicenceStatus.Draft, actual.FellingLicenceStatus);
        Assert.Equal(application.ApplicationReference, actual.ApplicationReference);
        Assert.Equal(application.FellingLicenceApplicationStepStatus.TenYearLicenceStepStatus, actual.StepComplete);
        Assert.True(actual.StepRequiredForApplication);
        Assert.Single(actual.Documents);
        
        Assert.Equal(document.Id, actual.Documents.Single().Id);
        Assert.Equal(document.FileName, actual.Documents.Single().FileName);
        Assert.Equal(document.FileSize, actual.Documents.Single().FileSize);
        Assert.Equal(document.MimeType, actual.Documents.Single().MimeType);
        Assert.Equal(document.FileType, actual.Documents.Single().FileType);
        Assert.Equal(document.VisibleToApplicant, actual.Documents.Single().VisibleToApplicant);
        Assert.Equal(document.VisibleToConsultee, actual.Documents.Single().VisibleToConsultee);
        Assert.Equal(document.AttachedByType, actual.Documents.Single().AttachedByType);
        Assert.Equal(document.Purpose, actual.Documents.Single().DocumentPurpose);
        Assert.Equal(document.CreatedTimestamp, actual.Documents.Single().CreatedTimestamp);

        Assert.Equal(DocumentPurpose.WmpDocument, actual.AddSupportingDocumentModel.Purpose);
        Assert.Equal(applicationId, actual.AddSupportingDocumentModel.FellingLicenceApplicationId);
        Assert.Equal(returnToApplicationSummary, actual.AddSupportingDocumentModel.ReturnToApplicationSummary);
        Assert.Equal(fromDataImport, actual.AddSupportingDocumentModel.FromDataImport);
        Assert.Equal(0, actual.AddSupportingDocumentModel.DocumentCount);

        Assert.Equal(application.Id, actual.ApplicationSummary.Id);
        Assert.Equal(application.ApplicationReference, actual.ApplicationSummary.ApplicationReference);
        Assert.Equal(FellingLicenceStatus.Draft, actual.ApplicationSummary.Status);
        Assert.Equal(application.LinkedPropertyProfile.PropertyProfileId, actual.ApplicationSummary.PropertyProfileId);
        Assert.Equal(propertyProfile.Name, actual.ApplicationSummary.PropertyName);
        Assert.Equal(propertyProfile.NameOfWood, actual.ApplicationSummary.NameOfWood);
        Assert.Equal(application.WoodlandOwnerId, actual.ApplicationSummary.WoodlandOwnerId);
        Assert.Equal(woodlandOwner.GetContactNameForDisplay, actual.ApplicationSummary.WoodlandOwnerName);
        Assert.Equal(agency.OrganisationName ?? agency.ContactName, actual.ApplicationSummary.AgencyName);

        _retrieveUserAccountsMock.Verify(x => x.RetrieveUserAccessAsync(userId, It.IsAny<CancellationToken>()), Times.Exactly(3));
        _retrieveUserAccountsMock.VerifyNoOtherCalls();

        _retrieveWoodlandOwnersMock.Verify(x => x.RetrieveWoodlandOwnerByIdAsync(application.WoodlandOwnerId, It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()), Times.Once);
        _retrieveWoodlandOwnersMock.VerifyNoOtherCalls();

        _retrieveFellingLicenceApplicationMock.Verify(x => x.GetApplicationByIdAsync(applicationId, userAccess, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveFellingLicenceApplicationMock.VerifyNoOtherCalls();

        _retrievePropertyProfilesMock.Verify(x => x.GetPropertyByIdAsync(application.LinkedPropertyProfile!.PropertyProfileId, userAccess, It.IsAny<CancellationToken>()), Times.Once);
        _retrievePropertyProfilesMock.VerifyNoOtherCalls();

        _retrieveCompartmentsMock.VerifyNoOtherCalls();

        _retrieveAgentAuthorityMock.Verify(x => x.GetAgencyForWoodlandOwnerAsync(application.WoodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once);
        _retrieveAgentAuthorityMock.VerifyNoOtherCalls();

        _updateFellingLicenceApplicationMock.VerifyNoOtherCalls();
        _auditMock.VerifyNoOtherCalls();
    }
}