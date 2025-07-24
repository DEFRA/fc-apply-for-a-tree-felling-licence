namespace Forestry.Flo.External.Web.Models.FellingLicenceApplication;

public interface IApplicationWithBreadcrumbsViewModel
{
    public Guid ApplicationId { get; set; }
    
    public string? ApplicationReference { get; set; } 

    public BreadcrumbsModel? Breadcrumbs { get; set; }

    public string TaskName { get; }
}