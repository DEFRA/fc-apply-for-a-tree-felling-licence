namespace Forestry.Flo.Internal.Web.Services.FellingLicenceApplication;

/// <summary>
/// Enum representation of the possible outcomes of the processing a Felling Licence application,
/// with respect to the public register.
/// </summary>
public enum SendToDecisionPublicRegisterOutcome
{
    /// <summary>
    /// The Felling Licence application was successfully added to the Decision Public Register.
    /// </summary>
    Success,

    /// <summary>
    /// The Felling Licence application could not be added to the Decision Public Register.
    /// </summary>
    Failure,

    /// <summary>
    /// Internally the Felling Licence application has been marked as exempt from the Decision Public Register.
    /// So was not required to be published to the public register.
    /// </summary>
    Exempt,

    /// <summary>
    /// Internally the Felling Licence application failed to store the details of when it was saved to the
    /// Decision Public Register.
    /// </summary>
    FailedToSaveDecisionDetailsLocally
}
