using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Services.FellingLicenceApplications.Models;

public class PublicRegisterPeriodEndModel
{
    /// <summary>
    /// Gets and sets the public register entity for the application.
    /// </summary>
    public PublicRegister? PublicRegister { get; set; }

    /// <summary>
    /// Gets and sets a list of assigned users for an application.
    /// </summary>
    public IList<Guid>? AssignedUserIds{ get; set; }

    /// <summary>
    /// Gets and sets the application reference.
    /// </summary>
    public string? ApplicationReference { get; set; }

    /// <summary>
    /// Gets and sets the property name for the application.
    /// </summary>
    public string? PropertyName { get; set; }

    /// <summary>
    /// Gets and sets the name of the admin hub for the application.
    /// </summary>
    public string AdminHubName { get; set; }
}