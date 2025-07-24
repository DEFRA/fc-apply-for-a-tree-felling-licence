using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;

public class AddCaseNoteModel
{
    /// <summary>
    /// The ID of the felling licence application to add the case note for.
    /// </summary>
    /// <remarks>
    /// Must already be set on the model before it is passed to the partial view.
    /// </remarks>
    [HiddenInput]
    public Guid FellingLicenceApplicationId { get; set; }

    /// <summary>
    /// The type for the case note to be added.
    /// </summary>
    /// <remarks>
    /// Must already be set on the model before it is passed to the partial view.
    /// </remarks>
    [HiddenInput]
    public CaseNoteType CaseNoteType { get; set; }

    /// <summary>
    /// The text of the case note.
    /// </summary>
    [Required]
    public string Text { get; set; }

    /// <summary>
    /// A flag indicating whether or note the case note should be visible to external applicant users.
    /// </summary>
    [BindRequired]
    public bool VisibleToApplicant { get; set; }

    /// <summary>
    /// A flag indicating whether or note the case note should be visible to external consultees.
    /// </summary>
    [BindRequired]
    public bool VisibleToConsultee { get; set; }

    /// <summary>
    /// The URL that the CaseNoteController should redirect to on successfully saving a case note.
    /// </summary>
    /// <remarks>
    /// Must already be set on the model before it is passed to the partial view.
    /// </remarks>
    [HiddenInput]
    public string ReturnUrl { get; set; }
}