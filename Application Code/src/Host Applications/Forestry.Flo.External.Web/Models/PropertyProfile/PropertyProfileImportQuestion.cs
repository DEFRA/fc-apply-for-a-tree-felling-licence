using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.External.Web.Models.PropertyProfile;

public class PropertyProfileImportQuestion : PageWithBreadcrumbsViewModel
{
    /// <summary>
    /// Gets and Sets the property profile id
    /// </summary>
    public Guid Id { get; init; }

    [Required(ErrorMessage = "Select whether you want to import shapefiles")]
    public bool? ImportShapes { get; set; }
    public Guid? AgencyId { get; set; }
}
