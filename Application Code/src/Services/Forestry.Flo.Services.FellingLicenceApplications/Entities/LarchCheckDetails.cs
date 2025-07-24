using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.FellingLicenceApplications.Entities;

/// <summary>
/// Larch Check Details entity class.
/// </summary>
public class LarchCheckDetails
{
    /// <summary>
    /// Gets and sets the Id of this entity.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets and sets the felling licence application id.
    /// </summary>
    public Guid FellingLicenceApplicationId { get; set; }

    /// <summary>
    /// Gets and sets the related felling licence application.
    /// </summary>
    public FellingLicenceApplication? FellingLicenceApplication { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether only larch species are in the application.
    /// </summary>
    public bool? ConfirmLarchOnly { get; set; }

    /// <summary>
    /// Gets or sets a value indicating felling is in Zone 1.
    /// </summary>
    public bool Zone1 { get; set; }

    /// <summary>
    /// Gets or sets a value indicating felling is in Zone 2.
    /// </summary>
    public bool Zone2 { get; set; }

    /// <summary>
    /// Gets or sets a value indicating felling is in Zone 3.
    /// </summary>
    public bool Zone3 { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether felling is in the moratorium.
    /// </summary>
    public bool? ConfirmMoratorium { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the inspection log is confirmed.
    /// </summary>
    public bool ConfirmInspectionLog { get; set; }

    /// <summary>
    /// Gets or sets the recommended split application due to selected reason.
    /// </summary>
    public int RecommendSplitApplicationDue { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who last updated this record.
    /// </summary>
    public Guid LastUpdatedById { get; set; }

    /// <summary>
    /// Gets or sets the date and time when this record was last updated.
    /// </summary>
    public DateTime LastUpdatedDate { get; set; }

    /// <summary>
    /// Gets or sets the date of the flight.
    /// </summary>
    public DateTime? FlightDate { get; set; }

    /// <summary>
    /// Gets or sets the observations made during the flight.
    /// </summary>
    public string? FlightObservations { get; set; }
}
