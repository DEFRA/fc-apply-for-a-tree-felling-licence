using AutoFixture;
using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Tests.Common;
using Moq;
using Xunit;
using System.Linq;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.Gis.Models.Esri.Responses.Layers;
using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;
using LinqKit;
using Newtonsoft.Json;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services;

public partial class GetWoodlandOfficerReviewServiceTests
{
    [Theory, AutoData]
    public async Task GetApplicationDetailsToSendToPublicRegisterWhenRepositoryThrows(Guid applicationId)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception());

        var result = await sut.GetApplicationDetailsToSendToPublicRegisterAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsFailure);

        _fellingLicenceApplicationRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
        
        _foresterServices.VerifyNoOtherCalls();
    }

    [Theory, CombinatorialData]
    public async Task GetApplicationDetailsToSendToPublicRegisterWhenFlaDataMissing(
        bool withFla,
        bool withLinkedPropertyProfile, 
        bool withSubmittedProperty)
    {
        if (withFla && withLinkedPropertyProfile && withSubmittedProperty)
        {
            // This test is for missing data scenarios only
            return;
        }

        var applicationId = Guid.NewGuid();

        var application = withFla ? _fixture.Create<FellingLicenceApplication>() : null;

        if (withFla)
        {
            if (!withLinkedPropertyProfile)
            {
                application!.LinkedPropertyProfile = null;
            }
            if (!withSubmittedProperty)
            {
                application!.SubmittedFlaPropertyDetail = null;
            }
        }

        var sut = CreateSut();

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(withFla ? Maybe<FellingLicenceApplication>.From(application!) : Maybe<FellingLicenceApplication>.None);

        var result = await sut.GetApplicationDetailsToSendToPublicRegisterAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsFailure);

        _fellingLicenceApplicationRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();

        _foresterServices.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task GetApplicationDetailsToSendToPublicRegisterWhenConfirmedFAndRNotCompleteWithLocalAuthorityFound(
        Guid applicationId,
        FellingLicenceApplication application,
        Point centrePoint,
        LocalAuthority authority,
        Polygon gisData)
    {
        application.WoodlandOfficerReview!.ConfirmedFellingAndRestockingComplete = false;
        application.CentrePoint = JsonConvert.SerializeObject(centrePoint);
        application.AssigneeHistories.ForEach(x => x.TimestampUnassigned = null);
        application.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments.ForEach(c => c.GISData = JsonConvert.SerializeObject(gisData));

        var expectedTotalArea =
            application.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments?.Sum(x => x.TotalHectares) ?? 0;

        double expectedCompartmentArea = 0;
        application.LinkedPropertyProfile.ProposedFellingDetails
            .ForEach(x => expectedCompartmentArea += x.AreaToBeFelled);

        var expectedCompartmentDetails = application.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments?
            .Select(x => x.ToInternalCompartmentDetails())
            .ToList() ?? [];

        var expectedAssignedInternalUserIds = application.AssigneeHistories
            .Where(x => x.Role != AssignedUserRole.Applicant && x.Role != AssignedUserRole.Author && x.TimestampUnassigned == null)
            .Select(x => x.AssignedUserId)
            .ToList();

        var sut = CreateSut();

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));

        _foresterServices.Setup(x => x.GetLocalAuthorityAsync(It.IsAny<Point>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(authority));

        var result = await sut.GetApplicationDetailsToSendToPublicRegisterAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(application.ApplicationReference, result.Value.CaseReference);
        Assert.Equal(application.SubmittedFlaPropertyDetail.Name, result.Value.PropertyName);
        Assert.Equal(application.OSGridReference, result.Value.GridReference);
        Assert.Equal(application.SubmittedFlaPropertyDetail.NearestTown, result.Value.NearestTown);
        Assert.Equal(authority.Name, result.Value.LocalAuthority);
        Assert.Equal(application.AdministrativeRegion, result.Value.AdminRegion);
        Assert.Equal(expectedTotalArea, result.Value.TotalArea);
        Assert.Equal(expectedTotalArea - expectedCompartmentArea, result.Value.OpenGroundArea);
        Assert.Equivalent(centrePoint, result.Value.CentrePoint);
        Assert.Equivalent(expectedCompartmentDetails, result.Value.Compartments);
        Assert.Equivalent(expectedAssignedInternalUserIds, result.Value.AssignedInternalUserIds);


        _fellingLicenceApplicationRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();

        _foresterServices.Verify(x => x.GetLocalAuthorityAsync(It.Is<Point>(
            c => c.X == centrePoint.X && c.Y == centrePoint.Y), It.IsAny<CancellationToken>()), Times.Once);
        _foresterServices.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task GetApplicationDetailsToSendToPublicRegisterWhenConfirmedFAndRNotCompleteWithLocalAuthoritySearchFails(
        Guid applicationId,
        FellingLicenceApplication application,
        Point centrePoint,
        Polygon gisData)
    {
        application.WoodlandOfficerReview!.ConfirmedFellingAndRestockingComplete = false;
        application.CentrePoint = JsonConvert.SerializeObject(centrePoint);
        application.AssigneeHistories.ForEach(x => x.TimestampUnassigned = null);
        application.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments.ForEach(c => c.GISData = JsonConvert.SerializeObject(gisData));

        var expectedTotalArea =
            application.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments?.Sum(x => x.TotalHectares) ?? 0;

        double expectedCompartmentArea = 0;
        application.LinkedPropertyProfile.ProposedFellingDetails
            .ForEach(x => expectedCompartmentArea += x.AreaToBeFelled);

        var expectedCompartmentDetails = application.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments?
            .Select(x => x.ToInternalCompartmentDetails())
            .ToList() ?? [];

        var expectedAssignedInternalUserIds = application.AssigneeHistories
            .Where(x => x.Role != AssignedUserRole.Applicant && x.Role != AssignedUserRole.Author && x.TimestampUnassigned == null)
            .Select(x => x.AssignedUserId)
            .ToList();

        var sut = CreateSut();

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));

        _foresterServices.Setup(x => x.GetLocalAuthorityAsync(It.IsAny<Point>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<LocalAuthority>("error"));

        var result = await sut.GetApplicationDetailsToSendToPublicRegisterAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(application.ApplicationReference, result.Value.CaseReference);
        Assert.Equal(application.SubmittedFlaPropertyDetail.Name, result.Value.PropertyName);
        Assert.Equal(application.OSGridReference, result.Value.GridReference);
        Assert.Equal(application.SubmittedFlaPropertyDetail.NearestTown, result.Value.NearestTown);
        Assert.Equal(string.Empty, result.Value.LocalAuthority);
        Assert.Equal(application.AdministrativeRegion, result.Value.AdminRegion);
        Assert.Equal(expectedTotalArea, result.Value.TotalArea);
        Assert.Equal(expectedTotalArea - expectedCompartmentArea, result.Value.OpenGroundArea);
        Assert.Equivalent(centrePoint, result.Value.CentrePoint);
        Assert.Equivalent(expectedCompartmentDetails, result.Value.Compartments);
        Assert.Equivalent(expectedAssignedInternalUserIds, result.Value.AssignedInternalUserIds);


        _fellingLicenceApplicationRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();

        _foresterServices.Verify(x => x.GetLocalAuthorityAsync(It.Is<Point>(
            c => c.X == centrePoint.X && c.Y == centrePoint.Y), It.IsAny<CancellationToken>()), Times.Once);
        _foresterServices.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task GetApplicationDetailsToSendToPublicRegisterWhenConfirmedFAndRNotCompleteWithLocalAuthorityNotFound(
        Guid applicationId,
        FellingLicenceApplication application,
        Point centrePoint,
        Polygon gisData)
    {
        application.WoodlandOfficerReview!.ConfirmedFellingAndRestockingComplete = false;
        application.CentrePoint = JsonConvert.SerializeObject(centrePoint);
        application.AssigneeHistories.ForEach(x => x.TimestampUnassigned = null);
        application.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments.ForEach(c => c.GISData = JsonConvert.SerializeObject(gisData));

        var expectedTotalArea =
            application.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments?.Sum(x => x.TotalHectares) ?? 0;

        double expectedCompartmentArea = 0;
        application.LinkedPropertyProfile.ProposedFellingDetails
            .ForEach(x => expectedCompartmentArea += x.AreaToBeFelled);

        var expectedCompartmentDetails = application.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments?
            .Select(x => x.ToInternalCompartmentDetails())
            .ToList() ?? [];

        var expectedAssignedInternalUserIds = application.AssigneeHistories
            .Where(x => x.Role != AssignedUserRole.Applicant && x.Role != AssignedUserRole.Author && x.TimestampUnassigned == null)
            .Select(x => x.AssignedUserId)
            .ToList();

        var sut = CreateSut();

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));

        _foresterServices.Setup(x => x.GetLocalAuthorityAsync(It.IsAny<Point>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<LocalAuthority>(null!));

        var result = await sut.GetApplicationDetailsToSendToPublicRegisterAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(application.ApplicationReference, result.Value.CaseReference);
        Assert.Equal(application.SubmittedFlaPropertyDetail.Name, result.Value.PropertyName);
        Assert.Equal(application.OSGridReference, result.Value.GridReference);
        Assert.Equal(application.SubmittedFlaPropertyDetail.NearestTown, result.Value.NearestTown);
        Assert.Equal(string.Empty, result.Value.LocalAuthority);
        Assert.Equal(application.AdministrativeRegion, result.Value.AdminRegion);
        Assert.Equal(expectedTotalArea, result.Value.TotalArea);
        Assert.Equal(expectedTotalArea - expectedCompartmentArea, result.Value.OpenGroundArea);
        Assert.Equivalent(centrePoint, result.Value.CentrePoint);
        Assert.Equivalent(expectedCompartmentDetails, result.Value.Compartments);
        Assert.Equivalent(expectedAssignedInternalUserIds, result.Value.AssignedInternalUserIds);


        _fellingLicenceApplicationRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();

        _foresterServices.Verify(x => x.GetLocalAuthorityAsync(It.Is<Point>(
            c => c.X == centrePoint.X && c.Y == centrePoint.Y), It.IsAny<CancellationToken>()), Times.Once);
        _foresterServices.VerifyNoOtherCalls();
    }


    [Theory, AutoMoqData]
    public async Task GetApplicationDetailsToSendToPublicRegisterWhenConfirmedFAndRNotCompleteWithNoCentrepoint(
        Guid applicationId,
        FellingLicenceApplication application,
        Polygon gisData)
    {
        application.WoodlandOfficerReview!.ConfirmedFellingAndRestockingComplete = false;
        application.CentrePoint = null;
        application.AssigneeHistories.ForEach(x => x.TimestampUnassigned = null);
        application.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments.ForEach(c => c.GISData = JsonConvert.SerializeObject(gisData));

        var expectedTotalArea =
            application.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments?.Sum(x => x.TotalHectares) ?? 0;

        double expectedCompartmentArea = 0;
        application.LinkedPropertyProfile.ProposedFellingDetails
            .ForEach(x => expectedCompartmentArea += x.AreaToBeFelled);

        var expectedCompartmentDetails = application.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments?
            .Select(x => x.ToInternalCompartmentDetails())
            .ToList() ?? [];

        var expectedAssignedInternalUserIds = application.AssigneeHistories
            .Where(x => x.Role != AssignedUserRole.Applicant && x.Role != AssignedUserRole.Author && x.TimestampUnassigned == null)
            .Select(x => x.AssignedUserId)
            .ToList();

        var sut = CreateSut();

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));

        var result = await sut.GetApplicationDetailsToSendToPublicRegisterAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(application.ApplicationReference, result.Value.CaseReference);
        Assert.Equal(application.SubmittedFlaPropertyDetail.Name, result.Value.PropertyName);
        Assert.Equal(application.OSGridReference, result.Value.GridReference);
        Assert.Equal(application.SubmittedFlaPropertyDetail.NearestTown, result.Value.NearestTown);
        Assert.Equal(string.Empty, result.Value.LocalAuthority);
        Assert.Equal(application.AdministrativeRegion, result.Value.AdminRegion);
        Assert.Equal(expectedTotalArea, result.Value.TotalArea);
        Assert.Equal(expectedTotalArea - expectedCompartmentArea, result.Value.OpenGroundArea);
        Assert.Null(result.Value.CentrePoint);
        Assert.Equivalent(expectedCompartmentDetails, result.Value.Compartments);
        Assert.Equivalent(expectedAssignedInternalUserIds, result.Value.AssignedInternalUserIds);


        _fellingLicenceApplicationRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();

        _foresterServices.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task GetApplicationDetailsToSendToPublicRegisterWhenConfirmedFAndRComplete(
        Guid applicationId,
        FellingLicenceApplication application,
        Point centrePoint,
        LocalAuthority authority,
        Polygon gisData)
    {
        application.WoodlandOfficerReview!.ConfirmedFellingAndRestockingComplete = true;
        application.CentrePoint = JsonConvert.SerializeObject(centrePoint);
        application.AssigneeHistories.ForEach(x => x.TimestampUnassigned = null);
        application.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments.ForEach(c => c.GISData = JsonConvert.SerializeObject(gisData));

        var expectedTotalArea =
            application.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments?.Sum(x => x.TotalHectares) ?? 0;

        double expectedCompartmentArea = 0;
        application.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments?
            .SelectMany(x => x.ConfirmedFellingDetails)
            .ForEach(x => expectedCompartmentArea += x.AreaToBeFelled);

        var expectedCompartmentDetails = application.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments?
            .Select(x => x.ToInternalCompartmentDetails())
            .ToList() ?? [];

        var expectedAssignedInternalUserIds = application.AssigneeHistories
            .Where(x => x.Role != AssignedUserRole.Applicant && x.Role != AssignedUserRole.Author && x.TimestampUnassigned == null)
            .Select(x => x.AssignedUserId)
            .ToList();

        var sut = CreateSut();

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));

        _foresterServices.Setup(x => x.GetLocalAuthorityAsync(It.IsAny<Point>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(authority));

        var result = await sut.GetApplicationDetailsToSendToPublicRegisterAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(application.ApplicationReference, result.Value.CaseReference);
        Assert.Equal(application.SubmittedFlaPropertyDetail.Name, result.Value.PropertyName);
        Assert.Equal(application.OSGridReference, result.Value.GridReference);
        Assert.Equal(application.SubmittedFlaPropertyDetail.NearestTown, result.Value.NearestTown);
        Assert.Equal(authority.Name, result.Value.LocalAuthority);
        Assert.Equal(application.AdministrativeRegion, result.Value.AdminRegion);
        Assert.Equal(expectedTotalArea, result.Value.TotalArea);
        Assert.Equal(expectedTotalArea - expectedCompartmentArea, result.Value.OpenGroundArea);
        Assert.Equivalent(centrePoint, result.Value.CentrePoint);
        Assert.Equivalent(expectedCompartmentDetails, result.Value.Compartments);
        Assert.Equivalent(expectedAssignedInternalUserIds, result.Value.AssignedInternalUserIds);


        _fellingLicenceApplicationRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();

        _foresterServices.Verify(x => x.GetLocalAuthorityAsync(It.Is<Point>(
            c => c.X == centrePoint.X && c.Y == centrePoint.Y), It.IsAny<CancellationToken>()), Times.Once);
        _foresterServices.VerifyNoOtherCalls();
    }
}