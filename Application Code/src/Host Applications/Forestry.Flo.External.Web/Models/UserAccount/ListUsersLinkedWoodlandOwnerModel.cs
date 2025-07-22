namespace Forestry.Flo.External.Web.Models.UserAccount
{
    public class ListUsersLinkedWoodlandOwnerModel: PageWithBreadcrumbsViewModel
    {
        public IList<Flo.Services.Applicants.Models.UserAccountModel> WoodlandOwnerUsers { get; set; }
    }
}
