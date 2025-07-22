namespace Forestry.Flo.Services.Applicants.Models;

/// <summary>
/// Model class for an Agency including extra information for the FC user dashboard.
/// </summary>
public class AgencyFcModel
{
    /// <summary>
    /// Gets and sets the id of the agency in the repository.
    /// </summary>
    public Guid Id { get; set; }

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
    /// Gets and sets a flag indicating that the agency has active
    /// external applicant accounts linked directly with it.
    /// </summary>
    public bool HasActiveUserAccounts { get; set; }
}