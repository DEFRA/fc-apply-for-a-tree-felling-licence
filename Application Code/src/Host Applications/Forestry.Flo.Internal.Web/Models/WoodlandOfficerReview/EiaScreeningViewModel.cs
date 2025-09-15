using System.ComponentModel.DataAnnotations;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;

/// <summary>
/// A view model for the EIA screening section of the Woodland Officer review.
/// </summary>
public class EiaScreeningViewModel : WoodlandOfficerReviewModelBase
{
    /// <summary>
    /// Gets and sets the unique identifier of the application.
    /// </summary>
    public required Guid ApplicationId { get; set; }

    /// <summary>
    /// Gets and sets a flag indicating whether the EIA screening has been completed.
    /// </summary>
    public bool ScreeningCompleted { get; set; }

    /// <summary>
    /// Gets and sets the collection of request history items associated with the EIA.
    /// </summary>
    public IEnumerable<RequestHistoryItem> RequestHistoryItems { get; set; } = [];

    /// <summary>
    /// Gets and sets the collection of document models associated with the EIA.
    /// </summary>
    public required IEnumerable<DocumentModel> EiaDocumentModels { get; set; }
}

/// <summary>
/// Represents a single request history item for the EIA screening section, including
/// the user who made the request, the date and time it was made, and the type of request.
/// </summary>
/// <remarks>
/// These are used to populate the request history section of the EIA screening page.
/// </remarks>
/// <param name="RequestedBy">The name of the user who made the request.</param>
/// <param name="RequestedOn">The date and time when the request was made.</param>
/// <param name="RequestType">The type of EIA request made.</param>
public record RequestHistoryItem(string RequestedBy, DateTime RequestedOn, RequestType RequestType);