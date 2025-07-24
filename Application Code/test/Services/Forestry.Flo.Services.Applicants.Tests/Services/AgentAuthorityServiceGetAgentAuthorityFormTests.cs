using AutoFixture.Xunit2;
using Forestry.Flo.Services.Applicants.Entities.AgentAuthority;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Applicants.Repositories;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.FileStorage.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;

namespace Forestry.Flo.Services.Applicants.Tests.Services;

public class AgentAuthorityServiceGetAgentAuthorityFormTests
{

    private ApplicantsContext _applicantsContext;
    private Mock<IClock> _mockClock = new();
    private Instant _now = new Instant();

    [Theory, AutoData]
    public async Task WhenCannotLocateAgentAuthority(GetAgentAuthorityFormRequest request)
    {
        var sut = CreateSut();

        var result = await sut.GetAgentAuthorityFormAsync(request, CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task WhenNoCurrentAafAndNoPointInTimeInRequest(AgentAuthority entity)
    {
        entity.AgentAuthorityForms = new List<AgentAuthorityForm>
        {
            new AgentAuthorityForm
            {
                ValidFromDate = _now.ToDateTimeUtc().AddDays(-2),
                ValidToDate = _now.ToDateTimeUtc().AddDays(-1)
            }
        };

        var sut = CreateSut();

        _applicantsContext.AgentAuthorities.Add(entity);
        await _applicantsContext.SaveEntitiesAsync();

        var request = new GetAgentAuthorityFormRequest
        {
            PointInTime = null,
            AgencyId = entity.Agency.Id,
            WoodlandOwnerId = entity.WoodlandOwner.Id
        };
        var result = await sut.GetAgentAuthorityFormAsync(request, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(entity.Id, result.Value.AgentAuthorityId);
        Assert.True(result.Value.CurrentAgentAuthorityForm.HasNoValue);
        Assert.True(result.Value.SpecificTimestampAgentAuthorityForm.HasNoValue);
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task WhenNoCurrentAafAndNoMatchToPointInTimeInRequest(AgentAuthority entity)
    {
        entity.AgentAuthorityForms = new List<AgentAuthorityForm>
        {
            new AgentAuthorityForm
            {
                ValidFromDate = _now.ToDateTimeUtc().AddDays(-3),
                ValidToDate = _now.ToDateTimeUtc().AddDays(-2)
            }
        };

        var sut = CreateSut();

        _applicantsContext.AgentAuthorities.Add(entity);
        await _applicantsContext.SaveEntitiesAsync();

        var request = new GetAgentAuthorityFormRequest
        {
            PointInTime = _now.ToDateTimeUtc().AddDays(-1),
            AgencyId = entity.Agency.Id,
            WoodlandOwnerId = entity.WoodlandOwner.Id
        };

        var result = await sut.GetAgentAuthorityFormAsync(request, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(entity.Id, result.Value.AgentAuthorityId);
        Assert.True(result.Value.CurrentAgentAuthorityForm.HasNoValue);
        Assert.True(result.Value.SpecificTimestampAgentAuthorityForm.HasNoValue);
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task WhenNoCurrentAafAndMatchToPointInTimeInRequest(AgentAuthority entity)
    {
        entity.AgentAuthorityForms = new List<AgentAuthorityForm>
        {
            new AgentAuthorityForm
            {
                ValidFromDate = _now.ToDateTimeUtc().AddDays(-3),
                ValidToDate = _now.ToDateTimeUtc().AddDays(-1)
            }
        };

        var sut = CreateSut();

        _applicantsContext.AgentAuthorities.Add(entity);
        await _applicantsContext.SaveEntitiesAsync();

        var request = new GetAgentAuthorityFormRequest
        {
            PointInTime = _now.ToDateTimeUtc().AddDays(-2),
            AgencyId = entity.Agency.Id,
            WoodlandOwnerId = entity.WoodlandOwner.Id
        };

        var result = await sut.GetAgentAuthorityFormAsync(request, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(entity.Id, result.Value.AgentAuthorityId);
        Assert.True(result.Value.CurrentAgentAuthorityForm.HasNoValue);
        Assert.Equal(entity.AgentAuthorityForms.Single().Id, result.Value.SpecificTimestampAgentAuthorityForm.Value.Id);
        Assert.Equal(entity.AgentAuthorityForms.Single().ValidFromDate, result.Value.SpecificTimestampAgentAuthorityForm.Value.ValidFromDate);
        Assert.Equal(entity.AgentAuthorityForms.Single().ValidToDate, result.Value.SpecificTimestampAgentAuthorityForm.Value.ValidToDate);
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task WhenCurrentAafAndNoPointInTimeInRequest(AgentAuthority entity)
    {
        entity.AgentAuthorityForms = new List<AgentAuthorityForm>
        {
            new AgentAuthorityForm
            {
                ValidFromDate = _now.ToDateTimeUtc().AddDays(-2),
            }
        };

        var sut = CreateSut();

        _applicantsContext.AgentAuthorities.Add(entity);
        await _applicantsContext.SaveEntitiesAsync();

        var request = new GetAgentAuthorityFormRequest
        {
            PointInTime = null,
            AgencyId = entity.Agency.Id,
            WoodlandOwnerId = entity.WoodlandOwner.Id
        };
        var result = await sut.GetAgentAuthorityFormAsync(request, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(entity.Id, result.Value.AgentAuthorityId);
        Assert.Equal(entity.AgentAuthorityForms.Single().Id, result.Value.CurrentAgentAuthorityForm.Value.Id);
        Assert.Equal(entity.AgentAuthorityForms.Single().ValidFromDate, result.Value.CurrentAgentAuthorityForm.Value.ValidFromDate);
        Assert.Equal(entity.AgentAuthorityForms.Single().ValidToDate, result.Value.CurrentAgentAuthorityForm.Value.ValidToDate);
        Assert.Equal(entity.AgentAuthorityForms.Single().Id, result.Value.SpecificTimestampAgentAuthorityForm.Value.Id);
        Assert.Equal(entity.AgentAuthorityForms.Single().ValidFromDate, result.Value.SpecificTimestampAgentAuthorityForm.Value.ValidFromDate);
        Assert.Equal(entity.AgentAuthorityForms.Single().ValidToDate, result.Value.SpecificTimestampAgentAuthorityForm.Value.ValidToDate);
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task WhenCurrentAafAndNoMatchToPointInTimeInRequest(AgentAuthority entity)
    {
        entity.AgentAuthorityForms = new List<AgentAuthorityForm>
        {
            new AgentAuthorityForm
            {
                ValidFromDate = _now.ToDateTimeUtc().AddDays(-2),
            }
        };

        var sut = CreateSut();

        _applicantsContext.AgentAuthorities.Add(entity);
        await _applicantsContext.SaveEntitiesAsync();

        var request = new GetAgentAuthorityFormRequest
        {
            PointInTime = _now.ToDateTimeUtc().AddDays(-3),
            AgencyId = entity.Agency.Id,
            WoodlandOwnerId = entity.WoodlandOwner.Id
        };
        var result = await sut.GetAgentAuthorityFormAsync(request, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(entity.Id, result.Value.AgentAuthorityId);
        Assert.Equal(entity.AgentAuthorityForms.Single().Id, result.Value.CurrentAgentAuthorityForm.Value.Id);
        Assert.Equal(entity.AgentAuthorityForms.Single().ValidFromDate, result.Value.CurrentAgentAuthorityForm.Value.ValidFromDate);
        Assert.Equal(entity.AgentAuthorityForms.Single().ValidToDate, result.Value.CurrentAgentAuthorityForm.Value.ValidToDate);
        Assert.True(result.Value.SpecificTimestampAgentAuthorityForm.HasNoValue);
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task WhenCurrentAafAndMatchToPointInTimeInRequestAreTheSame(AgentAuthority entity)
    {
        entity.AgentAuthorityForms = new List<AgentAuthorityForm>
        {
            new AgentAuthorityForm
            {
                ValidFromDate = _now.ToDateTimeUtc().AddDays(-2),
            }
        };

        var sut = CreateSut();

        _applicantsContext.AgentAuthorities.Add(entity);
        await _applicantsContext.SaveEntitiesAsync();

        var request = new GetAgentAuthorityFormRequest
        {
            PointInTime = _now.ToDateTimeUtc().AddDays(-1),
            AgencyId = entity.Agency.Id,
            WoodlandOwnerId = entity.WoodlandOwner.Id
        };
        var result = await sut.GetAgentAuthorityFormAsync(request, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(entity.Id, result.Value.AgentAuthorityId);
        Assert.Equal(entity.AgentAuthorityForms.Single().Id, result.Value.CurrentAgentAuthorityForm.Value.Id);
        Assert.Equal(entity.AgentAuthorityForms.Single().ValidFromDate, result.Value.CurrentAgentAuthorityForm.Value.ValidFromDate);
        Assert.Equal(entity.AgentAuthorityForms.Single().ValidToDate, result.Value.CurrentAgentAuthorityForm.Value.ValidToDate);
        Assert.Equal(entity.AgentAuthorityForms.Single().Id, result.Value.SpecificTimestampAgentAuthorityForm.Value.Id);
        Assert.Equal(entity.AgentAuthorityForms.Single().ValidFromDate, result.Value.SpecificTimestampAgentAuthorityForm.Value.ValidFromDate);
        Assert.Equal(entity.AgentAuthorityForms.Single().ValidToDate, result.Value.SpecificTimestampAgentAuthorityForm.Value.ValidToDate);
    }

    [Theory, AutoDataWithNonFcAgency]
    public async Task WhenCurrentAafAndMatchToPointInTimeInRequestAreDifferent(AgentAuthority entity)
    {
        entity.AgentAuthorityForms = new List<AgentAuthorityForm>
        {
            new AgentAuthorityForm
            {
                ValidFromDate = _now.ToDateTimeUtc().AddDays(-3),
                ValidToDate = _now.ToDateTimeUtc().AddDays(-1)
            },
            new AgentAuthorityForm
            {
                ValidFromDate = _now.ToDateTimeUtc().AddDays(-1),
            }
        };

        var sut = CreateSut();

        _applicantsContext.AgentAuthorities.Add(entity);
        await _applicantsContext.SaveEntitiesAsync();

        var request = new GetAgentAuthorityFormRequest
        {
            PointInTime = _now.ToDateTimeUtc().AddDays(-2),
            AgencyId = entity.Agency.Id,
            WoodlandOwnerId = entity.WoodlandOwner.Id
        };
        var result = await sut.GetAgentAuthorityFormAsync(request, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(entity.Id, result.Value.AgentAuthorityId);
        Assert.Equal(entity.AgentAuthorityForms.Last().Id, result.Value.CurrentAgentAuthorityForm.Value.Id);
        Assert.Equal(entity.AgentAuthorityForms.Last().ValidFromDate, result.Value.CurrentAgentAuthorityForm.Value.ValidFromDate);
        Assert.Equal(entity.AgentAuthorityForms.Last().ValidToDate, result.Value.CurrentAgentAuthorityForm.Value.ValidToDate);
        Assert.Equal(entity.AgentAuthorityForms.First().Id, result.Value.SpecificTimestampAgentAuthorityForm.Value.Id);
        Assert.Equal(entity.AgentAuthorityForms.First().ValidFromDate, result.Value.SpecificTimestampAgentAuthorityForm.Value.ValidFromDate);
        Assert.Equal(entity.AgentAuthorityForms.First().ValidToDate, result.Value.SpecificTimestampAgentAuthorityForm.Value.ValidToDate);
    }

    private IAgentAuthorityInternalService CreateSut()
    {
        _applicantsContext = TestApplicantsDatabaseFactory.CreateDefaultTestContext();

        _mockClock.Reset();
        _mockClock.Setup(x => x.GetCurrentInstant()).Returns(_now);

        return new AgentAuthorityService(
            new AgencyRepository(_applicantsContext),
            new Mock<IUserAccountRepository>().Object,
            new Mock<IFileStorageService>().Object,
            new FileTypesProvider(),
            _mockClock.Object,
            new NullLogger<AgentAuthorityService>());
    }
}