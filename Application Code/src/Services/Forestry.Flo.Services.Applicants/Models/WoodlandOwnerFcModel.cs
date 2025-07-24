using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.Applicants.Models;

/// <summary>
/// Model class for a Woodland Owner including extra information for the FC user dashboard.
/// </summary>
public class WoodlandOwnerFcModel
{
    /// <summary>
    /// Gets and sets the id of the woodland owner in the repository.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets and Sets the email address of the contact for the woodland owner.
    /// </summary>
    public string? ContactEmail { get; set; }

    /// <summary>
    /// Gets and Sets the name of the contact for the woodland owner.
    /// </summary>
    public string? ContactName { get; set; }

    /// <summary>
    /// Gets and sets a flag indicating whether this woodland owner is an individual or an organisation.
    /// </summary>
    [Required]
    public bool IsOrganisation { get; set; }

    /// <summary>
    /// Gets and Sets the Organisation name that the woodland owner is part of.
    /// </summary>
    public string? OrganisationName { get; set; }

    /// <summary>
    /// Gets and sets a flag indicating that the woodland owner has active
    /// external applicant accounts linked directly with it (either directly as a woodland
    /// owner, or indirectly as managed by an external non-FC agent).
    /// </summary>
    public bool HasActiveUserAccounts { get; set; }

    /// <summary>
    /// Gets and sets the Agency Id, if this woodland owner is managed by an agency.
    /// </summary>
    public Guid? AgencyId { get; set; }

    /// <summary>
    /// Gets and sets the agency name, if this woodland owner is managed by an agency.
    /// </summary>
    public string? AgencyName { get; set; }

    /// <summary>
    /// Gets and sets the agency contact name, if this woodland owner is managed by an agency.
    /// </summary>
    public string? AgencyContactName { get; set; }
}