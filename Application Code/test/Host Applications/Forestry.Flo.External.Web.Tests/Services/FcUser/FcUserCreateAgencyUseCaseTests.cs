using System.Security.Claims;
using System.Text.Json;
using AutoFixture;
using Forestry.Flo.External.Web.Services;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Tests.Common;
using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Models.Agency;
using Forestry.Flo.External.Web.Services.FcUser;
using Forestry.Flo.Services.Applicants.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using AgencyModel = Forestry.Flo.Services.Applicants.Models.AgencyModel;

namespace Forestry.Flo.External.Web.Tests.Services.FcUser;

public class FcUserCreateAgencyUseCaseTests
{
    private readonly Mock<IAgencyCreationService> _mockAgencyCreationService = new();
    private readonly Mock<IAuditService<FcUserCreateAgencyUseCase>> _mockAudit = new();
    
    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly Fixture _fixture = new();
    private ExternalApplicant? _externalUser;

    [Theory, AutoMoqData]
    public async Task ReturnsSuccess_WithExpectedModelContainingAgencyId_Add(
        FcUserAgencyCreationModel model,
        AddAgencyDetailsResponse response)
    {
        model.AgencyId = null; // ensure we're testing the add scenario

        // arrange
        var sut = CreateSut();

        _mockAgencyCreationService.Setup(r =>
                r.AddAgencyAsync(
                    It.IsAny<AddAgencyDetailsRequest>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(response));

        // act
        var result = await sut.ExecuteAsync(
            _externalUser!,
            model,
            CancellationToken.None);

        // assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        
        _mockAgencyCreationService.Verify(
            x => x.AddAgencyAsync(It.IsAny<AddAgencyDetailsRequest>(),
                It.IsAny<CancellationToken>()), Times.Once);

        _mockAudit.Verify(s =>
            s.PublishAuditEventAsync(It.Is<AuditEvent>(
                    e => e.EventName == AuditEvents.FcUserCreateAgencyEvent
                         && e.SourceEntityId == response.AgencyId
                         && e.UserId == _externalUser!.UserAccountId
                         && JsonSerializer.Serialize(e.AuditData, _options) ==
                         JsonSerializer.Serialize(new
                         {
                            response.AgencyId,
                            model.OrganisationName,
                            model.ContactName
                         }, _options)
                ),
                CancellationToken.None), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ReturnsFailure_WhenServiceCallFails_Add(FcUserAgencyCreationModel model)
    {
        model.AgencyId = null; // ensure we're testing the add scenario

        // arrange
        var sut = CreateSut();

        _mockAgencyCreationService.Setup(r =>
                r.AddAgencyAsync(
                    It.IsAny<AddAgencyDetailsRequest>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<AddAgencyDetailsResponse>("error-message"));

        // act
        var result = await sut.ExecuteAsync(
            _externalUser!,
            model,
            CancellationToken.None);

        // assert
        Assert.True(result.IsFailure);

        _mockAgencyCreationService.Verify(
            x => x.AddAgencyAsync(It.IsAny<AddAgencyDetailsRequest>(),
                It.IsAny<CancellationToken>()), Times.Once);

        _mockAudit.Verify(s =>
            s.PublishAuditEventAsync(It.Is<AuditEvent>(
                    e => e.EventName == AuditEvents.FcUserCreateAgencyFailureEvent
                         && e.SourceEntityId == null
                         && e.UserId == _externalUser!.UserAccountId
                         && JsonSerializer.Serialize(e.AuditData, _options) ==
                         JsonSerializer.Serialize(new
                         {
                             model.OrganisationName,
                             model.ContactName,
                             result.Error
                         }, _options)
                ),
                CancellationToken.None), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ReturnsFailure_WhenServiceThrowsException_Add(FcUserAgencyCreationModel model)
    {
        model.AgencyId = null; // ensure we're testing the add scenario

        // arrange
        var sut = CreateSut();
        var expectedException = new Exception("oops");
        _mockAgencyCreationService.Setup(r =>
                r.AddAgencyAsync(
                    It.IsAny<AddAgencyDetailsRequest>(),
                    It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // act
        var result = await sut.ExecuteAsync(
            _externalUser!,
            model,
            CancellationToken.None);

        // assert
        Assert.True(result.IsFailure);

        _mockAgencyCreationService.Verify(
            x => x.AddAgencyAsync(It.IsAny<AddAgencyDetailsRequest>(),
                It.IsAny<CancellationToken>()), Times.Once);

        _mockAudit.Verify(s =>
            s.PublishAuditEventAsync(It.Is<AuditEvent>(
                    e => e.EventName == AuditEvents.FcUserCreateAgencyFailureEvent
                         && e.SourceEntityId == null
                         && e.UserId == _externalUser!.UserAccountId
                         && JsonSerializer.Serialize(e.AuditData, _options) ==
                         JsonSerializer.Serialize(new
                         {
                             model.OrganisationName,
                             model.ContactName,
                             Error = expectedException.Message
                         }, _options)
                ),
                CancellationToken.None), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ReturnsFailure_WhenNotAnFcUser_Add(FcUserAgencyCreationModel model)
    {
        model.AgencyId = null; // ensure we're testing the add scenario

        // arrange
        var sut = CreateSut(asFcUser:false);

        // act
        var result = await sut.ExecuteAsync(
            _externalUser!,
            model,
            CancellationToken.None);

        // assert
        Assert.True(result.IsFailure);

        _mockAgencyCreationService.Verify(
            x => x.AddAgencyAsync(It.IsAny<AddAgencyDetailsRequest>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory, AutoMoqData]
    public async Task Throws_WhenUserNotSupplied_Add(FcUserAgencyCreationModel model)
    {
        model.AgencyId = null; // ensure we're testing the add scenario

        // arrange
        var sut = CreateSut();

        // act/assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await sut.ExecuteAsync(null!, model, CancellationToken.None));
    }

    [Theory, AutoMoqData]
    public async Task Throws_WhenUserNotSupplied_Update(FcUserAgencyCreationModel model)
    {
        model.AgencyId = null; // ensure we're testing the add scenario

        // arrange
        var sut = CreateSut();

        // act/assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await sut.ExecuteAsync(null!, model, CancellationToken.None));
    }

    [Theory, AutoMoqData]
    public async Task ReturnsFailure_WhenNotAnFcUser_Update(FcUserAgencyCreationModel model)
    {
        // arrange
        var sut = CreateSut(asFcUser: false);

        // act
        var result = await sut.ExecuteAsync(
            _externalUser!,
            model,
            CancellationToken.None);

        // assert
        Assert.True(result.IsFailure);

        _mockAgencyCreationService.Verify(
            x => x.AddAgencyAsync(It.IsAny<AddAgencyDetailsRequest>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory, AutoMoqData]
    public async Task ReturnsFailure_WhenServiceThrowsException_Update(FcUserAgencyCreationModel model)
    {
        // arrange
        var sut = CreateSut();
        var expectedException = new Exception("oops");
        _mockAgencyCreationService.Setup(r =>
                r.UpdateAgencyAsync(
                    It.IsAny<UpdateAgencyDetailsRequest>(),
                    It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // act
        var result = await sut.ExecuteAsync(
            _externalUser!,
            model,
            CancellationToken.None);

        // assert
        Assert.True(result.IsFailure);

        _mockAgencyCreationService.Verify(
            x => x.UpdateAgencyAsync(It.IsAny<UpdateAgencyDetailsRequest>(),
                It.IsAny<CancellationToken>()), Times.Once);

        _mockAudit.Verify(s =>
            s.PublishAuditEventAsync(It.Is<AuditEvent>(
                    e => e.EventName == AuditEvents.FcUserUpdateAgencyFailureEvent
                         && e.SourceEntityId == model.AgencyId.Value
                         && e.UserId == _externalUser!.UserAccountId
                         && JsonSerializer.Serialize(e.AuditData, _options) ==
                         JsonSerializer.Serialize(new
                         {
                             model.OrganisationName,
                             model.ContactName,
                             Error = expectedException.Message
                         }, _options)
                ),
                CancellationToken.None), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ReturnsFailure_WhenServiceCallFails_Update(FcUserAgencyCreationModel model)
    {
        
        // arrange
        var sut = CreateSut();

        _mockAgencyCreationService.Setup(r =>
                r.UpdateAgencyAsync(
                    It.IsAny<UpdateAgencyDetailsRequest>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("error-message"));

        // act
        var result = await sut.ExecuteAsync(
            _externalUser!,
            model,
            CancellationToken.None);

        // assert
        Assert.True(result.IsFailure);

        _mockAgencyCreationService.Verify(
            x => x.UpdateAgencyAsync(It.IsAny<UpdateAgencyDetailsRequest>(),
                It.IsAny<CancellationToken>()), Times.Once);

        _mockAudit.Verify(s =>
            s.PublishAuditEventAsync(It.Is<AuditEvent>(
                    e => e.EventName == AuditEvents.FcUserUpdateAgencyFailureEvent
                         && e.SourceEntityId == model.AgencyId.Value
                         && e.UserId == _externalUser!.UserAccountId
                         && JsonSerializer.Serialize(e.AuditData, _options) ==
                         JsonSerializer.Serialize(new
                         {
                             model.OrganisationName,
                             model.ContactName,
                             result.Error
                         }, _options)
                ),
                CancellationToken.None), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ReturnsSuccess_WithExpectedModelContainingAgencyId_Update(
        FcUserAgencyCreationModel model)
    {
       // arrange
        var sut = CreateSut();

        _mockAgencyCreationService.Setup(r =>
                r.UpdateAgencyAsync(
                    It.IsAny<UpdateAgencyDetailsRequest>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // act
        var result = await sut.ExecuteAsync(
            _externalUser!,
            model,
            CancellationToken.None);

        // assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        _mockAgencyCreationService.Verify(
            x => x.UpdateAgencyAsync(It.IsAny<UpdateAgencyDetailsRequest>(),
                It.IsAny<CancellationToken>()), Times.Once);

        _mockAudit.Verify(s =>
            s.PublishAuditEventAsync(It.Is<AuditEvent>(
                    e => e.EventName == AuditEvents.FcUserUpdateAgencyEvent
                         && e.SourceEntityId == model.AgencyId.Value
                         && e.UserId == _externalUser!.UserAccountId
                         && JsonSerializer.Serialize(e.AuditData, _options) ==
                         JsonSerializer.Serialize(new
                         {
                             model.OrganisationName,
                             model.ContactName
                         }, _options)
                ),
                CancellationToken.None), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task GetAgencyWhenAgencyNotFound(Guid agencyId, string error)
    {
        var sut = CreateSut();

        _mockAgencyCreationService.Setup(x => x.GetAgencyDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<AgencyModel>(error));

        var result = await sut.GetExistingAgencyDetailsForEditAsync(_externalUser, agencyId, CancellationToken.None);

        Assert.False(result.IsSuccess);

        _mockAgencyCreationService.Verify(x => x.GetAgencyDetailsAsync(agencyId, It.IsAny<CancellationToken>()), Times.Once);
    }

    private FcUserCreateAgencyUseCase CreateSut(bool asFcUser = true)
    {
        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<Guid>(),
            _fixture.Create<Guid>(),
            isFcUser:asFcUser);

        _externalUser = new ExternalApplicant(user);

        _mockAgencyCreationService.Reset();
        _mockAudit.Reset();

        return new FcUserCreateAgencyUseCase(
            _mockAgencyCreationService.Object,
            _mockAudit.Object,
            new RequestContext("test", new RequestUserModel(new ClaimsPrincipal())),
            new NullLogger<FcUserCreateAgencyUseCase>()
        );
    }
}
