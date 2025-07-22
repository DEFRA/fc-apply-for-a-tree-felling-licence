using System.ComponentModel.DataAnnotations;
using Ardalis.GuardClauses;

namespace Forestry.Flo.Services.AdminHubs.Entities;

public class AdminHubOfficer
{
    /// <summary>
    /// Gets the unique internal identifier for this Admin Hub User Record on the system.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets the Internal User Account Id for this Admin Hub.
    /// </summary>
    public Guid UserAccountId { get; set; }

    /// <summary>
    /// Gets the Admin Hub associated with the Internal User.
    /// </summary>
    public AdminHub AdminHub { get; set; }

    public Guid AdminHubId { get; set; }

    public AdminHubOfficer()
    {
    }

    public AdminHubOfficer(AdminHub adminHub, Guid adminOfficerUserId)
    {
        Guard.Against.Null(adminHub);
        Guard.Against.Null(adminOfficerUserId);

        UserAccountId = adminOfficerUserId;
        AdminHub = adminHub;
        AdminHubId = adminHub.Id;
    }
}