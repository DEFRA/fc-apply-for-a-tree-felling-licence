using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.WoodlandOfficerReview;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.ConditionsBuilder.Models;
using Forestry.Flo.Services.ConditionsBuilder.Services;
using Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Services.Notifications.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NodaTime;
using System.Text.Json;

namespace Forestry.Flo.Internal.Web.Tests.Services.WoodlandOfficerReviewUseCase;

public class ConditionsUseCaseGenerateConditionsTests
{
    private readonly Mock<IGetWoodlandOfficerReviewService> _getWoodlandOfficerReviewService = new();
    private readonly Mock<ICalculateConditions> _conditionsService = new();
    private readonly Mock<IUpdateWoodlandOfficerReviewService> _updateWoodlandOfficerReviewService = new();
    private readonly Mock<IAuditService<ConditionsUseCase>> _auditService = new();
    private readonly Mock<IUpdateConfirmedFellingAndRestockingDetailsService> _fellingAndRestockingService = new();
    private readonly Mock<IAgentAuthorityService> _agentAuthorityService = new();

    private readonly Mock<IGetConfiguredFcAreas> _getConfiguredFcAreas = new();
    private const string AdminHubAddress = "admin hub address";

    private readonly string RequestContextCorrelationId = Guid.NewGuid().ToString();
    private readonly Guid RequestContextUserId = Guid.NewGuid();

    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Theory, AutoMoqData]
    public async Task WhenCannotRetrieveFellingAndRestocking(
        Guid applicationId,
        Guid userId,
        string error)
    {
        var user = new InternalUser(
            UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: userId));

        var sut = CreateSut();

        _fellingAndRestockingService
            .Setup(x => x.RetrieveConfirmedFellingAndRestockingDetailModelAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<CombinedConfirmedFellingAndRestockingDetailRecord>(error));

        var result = await sut.GenerateConditionsAsync(applicationId, user, CancellationToken.None);

        Assert.True(result.IsFailure);

        _fellingAndRestockingService.Verify(x => x.RetrieveConfirmedFellingAndRestockingDetailModelAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingAndRestockingService.VerifyNoOtherCalls();

        _conditionsService.VerifyNoOtherCalls();

        _auditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.UpdateWoodlandOfficerReviewFailure
                && a.ActorType == ActorType.InternalUser
                && a.UserId == userId
                && a.SourceEntityId == applicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == RequestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    section = "Conditions",
                    error = error
                }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        _auditService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenCannotGenerateConditions(
        Guid applicationId,
        Guid userId,
        ConfirmedFellingAndRestockingDetailModel fellingAndRestocking,
        string error)
    {
        var user = new InternalUser(
            UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: userId));

        var sut = CreateSut();

        _fellingAndRestockingService
            .Setup(x => x.RetrieveConfirmedFellingAndRestockingDetailModelAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new CombinedConfirmedFellingAndRestockingDetailRecord(new List<ConfirmedFellingAndRestockingDetailModel> { fellingAndRestocking }, false)));
        _conditionsService
            .Setup(x => x.CalculateConditionsAsync(It.IsAny<CalculateConditionsRequest>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<ConditionsResponse>(error));

        var expectedRestockingOperations = new List<RestockingOperationDetails>();
        foreach (var felling in fellingAndRestocking.ConfirmedFellingDetailModels)
        {

            foreach (var restocking in felling.ConfirmedRestockingDetailModels)
            {
                foreach (var restockingConfirmedRestockingSpecy in restocking.ConfirmedRestockingSpecies)
                {
                    restockingConfirmedRestockingSpecy.Species = "CLI";
                }

                expectedRestockingOperations.Add(new RestockingOperationDetails
                {
                    FellingCompartmentNumber = fellingAndRestocking.CompartmentNumber,
                    FellingSubcompartmentName = fellingAndRestocking.SubCompartmentName,
                    FellingOperationType = felling.OperationType.ToConditionsFellingType(),

                    RestockingSubmittedFlaPropertyCompartmentId = restocking.CompartmentId,
                    RestockingCompartmentNumber = restocking.CompartmentNumber,
                    RestockingSubcompartmentName = restocking.SubCompartmentName,
                    RestockingProposalType = restocking.RestockingProposal.ToConditionsRestockingType(),

                    PercentNaturalRegeneration = restocking.PercentNaturalRegeneration ?? 0,
                    PercentOpenSpace = restocking.PercentOpenSpace ?? 0,
                    RestockingDensity = restocking.RestockingDensity,
                    TotalRestockingArea = restocking.Area ?? 0,
                    RestockingSpecies = restocking.ConfirmedRestockingSpecies?.Select(y => new RestockingSpecies
                    {
                        SpeciesCode = y.Species,
                        SpeciesName = "Common lime",
                        Percentage = y.Percentage ?? 0
                    }).ToList() ?? new List<RestockingSpecies>(0)
                });
            }
        }

        var result = await sut.GenerateConditionsAsync(applicationId, user, CancellationToken.None);

        Assert.True(result.IsFailure);

        _fellingAndRestockingService.Verify(x => x.RetrieveConfirmedFellingAndRestockingDetailModelAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingAndRestockingService.VerifyNoOtherCalls();

        _conditionsService.Verify(x => x.CalculateConditionsAsync(
            It.Is<CalculateConditionsRequest>(c => c.IsDraft == false
            && expectedRestockingOperations.All(e => 
                c.RestockingOperations.Any(z => z.FellingCompartmentNumber == e.FellingCompartmentNumber
                    && z.FellingCompartmentNumber == e.FellingCompartmentNumber
                    && z.FellingSubcompartmentName == e.FellingSubcompartmentName
                    && z.FellingOperationType.ToString() == e.FellingOperationType.ToString()
                    && z.RestockingSubmittedFlaPropertyCompartmentId == e.RestockingSubmittedFlaPropertyCompartmentId
                    && z.PercentNaturalRegeneration == e.PercentNaturalRegeneration
                    && z.PercentOpenSpace == e.PercentOpenSpace
                    && z.RestockingDensity == e.RestockingDensity
                    && z.RestockingProposalType.ToString() == e.RestockingProposalType.ToString()
                    && z.TotalRestockingArea == e.TotalRestockingArea
                    && e.RestockingSpecies.All(r => z.RestockingSpecies.Any(zr =>
                        zr.SpeciesCode == r.SpeciesCode
                        && zr.SpeciesName == r.SpeciesName
                        && zr.Percentage == r.Percentage))
            ))), userId, It.IsAny<CancellationToken>()), Times.Once);
        _conditionsService.VerifyNoOtherCalls();

        _auditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.UpdateWoodlandOfficerReviewFailure
                && a.ActorType == ActorType.InternalUser
                && a.UserId == userId
                && a.SourceEntityId == applicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == RequestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    section = "Conditions",
                    error = error
                }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        _auditService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenGeneratesConditions(
        Guid applicationId,
        Guid userId,
        ConfirmedFellingAndRestockingDetailModel fellingAndRestocking,
        ConditionsResponse response)
    {
        var user = new InternalUser(
            UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId: userId));

        var sut = CreateSut();

        _fellingAndRestockingService
            .Setup(x => x.RetrieveConfirmedFellingAndRestockingDetailModelAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new CombinedConfirmedFellingAndRestockingDetailRecord(new List<ConfirmedFellingAndRestockingDetailModel> { fellingAndRestocking }, false)));
        _conditionsService
            .Setup(x => x.CalculateConditionsAsync(It.IsAny<CalculateConditionsRequest>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(response));
        
        var expectedRestockingOperations = new List<RestockingOperationDetails>();
        foreach (var felling in fellingAndRestocking.ConfirmedFellingDetailModels)
        {
            foreach (var restocking in felling.ConfirmedRestockingDetailModels)
            {
                foreach (var restockingConfirmedRestockingSpecy in restocking.ConfirmedRestockingSpecies)
                {
                    restockingConfirmedRestockingSpecy.Species = "CLI";
                }

                expectedRestockingOperations.Add(new RestockingOperationDetails
                {
                    FellingCompartmentNumber = fellingAndRestocking.CompartmentNumber,
                    FellingSubcompartmentName = fellingAndRestocking.SubCompartmentName,
                    FellingOperationType = felling.OperationType.ToConditionsFellingType(),

                    RestockingSubmittedFlaPropertyCompartmentId = restocking.CompartmentId,
                    RestockingCompartmentNumber = restocking.CompartmentNumber,
                    RestockingSubcompartmentName = restocking.SubCompartmentName,
                    RestockingProposalType = restocking.RestockingProposal.ToConditionsRestockingType(),

                    PercentNaturalRegeneration = restocking.PercentNaturalRegeneration ?? 0,
                    PercentOpenSpace = restocking.PercentOpenSpace ?? 0,
                    RestockingDensity = restocking.RestockingDensity,
                    TotalRestockingArea = restocking.Area ?? 0,
                    RestockingSpecies = restocking.ConfirmedRestockingSpecies?.Select(y => new RestockingSpecies
                    {
                        SpeciesCode = y.Species,
                        SpeciesName = "Common lime",
                        Percentage = y.Percentage ?? 0
                    }).ToList() ?? new List<RestockingSpecies>(0)
                });
            }
        }

        var result = await sut.GenerateConditionsAsync(applicationId, user, CancellationToken.None);

        Assert.True(result.IsSuccess);

        _fellingAndRestockingService.Verify(x => x.RetrieveConfirmedFellingAndRestockingDetailModelAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingAndRestockingService.VerifyNoOtherCalls();

        _conditionsService.Verify(x => x.CalculateConditionsAsync(
            It.Is<CalculateConditionsRequest>(c => c.IsDraft == false
            && expectedRestockingOperations.All(e =>
                c.RestockingOperations.Any(z => z.FellingCompartmentNumber == e.FellingCompartmentNumber
                    && z.FellingCompartmentNumber == e.FellingCompartmentNumber
                    && z.FellingSubcompartmentName == e.FellingSubcompartmentName
                    && z.FellingOperationType.ToString() == e.FellingOperationType.ToString()
                    && z.RestockingSubmittedFlaPropertyCompartmentId == e.RestockingSubmittedFlaPropertyCompartmentId
                    && z.PercentNaturalRegeneration == e.PercentNaturalRegeneration
                    && z.PercentOpenSpace == e.PercentOpenSpace
                    && z.RestockingDensity == e.RestockingDensity
                    && z.RestockingProposalType.ToString() == e.RestockingProposalType.ToString()
                    && z.TotalRestockingArea == e.TotalRestockingArea
                    && e.RestockingSpecies.All(r => z.RestockingSpecies.Any(zr =>
                        zr.SpeciesCode == r.SpeciesCode
                        && zr.SpeciesName == r.SpeciesName
                        && zr.Percentage == r.Percentage))
            ))), userId, It.IsAny<CancellationToken>()), Times.Once);
        _conditionsService.VerifyNoOtherCalls();

        _auditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.EventName == AuditEvents.UpdateWoodlandOfficerReview
                && a.ActorType == ActorType.InternalUser
                && a.UserId == userId
                && a.SourceEntityId == applicationId
                && a.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && a.CorrelationId == RequestContextCorrelationId
                && JsonSerializer.Serialize(a.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    section = "Conditions"
                }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        _auditService.VerifyNoOtherCalls();
    }

    private ConditionsUseCase CreateSut()
    {
        var user = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(
            localAccountId: RequestContextUserId,
            accountTypeInternal: AccountTypeInternal.WoodlandOfficer);
        var requestContext = new RequestContext(
            RequestContextCorrelationId,
            new RequestUserModel(user));

        _getWoodlandOfficerReviewService.Reset();
        _conditionsService.Reset();
        _updateWoodlandOfficerReviewService.Reset();
        _auditService.Reset();
        _fellingAndRestockingService.Reset();
        _agentAuthorityService.Reset();
        _getConfiguredFcAreas.Reset();

        _getConfiguredFcAreas.Setup(x => x.TryGetAdminHubAddress(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AdminHubAddress);

        return new ConditionsUseCase(
            new Mock<IUserAccountService>().Object,
            new Mock<IRetrieveUserAccountsService>().Object,
            new Mock<IFellingLicenceApplicationInternalRepository>().Object,
            new Mock<IRetrieveWoodlandOwners>().Object,
            _getWoodlandOfficerReviewService.Object,
            _updateWoodlandOfficerReviewService.Object,
            _auditService.Object,
            requestContext,
            _conditionsService.Object,
            _fellingAndRestockingService.Object,
            new Mock<ISendNotifications>().Object,
            _agentAuthorityService.Object,
            _getConfiguredFcAreas.Object,
            new Mock<IClock>().Object,
            new OptionsWrapper<ExternalApplicantSiteOptions>(new ExternalApplicantSiteOptions()),
            new NullLogger<ConditionsUseCase>());
    }
}