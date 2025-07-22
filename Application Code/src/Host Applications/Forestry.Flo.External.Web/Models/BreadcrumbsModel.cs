namespace Forestry.Flo.External.Web.Models;

public class BreadcrumbsModel
{
    public List<BreadCrumb> Breadcrumbs { get; set; }

    public string CurrentPage { get; set; }
}

public record BreadCrumb(string Text, string Controller, string Action, string? RouteId);