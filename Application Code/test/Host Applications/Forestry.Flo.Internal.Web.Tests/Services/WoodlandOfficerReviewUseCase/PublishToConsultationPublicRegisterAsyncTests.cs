using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.WoodlandOfficerReview;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Configuration;
using Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;
using Forestry.Flo.Services.Gis.Models.Esri.Responses.Layers;
using Forestry.Flo.Services.Gis.Models.Internal;
using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;
using Forestry.Flo.Services.Notifications.Entities;
using Forestry.Flo.Services.Notifications.Models;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using System.Text.Json;

namespace Forestry.Flo.Internal.Web.Tests.Services.WoodlandOfficerReviewUseCase;

public class PublishToConsultationPublicRegisterAsyncTests : WoodlandOfficerReviewUseCaseTestsBase<PublicRegisterUseCase>
{
    [Theory, AutoData]
    public async Task OnSuccessfulUpdate(
        Guid applicationId,
        ApplicationDetailsForPublicRegisterModel publishModel,
        int esriId,
        int period,
        Flo.Services.InternalUsers.Models.UserAccountModel assignedUser)
    {
        var userPrincipal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: RequestContextUserId);
        var user = new InternalUser(userPrincipal);
        var sut = CreateSut();

        WoodlandOfficerReviewService
            .Setup(x => x.GetApplicationDetailsToSendToPublicRegisterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(publishModel));
        PublicRegisterService.Setup(x => x.AddCaseToConsultationRegisterAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<double?>(),
                It.IsAny<double?>(), It.IsAny<double?>(), It.IsAny<double?>(),
                It.IsAny<List<InternalCompartmentDetails<Polygon>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(esriId));
        UpdateWoodlandOfficerReviewService
            .Setup(x => x.PublishedToPublicRegisterAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        InternalUserAccountService.Setup(x =>
                x.RetrieveUserAccountsByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new List<Flo.Services.InternalUsers.Models.UserAccountModel> { assignedUser }));
        NotificationService.Setup(x => x.SendNotificationAsync(
                It.IsAny<InformFcStaffOfApplicationAddedToPublicRegisterDataModel>(), 
                It.IsAny<NotificationType>(),
                It.IsAny<NotificationRecipient>(),
                It.IsAny<NotificationRecipient[]?>(),
                It.IsAny<NotificationAttachment[]?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await sut.PublishToConsultationPublicRegisterAsync(applicationId, TimeSpan.FromDays(period), user, CancellationToken.None);

        Assert.True(result.IsSuccess);

        WoodlandOfficerReviewService.Verify(x => x.GetApplicationDetailsToSendToPublicRegisterAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        WoodlandOfficerReviewService.VerifyNoOtherCalls();

        PublicRegisterService.Verify(x => x.AddCaseToConsultationRegisterAsync(publishModel.CaseReference, publishModel.PropertyName,
            WoodlandOfficerReviewOptions.DefaultCaseTypeOnPublishToPublicRegister, 
            publishModel.GridReference, 
            publishModel.NearestTown,
            publishModel.LocalAuthority,
            publishModel.AdminRegion, 
            Now.ToDateTimeUtc(), 
            period, 
            publishModel.BroadleafArea, 
            publishModel.ConiferousArea,
            publishModel.OpenGroundArea, 
            publishModel.TotalArea, 
            publishModel.Compartments, 
            It.IsAny<CancellationToken>()), Times.Once);
        PublicRegisterService.VerifyNoOtherCalls();

        UpdateWoodlandOfficerReviewService.Verify(x => x.PublishedToPublicRegisterAsync(applicationId, RequestContextUserId, esriId, Now.ToDateTimeUtc(), TimeSpan.FromDays(period), It.IsAny<CancellationToken>()), Times.Once);
        UpdateWoodlandOfficerReviewService.VerifyNoOtherCalls();

        AuditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.UpdateWoodlandOfficerReview
                && a.ActorType == ActorType.InternalUser
                && a.UserId == RequestContextUserId
                && a.SourceEntityId == applicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == RequestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, SerializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    section = "Public Register",
                }, SerializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        AuditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.AddToConsultationPublicRegisterSuccess
                && a.ActorType == ActorType.InternalUser
                && a.UserId == RequestContextUserId
                && a.SourceEntityId == applicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == RequestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, SerializerOptions) ==
                    JsonSerializer.Serialize(new
                    {
                        publicationDate = Now.ToDateTimeUtc(),
                        esriId = esriId
                    }, SerializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        AuditingService.VerifyNoOtherCalls();

        NotificationService.Verify(x => x.SendNotificationAsync(
            It.Is<InformFcStaffOfApplicationAddedToPublicRegisterDataModel>(m =>
                m.PropertyName == publishModel.PropertyName
                && m.ApplicationReference == publishModel.CaseReference
                && m.PublishedDate == DateTimeDisplay.GetDateDisplayString(Now.ToDateTimeUtc())
                && m.ExpiryDate == DateTimeDisplay.GetDateDisplayString(Now.ToDateTimeUtc().AddDays(period))
                && m.AdminHubFooter == AdminHubAddress
                && m.Name == assignedUser.FullName),
            NotificationType.InformFcStaffOfApplicationAddedToConsultationPublicRegister,
            It.Is<NotificationRecipient>(r => r.Name == assignedUser.FullName && r.Address == assignedUser.Email),
            null,
            null,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
        NotificationService.VerifyNoOtherCalls();
    }
    
    [Theory, AutoData]
    public async Task OnUnsuccessfulGetPublishModel(
        Guid applicationId,
        int period,
        string error)
    {
        var userPrincipal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: RequestContextUserId);
        var user = new InternalUser(userPrincipal);
        var sut = CreateSut();

        WoodlandOfficerReviewService
            .Setup(x => x.GetApplicationDetailsToSendToPublicRegisterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<ApplicationDetailsForPublicRegisterModel>(error));

        var result = await sut.PublishToConsultationPublicRegisterAsync(applicationId, TimeSpan.FromDays(period), user, CancellationToken.None);

        Assert.True(result.IsFailure);

        WoodlandOfficerReviewService.Verify(x => x.GetApplicationDetailsToSendToPublicRegisterAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        WoodlandOfficerReviewService.VerifyNoOtherCalls();

        UpdateWoodlandOfficerReviewService.VerifyNoOtherCalls();

        PublicRegisterService.VerifyNoOtherCalls();

        AuditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.UpdateWoodlandOfficerReviewFailure
                && a.ActorType == ActorType.InternalUser
                && a.UserId == RequestContextUserId
                && a.SourceEntityId == applicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == RequestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, SerializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    section = "Public Register",
                    error = error
                }, SerializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        AuditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.AddToConsultationPublicRegisterFailure
                && a.ActorType == ActorType.InternalUser
                && a.UserId == RequestContextUserId
                && a.SourceEntityId == applicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == RequestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, SerializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    error = error
                }, SerializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        AuditingService.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task OnUnsuccessfulPublishToPublicRegister(
        Guid applicationId,
        ApplicationDetailsForPublicRegisterModel publishModel,
        int period,
        string error)
    {
        var userPrincipal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: RequestContextUserId);
        var user = new InternalUser(userPrincipal);
        var sut = CreateSut();

        WoodlandOfficerReviewService
            .Setup(x => x.GetApplicationDetailsToSendToPublicRegisterAsync(It.IsAny<Guid>(),  It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(publishModel));
        PublicRegisterService
            .Setup(x => x.AddCaseToConsultationRegisterAsync(
                 It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<double?>(),
                It.IsAny<double?>(), It.IsAny<double?>(), It.IsAny<double?>(),
                It.IsAny<List<InternalCompartmentDetails<Polygon>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<int>(error));

        var result = await sut.PublishToConsultationPublicRegisterAsync(applicationId, TimeSpan.FromDays(period), user, CancellationToken.None);

        Assert.True(result.IsFailure);

        WoodlandOfficerReviewService.Verify(x => x.GetApplicationDetailsToSendToPublicRegisterAsync(applicationId,  It.IsAny<CancellationToken>()), Times.Once);
        WoodlandOfficerReviewService.VerifyNoOtherCalls();

        PublicRegisterService.Verify(x => x.AddCaseToConsultationRegisterAsync(
            publishModel.CaseReference, 
            publishModel.PropertyName,
            WoodlandOfficerReviewOptions.DefaultCaseTypeOnPublishToPublicRegister, 
            publishModel.GridReference, 
            publishModel.NearestTown,
            publishModel.LocalAuthority,
            publishModel.AdminRegion, 
            Now.ToDateTimeUtc(),
            period, 
            publishModel.BroadleafArea, 
            publishModel.ConiferousArea,
            publishModel.OpenGroundArea, 
            publishModel.TotalArea, 
            publishModel.Compartments, 
            It.IsAny<CancellationToken>()), 
            Times.Once);
        PublicRegisterService.VerifyNoOtherCalls();

        UpdateWoodlandOfficerReviewService.VerifyNoOtherCalls();

        AuditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.UpdateWoodlandOfficerReviewFailure
                && a.ActorType == ActorType.InternalUser
                && a.UserId == RequestContextUserId
                && a.SourceEntityId == applicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == RequestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, SerializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    section = "Public Register",
                    error = error
                }, SerializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        AuditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.AddToConsultationPublicRegisterFailure
                && a.ActorType == ActorType.InternalUser
                && a.UserId == RequestContextUserId
                && a.SourceEntityId == applicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == RequestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, SerializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    error = error
                }, SerializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        AuditingService.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task OnUnsuccessfulDatabaseUpdate(
        Guid applicationId,
        ApplicationDetailsForPublicRegisterModel publishModel,
        int esriId,
        int period,
        string error)
    {
        var userPrincipal = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: RequestContextUserId);
        var user = new InternalUser(userPrincipal);
        var sut = CreateSut();

        _foresterServices.Setup(x => x.GetLocalAuthorityAsync(It.IsAny<Point>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<LocalAuthority>(new LocalAuthority(){Code = "LACODE",Name = "LANAME"} ));

        WoodlandOfficerReviewService
            .Setup(x => x.GetApplicationDetailsToSendToPublicRegisterAsync(It.IsAny<Guid>(),  It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(publishModel));

        PublicRegisterService.Setup(x => x.AddCaseToConsultationRegisterAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<double?>(),
                It.IsAny<double?>(), It.IsAny<double?>(), It.IsAny<double?>(),
                It.IsAny<List<InternalCompartmentDetails<Polygon>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(esriId));
        
        UpdateWoodlandOfficerReviewService
            .Setup(x => x.PublishedToPublicRegisterAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(error));

        var result = await sut.PublishToConsultationPublicRegisterAsync(applicationId, TimeSpan.FromDays(period), user, CancellationToken.None);

        Assert.True(result.IsFailure);

        WoodlandOfficerReviewService.Verify(x => x.GetApplicationDetailsToSendToPublicRegisterAsync(applicationId,  It.IsAny<CancellationToken>()), Times.Once);
        WoodlandOfficerReviewService.VerifyNoOtherCalls();

        PublicRegisterService.Verify(x => x.AddCaseToConsultationRegisterAsync(
            publishModel.CaseReference,
            publishModel.PropertyName,
            WoodlandOfficerReviewOptions.DefaultCaseTypeOnPublishToPublicRegister,
            publishModel.GridReference,
            publishModel.NearestTown,
            publishModel.LocalAuthority,
            publishModel.AdminRegion, 
            Now.ToDateTimeUtc(),
            period, 
            publishModel.BroadleafArea, 
            publishModel.ConiferousArea,
            publishModel.OpenGroundArea, 
            publishModel.TotalArea, 
            publishModel.Compartments, 
            It.IsAny<CancellationToken>()),
            Times.Once);
        PublicRegisterService.VerifyNoOtherCalls();

        UpdateWoodlandOfficerReviewService.Verify(x => x.PublishedToPublicRegisterAsync(applicationId, RequestContextUserId, esriId, Now.ToDateTimeUtc(), TimeSpan.FromDays(period), It.IsAny<CancellationToken>()), Times.Once);
        UpdateWoodlandOfficerReviewService.VerifyNoOtherCalls();

        AuditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.UpdateWoodlandOfficerReviewFailure
                && a.ActorType == ActorType.InternalUser
                && a.UserId == RequestContextUserId
                && a.SourceEntityId == applicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == RequestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, SerializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    section = "Public Register",
                    error = error
                }, SerializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        AuditingService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.AddToConsultationPublicRegisterFailure
                && a.ActorType == ActorType.InternalUser
                && a.UserId == RequestContextUserId
                && a.SourceEntityId == applicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == RequestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, SerializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    error = error
                }, SerializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);

        AuditingService.VerifyNoOtherCalls();
    }

    private PublicRegisterUseCase CreateSut()
    {
        ResetMocks();

        return new PublicRegisterUseCase(
            InternalUserAccountService.Object,
            ExternalUserAccountRepository.Object,
            FlaRepository.Object,
            WoodlandOwnerService.Object,
            WoodlandOfficerReviewService.Object,
            UpdateWoodlandOfficerReviewService.Object,
            PublicRegisterService.Object,
            NotificationHistoryService.Object,
            AuditingService.Object,
            MockAgentAuthorityService.Object,
            RequestContext,
            Clock.Object,
            NotificationService.Object,
            GetConfiguredFcAreas.Object,
            new OptionsWrapper<WoodlandOfficerReviewOptions>(WoodlandOfficerReviewOptions),
            WoodlandOfficerReviewSubStatusService.Object,
            new NullLogger<PublicRegisterUseCase>());
    }
}