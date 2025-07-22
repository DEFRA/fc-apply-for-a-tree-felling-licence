using System.ComponentModel.DataAnnotations;
using Forestry.Flo.Services.Applicants.Entities;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Entities.WoodlandOwner;
using Forestry.Flo.Services.Common.User;

namespace Forestry.Flo.Services.Applicants.Models;

/// <summary>
/// Model class representing an Applicant User
/// </summary>
public class ApplicantUserModel
{
    public Guid Id { get; set; }

    public string? IdentityProviderId { get; set; }

    public AccountTypeExternal AccountType { get; set; }

    public string? Title { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string Email { get; set; }

    public PreferredContactMethod? PreferredContactMethod { get; set; }

    public Address? ContactAddress { get; set; }

    public string? ContactTelephone { get; set; }

    public string? ContactMobileTelephone { get; set; }

    public UserAccountStatus Status { get; set; }

    public Guid? WoodlandOwnerId { get; set; }

    public Guid? AgencyId { get; set; }
}