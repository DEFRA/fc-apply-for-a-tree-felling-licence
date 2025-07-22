namespace Forestry.Flo.External.Web.Models.PropertyProfile;

public interface IPropertyWithBreadcrumbsViewModel
{
    public string Name { get; set; }

    public BreadcrumbsModel? Breadcrumbs { get; set; }
}