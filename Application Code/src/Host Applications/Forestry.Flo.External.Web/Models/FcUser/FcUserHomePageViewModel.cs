using Forestry.Flo.Services.Applicants.Models;

namespace Forestry.Flo.External.Web.Models.FcUser;

/// <summary>
/// The View model to support the FC User homepage 
/// </summary>
public class FcUserHomePageViewModel
{
    /// <summary>
    /// The list of all Woodland Owners managed by FC, having no connected external users
    /// </summary>
    public IReadOnlyList<WoodlandOwnerFcModel> AllWoodlandOwnersManagedByFc { get; set; } = new List<WoodlandOwnerFcModel>();

    /// <summary>
    /// The list of all External Woodland Owners not managed by FC, but by non-fc external users
    /// </summary>
    public IReadOnlyList<WoodlandOwnerFcModel> AllExternalWoodlandOwners { get; set; } = new List<WoodlandOwnerFcModel>();

    /// <summary>
    /// The list of all Agencies managed by FC, having no connected external users
    /// </summary>

    public IReadOnlyList<AgencyFcModel> AllAgenciesManagedByFc { get; set; } = new List<AgencyFcModel>();

    /// <summary>
    /// The list of all External Agencies not managed by FC, but by non-fc external users
    /// </summary>
    public IReadOnlyList<AgencyFcModel> AllExternalAgencies { get; set; } = new List<AgencyFcModel>();
}
