using Forestry.Flo.External.Web.Infrastructure;

namespace Forestry.Flo.External.Web.Models.FellingLicenceApplication;

public class SelectWoodlandModel : ApplicationStepBase, IApplicationWithBreadcrumbsViewModel
{
    public override Guid ApplicationId { get; set; } = Guid.Empty;

    public Guid WoodlandOwnerId { get; set; }

    public Guid? AgencyId { get; set; }

    [NoGuidEmpty(ErrorMessage = "Select a woodland property or create a new one")]
    public Guid PropertyProfileId { get; set; }

    public string? ApplicationReference { get; set; }

    public BreadcrumbsModel? Breadcrumbs { get; set; }

    public CreateApplicationAgencySourcePage? AgencySourcePage { get; set; }

    public string TaskName => "Application details";
}