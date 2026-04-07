using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;

namespace Forestry.Flo.Services.Gis.Models.Internal.Request;


/// <summary>
/// Model of the data items required to add or update the case on the public register.
/// </summary>
public class AddToPublicRegisterModel
{
    /// <summary>
    /// The existing Esri ID of the case, if it exists. This is used to determine whether to
    /// add a new case to the public register, or update an existing case.
    /// </summary>
    public int? ExistingEsriId { get; set; }
    
    /// <summary>
    /// The reference for the case.
    /// </summary>
    public string CaseReference { get; set; }

    /// <summary>
    /// The name of the property.
    /// </summary>
    public string PropertyName { get; set; }

    /// <summary>
    /// The case type for this case on the public register.
    /// </summary>
    public string CaseType { get; set; }

    /// <summary>
    /// The OS grid reference for the centre point of the case.
    /// </summary>
    public string GridReference { get; set; }

    /// <summary>
    /// The nearest town to the case location, used for display purposes on the public register.
    /// </summary>
    public string NearestTown { get; set; }

    /// <summary>
    /// The name of the local authority.
    /// </summary>
    public string LocalAuthority { get; set; }

    /// <summary>
    /// The name of the FC admin region covering the case.
    /// </summary>
    public string AdminRegion { get; set; }

    /// <summary>
    /// The date and time that the case is being added to the register.
    /// </summary>
    public DateTime PublicRegisterStart { get; set; }

    /// <summary>
    /// The length of time in days that the case should appear on the register.
    /// </summary>
    public int Period { get; set; }

    /// <summary>
    /// The total area of the case, in hectares. This is used for display purposes on the public register.
    /// </summary>
    public double? TotalArea { get; set; }

    /// <summary>
    /// The geometry of the compartments associated with the case. This is used to display the compartments on the public register map.
    /// </summary>
    public List<InternalCompartmentDetails<Polygon>> Compartments { get; set; }
}

/// <summary>
/// Model of the data items required to add or update the case on the Decision public register.
/// </summary>
public class AddToDecisionPublicRegisterModel : AddToPublicRegisterModel
{
    /// <summary>
    /// A string representing the final outcome status of the case.
    /// </summary>
    public string FellingLicenceOutcome { get; set; }

    /// <summary>
    /// The date and time that the case reached its final outcome status.
    /// </summary>
    public DateTime CaseApprovalDate { get; set; }
}