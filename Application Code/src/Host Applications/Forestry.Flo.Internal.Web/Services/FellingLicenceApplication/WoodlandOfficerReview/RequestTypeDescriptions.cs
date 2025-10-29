using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.WoodlandOfficerReview;

/// <summary>
/// Provides display text descriptions for <see cref="RequestType"/> values used in woodland officer reviews.
/// </summary>
public static class RequestTypeDescriptions
{
    /// <summary>
    /// A mapping of <see cref="RequestType"/> values to their corresponding display text descriptions.
    /// </summary>
    private static readonly Dictionary<RequestType, string> Messages = new()
        {
            { RequestType.Reminder, "The applicant was reminded to submit the EIA form." },
            { RequestType.MissingDocuments, "The applicant was reminded to submit missing EIA documents." }
        };

    /// <summary>
    /// Gets the display text description for the specified <see cref="RequestType"/>.
    /// </summary>
    /// <param name="code">The <see cref="RequestType"/> for which to get the description.</param>
    /// <returns>
    /// The display text associated with the specified <paramref name="code"/>, or an empty string if not found.
    /// </returns>
    public static string GetDisplayText(RequestType code) =>
        Messages.TryGetValue(code, out var value)
            ? value
            : string.Empty;
}