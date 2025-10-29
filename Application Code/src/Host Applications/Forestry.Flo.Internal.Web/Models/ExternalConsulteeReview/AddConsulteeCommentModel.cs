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

    [DisplayName("Your name or organisation name")]
    [Required(ErrorMessage = "Enter your name or organisation name")]
    public string AuthorName { get; set; }

    [HiddenInput]
    public string AuthorContactEmail { get; set; }

    [DisplayName("Your comments")]
    [Required(ErrorMessage = "Enter your comments on the application")]
    [MaxLength(DataValueConstants.ConsulteeCommentMaxLength)]
    public string Comment { get; set; } = null!;

    [HiddenInput]
    public DateTime LinkExpiryDateTime { get; set; }
}