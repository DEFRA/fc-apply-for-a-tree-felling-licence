using Forestry.Flo.Services.Applicants.Entities;

namespace Forestry.Flo.Services.Applicants.Models;

/// <summary>
/// Model class representing an Agency within the system.
/// </summary>
public class AgencyModel
{
    /// <summary>
    /// Gets and Sets the  address of the Agency on the system.
    /// </summary>
    public Address? Address { get; set; }

    /// <summary>
    /// Gets and Sets the email address of the external user agency.
    /// </summary>
    public string? ContactEmail { get; set; }

    /// <summary>
    /// Gets and Sets the full name of the agency.
    /// </summary>
    public string? ContactName { get; set; }

    /// <summary>
    /// Gets and Sets the Organisation name that the agent is part of.
    /// </summary>
    public string? OrganisationName { get; set; }

    /// <summary>
    /// Gets and Sets the agency id of the agency.
    /// </summary>
    public Guid? AgencyId { get; set; }

    /// <summary>
    /// Get and sets a flag indicating whether this Agency is the Internal FC agency.
    /// </summary>
    public bool IsFcAgency { get; set; } = false;
}