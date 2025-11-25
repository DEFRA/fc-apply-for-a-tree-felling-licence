using System.Security.Claims;
using System.Text.Json;
using AutoFixture;
using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Models.WoodlandOwner;
using Forestry.Flo.External.Web.Services;
using Forestry.Flo.Services.Applicants.Configuration;
using Forestry.Flo.Services.Applicants.Entities;
using Forestry.Flo.Services.Applicants.Entities.WoodlandOwner;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Tests.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WoodlandOwnerModel = Forestry.Flo.Services.Applicants.Models.WoodlandOwnerModel;

namespace Forestry.Flo.External.Web.Tests.Services;

public class ManageWoodlandOwnerDetailsUseCaseTests
{
    private readonly Mock<IWoodlandOwnerCreationService> _mockWoodlandOwnerCreationService = new();
    private readonly Mock<IAuditService<ManageWoodlandOwnerDetailsUseCase>> _mockAudit = new();
    private readonly Mock<IRetrieveWoodlandOwners> _mockRetrieveWoodlandOwnerService = new();
    private readonly Mock<IAgentAuthorityService> _mockAgentAuthorityService = new();
    private readonly Mock<IHttpContextAccessor> _mockContextAccessor = new();
    private readonly Mock<IRetrieveUserAccountsService> _mockAccountsService = new();
    private readonly Fixture _fixture = new();
    private ExternalApplicant? _externalApplicant;
    private const string Error = "test error";


    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private ManageWoodlandOwnerDetailsUseCase CreateSut()
    {
        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<Guid>(),
            _fixture.Create<Guid>(),
            agencyId: _fixture.Create<Guid>(),
            woodlandOwnerName: _fixture.Create<string>());

        _externalApplicant = new ExternalApplicant(user);

        _mockWoodlandOwnerCreationService.Reset();
        _mockRetrieveWoodlandOwnerService.Reset();
        _mockAudit.Reset();

        return new ManageWoodlandOwnerDetailsUseCase(
            _mockWoodlandOwnerCreationService.Object,
            _mockRetrieveWoodlandOwnerService.Object,
            _mockAudit.Object,
            _mockAgentAuthorityService.Object,
            new RequestContext("test", new RequestUserModel(_externalApplicant.Principal)),
            new NullLogger<ManageWoodlandOwnerDetailsUseCase>(),
            _mockContextAccessor.Object,
            _mockAccountsService.Object,
            Options.Create(new FcAgencyOptions { PermittedEmailDomainsForFcAgent = new List<string> { "qxlva.com", "forestrycommission.gov.uk" } })
        );
    }

    [Theory, AutoData]
    public async Task RetrievesDetailsModel_ForOrganisations(WoodlandOwnerModel owner, UserAccessModel userAccess)
    {
        owner.IsOrganisation = true;

        var sut = CreateSut();

        _mockAccountsService.Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(userAccess));

        _mockRetrieveWoodlandOwnerService.Setup(s =>
                s.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(owner);

        var result = await sut.GetWoodlandOwnerModelAsync(owner.Id.Value, _externalApplicant, CancellationToken.None);

        Assert.True(result.IsSuccess);

        // assert values are mapped correctly

        Assert.Equal(owner.ContactName, result.Value.ContactName);
        Assert.Equal(owner.Id.Value, result.Value.Id);
        Assert.True(result.Value.IsOrganisation);

        Assert.Equal(owner.ContactAddress.Line1, result.Value.ContactAddress.Line1);
        Assert.Equal(owner.ContactAddress.Line2, result.Value.ContactAddress.Line2);
        Assert.Equal(owner.ContactAddress.Line3, result.Value.ContactAddress.Line3);
        Assert.Equal(owner.ContactAddress.Line4, result.Value.ContactAddress.Line4);
        Assert.Equal(owner.ContactAddress.PostalCode, result.Value.ContactAddress.PostalCode);

        Assert.Equal(owner.ContactTelephone, result.Value.ContactTelephoneNumber);
        Assert.Equal(owner.ContactEmail, result.Value.ContactEmail);

        Assert.Equal(owner.OrganisationAddress.Line1, result.Value.OrganisationAddress.Line1);
        Assert.Equal(owner.OrganisationAddress.Line2, result.Value.OrganisationAddress.Line2);
        Assert.Equal(owner.OrganisationAddress.Line3, result.Value.OrganisationAddress.Line3);
        Assert.Equal(owner.OrganisationAddress.Line4, result.Value.OrganisationAddress.Line4);
        Assert.Equal(owner.OrganisationAddress.PostalCode, result.Value.OrganisationAddress.PostalCode);

        Assert.Equal(owner.OrganisationName, result.Value.OrganisationName);

        _mockRetrieveWoodlandOwnerService.Verify(v => v.RetrieveWoodlandOwnerByIdAsync(owner.Id.Value, userAccess, CancellationToken.None), Times.Once);
    }

    [Theory, AutoData]
    public async Task RetrievesDetailsModel_ForIndividuals(WoodlandOwnerModel owner, UserAccessModel userAccess)
    {
        owner.IsOrganisation = false;
        owner.OrganisationAddress = null;
        owner.OrganisationName = null;

        var sut = CreateSut();

        _mockAccountsService.Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(userAccess));

        _mockRetrieveWoodlandOwnerService.Setup(s =>
                s.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),It.IsAny<CancellationToken>()))
            .ReturnsAsync(owner);

        var result = await sut.GetWoodlandOwnerModelAsync(owner.Id.Value, _externalApplicant, CancellationToken.None);

        Assert.True(result.IsSuccess);

        // assert values are mapped correctly

        Assert.Equal(owner.ContactName, result.Value.ContactName);
        Assert.Equal(owner.Id.Value, result.Value.Id);
        Assert.False(result.Value.IsOrganisation);

        Assert.Equal(owner.ContactAddress.Line1, result.Value.ContactAddress.Line1);
        Assert.Equal(owner.ContactAddress.Line2, result.Value.ContactAddress.Line2);
        Assert.Equal(owner.ContactAddress.Line3, result.Value.ContactAddress.Line3);
        Assert.Equal(owner.ContactAddress.Line4, result.Value.ContactAddress.Line4);
        Assert.Equal(owner.ContactAddress.PostalCode, result.Value.ContactAddress.PostalCode);

        Assert.Equal(owner.ContactTelephone, result.Value.ContactTelephoneNumber);
        Assert.Equal(owner.ContactEmail, result.Value.ContactEmail);

        Assert.Null(result.Value.OrganisationAddress);
        Assert.Null(result.Value.OrganisationName);

        _mockRetrieveWoodlandOwnerService.Verify(v => v.RetrieveWoodlandOwnerByIdAsync(owner.Id.Value, userAccess, CancellationToken.None), Times.Once);
    }

    [Theory, AutoData]
    public async Task ReturnsFailure_WhenWoodlandOwnerNotRetrieved(WoodlandOwnerModel owner, UserAccessModel userAccess)
    {
        var sut = CreateSut();

        _mockAccountsService.Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(userAccess));

        _mockRetrieveWoodlandOwnerService.Setup(s =>
                s.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<WoodlandOwnerModel>(Error));

        var result = await sut.GetWoodlandOwnerModelAsync(owner.Id.Value, _externalApplicant, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockRetrieveWoodlandOwnerService.Verify(v => v.RetrieveWoodlandOwnerByIdAsync(owner.Id.Value, userAccess, CancellationToken.None), Times.Once);
    }

    [Theory, AutoData]
    public async Task ReturnsSuccessAndAudits_WhenWoodlandOwnerOrganisationUpdated(ManageWoodlandOwnerDetailsModel model)
    {
        var sut = CreateSut();

        model.IsOrganisation = true;

        _mockWoodlandOwnerCreationService.Setup(s =>
                s.AmendWoodlandOwnerDetailsAsync(It.IsAny<WoodlandOwnerModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await sut.UpdateWoodlandOwnerEntityAsync(model, _externalApplicant!, CancellationToken.None);

        Assert.True(result.IsSuccess);

        _mockWoodlandOwnerCreationService.Verify(v => v.AmendWoodlandOwnerDetailsAsync(
            It.Is<WoodlandOwnerModel>(
                x => x.IsOrganisation == model.IsOrganisation
                     && CompareAddresses(x.ContactAddress, model.ContactAddress)
                     && x.ContactEmail == model.ContactEmail
                     && x.ContactName == model.ContactName
                     && x.ContactTelephone == model.ContactTelephoneNumber
                     && x.Id == model.Id
                     && x.OrganisationName == model.OrganisationName
                     && CompareAddresses(x.OrganisationAddress, model.OrganisationAddress)
                     ), CancellationToken.None), Times.Once);

        _mockAudit.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(a =>
                a.UserId == _externalApplicant!.UserAccountId
                && a.SourceEntityId == model.Id
                && a.EventName == AuditEvents.UpdateWoodlandOwnerEvent
                && JsonSerializer.Serialize(a.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    IsOrganisation = model.IsOrganisation
                }, _options)), CancellationToken.None), Times.Once);
    }

    [Theory, AutoData]
    public async Task ReturnsSuccessAndAudits_WhenWoodlandOwnerIndividualUpdated(ManageWoodlandOwnerDetailsModel model)
    {
        var sut = CreateSut();

        model.IsOrganisation = false;
        model.OrganisationName = null;

        _mockWoodlandOwnerCreationService.Setup(s =>
                s.AmendWoodlandOwnerDetailsAsync(It.IsAny<WoodlandOwnerModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await sut.UpdateWoodlandOwnerEntityAsync(model, _externalApplicant!, CancellationToken.None);

        Assert.True(result.IsSuccess);

        _mockWoodlandOwnerCreationService.Verify(v => v.AmendWoodlandOwnerDetailsAsync(
            It.Is<WoodlandOwnerModel>(
                x => x.IsOrganisation == model.IsOrganisation
                     && CompareAddresses(x.ContactAddress, model.ContactAddress)
                     && x.ContactEmail == model.ContactEmail
                     && x.ContactName == model.ContactName
                     && x.ContactTelephone == model.ContactTelephoneNumber
                     && x.Id == model.Id
                     && x.OrganisationName == null
                     && x.OrganisationAddress == null
            ), CancellationToken.None), Times.Once);

        _mockAudit.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(a =>
                a.UserId == _externalApplicant!.UserAccountId
                && a.SourceEntityId == model.Id
                && a.EventName == AuditEvents.UpdateWoodlandOwnerEvent
                && JsonSerializer.Serialize(a.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    IsOrganisation = model.IsOrganisation
                }, _options)), CancellationToken.None), Times.Once);
    }

    [Theory, AutoData]
    public async Task ReturnsSuccessAndDoesNotAudit_WhenWoodlandOwnerNotChanged(ManageWoodlandOwnerDetailsModel model)
    {
        var sut = CreateSut();

        model.IsOrganisation = true;

        _mockWoodlandOwnerCreationService.Setup(s =>
                s.AmendWoodlandOwnerDetailsAsync(It.IsAny<WoodlandOwnerModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await sut.UpdateWoodlandOwnerEntityAsync(model, _externalApplicant!, CancellationToken.None);

        Assert.True(result.IsSuccess);

        _mockWoodlandOwnerCreationService.Verify(v => v.AmendWoodlandOwnerDetailsAsync(
            It.Is<WoodlandOwnerModel>(
                x => x.IsOrganisation == model.IsOrganisation
                     && CompareAddresses(x.ContactAddress, model.ContactAddress)
                     && x.ContactEmail == model.ContactEmail
                     && x.ContactName == model.ContactName
                     && x.ContactTelephone == model.ContactTelephoneNumber
                     && x.Id == model.Id
                     && x.OrganisationName == model.OrganisationName
                     && CompareAddresses(x.OrganisationAddress, model.OrganisationAddress)
            ), CancellationToken.None), Times.Once);

        _mockAudit.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(a =>
                a.UserId == _externalApplicant!.UserAccountId
                && a.SourceEntityId == model.Id
                && a.EventName == AuditEvents.UpdateWoodlandOwnerEvent
                && JsonSerializer.Serialize(a.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    IsOrganisation = model.IsOrganisation
                }, _options)), CancellationToken.None), Times.Never);
    }

    [Theory, AutoData]
    public async Task ReturnsFailure_WhenWoodlandOwnerCannotBeUpdated(ManageWoodlandOwnerDetailsModel model)
    {
        var sut = CreateSut();

        model.IsOrganisation = true;

        _mockWoodlandOwnerCreationService.Setup(s =>
                s.AmendWoodlandOwnerDetailsAsync(It.IsAny<WoodlandOwnerModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<bool>(Error));

        var result = await sut.UpdateWoodlandOwnerEntityAsync(model, _externalApplicant!, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockWoodlandOwnerCreationService.Verify(v => v.AmendWoodlandOwnerDetailsAsync(
            It.Is<WoodlandOwnerModel>(
                x => x.IsOrganisation == model.IsOrganisation
                     && CompareAddresses(x.ContactAddress, model.ContactAddress)
                     && x.ContactEmail == model.ContactEmail
                     && x.ContactName == model.ContactName
                     && x.ContactTelephone == model.ContactTelephoneNumber
                     && x.Id == model.Id
                     && x.OrganisationName == model.OrganisationName
                     && CompareAddresses(x.OrganisationAddress, model.OrganisationAddress)
            ), CancellationToken.None), Times.Once);

        _mockAudit.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(a =>
                a.UserId == _externalApplicant!.UserAccountId
                && a.SourceEntityId == model.Id
                && a.EventName == AuditEvents.UpdateWoodlandOwnerFailureEvent
                && JsonSerializer.Serialize(a.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    IsOrganisation = model.IsOrganisation,
                    Error = Error
                }, _options)), CancellationToken.None), Times.Once);

        _mockAudit.VerifyNoOtherCalls();
    }

    private static bool CompareAddresses(Address address1, Models.Address address2)
    {
        return address1.Line1 == address2.Line1
            && address1.Line2 == address2.Line2
            && address1.Line3 == address2.Line3
            && address1.Line4 == address2.Line4
            && address1.PostalCode == address2.PostalCode;
    }
}
