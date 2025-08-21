using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;

/// <summary>
/// View model for a detailed summary of a felling licence application.
/// </summary>
public class FellingLicenceApplicationSummaryModel
{
    /// <summary>
    /// Gets and sets the unique identifier for the felling licence application.
    /// </summary>
    [HiddenInput]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets and sets the application reference for the felling licence application.
    /// </summary>
    public string ApplicationReference { get; set; }

    /// <summary>
    /// Gets and sets the current status of the felling licence application.
    /// </summary>
    public FellingLicenceStatus Status { get; set; }

    /// <summary>
    /// Gets and sets the list of status histories for the felling licence application.
    /// </summary>
    public IList<StatusHistoryModel> StatusHistories { get; set; } = [];

    /// <summary>
    /// Gets and sets the name of the property associated with the felling licence application.
    /// </summary>
    public string? PropertyName { get; set; }

    /// <summary>
    /// Gets and sets the name of the nearest town for the property associated with the felling licence application.
    /// </summary>
    public string? NearestTown { get; set; }

    /// <summary>
    /// Gets and sets the name of the woodland associated with the felling licence application.
    /// </summary>
    public string? NameOfWood { get; set; }

    /// <summary>
    /// Gets and sets the name of the woodland owner associated with the felling licence application.
    /// </summary>
    public string? WoodlandOwnerName { get; set; }

    /// <summary>
    /// Gets and sets the unique identifier of the woodland owner associated with the felling licence application.
    /// </summary>
    public Guid? WoodlandOwnerId { get; set; }

    /// <summary>
    /// Gets and sets the name of the agent or agency associated with the felling licence application.
    /// </summary>
    public string? AgentOrAgencyName { get; set; }

    /// <summary>
    /// Gets and sets a <see cref="DocumentModel"/> representing the most recent LIS report attached to the felling licence application.
    /// </summary>
    public Maybe<DocumentModel> MostRecentFcLisReport { get; set; }

    /// <summary>
    /// Gets and sets a <see cref="DocumentModel"/> representing the most recent application document attached to the felling licence application.
    /// </summary>
    public Maybe<DocumentModel> MostRecentApplicationDocument { get; set; }

    /// <summary>
    /// Gets and sets the assignee history for the felling licence application.
    /// </summary>
    public IList<AssigneeHistoryModel> AssigneeHistories { get; set; }

    /// <summary>
    /// Gets and sets the citizens charter date for the felling licence application, if applicable.
    /// </summary>
    public DateTime? CitizensCharterDate { get; set; }

    /// <summary>
    /// Gets and sets the final action date for the felling licence application, if applicable.
    /// </summary>
    public DateTime? FinalActionDate { get; set; }

    /// <summary>
    /// Gets and sets the date when the felling licence application was received, if applicable.
    /// </summary>
    public DateTime? DateReceived { get; set; }

    /// <summary>
    /// Gets and sets the proposed start date for the felling licence application.
    /// </summary>
    public DateTime? ProposedStartDate { get; set; }

    /// <summary>
    /// Gets and sets the proposed end date for the felling licence application.
    /// </summary>
    public DateTime? ProposedEndDate { get; set; }

    /// <summary>
    /// Gets and sets the source of the felling licence application, indicating where it originated from.
    /// </summary>
    public FellingLicenceApplicationSource Source { get; set; }

    /// <summary>
    /// Gets and sets the list of details for proposed felling and restocking associated with the felling licence application.
    /// </summary>
    public List<FellingAndRestockingDetail> DetailsList { get; set; } = new();

    /// <summary>
    /// Gets and sets the unique identifier of the user who created the felling licence application.
    /// </summary>
    public Guid CreatedById { get; set; }

    /// <summary>
    /// Gets and sets the area code for the felling licence application.
    /// </summary>
    public string? AreaCode { get; set; }

    /// <summary>
    /// Gets and sets the administrative region name for the felling licence application.
    /// </summary>
    public string? AdministrativeRegion { get; set; }

    /// <summary>
    /// Gets if there are any larch species in the proposed felling details for the application.
    /// </summary>
    public bool AreAnyLarchSpecies => (DetailsList
            .SelectMany(detail => detail.FellingDetail.Species)
            .Select(species => TreeSpeciesFactory.SpeciesDictionary.Values.FirstOrDefault(treeSpecies => treeSpecies.Code == species.Key))
        )
        .Where(specie => specie?.IsLarch ?? false).Any();

    /// <summary>
    /// Gets if all species in the proposed felling details for the application are larch species.
    /// </summary>
    public bool AreAllSpeciesLarch => DetailsList
        .SelectMany(detail => detail.FellingDetail.Species)
        .Select(species => TreeSpeciesFactory.SpeciesDictionary.Values.FirstOrDefault(treeSpecies => treeSpecies.Code == species.Key))
        .All(specie => specie?.IsLarch ?? false);

    /// <summary>
    /// Gets a collection of all species in the proposed felling details for the application, ordered with larch species first.
    /// </summary>
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

    /// <summary>
    /// Gets a collection of all species in the proposed felling details for the application, with larch species in bold.
    /// </summary>
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

    /// <summary>
    /// Gets a collection of all larch species in the proposed felling details for the application.
    /// </summary>
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

    /// <summary>
    /// Calculates the Final Action Date (Fad) extension for larch based on the submission date and the provided larch options.
    /// </summary>
    /// <param name="o"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Gets whether the application can be considered a Cricket Bat Willow (CBW) application.
    /// </summary>
    public bool IsCBWapplication
    {
        get
        {
            bool areAllSpeciesCBW = DetailsList
                .All(detail =>
                    detail.FellingDetail.Species.All(specie => specie.Key == "CBW") &&
                    detail.RestockingDetail.All(r => r.Species.All(specie => specie.Key == "CBW"))
                );

            bool areAllFellingIndividualTrees = DetailsList
                .All(detail => detail.FellingDetail.OperationType == FellingOperationType.FellingIndividualTrees);

            bool areAllRestockingIndividualTrees = DetailsList.SelectMany(x => x.RestockingDetail)
                .All(detail => detail.RestockingProposal == TypeOfProposal.RestockWithIndividualTrees);

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