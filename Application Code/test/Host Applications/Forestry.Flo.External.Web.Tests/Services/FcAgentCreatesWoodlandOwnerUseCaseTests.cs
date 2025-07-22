using System.Security.Claims;
using System.Text.Json;
using AutoFixture;
using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Services;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;

namespace Forestry.Flo.External.Web.Tests.Services;

public class FcAgentCreatesWoodlandOwnerUseCaseTests
{
    private readonly Mock<IWoodlandOwnerCreationService> _mockWoodlandOwnerCreationService = new();
    private readonly Mock<IAuditService<FcAgentCreatesWoodlandOwnerUseCase>> _mockAudit = new();
    private readonly Fixture _fixture = new();
    private ExternalApplicant? _externalApplicant;
    private const string Error = "some error";


    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private FcAgentCreatesWoodlandOwnerUseCase CreateSut(bool asFcUser = false, bool asRegisteredUser = true)
    {
        if (asFcUser && !asRegisteredUser)
        {
            throw new Exception("Cannot set test user as an FCUser and also as Unregistered!");
        }

        ClaimsPrincipal user;

        if (asRegisteredUser)
        {
            user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
                _fixture.Create<string>(),
                _fixture.Create<string>(),
                _fixture.Create<Guid>(),
                _fixture.Create<Guid>(),
                agencyId: _fixture.Create<Guid>(),
                woodlandOwnerName: _fixture.Create<string>(), 
                isFcUser:asFcUser);
        }
        else
        {
            user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
                _fixture.Create<string>(),
                _fixture.Create<string>(),
                agencyId: null,
                woodlandOwnerName: _fixture.Create<string>());
        }
        
        _externalApplicant = new ExternalApplicant(user);

        _mockWoodlandOwnerCreationService.Reset();
        _mockAudit.Reset();

        return new FcAgentCreatesWoodlandOwnerUseCase(
            _mockWoodlandOwnerCreationService.Object,
            _mockAudit.Object,
            new RequestContext("test", new RequestUserModel(_externalApplicant.Principal)),
            new NullLogger<FcAgentCreatesWoodlandOwnerUseCase>()
        );
    }

    [Theory, AutoMoqData]

    public async Task ReturnsSuccess_WhenUserIsFcUserAndServiceCanProcess(Models.WoodlandOwner.WoodlandOwnerModel model)
    {
        //arrange
        var sut = CreateSut(asFcUser:true);

        var response = new AddWoodlandOwnerDetailsResponse { WoodlandOwnerId  = Guid.NewGuid() };

        //setup
        _mockWoodlandOwnerCreationService.Setup(r =>
                r.AddWoodlandOwnerDetails(It.IsAny<AddWoodlandOwnerDetailsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response).Verifiable();

        //act
        var result = await sut.CreateWoodlandOwnerAsync(_externalApplicant!, model, CancellationToken.None);

        //assert
        result.IsSuccess.Should().BeTrue();
        result.Value.WoodlandOwnerId.Should().NotBeEmpty();
        _mockWoodlandOwnerCreationService.Verify();

        _mockAudit.Verify(v => v.PublishAuditEventAsync(
                    It.Is<AuditEvent>(a =>
                        a.UserId == _externalApplicant!.UserAccountId
                        && a.SourceEntityId.ToString() == result.Value.WoodlandOwnerId.ToString()
                        && a.EventName == AuditEvents.FcAgentUserCreateWoodlandOwnerEvent
                        && JsonSerializer.Serialize(a.AuditData, _options) ==
                        JsonSerializer.Serialize(new
                        {
                            result.Value.WoodlandOwnerId,
                            model.ContactName,
                            model.ContactEmail,
                            ContactTelephone = model.ContactTelephoneNumber,
                            model.ContactAddress,
                            model.IsOrganisation,
                            model.OrganisationName,
                            model.OrganisationAddress
                        }, _options))
                    , CancellationToken.None), Times.Once);
    }

    [Theory, AutoMoqData]

    public async Task ReturnsFailure_WhenServiceCannotProcess(Models.WoodlandOwner.WoodlandOwnerModel model)
    {
        //arrange
        var sut = CreateSut(asFcUser:true);

        //setup
        _mockWoodlandOwnerCreationService.Setup(r =>
                r.AddWoodlandOwnerDetails(It.IsAny<AddWoodlandOwnerDetailsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<AddWoodlandOwnerDetailsResponse>(Error));

        //act
        var result = await sut.CreateWoodlandOwnerAsync(_externalApplicant!, model, CancellationToken.None);

        //assert
        result.IsSuccess.Should().BeFalse();
        _mockWoodlandOwnerCreationService.Verify();

        _mockAudit.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(a =>
                a.UserId == _externalApplicant!.UserAccountId
                && a.SourceEntityId == null
                && a.EventName == AuditEvents.FcAgentUserCreateWoodlandOwnerFailureEvent
                && JsonSerializer.Serialize(a.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    model.ContactName,
                    model.ContactEmail,
                    ContactTelephone = model.ContactTelephoneNumber,
                    model.ContactAddress,
                    model.IsOrganisation,
                    model.OrganisationName,
                    model.OrganisationAddress,
                    error = Error
                }, _options)
                )
            , CancellationToken.None), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ReturnsFailure_WhenServiceThrowsException(Models.WoodlandOwner.WoodlandOwnerModel model)
    {
        //arrange
        var sut = CreateSut(asFcUser:true);

        //setup
        _mockWoodlandOwnerCreationService.Setup(r =>
                r.AddWoodlandOwnerDetails(It.IsAny<AddWoodlandOwnerDetailsRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception(Error));

        //act
        var result = await sut.CreateWoodlandOwnerAsync(_externalApplicant!, model, CancellationToken.None);

        //assert
        result.IsSuccess.Should().BeFalse();
        _mockWoodlandOwnerCreationService.Verify();

        _mockAudit.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(a =>
                a.UserId == _externalApplicant!.UserAccountId
                && a.SourceEntityId == null
                && a.EventName == AuditEvents.FcAgentUserCreateWoodlandOwnerFailureEvent
                && JsonSerializer.Serialize(a.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    model.ContactName,
                    model.ContactEmail,
                    ContactTelephone = model.ContactTelephoneNumber,
                    model.ContactAddress,
                    model.IsOrganisation,
                    model.OrganisationName,
                    model.OrganisationAddress,
                    error = Error
                }, _options)
            )
            ,CancellationToken.None), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ReturnsFailure_WhenUserIsNotFcUser(Models.WoodlandOwner.WoodlandOwnerModel model)
    {
        //arrange
        var sut = CreateSut(asFcUser:false);

        //act
        var result = await sut.CreateWoodlandOwnerAsync(_externalApplicant!, model, CancellationToken.None);

        //assert
        result.IsSuccess.Should().BeFalse();
        _mockWoodlandOwnerCreationService.VerifyNoOtherCalls();
        _mockAudit.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task ReturnsFailure_WhenUserIsNotRegistered(Models.WoodlandOwner.WoodlandOwnerModel model)
    {
        //arrange
        var sut = CreateSut(asRegisteredUser:false);

        //act
        var result = await sut.CreateWoodlandOwnerAsync(_externalApplicant!, model, CancellationToken.None);

        //assert
        result.IsSuccess.Should().BeFalse();
        _mockWoodlandOwnerCreationService.VerifyNoOtherCalls();
        _mockAudit.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task ReturnsFailure_WhenUserIsNull(Models.WoodlandOwner.WoodlandOwnerModel model)
    {
        //arrange
        var sut = CreateSut();

        //act

        //assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            sut.CreateWoodlandOwnerAsync(null!, model, CancellationToken.None));
        
        _mockWoodlandOwnerCreationService.VerifyNoOtherCalls();
        _mockAudit.VerifyNoOtherCalls();
    }


    [Fact]
    public async Task ReturnsFailure_WhenModelIsNull()
    {
        //arrange
        var sut = CreateSut();

        //act

        //assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            sut.CreateWoodlandOwnerAsync(_externalApplicant!, null!, CancellationToken.None));
        
        _mockWoodlandOwnerCreationService.VerifyNoOtherCalls();
        _mockAudit.VerifyNoOtherCalls();
    }
}
