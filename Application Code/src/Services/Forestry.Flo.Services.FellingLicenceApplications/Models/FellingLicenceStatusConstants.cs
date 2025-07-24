using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Services.FellingLicenceApplications.Models;

/// <summary>
/// A static class containing constants for FellingLicenceStatus.
/// </summary>
public static class FellingLicenceStatusConstants
{
    /// <summary>
    /// A collection of FellingLicenceStatus values that can transition to withdrawn.
    /// </summary>
    public static readonly FellingLicenceStatus[] WithdrawalStatuses =
    [
        FellingLicenceStatus.WithApplicant,
        FellingLicenceStatus.ReturnedToApplicant,
        FellingLicenceStatus.Submitted,
        FellingLicenceStatus.AdminOfficerReview,
        FellingLicenceStatus.WoodlandOfficerReview,
        FellingLicenceStatus.SentForApproval
    ];

    /// <summary>
    /// A collection of FellingLicenceStatus values that can transition to submitted.
    /// </summary>
    public static readonly FellingLicenceStatus[] SubmitStatuses =
    [
        FellingLicenceStatus.Draft,
        FellingLicenceStatus.WithApplicant,
        FellingLicenceStatus.ReturnedToApplicant
    ];
}