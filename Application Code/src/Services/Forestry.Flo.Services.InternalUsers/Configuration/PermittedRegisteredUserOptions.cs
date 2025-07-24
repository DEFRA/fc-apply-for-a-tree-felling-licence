namespace Forestry.Flo.Services.InternalUsers.Configuration;

public class PermittedRegisteredUserOptions
{
    public const string ConfigurationKey = "PermittedRegisteredUser";

    /// <summary>
    /// List of email domains which are checked against before allowing access to the system.
    /// </summary>
    public List<string> PermittedEmailDomainsForRegisteredUser { get; set; } = new()
    {
        "qxlva.com",
        "forestrycommission.gov.uk",
        "harriscomputer.com"
    };
}