using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;
using Microsoft.AspNetCore.Mvc;
namespace Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;

/// <summary>
/// Represents the details of confirmed felling and restocking for woodland officer review.
/// This model is used to display and manage the compartments, felling, and restocking operations
/// as reviewed and confirmed by a woodland officer, including tracking amendments and activity feed items.
/// </summary>
public class ConfirmedFellingRestockingDetailsModel : WoodlandOfficerReviewModelBase
{
    /// <summary>
    /// Gets or sets the unique identifier for the felling licence application.
    /// </summary>
    [HiddenInput]
    public Guid ApplicationId { get; set; }

    /// <summary>
    /// Gets or sets the collection of compartments with confirmed felling and restocking details.
    /// Each compartment contains the confirmed felling details and associated restocking information
    /// as reviewed and confirmed by the woodland officer.
    /// </summary>
    public CompartmentConfirmedFellingRestockingDetailsModel[] Compartments { get; set; } = [];

    /// <summary>
    /// Gets or sets the collection of proposed felling details for each compartment.
    /// Each item contains the proposed felling operations and associated restocking information
    /// as originally submitted in the application, prior to woodland officer review.
    /// </summary>
    public CompartmentProposedFellingRestockingDetailsModel[] ProposedFellingDetails { get; set; } = [];

    /// <summary>
    /// Gets or sets a collection of submitted felling licence application property compartments.
    /// </summary>
    /// <remarks>
    /// This is the entirety of compartments submitted with the application.
    /// </remarks>
    public List<SubmittedFlaPropertyCompartment> SubmittedFlaPropertyCompartments { get; set; } = [];

    /// <summary>
    /// Gets or sets a collection of activity feed items related to the confirmed felling and restocking details,
    /// such as amendments, comments, or status changes.
    /// </summary>
    public List<ActivityFeedItemModel> ActivityFeedItems { get; set; } = [];

    /// <summary>
    /// Retrieves the deleted felling operations for a given compartment.
    /// Deleted operations are those that exist in the proposed felling details
    /// but do not have a corresponding confirmed felling detail for the specified compartment.
    /// </summary>
    /// <param name="compartmentId">The unique identifier of the compartment.</param>
    /// <returns>
    /// An array of <see cref="ProposedFellingDetailModel"/> representing the deleted felling operations
    /// for the specified compartment, or an empty array if none are found.
    /// </returns>
    public ProposedFellingDetailModel[] DeletedFellingOperations(Guid? compartmentId)
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
    /// An array of <see cref="ProposedRestockingDetailModel"/> representing the deleted restocking operations
    /// for the specified felling detail, or an empty array if none are found.
    /// </returns>
    public ProposedRestockingDetailModel[] DeletedRestockingOperations(Guid? proposedFellingDetailId)
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
    /// Combined amendment information for this application.
    /// </summary>
    public required AmendmentReview Amendment { get; set; }
}

public class AmendmentReview
{
    /// <summary>
    /// Gets or sets a value indicating whether the current user can amend the confirmed felling and restocking details.
    /// </summary>
    public required bool CanCurrentUserAmend { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether further amendments are allowed or present.
    /// </summary>
    public bool FurtherAmendments { get; set; }

    /// <summary>
    /// Gets or sets the current amendment state for the application.
    /// </summary>
    public required FellingLicenceApplication.AmendmentStateEnum AmendmentState { get; set; }

    /// <summary>
    /// Gets or sets the date when amendments were sent to the applicant, if applicable.
    /// </summary>
    public DateTime? AmendmentsSentDate { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier for the amendment review, if applicable.
    /// </summary>
    public Guid? AmendmentReviewId { get; set; }

    /// <summary>
    /// Gets or sets the reason for the amendment, if provided by the woodland officer.
    /// </summary>
    public string? AmendmentReason { get; set; }

    /// <summary>
    /// Gets or sets the reason provided by the applicant for disagreeing with the amendment, if applicable.
    /// </summary>
    public string? ApplicantDisagreementReason { get; set; }

    /// <summary>
    /// Gets a value indicating whether any confirmed felling details have amended properties.
    /// Returns true if at least one confirmed felling detail contains amended properties.
    /// </summary>
    public bool IsAmended { get; set; }
}