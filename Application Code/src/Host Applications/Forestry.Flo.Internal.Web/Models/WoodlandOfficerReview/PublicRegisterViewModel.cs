using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.Notifications.Models;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;

/// <summary>
/// View model class for the Public Register page of the woodland officer review.
/// </summary>
public class PublicRegisterViewModel : WoodlandOfficerReviewModelBase
{
    /// <summary>
    /// Gets and sets the id of the application.
    /// </summary>
    [HiddenInput]
    public Guid ApplicationId { get; set; }
    
    /// <summary>
    /// Gets and sets a populated <see cref="PublicRegisterModel"/>.
    /// </summary>
    public PublicRegisterModel PublicRegister { get; set; }

    /// <summary>
    /// Gets and sets a collection of <see cref="NotificationHistoryModel"/> representing received public register comments.
    /// </summary>
    public IEnumerable<NotificationHistoryModel> ReceivedPublicRegisterComments { get; set; }

    /// <summary>
    /// Gets and sets a model for the remove from public register form.
    /// </summary>
    public RemoveFromPublicRegisterModel RemoveFromPublicRegister { get; set; }
}