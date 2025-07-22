using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Services.FellingLicenceApplications.Models.ExternalConsultee;

public record ConsulteeCommentModel
{
    public Guid FellingLicenceApplicationId { get; init; }

    public string AuthorName { get; init; } = null!;

    public string AuthorContactEmail { get; init; } = null!;
    
    public string Comment { get; set; } = null!;
    
    public ApplicationSection? ApplicableToSection { get; init; }

    public DateTime CreatedTimestamp { get; set; }
}