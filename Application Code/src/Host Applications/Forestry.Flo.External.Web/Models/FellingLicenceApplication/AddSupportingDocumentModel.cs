using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Forestry.Flo.External.Web.Models.FellingLicenceApplication;

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
    /// Always true - documents uploaded via the External app should now all be visible to external consultees.
    /// </summary>
    [BindNever]
    public bool AvailableToConsultees { get; } = true;

    /// <summary>
    /// A count of how many documents are attached to the application.
    /// </summary>
    public int DocumentCount { get; set; }

    /// <summary>
    /// A flag indicating whether or not to return to the Application Summary
    /// </summary>
    public bool ReturnToApplicationSummary { get; set; }

    /// <summary>
    /// A flag indicating whether the user navigated to this page via the data import.
    /// </summary>
    /// <remarks>
    /// Only applicable when used on the WMP Documents page.
    /// </remarks>
    public bool FromDataImport { get; set; }

    /// <summary>
    /// Gets and sets the purpose of the document(s) being uploaded.
    /// </summary>
    public DocumentPurpose Purpose { get; set; } = DocumentPurpose.Attachment;
}