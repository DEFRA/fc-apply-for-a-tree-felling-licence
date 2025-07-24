
namespace Forestry.Flo.Services.AdminHubs.Model;

/// <summary>
/// Model class representing an admin hub.
/// </summary>
public class AdminHubModel
{
    /// <summary>
    /// Gets and sets the unique internal identifier for the admin hub.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Admin Hub name, such as 'Buller&apos;s Hill'
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets and sets the address of the admin hub.
    /// </summary>
    public string Address { get; set; }

    /// <summary>
    /// Gets and sets the ID of the user account for the manager of the admin hub.
    /// </summary>
    public Guid AdminManagerUserAccountId { get; set; }

    /// <summary>
    /// Gets and sets a list of the admin officers linked to the admin hub.
    /// </summary>
    public IReadOnlyList<AdminHubOfficerModel> AdminOfficers { get; set; } = Array.Empty<AdminHubOfficerModel>();

    /// <summary>
    /// Gets and sets a list of the areas linked to the admin hub.
    /// </summary>
    public IReadOnlyList<AreaModel> Areas { get; set; } = Array.Empty<AreaModel>();
}

/// <summary>
/// Model class representing an admin officer at an admin hub.
/// </summary>
public class AdminHubOfficerModel
{
    /// <summary>
    /// Gets and sets the unique identifier for the admin officer at their admin hub.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets and sets the ID of the user account for the admin officer.
    /// </summary>
    public Guid UserAccountId { get; set; }
}

/// <summary>
/// Model class representing an area served by an admin hub.
/// </summary>
public class AreaModel
{
    /// <summary>
    /// Gets and sets the unique identifier for the admin hub area.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets and sets the name of the regional area served, such as 'South East &amp; London'
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets and sets the FC code for this area, such as 'SEL'
    /// </summary>
    public string Code { get; set; }
}

/// <summary>
/// Enum representing the possible outcomes of operations managing admin hubs.
/// </summary>
public enum ManageAdminHubOutcome
{
    Unauthorized,
    AdminHubNotFound,
    AdminHubsNotFound,
    NoChangeSubmitted,
    InvalidAssignment,
    UpdateFailure
}