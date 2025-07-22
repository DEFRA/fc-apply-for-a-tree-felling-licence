using System.ComponentModel.DataAnnotations;
using System.Runtime.Intrinsics.X86;

namespace Forestry.Flo.Services.FellingLicenceApplications.Entities;

/// <summary>
/// StatusHistory entity class
/// </summary>
public class StatusHistory
{
    /// <summary>
    /// Gets and Sets the status History ID.
    /// </summary>
    [Key]
    public Guid Id { get; protected set; }

    /// <summary>
    /// Gets or sets the felling licence application ID.
    /// </summary>
    [Required]
    public Guid FellingLicenceApplicationId { get; set; }

    /// <summary>
    /// Gets or sets the felling licence application.
    /// </summary>
    [Required]
    public FellingLicenceApplication FellingLicenceApplication { get; set; }

    /// <summary>
    /// Gets or sets the created date / time.
    /// </summary>
    public DateTime Created { get; set; }

    /// <summary>
    /// Gets and Sets the id of the user who changed the status of the application.
    /// </summary>
    public Guid? CreatedById { get; set; }

    /// <summary>
    /// Gets or sets the Felling Licence Status.
    /// </summary>
    public FellingLicenceStatus Status { get; set; }
}
