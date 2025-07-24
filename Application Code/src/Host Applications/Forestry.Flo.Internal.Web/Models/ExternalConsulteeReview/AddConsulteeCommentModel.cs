using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.Internal.Web.Models.ExternalConsulteeReview;

public class AddConsulteeCommentModel
{
    [HiddenInput]
    public Guid ApplicationId { get; set; }

    [HiddenInput]
    public string AccessCode { get; set; }

    [DisplayName("Your name")]
    [Required(ErrorMessage = "Your name must be provided")]
    public string AuthorName { get; set; }

    [HiddenInput]
    public string AuthorContactEmail { get; set; }

    [Required(ErrorMessage = "Comment text must be provided")]
    [MaxLength(DataValueConstants.ConsulteeCommentMaxLength)]
    public string Comment { get; set; } = null!;

    [DisplayName("Applicable to section (optional)")]
    public ApplicationSection? ApplicableToSection { get; set; }

    [HiddenInput]
    public DateTime LinkExpiryDateTime { get; set; }
}