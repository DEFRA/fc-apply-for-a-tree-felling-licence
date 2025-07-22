using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;

public class AddSupportingDocumentModel
{
    /// <summary>
    /// The ID of the felling licence application to add the supporting document to.
    /// </summary>
    /// <remarks>
    /// Must already be set on the model before it is passed to the partial view.
    /// </remarks>
    [HiddenInput]
    public Guid FellingLicenceApplicationId { get; set; }

    /// <summary>
    /// A flag indicating whether or not the document should be visible to external applicant users.
    /// </summary>
    [BindRequired]
    public bool AvailableToApplicant { get; set; }

    /// <summary>
    /// A flag indicating whether or not the document should be visible to external consultees.
    /// </summary>
    [BindRequired]
    public bool AvailableToConsultees { get; set; }

    /// <summary>
    /// A count of how many documents are attached to the application.
    /// </summary>
    public int DocumentCount { get; set; }
}