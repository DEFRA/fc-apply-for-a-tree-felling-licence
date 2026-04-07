using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Internal.Web.Models.ExternalConsulteeReview;

public class AddConsulteeCommentModel
{
    [HiddenInput]
    public Guid ApplicationId { get; set; }

    [HiddenInput]
    public Guid AccessCode { get; set; }

    [DisplayName("Name")]
    [Required(ErrorMessage = "Enter your full name")]
    public string AuthorName { get; set; }

    [DisplayName("Job role")]
    [Required(ErrorMessage = "Enter your job role")]
    public string AuthorJobRole { get; set; }

    [DisplayName("Organisation")]
    [Required(ErrorMessage = "Enter your organisation's name")]
    public string AuthorOrganisation { get; set; }

    [HiddenInput]
    public string AuthorContactEmail { get; set; }

    [DisplayName("Your comments")]
    [Required(ErrorMessage = "Enter your comments on the application")]
    public string Comment { get; set; } = null!;

    [HiddenInput]
    public DateTime LinkExpiryDateTime { get; set; }
}