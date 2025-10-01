using Forestry.Flo.Services.Common.Extensions;
using ServiceProposedRestockingDetailModel = Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview.ProposedRestockingDetailModel;
using ServiceProposedFellingDetailModel = Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview.ProposedFellingDetailModel;

namespace Forestry.Flo.External.Web.Models.FellingLicenceApplication.ReviewFellingAndRestockingAmendments;

/// <summary>
/// Represents the details of confirmed felling and restocking for display to the applicant when reviewing amendments.
/// Provides methods to retrieve deleted felling and restocking operations for compartments.
/// </summary>
public class AmendedFellingRestockingDetailsModel
{
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
    /// Retrieves the deleted felling operations for a given compartment.
    /// Deleted operations are those that exist in the proposed felling details
    /// but do not have a corresponding confirmed felling detail for the specified compartment.
    /// </summary>
    /// <param name="compartmentId">The unique identifier of the compartment.</param>
    /// <returns>
    /// An array of <see cref="Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview.ProposedFellingDetailModel"/> representing the deleted felling operations.
    /// </returns>
    public ServiceProposedFellingDetailModel[] DeletedFellingOperations(Guid? compartmentId)
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
    /// Retrieves the confirmed felling operations that have been amended for a given submitted compartment.
    /// An amended operation is one where the confirmed felling detail is not of type <see cref="ConfirmedFellingType.Unmodified"/>,
    /// which may include new, amended, or deleted restocking details.
    /// </summary>
    /// <param name="submittedCompartmentId">The unique identifier of the submitted compartment.</param>
    /// <returns>
    /// An array of <see cref="ConfirmedFellingDetailViewModel"/> representing the amended felling operations for the specified compartment.
    /// </returns>
    public List<(ConfirmedFellingDetailViewModel felling, ServiceProposedRestockingDetailModel[] deletedRestockings, ConfirmedFellingType type)> AmendedFellingOperations(Guid submittedCompartmentId)
    {
        var result =
            new List<(ConfirmedFellingDetailViewModel felling, ServiceProposedRestockingDetailModel[] deletedRestockings, ConfirmedFellingType type)>();

        var compartment = Compartments.First(x => x.SubmittedFlaPropertyCompartmentId == submittedCompartmentId);

        foreach (var confirmedFelling in compartment.ConfirmedFellingDetails)
        {
            var deletedRestockingDetails = DeletedRestockingOperations(confirmedFelling.ProposedFellingDetailsId);
            var type = confirmedFelling.GetConfirmedFellingType(deletedRestockingDetails.Length > 0);

            if (type is not ConfirmedFellingType.Unmodified)
            {
                result.Add((confirmedFelling, deletedRestockingDetails, type));
            }
        }

        return result;
    }

    /// <summary>
    /// Retrieves the deleted restocking operations for a given proposed felling detail ID.
    /// Deleted operations are those that exist in the proposed restocking details
    /// but do not have a corresponding confirmed restocking detail for the specified felling detail.
    /// </summary>
    /// <param name="proposedFellingDetailId">The unique identifier of the proposed felling detail.</param>
    /// <returns>
    /// An array of <see cref="ServiceProposedRestockingDetailModel"/> representing the deleted restocking operations.
    /// </returns>
    public ServiceProposedRestockingDetailModel[] DeletedRestockingOperations(Guid? proposedFellingDetailId)
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

    /// <summary>
    /// Gets or sets the mapping between compartment IDs and their corresponding submitted compartment IDs.
    /// This dictionary is used to relate compartments in the current review context to their original submitted counterparts.
    /// </summary>
    public required Dictionary<Guid, Guid> CompartmentIdToSubmittedCompartmentId { get; set; }

    /// <summary>
    /// Gets or sets the mapping between compartment IDs and their associated GIS lookup values.
    /// The value is a string representing GIS data or a lookup key, or null if not available.
    /// </summary>
    public required Dictionary<Guid, (string? GisData, string DisplayName)> CompartmentGisLookup { get; set; }
}