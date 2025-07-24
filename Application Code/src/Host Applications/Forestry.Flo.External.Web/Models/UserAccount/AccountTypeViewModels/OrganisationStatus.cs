namespace Forestry.Flo.External.Web.Models.UserAccount.AccountTypeViewModels;

/// <summary>
/// A flag indicating whether an entity is part of an organisation, or operates individually.
/// </summary>
public enum OrganisationStatus
{
    /// <summary>
    /// Represents an entity that is part of an organisation.
    /// </summary>
    Organisation,
    /// <summary>
    /// Represents an entity that is not part of an organisation.
    /// </summary>
    Individual
}