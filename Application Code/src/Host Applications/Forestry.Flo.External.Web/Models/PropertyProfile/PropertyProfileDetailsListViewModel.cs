namespace Forestry.Flo.External.Web.Models.PropertyProfile;

public class PropertyProfileDetailsListViewModel : PageWithBreadcrumbsViewModel
{
    public IEnumerable<PropertyProfileDetails> PropertyProfileDetailsList { get; init; } =
        new List<PropertyProfileDetails>();
}