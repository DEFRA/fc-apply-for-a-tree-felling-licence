using Forestry.Flo.External.Web.Models.FellingLicenceApplication.HabitatRestoration;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;

namespace Forestry.Flo.External.Web.Models.FellingLicenceApplication;

/// <summary>
/// View model for the application summary page.
/// </summary>
public class FellingLicenceApplicationSummaryViewModel
{
    public FellingLicenceApplicationModel Application { get; set; }

    public AgencyModel? Agency { get; set; }

    public WoodlandOwnerModel WoodlandOwner { get; set; }

    public FellingLicenceApplicationSummaryPropertyDetails PropertyProfile { get; set; }

    public FellingAndRestockingPlaybackViewModel FellingAndRestocking { get; set; }

    public List<PawsCompartmentDesignationsModel> PawsCompartmentDesignations { get; set; }

    public IReadOnlyList<HabitatRestorationViewModel> HabitatRestorations { get; set; }
}


public record FellingLicenceApplicationSummaryPropertyDetails
{
    /// <summary>
    /// Gets and Sets the property profile name.
    /// </summary>
    public string Name { get; protected set; } = null!;

    /// <summary>
    /// Gets and Sets the nearest town.
    /// </summary>
    public string? NearestTown { get; protected set; }

    /// <summary>
    /// Gets and sets a flag indicating the property profile has Woodland Management Plan.
    /// </summary>
    public bool HasWoodlandManagementPlan { get; protected set; }

    /// <summary>
    /// Gets and Sets the Woodland Management Plan reference.
    /// </summary>
    public string? WoodlandManagementPlanReference { get; protected set; }

    /// <summary>
    /// Gets and sets a flag indicating the property profile has Woodland Certification Scheme.
    /// </summary>
    public bool IsWoodlandCertificationScheme { get; protected set; }

    /// <summary>
    /// Gets and Sets the Woodland Certification Scheme reference.
    /// </summary>
    public string? WoodlandCertificationSchemeReference { get; set; }

    public FellingLicenceApplicationSummaryPropertyDetails(Flo.Services.PropertyProfiles.Entities.PropertyProfile property)
    {
        Name = property.Name;
        NearestTown = property.NearestTown;
        HasWoodlandManagementPlan = property.HasWoodlandManagementPlan;
        WoodlandManagementPlanReference = property.WoodlandManagementPlanReference;
        IsWoodlandCertificationScheme = property.IsWoodlandCertificationScheme;
        WoodlandCertificationSchemeReference = property.WoodlandCertificationSchemeReference;
    }

    public FellingLicenceApplicationSummaryPropertyDetails(SubmittedFlaPropertyDetail property)
    {
        Name = property.Name;
        NearestTown = property.NearestTown;
        HasWoodlandManagementPlan = property.HasWoodlandManagementPlan;
        WoodlandManagementPlanReference = property.WoodlandManagementPlanReference;
        IsWoodlandCertificationScheme = property.IsWoodlandCertificationScheme is true;
        WoodlandCertificationSchemeReference = property.WoodlandCertificationSchemeReference;
    }
}