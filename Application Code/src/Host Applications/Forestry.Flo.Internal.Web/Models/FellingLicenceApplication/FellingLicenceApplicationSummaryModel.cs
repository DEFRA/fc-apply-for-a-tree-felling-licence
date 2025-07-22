using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;

public class FellingLicenceApplicationSummaryModel
{
    [HiddenInput]
    public Guid Id { get; set; }

    public string ApplicationReference { get; set; }

    public FellingLicenceStatus Status { get; set; }

    public IList<StatusHistoryModel> StatusHistories { get; set; } = [];

    public string? PropertyName { get; set; }

    public string? NameOfWood { get; set; }

    public string? WoodlandOwnerName { get; set; }

    public Guid? WoodlandOwnerId { get; set; }

    public string? AgentOrAgencyName { get; set; }

    public Maybe<DocumentModel> MostRecentFcLisReport { get; set; }

    public Maybe<DocumentModel> MostRecentApplicationDocument { get; set; }

    public IList<AssigneeHistoryModel> AssigneeHistories { get; set; }

    public DateTime? CitizensCharterDate { get; set; }

    public DateTime? FinalActionDate { get; set; }

    public DateTime? DateReceived { get; set; }

    public FellingLicenceApplicationSource Source { get; set; }

    public List<FellingAndRestockingDetail> DetailsList { get; set; } = new();

    public Guid CreatedById { get; set; }

    public string? AreaCode { get; set; }

    public string? AdministrativeRegion { get; set; }

    public bool AreAnyLarchSpecies => (DetailsList
            .SelectMany(detail => detail.FellingDetail.Species)
            .Select(species => TreeSpeciesFactory.SpeciesDictionary.Values.FirstOrDefault(treeSpecies => treeSpecies.Code == species.Key))
        )
        .Where(specie => specie?.IsLarch ?? false).Any();

    public bool AreAllSpeciesLarch => DetailsList
        .SelectMany(detail => detail.FellingDetail.Species)
        .Select(species => TreeSpeciesFactory.SpeciesDictionary.Values.FirstOrDefault(treeSpecies => treeSpecies.Code == species.Key))
        .All(specie => specie?.IsLarch ?? false);

    public IEnumerable<FellingSpeciesModel> AllSpeciesLarchFirst => DetailsList
        .SelectMany(detail => detail.FellingDetail.Species)
        .Select(species => TreeSpeciesFactory.SpeciesDictionary.Values.FirstOrDefault(treeSpecies => treeSpecies.Code == species.Key))
        .Where(specie => specie != null)
        .Select(specie => new FellingSpeciesModel
        {
            Id = Guid.NewGuid(),
            Species = specie.Code,
            SpeciesName = specie.Name
        })
        .OrderByDescending(specie => TreeSpeciesFactory.SpeciesDictionary.Values
            .FirstOrDefault(treeSpecies => treeSpecies.Code == specie.Species)?.IsLarch ?? false) // Larch species first
        .ThenBy(specie => specie.SpeciesName);

    public IEnumerable<FellingSpeciesModel> AllSpeciesBoldLarch => DetailsList
        .SelectMany(detail => detail.FellingDetail.Species)
        .Select(species => TreeSpeciesFactory.SpeciesDictionary.Values.FirstOrDefault(treeSpecies => treeSpecies.Code == species.Key))
        .Where(specie => specie != null)
        .Select(specie => new FellingSpeciesModel
        {
            Id = Guid.NewGuid(),
            Species = specie.Code,
            SpeciesName = specie.IsLarch ? $"<b>{specie.Name}</b>" : specie.Name
        })
        .OrderBy(specie => !specie.SpeciesName.Contains("<b>")) // Larch species (with <b> tags) come first
        .ThenBy(specie => specie.SpeciesName.Replace("<b>", "").Replace("</b>", "")); // Then sort alphabetically

    public IEnumerable<FellingSpeciesModel> AllLarchOnlySpecies => DetailsList
        .SelectMany(detail => detail.FellingDetail.Species)
        .Select(species => TreeSpeciesFactory.SpeciesDictionary.Values.FirstOrDefault(treeSpecies => treeSpecies.Code == species.Key))
        .Where(specie => specie?.IsLarch ?? false)
        .Select(specie => new FellingSpeciesModel
        {
            Id = Guid.NewGuid(),
            Species = specie.Code,
            SpeciesName = specie.Name
        })
        .OrderBy(specie => specie.SpeciesName);

    public DateTime FadLarchExtension(LarchOptions o)
    {
        DateTime FlyoverPeriodStartDate = new DateTime(DateTime.UtcNow.Year, o.FlyoverPeriodStartMonth, o.FlyoverPeriodStartDay, 0, 0, 0, DateTimeKind.Utc);
        DateTime FlyoverPeriodEndDate = new DateTime(DateTime.UtcNow.Year, o.FlyoverPeriodEndMonth, o.FlyoverPeriodEndDay, 0, 0, 0, DateTimeKind.Utc);
        DateTime EarlyFad = new DateTime(DateTime.UtcNow.Year, o.EarlyFadMonth, o.EarlyFadDay, 0, 0, 0, DateTimeKind.Utc);
        DateTime LateFad = new DateTime(DateTime.UtcNow.Year, o.LateFadMonth, o.LateFadDay, 0, 0, 0, DateTimeKind.Utc);

        var submissionDate = DateReceived!.Value;

        return (submissionDate >= FlyoverPeriodStartDate && submissionDate <= FlyoverPeriodEndDate)
            ? LateFad                                                                               // inside Moratorium - Late FAD
            : submissionDate < FlyoverPeriodStartDate
                ? EarlyFad                                                                          // before Moratorium - Early FAD
                : EarlyFad.AddYears(1);                                                             // after  Moratorium - Next year Early FAD

    }

    public bool IsCBWapplication
    {
        get
        {
            bool areAllSpeciesCBW = DetailsList
                .All(detail =>
                    detail.FellingDetail.Species.All(specie => specie.Key == "CBW") &&
                    (detail.RestockingDetail?.Species == null || detail.RestockingDetail.Species.All(specie => specie.Key == "CBW"))
                );

            bool areAllFellingIndividualTrees = DetailsList
                .All(detail => detail.FellingDetail.OperationType == FellingOperationType.FellingIndividualTrees);

            bool areAllRestockingIndividualTrees = DetailsList
                .All(detail => detail.RestockingDetail?.RestockingProposal == TypeOfProposal.RestockWithIndividualTrees);

            return areAllSpeciesCBW && areAllFellingIndividualTrees && areAllRestockingIndividualTrees;
        }
    }


    /// <summary>
    /// Gets the previous status of the felling licence application based on the status history.
    /// </summary>
    /// <remarks>
    /// If there are no status histories, returns <see cref="FellingLicenceStatus.Submitted"/>.
    /// If there is only one status history, returns its status.
    /// Otherwise, returns the status of the second most recent status history.
    /// </remarks>
    public FellingLicenceStatus PreviousStatus =>
    StatusHistories.Count < 1
        ? FellingLicenceStatus.Submitted
        : StatusHistories.Count < 2
            ? StatusHistories.OrderByDescending(x => x.Created).First().Status
            : StatusHistories.OrderByDescending(x => x.Created).ElementAt(1).Status;

    /// <summary>
    /// Gets a flag to indicate that the application is in a state
    /// where it is being worked on by an internal user.
    /// </summary>
    public bool IsWithFcStatus => Status is FellingLicenceStatus.Submitted
        or FellingLicenceStatus.AdminOfficerReview
        or FellingLicenceStatus.WoodlandOfficerReview
        or FellingLicenceStatus.SentForApproval;
}