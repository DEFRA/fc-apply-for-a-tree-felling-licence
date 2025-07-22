namespace Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;

/// <summary>
/// Model class representing an external applicant user.
/// </summary>
public class ExternalApplicantModel
{
    public Guid Id { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? Email { get; set; }

    public ExternalApplicantType ApplicantType { get; set; }

    public bool IsActiveAccount { get; set; }
}

public enum ExternalApplicantType
{
    WoodlandOwner,
    Agent,
    FcStaffMember
}