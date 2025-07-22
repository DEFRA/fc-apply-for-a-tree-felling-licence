using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.FellingLicenceApplications.Entities;

/// <summary>
/// AssigneeHistory entity class
/// </summary>
public class AssigneeHistory
{
    /// <summary>
    /// Gets and Sets the property document ID.
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
    /// Gets or sets the assigned user.
    /// </summary>
    [Required]
    public Guid AssignedUserId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp assigned.
    /// </summary>
    [Required]
    public DateTime TimestampAssigned { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of when it was unassigned
    /// </summary>
    public DateTime? TimestampUnassigned { get; set; }

    /// <summary>
    /// Gets or sets the role.
    /// </summary>
    [Required]
    public AssignedUserRole Role { get; set; }
}