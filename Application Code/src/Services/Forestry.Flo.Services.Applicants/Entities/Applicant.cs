using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.Applicants.Entities;

/// <summary>
/// Entity class representing an applicant (user account, woodland owner, or agent) in the system,
/// as returned by the Applicants view.
/// </summary>
public class Applicant
{
    /// <summary>
    /// Gets the unique internal identifier for the applicant, be it the WoodlandOwner entity ID for
    /// woodland owners or the Agency ID for agent/agencies.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets and sets the name of the applicant to display, if this applicant represents a user account in the
    /// system it will be their first name plus last name, if this applicant is a managed woodland owner it will
    /// be their contact name or organisation name, and if this applicant is an agent/agency it will be their contact
    /// name or organisation name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets and sets the contact email address of the applicant; if this applicant represents a user account in the system
    /// this will be their user account email address, otherwise it will be the contact email address of the woodland
    /// owner or agent/agency.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Gets and sets the name of the person or entity that manages this applicant.
    /// If the applicant is a woodland owner or woodland owner-linked user account it will be the contact name or organisation name
    /// of either the managing agent/agency for their woodland owner if there is one, otherwise it will be the contact name or
    /// organisation name of the woodland owner itself.
    /// If the applicant is an agent/agency or agent/agency-linked user account it will be the contact name or organisation name of
    /// that agent/agency.
    /// If the applicant is a woodland owner with no user accounts and not linked to an agent/agency, or an agent/agency with
    /// no user accounts, this will be "Forestry Commission".
    /// </summary>
    public string? ManagedBy { get; set; }

    /// <summary>
    /// Gets and sets the type of applicant.
    /// </summary>
    public ApplicantType Type { get; set; }
}

/// <summary>
/// Enumeration of applicant types that may be returned by the Applicants view, used to determine how to display
/// the applicant details on the frontend.
/// </summary>
public enum ApplicantType
{
    [Display(Name = "Agent/agency (individual)")]
    AgentIndividual,

    [Display(Name = "Agent/agency (organisation)")]
    AgentOrganisation,

    [Display(Name="Woodland owner (individual)")]
    WoodlandOwnerIndividual,

    [Display(Name = "Woodland owner (organisation)")]
    WoodlandOwnerOrganisation,

}