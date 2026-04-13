using Forestry.Flo.Services.Gis.Models.Internal;
using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;

namespace Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;

/// <summary>
/// Model of the application data items required to publish to the Public Register layers in Forester.
/// </summary>
public class ApplicationDetailsForPublicRegisterModel
{
    /// <summary>
    /// Gets and sets the ESRI id of the application if it has already been published
    /// to the public register.
    /// </summary>
    public int? ExistingEsriId { get; set; }
    
    /// <summary>
    /// Gets and sets the application reference.
    /// </summary>
    public string CaseReference { get; set; }

    /// <summary>
    /// Gets and sets the name of the property for the application, used for display purposes on the public register.
    /// </summary>
    public string PropertyName { get; set; }

    /// <summary>
    /// Gets and sets the OS grid reference for the centre point of the application, used for display purposes on the public register.
    /// </summary>
    public string GridReference { get; set; }

    /// <summary>
    /// Gets and sets the nearest town entered by the applicant, used for display purposes on the public register.
    /// </summary>
    public string NearestTown { get; set; }

    /// <summary>
    /// Gets and sets the name of the local authority covering the application, used for display purposes on the public register.
    /// </summary>
    public string LocalAuthority { get; set; }

    /// <summary>
    /// Gets and sets the Name of the FC administrative region the application is in, for example Buller's Hill.
    /// </summary>
    public string AdminRegion { get; set; }

    /// <summary>
    /// Gets and sets the sum total of the felling operation areas in the application.
    /// </summary>
    public double? TotalArea { get; set; }

    /// <summary>
    /// Gets and sets the geometry data for the compartments associated with the application,
    /// used to display the compartments on the public register map.
    /// </summary>
    public List<InternalCompartmentDetails<Polygon>> Compartments { get; set; }

    /// <summary>
    /// Gets and sets the calculated application centre-point, used in order to calculate the Local Authority and OS grid reference.
    /// </summary>
    public Point? CentrePoint { get; set; }

    /// <summary>
    /// Gets and sets the list of user IDs for users currently assigned to the application.
    /// </summary>
    public List<Guid> AssignedInternalUserIds { get; set; }
}