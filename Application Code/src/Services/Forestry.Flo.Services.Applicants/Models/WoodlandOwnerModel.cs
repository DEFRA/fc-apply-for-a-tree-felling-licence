using System.ComponentModel.DataAnnotations;
using Forestry.Flo.Services.Applicants.Entities;

namespace Forestry.Flo.Services.Applicants.Models;

/// <summary>
/// Model class representing a Woodland Owner
/// </summary>
public class WoodlandOwnerModel
{
    /// <summary>
    /// Gets and sets the id of the woodland owner in the repository.
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// Gets and Sets the contact address of the Woodland Owner on the system.
    /// </summary>
    public Address? ContactAddress { get; set; }

    /// <summary>
    /// Gets and sets the contact telephone number of the woodland owner.
    /// </summary>
    public string? ContactTelephone { get; set; }

    /// <summary>
    /// Gets and Sets the email address of the external user.
    /// </summary>
    public string? ContactEmail { get; set; }

    /// <summary>
    /// Gets and Sets the full name of the Woodland owner.
    /// </summary>
    public string? ContactName { get; set; }

    /// <summary>
    /// Gets and sets a flag indicating whether or not this woodland owner is an individual or an organisation.
    /// </summary>
    [Required]
    public bool IsOrganisation { get; set; }

    /// <summary>
    /// Gets and Sets the Organisation name that the woodland owner is part of.
    /// </summary>
    public string? OrganisationName { get; set; }

    /// <summary>
    /// Gets and Sets the address of the Organisation that the woodland owner is part of.
    /// </summary>
    public Address? OrganisationAddress { get; set; }

    /// <summary>
    /// Retrieve the appropriate name to display for this model.
    /// Either the contact name or if an organisation then the organisation name.
    /// </summary>
    public string GetContactNameForDisplay =>
        IsOrganisation
        && string.IsNullOrWhiteSpace(OrganisationName) == false
            ? OrganisationName
            : ContactName!;
}