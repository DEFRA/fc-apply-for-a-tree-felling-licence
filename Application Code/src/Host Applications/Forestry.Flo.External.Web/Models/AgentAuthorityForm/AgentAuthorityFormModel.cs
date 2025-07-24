namespace Forestry.Flo.External.Web.Models.AgentAuthorityForm;

public record AgentAuthorityFormModel
{
    /// <summary>
    /// Gets and inits the email address of the contact to act on behalf of.
    /// </summary>
    public string ContactEmail { get; init; } = null!;

    /// <summary>
    /// Gets and inits the telephone number of the contact to act on behalf of.
    /// </summary>
    public string ContactTelephoneNumber { get; init; } = null!;

    /// <summary>
    /// Gets and inits the address of the contact to act on behalf of.
    /// </summary>
    public Address ContactAddress { get; init; } = null!;

    /// <summary>
    /// Gets and inits the name of the contact to act on behalf of.
    /// </summary>
    public string ContactName { get; init; } = null!;

    /// <summary>
    /// Gets and inits a flag indicating whether the applicant is an organisation.
    /// </summary>
    public bool IsOrganisation { get; init; }

    /// <summary>
    /// Gets and inits the address of the organisation to act on behalf of.
    /// </summary>
    public Address? OrganisationAddress { get; init; } = null!;

    /// <summary>
    /// Gets and inits the name of the organisation to act on behalf of.
    /// </summary>
    public string? OrganisationName { get; init; } = null!;

    public BreadcrumbsModel? Breadcrumbs { get; set; }

    /// <summary>
    /// Gets and sets the id of the agency that this authority is for.
    /// </summary>
    public Guid AgencyId { get; init; }
}