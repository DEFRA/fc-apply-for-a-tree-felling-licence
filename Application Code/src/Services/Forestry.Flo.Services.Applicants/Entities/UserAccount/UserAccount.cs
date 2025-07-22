using System.ComponentModel.DataAnnotations;
using Forestry.Flo.Services.Applicants.Entities.Agent;
using Forestry.Flo.Services.Common.User;

namespace Forestry.Flo.Services.Applicants.Entities.UserAccount;

/// <summary>
/// Model class representing a user's account of the system.
/// </summary>
public class UserAccount
{
    /// <summary>
    /// Gets the unique internal identifier for the user account on the system.
    /// </summary>
    [Key]
    public Guid Id { get; protected set; }

    /// <summary>
    /// Gets or Sets the User Identifier as known to the the Identity Provider system.
    /// </summary>
    public string? IdentityProviderId { get; set; }

    /// <summary>
    /// Gets and sets the account type for this local account.
    /// </summary>
    [Required]
    public AccountTypeExternal AccountType { get; set; }

    /// <summary>
    /// Gets and Sets the Title for the external user.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets and Sets the first name of the external user.
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// Gets and Sets the last name of the external user.
    /// </summary>
    public string? LastName { get; set; }


    private string _email;
    
    /// <summary>
    /// Gets and Sets the email address of the external user.
    /// </summary>
    [Required]
    public string Email
    {
        get => _email;
        set => _email = value.ToLower();
    }

    /// <summary>
    /// Gets and Sets the preferred contact method of the external user.
    /// </summary>
    public PreferredContactMethod? PreferredContactMethod { get; set; }

    /// <summary>
    /// Gets and Sets the contact address of the external user.
    /// </summary>
    public Address? ContactAddress { get; set; }
    
    /// <summary>
    /// Gets and Sets the contact telephone number of the external user.
    /// </summary>
    public string? ContactTelephone { get; set; }

    /// <summary>
    /// Gets and Sets the contact mobile telephone number of the external user.
    /// </summary>
    public string? ContactMobileTelephone { get; set; }

    /// <summary>
    /// Gets and Sets the account status of the external user.
    /// </summary>
    public UserAccountStatus Status { get; set; }

    /// <summary>
    /// Gets and Sets the date that the user last accepted the terms and conditions.
    /// </summary>
    public DateTime? DateAcceptedTermsAndConditions { get; set; }

    /// <summary>
    /// Gets and Sets the date that the user last accepted the privacy policy.
    /// </summary>
    public DateTime? DateAcceptedPrivacyPolicy { get; set; }
    
    /// <summary>
    /// Gets and Sets the user invitation token
    /// </summary>
    public Guid? InviteToken { get; set; }

    /// <summary>
    /// Gets and Sets the user invitation token expiry time
    /// </summary>
    public DateTime? InviteTokenExpiry { get; set; }

    /// <summary>
    /// Gets and sets the associated with this account woodland owner id.
    /// </summary>
    public Guid? WoodlandOwnerId { get; set; }
    
    /// <summary>
    /// Gets and sets the associated with this account woodland owner id.
    /// </summary>
    public Guid? AgencyId { get; set; }

    /// <summary>
    /// Gets and sets the woodland owner details associated with this account.
    /// </summary>
    public WoodlandOwner.WoodlandOwner? WoodlandOwner { get; set; }
    
    /// <summary>
    /// Gets and sets the agency details associated with this account.
    /// </summary>
    public Agency? Agency { get; set; }

    public DateTime LastChanged { get; set; }
}