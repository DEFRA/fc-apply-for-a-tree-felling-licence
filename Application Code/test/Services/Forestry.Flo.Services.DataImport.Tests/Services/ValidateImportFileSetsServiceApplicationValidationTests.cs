using AutoFixture;
using AutoFixture.Xunit2;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Forestry.Flo.Services.DataImport.Tests.Services;

public class ValidateImportFileSetsServiceApplicationValidationTests: ApplicationFileSetTestsBase
{
    [Theory, AutoData]
    public async Task WithValidApplications(Guid woodlandOwnerId)
    {
        var input = GenerateValidImportSets();

        var sut = CreateSut();

        var result = await sut.ValidateImportFileSetAsync(woodlandOwnerId, Properties, input, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Theory, AutoData]
    public async Task WhenApplicationsHaveSameId(Guid woodlandOwnerId)
    {
        var input = GenerateValidImportSets(fellingPerApplication:0, restockingPerFelling:0);

        input.ApplicationSourceRecords!.ForEach(x => x.ApplicationId = 1);

        var sut = CreateSut();

        var result = await sut.ValidateImportFileSetAsync(woodlandOwnerId, Properties, input, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Single(result.Error);

        Assert.Equal("There are repeated id values within the Application records source", result.Error.First().ErrorMessage);
    }

    [Theory, AutoData]
    public async Task WhenApplicationHasUnknownPropertyName(Guid woodlandOwnerId)
    {
        var input = GenerateValidImportSets(fellingPerApplication: 0, restockingPerFelling: 0);
        var unknownProperty = FixtureInstance.Create<string>();

        input.ApplicationSourceRecords!.First().Flov2PropertyName = unknownProperty;

        var sut = CreateSut();

        var result = await sut.ValidateImportFileSetAsync(woodlandOwnerId, Properties, input, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Single(result.Error);

        Assert.Equal($"FLOv2 Property Name {unknownProperty} was not found amongst all properties for woodland owner for application with id {input.ApplicationSourceRecords!.First().ApplicationId}", result.Error.First().ErrorMessage);
    }

    [Theory, AutoData]
    public async Task WhenApplicationHasProposedFellingStartInThePast(Guid woodlandOwnerId)
    {
        var input = GenerateValidImportSets(fellingPerApplication: 0, restockingPerFelling: 0);
        
        input.ApplicationSourceRecords!.First().ProposedFellingStart = DateOnly.FromDateTime(Clock.GetCurrentInstant().ToDateTimeUtc().AddDays(-2));

        var sut = CreateSut();

        var result = await sut.ValidateImportFileSetAsync(woodlandOwnerId, Properties, input, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Single(result.Error);

        Assert.Equal($"Proposed felling start must be in the future for application with id {input.ApplicationSourceRecords!.First().ApplicationId}", result.Error.First().ErrorMessage);
    }

    [Theory, AutoData]
    public async Task WhenApplicationHasProposedFellingEndBeforeProposedFellingStart(Guid woodlandOwnerId)
    {
        var input = GenerateValidImportSets(fellingPerApplication: 0, restockingPerFelling: 0);

        input.ApplicationSourceRecords!.First().ProposedFellingEnd = input.ApplicationSourceRecords!.First().ProposedFellingStart.Value.AddDays(-2);

        var sut = CreateSut();

        var result = await sut.ValidateImportFileSetAsync(woodlandOwnerId, Properties, input, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Single(result.Error);

        Assert.Equal($"Proposed felling end must be after the proposed felling start for application with id {input.ApplicationSourceRecords!.First().ApplicationId}", result.Error.First().ErrorMessage);
    }
}