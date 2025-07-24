using Forestry.Flo.Services.ConditionsBuilder.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;

/// <summary>
/// View model class for the Conditions page of the woodland officer review
/// </summary>
public class ConditionsViewModel : WoodlandOfficerReviewModelBase
{
    /// <summary>
    /// Gets and sets the application id.
    /// </summary>
    [HiddenInput]
    public Guid ApplicationId { get; set; }

    /// <summary>
    /// Gets and sets a flag indicating whether or not the confirmed felling and restocking details are complete.
    /// </summary>
    public bool ConfirmedFellingAndRestockingComplete { get; set; }
    
    /// <summary>
    /// Gets and sets the current status of conditions as part of the woodland officer review.
    /// </summary>
    public ConditionsStatusModel ConditionsStatus { get; set; }

    /// <summary>
    /// Gets and sets the list of conditions as calculated by the conditions builder engine.
    /// </summary>
    public List<CalculatedCondition> Conditions { get; set; }
}