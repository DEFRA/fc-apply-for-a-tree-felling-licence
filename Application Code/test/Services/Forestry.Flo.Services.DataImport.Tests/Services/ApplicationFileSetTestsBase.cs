using AutoFixture;
using Forestry.Flo.Services.DataImport.Models;
using Forestry.Flo.Services.DataImport.Services;
using Forestry.Flo.Services.FellingLicenceApplications.DataImports.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.PropertyProfiles.DataImports;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Forestry.Flo.Services.DataImport.Tests.Services;

public abstract class ApplicationFileSetTestsBase
{
    protected readonly Fixture FixtureInstance = new();
    protected List<string> SpeciesCodes;
    protected List<PropertyIds> Properties;
    protected IClock Clock;

    public ApplicationFileSetTestsBase()
    {
        SpeciesCodes = TreeSpeciesFactory.SpeciesDictionary.Values.Select(x => x.Code).ToList();
        FixtureInstance.Customize<DateOnly>(composer => composer.FromFactory<DateTime>(DateOnly.FromDateTime));
        Properties = FixtureInstance.CreateMany<PropertyIds>().ToList();
        Clock = SystemClock.Instance;
    }

    protected ImportFileSetContents GenerateValidImportSets(
        int applicationsCount = 1,
        int fellingPerApplication = 3,
        int restockingPerFelling = 1)
    {
        List<ApplicationSource> applications = [];
        List<ProposedFellingSource> proposedFelling = [];
        List<ProposedRestockingSource> proposedRestocking = [];

        int fellingCount = 0;

        for (int a = 0; a < applicationsCount; a++)
        {
            var randomProperty = Properties.RandomElement();
            var proposedFellStart = DateTime.Today.GetRandomLaterDate();

            var nextApplication = FixtureInstance.Build<ApplicationSource>()
                .With(x => x.ApplicationId, a + 1)
                .With(x => x.Flov2PropertyName, randomProperty.Name)
                .With(x => x.ProposedFellingStart, DateOnly.FromDateTime(proposedFellStart))
                .With(x => x.ProposedFellingEnd, DateOnly.FromDateTime(proposedFellStart.GetRandomLaterDate()))
                .Create();

            applications.Add(nextApplication);

            for (int f = 0; f < fellingPerApplication; f++)
            {
                fellingCount++;

                var fellingCompartment = randomProperty.CompartmentIds.RandomElement();

                var fellingOperation = FixtureInstance.Create<FellingOperationType>();

                int loopCount = 0;
                while (fellingOperation == FellingOperationType.None
                       || proposedFelling.Any(x => x.Flov2CompartmentName == fellingCompartment.CompartmentName && x.OperationType == fellingOperation))
                {
                    loopCount++;
                    fellingOperation = FixtureInstance.Create<FellingOperationType>();
                    if (loopCount > 10)
                    {
                        break;
                    }
                }

                if (loopCount > 10)
                {
                    continue;
                }

                List<string> species = [SpeciesCodes.RandomElement()];
                species.Add(SpeciesCodes.Where(x => !species.Contains(x)).RandomElement());
                species.Add(SpeciesCodes.Where(x => !species.Contains(x)).RandomElement());

                var validRestockingTypes = fellingOperation.AllowedRestockingForFellingType(false).ToList();
                validRestockingTypes.Remove(TypeOfProposal.DoNotIntendToRestock);

                var restockingForThisFellingCount = int.Min(restockingPerFelling, validRestockingTypes.Count);

                var isRestocking = restockingForThisFellingCount != 0  
                                   && fellingOperation != FellingOperationType.Thinning 
                                   && FixtureInstance.Create<bool>();
                var noRestockingReason = isRestocking || fellingOperation == FellingOperationType.Thinning
                    ? null 
                    : FixtureInstance.Create<string>();

                var nextFelling = FixtureInstance.Build<ProposedFellingSource>()
                    .With(x => x.ProposedFellingId, fellingCount)
                    .With(x => x.ApplicationId, nextApplication.ApplicationId)
                    .With(x => x.Flov2CompartmentName, fellingCompartment.CompartmentName)
                    .With(x => x.OperationType, fellingOperation)
                    .With(x => x.AreaToBeFelled, fellingCompartment.Area.Value)
                    .With(x => x.Species, string.Join(',', species))
                    .With(x => x.IsRestocking, isRestocking)
                    .With(x => x.NoRestockingReason, noRestockingReason)
                    .Create();

                proposedFelling.Add(nextFelling);
                
                for (int r = 0; r < restockingForThisFellingCount; r++)
                {
                    var operation = validRestockingTypes.RandomElement();
                    loopCount = 0;
                    while (proposedRestocking.Any(x => x.RestockingProposal == operation && x.ProposedFellingId == nextFelling.ProposedFellingId))
                    {
                        loopCount++;
                        operation = validRestockingTypes.RandomElement();
                        if (loopCount > 10)
                        {
                            break;
                        }
                    }
                    if (loopCount > 10)
                    {
                        continue;
                    }

                    var restockingCompartment = operation.IsAlternativeCompartmentRestockingType()
                        ? randomProperty.CompartmentIds.Where(x => x.CompartmentName != nextFelling.Flov2CompartmentName).RandomElement()
                        : null;

                    double? density = operation.IsNumberOfTreesRestockingType() || operation == TypeOfProposal.CreateDesignedOpenGround
                        ? null
                        : FixtureInstance.Create<double>();

                    int? numberOfTrees = operation.IsNumberOfTreesRestockingType()
                        ? FixtureInstance.Create<int>()
                        : null;

                    string? restockingSpecies = null;
                    if (operation != TypeOfProposal.CreateDesignedOpenGround)
                    {
                        var speciesCode1 = SpeciesCodes.RandomElement();
                        var speciesPercent1 = new Random().Next(1, 80);
                        var speciesCode2 = SpeciesCodes.Where(x => x != speciesCode1).RandomElement();
                        var speciesPercent2 = 100 - speciesPercent1;
                        restockingSpecies = $"{speciesCode1},{speciesPercent1},{speciesCode2},{speciesPercent2}";
                    }

                    var nextRestocking = FixtureInstance.Build<ProposedRestockingSource>()
                        .With(x => x.ProposedFellingId, nextFelling.ProposedFellingId)
                        .With(x => x.Flov2CompartmentName, restockingCompartment?.CompartmentName)
                        .With(x => x.RestockingDensity, density)
                        .With(x => x.NumberOfTrees, numberOfTrees)
                        .With(x => x.AreaToBeRestocked, restockingCompartment?.Area ?? fellingCompartment.Area)
                        .With(x => x.RestockingProposal, operation)
                        .With(x => x.SpeciesAndPercentages, restockingSpecies)
                        .Create();
                    proposedRestocking.Add(nextRestocking);
                }
            }
        }

        return new ImportFileSetContents
        {
            ApplicationSourceRecords = applications,
            ProposedFellingSourceRecords = proposedFelling,
            ProposedRestockingSourceRecords = proposedRestocking
        };
    }

    protected ValidateImportFileSetsService CreateSut()
    {
        return new ValidateImportFileSetsService(
            Clock, 
            new NullLogger<ValidateImportFileSetsService>());
    }
}