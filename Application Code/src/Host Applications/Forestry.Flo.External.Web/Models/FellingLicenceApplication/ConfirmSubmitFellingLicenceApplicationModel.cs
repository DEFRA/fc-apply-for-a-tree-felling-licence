namespace Forestry.Flo.External.Web.Models.FellingLicenceApplication
{
    public class ConfirmSubmitFellingLicenceApplicationModel : IApplicationWithBreadcrumbsViewModel
    {
        public Guid ApplicationId { get; set; }
        public string? ApplicationReference { get; set; }
        public BreadcrumbsModel? Breadcrumbs { get; set; }
        public string TaskName { get; set; }
        public bool ValidAuthorityForm { get; set; } = true;
    }
}
