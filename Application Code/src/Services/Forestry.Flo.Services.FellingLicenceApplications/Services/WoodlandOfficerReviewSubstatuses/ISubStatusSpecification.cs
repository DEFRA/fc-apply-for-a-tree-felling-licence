using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services.WoodlandOfficerReviewSubstatuses;

/// <summary>
/// Defines a specification for determining if a <see cref="FellingLicenceApplication"/> 
/// satisfies a particular woodland officer review sub-status.
/// </summary>
public interface ISubStatusSpecification
{
    /// <summary>
    /// Determines whether the specified <see cref="FellingLicenceApplication"/> satisfies the sub-status condition.
    /// </summary>
    /// <param name="application">The felling licence application to evaluate.</param>
    /// <returns><c>true</c> if the application satisfies the sub-status; otherwise, <c>false</c>.</returns>
    bool IsSatisfiedBy(FellingLicenceApplication application);

    /// <summary>
    /// Gets the woodland officer review sub-status associated with this specification.
    /// </summary>
    WoodlandOfficerReviewSubStatus SubStatus { get; }
}