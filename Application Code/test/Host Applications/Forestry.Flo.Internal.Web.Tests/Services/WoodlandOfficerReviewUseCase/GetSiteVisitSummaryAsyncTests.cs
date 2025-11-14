using AutoFixture;
using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using FluentEmail.Core;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.WoodlandOfficerReview;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;
using Forestry.Flo.Services.Gis.Models.Internal;
using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Newtonsoft.Json;

namespace Forestry.Flo.Internal.Web.Tests.Services.WoodlandOfficerReviewUseCase;

public class GetSiteVisitSummaryAsyncTests : WoodlandOfficerReviewUseCaseTestsBase<SiteVisitUseCase>
{
    private static readonly Fixture Fixture = new Fixture();

    public GetSiteVisitSummaryAsyncTests()
    {
        Fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Theory, AutoData]
    public async Task ShouldReturnFailureWhenUnableToLoadApplicationSummary(
        Guid applicationId,
        string hostingPage,
        SiteVisitModel siteVisit)
    {
        var sut = CreateSut();

        FlaRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.None);

        var result = await sut.GetSiteVisitSummaryAsync(applicationId, hostingPage, CancellationToken.None);

        Assert.True(result.IsFailure);

        FlaRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        WoodlandOwnerService.VerifyNoOtherCalls();
        ExternalUserAccountRepository.VerifyNoOtherCalls();
        InternalUserAccountService.VerifyNoOtherCalls();
        NotificationHistoryService.VerifyNoOtherCalls();
        ActivityFeedItemProvider.VerifyNoOtherCalls();
        MockAgentAuthorityService.VerifyNoOtherCalls();
        _foresterServices.VerifyNoOtherCalls();
    }

    //It is assumed that the various other permutations of outcome for FellingLicenceApplicationUseCaseBase.GetFellingLicenceDetailsAsync are already tested - see AssignToUserUseCaseTestsBase

    [Theory, AutoMoqData]
    public async Task ShouldReturnFailureWhenUnableToLoadSiteVisitComments(
        Guid applicationId,
        string hostingPage,
        SiteVisitModel siteVisit,
        FellingLicenceApplication fla,
        WoodlandOwnerModel woodlandOwner,
        UserAccount externalApplicant,
        Flo.Services.InternalUsers.Entities.UserAccount.UserAccount internalUser)
    {
        var sut = CreateSut();

        fla.LinkedPropertyProfile.ProposedFellingDetails = new List<ProposedFellingDetail>();

        WoodlandOfficerReviewService
            .Setup(x => x.GetSiteVisitDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(Maybe.From(siteVisit)));

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

        ActivityFeedItemProvider
            .Setup(x => x.RetrieveAllRelevantActivityFeedItemsAsync(It.IsAny<ActivityFeedItemProviderModel>(), It.IsAny<ActorType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<IList<ActivityFeedItemModel>>("error"));

        var result = await sut.GetSiteVisitSummaryAsync(applicationId, hostingPage, CancellationToken.None);

        Assert.True(result.IsFailure);

        FlaRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);

        ActivityFeedItemProvider.Verify(x => x.RetrieveAllRelevantActivityFeedItemsAsync(
            It.Is<ActivityFeedItemProviderModel>(m => m.FellingLicenceId == applicationId && m.FellingLicenceReference == fla.ApplicationReference && m.ItemTypes.Single() == ActivityFeedItemType.SiteVisitComment),
            ActorType.InternalUser,
            It.IsAny<CancellationToken>()), Times.Once);
        ActivityFeedItemProvider.VerifyNoOtherCalls();

        MockAgentAuthorityService.Verify(x => x.GetAgencyForWoodlandOwnerAsync(fla.WoodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once);
        MockAgentAuthorityService.VerifyNoOtherCalls();

        _foresterServices.VerifyNoOtherCalls();
    }
    
    [Theory, AutoMoqData]
    public async Task ShouldReturnFailureWhenUnableToGenerateFellingImage(
        Guid applicationId,
        string hostingPage,
        SiteVisitModel siteVisit,
        FellingLicenceApplication fla,
        WoodlandOwnerModel woodlandOwner,
        AgencyModel agency,
        UserAccount externalApplicant,
        Flo.Services.InternalUsers.Entities.UserAccount.UserAccount internalUser,
        List<ActivityFeedItemModel> siteVisitComments)
    {
        var sut = CreateSut();

        var polygon = new Polygon();
        var serializedPolygon = JsonConvert.SerializeObject(polygon);

        var felling = Fixture.Build<ProposedFellingDetail>()
            .With(x => x.LinkedPropertyProfile, fla.LinkedPropertyProfile)
            .With(x => x.LinkedPropertyProfileId, fla.LinkedPropertyProfile.Id)
            .With(x => x.PropertyProfileCompartmentId, fla.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments[0].CompartmentId)
            .With(x => x.FellingSpecies, [])
            .Create();

        var restocking = Fixture.Build<ProposedRestockingDetail>()
            .With(x => x.PropertyProfileCompartmentId, fla.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments[1].CompartmentId)
            .With(x => x.ProposedFellingDetail, felling)
            .With(x => x.RestockingSpecies, [])
            .With(x => x.RestockingOutcomes, [])
            .Create();

        felling.ProposedRestockingDetails = [restocking];

        fla.LinkedPropertyProfile.ProposedFellingDetails = [felling];

        fla.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments.ForEach(x => x.GISData = serializedPolygon);

        WoodlandOfficerReviewService
            .Setup(x => x.GetSiteVisitDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(Maybe.From(siteVisit)));

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

        ActivityFeedItemProvider
            .Setup(x => x.RetrieveAllRelevantActivityFeedItemsAsync(It.IsAny<ActivityFeedItemProviderModel>(), It.IsAny<ActorType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IList<ActivityFeedItemModel>>(siteVisitComments));

        MockAgentAuthorityService
            .Setup(x => x.GetAgencyForWoodlandOwnerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(agency));

        _foresterServices.Setup(x =>
                x.GenerateImage_MultipleCompartmentsAsync(It.IsAny<List<InternalCompartmentDetails<BaseShape>>>(), It.IsAny<CancellationToken>(), It.IsAny<int>(), It.IsAny<MapGeneration>(), It.IsAny<string>()))
            .ReturnsAsync(Result.Failure<Stream>("error"));

        var result = await sut.GetSiteVisitSummaryAsync(applicationId, hostingPage, CancellationToken.None);

        Assert.True(result.IsFailure);

        FlaRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);

        ActivityFeedItemProvider.Verify(x => x.RetrieveAllRelevantActivityFeedItemsAsync(
            It.Is<ActivityFeedItemProviderModel>(m => m.FellingLicenceId == applicationId && m.FellingLicenceReference == fla.ApplicationReference && m.ItemTypes.Single() == ActivityFeedItemType.SiteVisitComment),
            ActorType.InternalUser,
            It.IsAny<CancellationToken>()), Times.Once);
        ActivityFeedItemProvider.VerifyNoOtherCalls();

        WoodlandOwnerService.Verify(x => x.RetrieveWoodlandOwnerByIdAsync(fla.WoodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once);
        WoodlandOwnerService.Verify(x => x.RetrieveWoodlandOwnerByIdAsync(woodlandOwner.Id.Value, It.IsAny<CancellationToken>()), Times.Once);
        WoodlandOwnerService.VerifyNoOtherCalls();

        MockAgentAuthorityService.Verify(x => x.GetAgencyForWoodlandOwnerAsync(fla.WoodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once);
        MockAgentAuthorityService.Verify(x => x.GetAgencyForWoodlandOwnerAsync(woodlandOwner.Id.Value, It.IsAny<CancellationToken>()), Times.Once);
        MockAgentAuthorityService.VerifyNoOtherCalls();

        var fellingCpt = fla.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments[0];

        _foresterServices.Verify(x => x.GenerateImage_MultipleCompartmentsAsync(
            It.Is<List<InternalCompartmentDetails<BaseShape>>>(s => s.Single().CompartmentLabel == fellingCpt.CompartmentNumber && JsonConvert.SerializeObject(s.Single().ShapeGeometry) == serializedPolygon), 
            It.IsAny<CancellationToken>(), 3000, MapGeneration.Other, ""), Times.Once);

        _foresterServices.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnFailureWhenUnableToGenerateRestockingImage(
        Guid applicationId,
        string hostingPage,
        SiteVisitModel siteVisit,
        FellingLicenceApplication fla,
        WoodlandOwnerModel woodlandOwner,
        AgencyModel agency,
        UserAccount externalApplicant,
        Flo.Services.InternalUsers.Entities.UserAccount.UserAccount internalUser,
        List<ActivityFeedItemModel> siteVisitComments)
    {
        var sut = CreateSut();

        using var fellingImageBytes = new MemoryStream([1, 2, 3]);

        var polygon = new Polygon();
        var serializedPolygon = JsonConvert.SerializeObject(polygon);

        var felling = Fixture.Build<ProposedFellingDetail>()
            .With(x => x.LinkedPropertyProfile, fla.LinkedPropertyProfile)
            .With(x => x.LinkedPropertyProfileId, fla.LinkedPropertyProfile.Id)
            .With(x => x.PropertyProfileCompartmentId, fla.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments[0].CompartmentId)
            .With(x => x.FellingSpecies, [])
            .Create();

        var restocking = Fixture.Build<ProposedRestockingDetail>()
            .With(x => x.PropertyProfileCompartmentId, fla.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments[1].CompartmentId)
            .With(x => x.ProposedFellingDetail, felling)
            .With(x => x.RestockingSpecies, [])
            .With(x => x.RestockingOutcomes, [])
            .Create();

        felling.ProposedRestockingDetails = [restocking];

        fla.LinkedPropertyProfile.ProposedFellingDetails = [felling];

        fla.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments.ForEach(x => x.GISData = serializedPolygon);

        WoodlandOfficerReviewService
            .Setup(x => x.GetSiteVisitDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(Maybe.From(siteVisit)));

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

        ActivityFeedItemProvider
            .Setup(x => x.RetrieveAllRelevantActivityFeedItemsAsync(It.IsAny<ActivityFeedItemProviderModel>(), It.IsAny<ActorType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IList<ActivityFeedItemModel>>(siteVisitComments));

        MockAgentAuthorityService
            .Setup(x => x.GetAgencyForWoodlandOwnerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(agency));

        var fellingCpt = fla.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments[0];
        var restockingCpt = fla.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments[1];

        _foresterServices.Setup(x =>
                x.GenerateImage_MultipleCompartmentsAsync(It.Is<List<InternalCompartmentDetails<BaseShape>>>(s => s.Single().CompartmentLabel == fellingCpt.CompartmentNumber), It.IsAny<CancellationToken>(), It.IsAny<int>(), It.IsAny<MapGeneration>(), It.IsAny<string>()))
            .ReturnsAsync(Result.Success<Stream>(fellingImageBytes));

        _foresterServices.Setup(x =>
                x.GenerateImage_MultipleCompartmentsAsync(It.Is<List<InternalCompartmentDetails<BaseShape>>>(s => s.Single().CompartmentLabel == restockingCpt.CompartmentNumber), It.IsAny<CancellationToken>(), It.IsAny<int>(), It.IsAny<MapGeneration>(), It.IsAny<string>()))
            .ReturnsAsync(Result.Failure<Stream>("error"));

        var result = await sut.GetSiteVisitSummaryAsync(applicationId, hostingPage, CancellationToken.None);

        Assert.True(result.IsFailure);

        FlaRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);

        ActivityFeedItemProvider.Verify(x => x.RetrieveAllRelevantActivityFeedItemsAsync(
            It.Is<ActivityFeedItemProviderModel>(m => m.FellingLicenceId == applicationId && m.FellingLicenceReference == fla.ApplicationReference && m.ItemTypes.Single() == ActivityFeedItemType.SiteVisitComment),
            ActorType.InternalUser,
            It.IsAny<CancellationToken>()), Times.Once);
        ActivityFeedItemProvider.VerifyNoOtherCalls();

        WoodlandOwnerService.Verify(x => x.RetrieveWoodlandOwnerByIdAsync(fla.WoodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once);
        WoodlandOwnerService.Verify(x => x.RetrieveWoodlandOwnerByIdAsync(woodlandOwner.Id.Value, It.IsAny<CancellationToken>()), Times.Once);
        WoodlandOwnerService.VerifyNoOtherCalls();

        MockAgentAuthorityService.Verify(x => x.GetAgencyForWoodlandOwnerAsync(fla.WoodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once);
        MockAgentAuthorityService.Verify(x => x.GetAgencyForWoodlandOwnerAsync(woodlandOwner.Id.Value, It.IsAny<CancellationToken>()), Times.Once);
        MockAgentAuthorityService.VerifyNoOtherCalls();

        _foresterServices.Verify(x => x.GenerateImage_MultipleCompartmentsAsync(
            It.Is<List<InternalCompartmentDetails<BaseShape>>>(s => s.Single().CompartmentLabel == fellingCpt.CompartmentNumber && JsonConvert.SerializeObject(s.Single().ShapeGeometry) == serializedPolygon),
            It.IsAny<CancellationToken>(), 3000, MapGeneration.Other, ""), Times.Once);
        _foresterServices.Verify(x => x.GenerateImage_MultipleCompartmentsAsync(
            It.Is<List<InternalCompartmentDetails<BaseShape>>>(s => s.Single().CompartmentLabel == restockingCpt.CompartmentNumber && JsonConvert.SerializeObject(s.Single().ShapeGeometry) == serializedPolygon),
            It.IsAny<CancellationToken>(), 3000, MapGeneration.Other, ""), Times.Once);

        _foresterServices.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnSuccessWhenAbleToRetrieveDetails(
        Guid applicationId,
        string hostingPage,
        SiteVisitModel siteVisit,
        FellingLicenceApplication fla,
        WoodlandOwnerModel woodlandOwner,
        AgencyModel agency,
        UserAccount externalApplicant,
        Flo.Services.InternalUsers.Entities.UserAccount.UserAccount internalUser,
        List<ActivityFeedItemModel> siteVisitComments)
    {
        var sut = CreateSut();

        using var fellingImageBytes = new MemoryStream([1, 2, 3]);
        using var restockingImageBytes = new MemoryStream([4, 5, 6]);

        var polygon = new Polygon();
        var serializedPolygon = JsonConvert.SerializeObject(polygon);

        var felling = Fixture.Build<ProposedFellingDetail>()
            .With(x => x.LinkedPropertyProfile, fla.LinkedPropertyProfile)
            .With(x => x.LinkedPropertyProfileId, fla.LinkedPropertyProfile.Id)
            .With(x => x.PropertyProfileCompartmentId, fla.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments[0].CompartmentId)
            .With(x => x.FellingSpecies, [])
            .Create();

        var restocking = Fixture.Build<ProposedRestockingDetail>()
            .With(x => x.PropertyProfileCompartmentId, fla.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments[1].CompartmentId)
            .With(x => x.ProposedFellingDetail, felling)
            .With(x => x.RestockingSpecies, [])
            .With(x => x.RestockingOutcomes, [])
            .Create();

        felling.ProposedRestockingDetails = [restocking];

        fla.LinkedPropertyProfile.ProposedFellingDetails = [felling];

        fla.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments.ForEach(x => x.GISData = serializedPolygon);

        WoodlandOfficerReviewService
            .Setup(x => x.GetSiteVisitDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(Maybe.From(siteVisit)));

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

        ActivityFeedItemProvider
            .Setup(x => x.RetrieveAllRelevantActivityFeedItemsAsync(It.IsAny<ActivityFeedItemProviderModel>(), It.IsAny<ActorType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IList<ActivityFeedItemModel>>(siteVisitComments));

        MockAgentAuthorityService
            .Setup(x => x.GetAgencyForWoodlandOwnerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(agency));

        var fellingCpt = fla.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments[0];
        var restockingCpt = fla.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments[1];

        _foresterServices.Setup(x =>
                x.GenerateImage_MultipleCompartmentsAsync(It.Is<List<InternalCompartmentDetails<BaseShape>>>(s => s.Single().CompartmentLabel == fellingCpt.CompartmentNumber), It.IsAny<CancellationToken>(), It.IsAny<int>(), It.IsAny<MapGeneration>(), It.IsAny<string>()))
            .ReturnsAsync(Result.Success<Stream>(fellingImageBytes));

        _foresterServices.Setup(x =>
                x.GenerateImage_MultipleCompartmentsAsync(It.Is<List<InternalCompartmentDetails<BaseShape>>>(s => s.Single().CompartmentLabel == restockingCpt.CompartmentNumber), It.IsAny<CancellationToken>(), It.IsAny<int>(), It.IsAny<MapGeneration>(), It.IsAny<string>()))
            .ReturnsAsync(Result.Success<Stream>(restockingImageBytes));

        var result = await sut.GetSiteVisitSummaryAsync(applicationId, hostingPage, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(woodlandOwner.ContactName, result.Value.ApplicationOwner.WoodlandOwner.ContactName);
        Assert.Equal(woodlandOwner.ContactEmail, result.Value.ApplicationOwner.WoodlandOwner.ContactEmail);
        Assert.Equivalent(woodlandOwner.ContactAddress, result.Value.ApplicationOwner.WoodlandOwner.ContactAddress);
        Assert.Equal(agency, result.Value.ApplicationOwner.Agency);

        Assert.Equal(CaseNoteType.SiteVisitComment, result.Value.SiteVisitComments.DefaultCaseNoteFilter);
        Assert.False(result.Value.SiteVisitComments.ShowAddCaseNote);
        Assert.Equal(hostingPage, result.Value.SiteVisitComments.HostingPage);
        Assert.Equivalent(siteVisitComments, result.Value.SiteVisitComments.ActivityFeedItemModels);

        Assert.Equal(applicationId, result.Value.FellingAndRestockingDetail.ApplicationId);
        Assert.Equal(fla.ApplicationReference, result.Value.FellingAndRestockingDetail.ApplicationReference);

        Assert.Equal(result.Value.FellingLicenceApplicationSummary.DetailsList, result.Value.FellingAndRestockingDetail.DetailsList);

        Assert.Equal(Convert.ToBase64String([1,2,3]), result.Value.FellingMapBase64);
        Assert.Equal(Convert.ToBase64String([4,5,6]), result.Value.RestockingMapBase64);

        FlaRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);

        ActivityFeedItemProvider.Verify(x => x.RetrieveAllRelevantActivityFeedItemsAsync(
            It.Is<ActivityFeedItemProviderModel>(m => m.FellingLicenceId == applicationId && m.FellingLicenceReference == fla.ApplicationReference && m.ItemTypes.Single() == ActivityFeedItemType.SiteVisitComment),
            ActorType.InternalUser,
            It.IsAny<CancellationToken>()), Times.Once);
        ActivityFeedItemProvider.VerifyNoOtherCalls();

        WoodlandOwnerService.Verify(x => x.RetrieveWoodlandOwnerByIdAsync(fla.WoodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once);
        WoodlandOwnerService.Verify(x => x.RetrieveWoodlandOwnerByIdAsync(woodlandOwner.Id.Value, It.IsAny<CancellationToken>()), Times.Once);
        WoodlandOwnerService.VerifyNoOtherCalls();

        MockAgentAuthorityService.Verify(x => x.GetAgencyForWoodlandOwnerAsync(fla.WoodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once);
        MockAgentAuthorityService.Verify(x => x.GetAgencyForWoodlandOwnerAsync(woodlandOwner.Id.Value, It.IsAny<CancellationToken>()), Times.Once);
        MockAgentAuthorityService.VerifyNoOtherCalls();

        _foresterServices.Verify(x => x.GenerateImage_MultipleCompartmentsAsync(
            It.Is<List<InternalCompartmentDetails<BaseShape>>>(s => s.Single().CompartmentLabel == fellingCpt.CompartmentNumber && JsonConvert.SerializeObject(s.Single().ShapeGeometry) == serializedPolygon),
            It.IsAny<CancellationToken>(), 3000, MapGeneration.Other, ""), Times.Once);
        _foresterServices.Verify(x => x.GenerateImage_MultipleCompartmentsAsync(
            It.Is<List<InternalCompartmentDetails<BaseShape>>>(s => s.Single().CompartmentLabel == restockingCpt.CompartmentNumber && JsonConvert.SerializeObject(s.Single().ShapeGeometry) == serializedPolygon),
            It.IsAny<CancellationToken>(), 3000, MapGeneration.Other, ""), Times.Once);

        _foresterServices.VerifyNoOtherCalls();
    }


    [Theory, AutoMoqData]
    public async Task ShouldReturnSuccessWhenAbleToRetrieveDetailsWithNoRestocking(
        Guid applicationId,
        string hostingPage,
        SiteVisitModel siteVisit,
        FellingLicenceApplication fla,
        WoodlandOwnerModel woodlandOwner,
        AgencyModel agency,
        UserAccount externalApplicant,
        Flo.Services.InternalUsers.Entities.UserAccount.UserAccount internalUser,
        List<ActivityFeedItemModel> siteVisitComments)
    {
        var sut = CreateSut();

        using var fellingImageBytes = new MemoryStream([1, 2, 3]);
        using var restockingImageBytes = new MemoryStream([4, 5, 6]);

        var polygon = new Polygon();
        var serializedPolygon = JsonConvert.SerializeObject(polygon);

        var felling = Fixture.Build<ProposedFellingDetail>()
            .With(x => x.LinkedPropertyProfile, fla.LinkedPropertyProfile)
            .With(x => x.LinkedPropertyProfileId, fla.LinkedPropertyProfile.Id)
            .With(x => x.PropertyProfileCompartmentId, fla.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments[0].CompartmentId)
            .With(x => x.FellingSpecies, [])
            .Create();

        felling.ProposedRestockingDetails = [];

        fla.LinkedPropertyProfile.ProposedFellingDetails = [felling];

        fla.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments.ForEach(x => x.GISData = serializedPolygon);

        WoodlandOfficerReviewService
            .Setup(x => x.GetSiteVisitDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(Maybe.From(siteVisit)));

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

        ActivityFeedItemProvider
            .Setup(x => x.RetrieveAllRelevantActivityFeedItemsAsync(It.IsAny<ActivityFeedItemProviderModel>(), It.IsAny<ActorType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IList<ActivityFeedItemModel>>(siteVisitComments));

        MockAgentAuthorityService
            .Setup(x => x.GetAgencyForWoodlandOwnerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(agency));

        var fellingCpt = fla.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments[0];

        _foresterServices.Setup(x =>
                x.GenerateImage_MultipleCompartmentsAsync(It.Is<List<InternalCompartmentDetails<BaseShape>>>(s => s.Single().CompartmentLabel == fellingCpt.CompartmentNumber), It.IsAny<CancellationToken>(), It.IsAny<int>(), It.IsAny<MapGeneration>(), It.IsAny<string>()))
            .ReturnsAsync(Result.Success<Stream>(fellingImageBytes));

        var result = await sut.GetSiteVisitSummaryAsync(applicationId, hostingPage, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(woodlandOwner.ContactName, result.Value.ApplicationOwner.WoodlandOwner.ContactName);
        Assert.Equal(woodlandOwner.ContactEmail, result.Value.ApplicationOwner.WoodlandOwner.ContactEmail);
        Assert.Equivalent(woodlandOwner.ContactAddress, result.Value.ApplicationOwner.WoodlandOwner.ContactAddress);
        Assert.Equal(agency, result.Value.ApplicationOwner.Agency);

        Assert.Equal(CaseNoteType.SiteVisitComment, result.Value.SiteVisitComments.DefaultCaseNoteFilter);
        Assert.False(result.Value.SiteVisitComments.ShowAddCaseNote);
        Assert.Equal(hostingPage, result.Value.SiteVisitComments.HostingPage);
        Assert.Equivalent(siteVisitComments, result.Value.SiteVisitComments.ActivityFeedItemModels);

        Assert.Equal(applicationId, result.Value.FellingAndRestockingDetail.ApplicationId);
        Assert.Equal(fla.ApplicationReference, result.Value.FellingAndRestockingDetail.ApplicationReference);

        Assert.Equal(result.Value.FellingLicenceApplicationSummary.DetailsList, result.Value.FellingAndRestockingDetail.DetailsList);

        Assert.Equal(Convert.ToBase64String([1, 2, 3]), result.Value.FellingMapBase64);
        Assert.Null(result.Value.RestockingMapBase64);

        FlaRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);

        ActivityFeedItemProvider.Verify(x => x.RetrieveAllRelevantActivityFeedItemsAsync(
            It.Is<ActivityFeedItemProviderModel>(m => m.FellingLicenceId == applicationId && m.FellingLicenceReference == fla.ApplicationReference && m.ItemTypes.Single() == ActivityFeedItemType.SiteVisitComment),
            ActorType.InternalUser,
            It.IsAny<CancellationToken>()), Times.Once);
        ActivityFeedItemProvider.VerifyNoOtherCalls();

        WoodlandOwnerService.Verify(x => x.RetrieveWoodlandOwnerByIdAsync(fla.WoodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once);
        WoodlandOwnerService.Verify(x => x.RetrieveWoodlandOwnerByIdAsync(woodlandOwner.Id.Value, It.IsAny<CancellationToken>()), Times.Once);
        WoodlandOwnerService.VerifyNoOtherCalls();

        MockAgentAuthorityService.Verify(x => x.GetAgencyForWoodlandOwnerAsync(fla.WoodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once);
        MockAgentAuthorityService.Verify(x => x.GetAgencyForWoodlandOwnerAsync(woodlandOwner.Id.Value, It.IsAny<CancellationToken>()), Times.Once);
        MockAgentAuthorityService.VerifyNoOtherCalls();

        _foresterServices.Verify(x => x.GenerateImage_MultipleCompartmentsAsync(
            It.Is<List<InternalCompartmentDetails<BaseShape>>>(s => s.Single().CompartmentLabel == fellingCpt.CompartmentNumber && JsonConvert.SerializeObject(s.Single().ShapeGeometry) == serializedPolygon),
            It.IsAny<CancellationToken>(), 3000, MapGeneration.Other, ""), Times.Once);

        _foresterServices.VerifyNoOtherCalls();
    }

    private SiteVisitUseCase CreateSut()
    {
        ResetMocks();
        
        return new SiteVisitUseCase(
            InternalUserAccountService.Object,
            ExternalUserAccountRepository.Object,
            FlaRepository.Object,
            WoodlandOwnerService.Object,
            WoodlandOfficerReviewService.Object,
            UpdateWoodlandOfficerReviewService.Object,
            ActivityFeedItemProvider.Object,
            AuditingService.Object,
            MockAgentAuthorityService.Object,
            GetConfiguredFcAreas.Object,
            MockAddDocumentService.Object,
            MockRemoveDocumentService.Object,
            _foresterServices.Object,
            RequestContext,
            WoodlandOfficerReviewSubStatusService.Object,
            new NullLogger<SiteVisitUseCase>());
    }
}