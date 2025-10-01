using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.External.Web.Models.FellingLicenceApplication.ReviewFellingAndRestockingAmendments;

/// <summary>
/// Represents the details of confirmed felling and restocking for woodland officer review.
/// Provides methods to retrieve deleted felling and restocking operations for compartments.
/// </summary>
public class ConfirmedFellingRestockingDetailsModel 
{
    /// <summary>
    /// Gets or sets the unique identifier for the application.
    /// </summary>
    [HiddenInput]
    public Guid ApplicationId { get; set; }

    /// <summary>
    /// Gets or sets the collection of compartments with confirmed felling and restocking details.
    /// Each compartment contains the confirmed felling details and associated restocking information
    /// as reviewed by the woodland officer.
    /// </summary>
    public CompartmentConfirmedFellingRestockingDetailsModel[] Compartments { get; set; } = [];

    /// <summary>
    /// Gets or sets the collection of proposed felling details for each compartment.
    /// Each item contains the proposed felling operations and associated restocking information
    /// as submitted in the application, prior to woodland officer review.
    /// </summary>
    public CompartmentProposedFellingRestockingDetailsModel[] ProposedFellingDetails { get; set; } = [];

    /// <summary>
    /// A collection of activity feed items related to the confirmed felling and restocking details.
    /// </summary>
    public List<ActivityFeedItemModel> ActivityFeedItems { get; set; } = [];

    /// <summary>
    /// Retrieves the deleted felling operations for a given compartment.
    /// Deleted operations are those that exist in the proposed felling details
    /// but do not have a corresponding confirmed felling detail for the specified compartment.
    /// </summary>
    /// <param name="compartmentId">The unique identifier of the compartment.</param>
    /// <returns>
    /// An array of <see cref="Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview.ProposedFellingDetailModel"/> representing the deleted felling operations.
    /// </returns>
    public Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview.ProposedFellingDetailModel[] DeletedFellingOperations(Guid? compartmentId)
    {
        if (compartmentId is null) return [];

        var proposedForCompartment = ProposedFellingDetails
            .SingleOrDefault(x => x.CompartmentId == compartmentId)?.ProposedFellingDetails;

        var confirmedForCompartment = Compartments
            .SingleOrDefault(x => x.CompartmentId == compartmentId)?.ConfirmedFellingDetails;

        if (proposedForCompartment is null || confirmedForCompartment is null)
        {
            return [];
        }

        return proposedForCompartment.Where(x =>
            confirmedForCompartment.NotAny(y =>
                y.ProposedFellingDetailsId == x.Id)).ToArray();
    }

    /// <summary>
    /// Retrieves the deleted restocking operations for a given proposed felling detail ID.
    /// Deleted operations are those that exist in the proposed restocking details
    /// but do not have a corresponding confirmed restocking detail for the specified felling detail.
    /// </summary>
    /// <param name="proposedFellingDetailId">The unique identifier of the proposed felling detail.</param>
    /// <returns>
    /// An array of <see cref="Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview.ProposedRestockingDetailModel"/> representing the deleted restocking operations.
    /// </returns>
    public Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview.ProposedRestockingDetailModel[] DeletedRestockingOperations(Guid? proposedFellingDetailId)
    {
        if (proposedFellingDetailId is null) return [];

        var proposedRestockingDetails = ProposedFellingDetails
            .SelectMany(x => x.ProposedFellingDetails)
            .Where(x => x.Id == proposedFellingDetailId)
            .SelectMany(x => x.ProposedRestockingDetails)
            .ToArray();
        var confirmedRestockingDetails = Compartments
            .SelectMany(x => x.ConfirmedFellingDetails)
            .Where(x => x.ProposedFellingDetailsId == proposedFellingDetailId)
            .SelectMany(x => x.ConfirmedRestockingDetails)
            .ToArray();

        return proposedRestockingDetails.Where(x =>
            confirmedRestockingDetails.NotAny(y =>
                y.ProposedRestockingDetailsId == x.Id)).ToArray();
    }

    /// <summary>
    /// Gets or sets a value indicating whether the confirmed felling and restocking process is complete.
    /// </summary>
    public bool ConfirmedFellingAndRestockingComplete { get; set; }

    /// <summary>
    /// Gets and sets a value indicating whether the current user can amend the confirmed felling and restocking details.
    /// </summary>
    public required bool CanCurrentUserAmend { get; set; }

    /// <summary>
    /// Gets a value indicating whether any confirmed felling details have amended properties.
    /// </summary>
    public bool IsAmended
    {
        get
        {
            return Compartments
                .Where(c => c?.ConfirmedFellingDetails != null)
                .SelectMany(c => c.ConfirmedFellingDetails)
                .Any(d => d?.AmendedProperties is { Count: > 0 });
        }
    }
}