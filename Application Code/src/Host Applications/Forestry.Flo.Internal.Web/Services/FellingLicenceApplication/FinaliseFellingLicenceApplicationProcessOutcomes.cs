using System.ComponentModel;

namespace Forestry.Flo.Internal.Web.Services.FellingLicenceApplication;

/// <summary>
/// Enum encapsulating the possible outcomes of setting the final status of a Felling Licence application.
/// </summary>
public enum FinaliseFellingLicenceApplicationProcessOutcomes
{
    /// <summary>
    /// The current user is not authorised to perform the state transition.
    /// </summary>
    [Description("User role is not authorised")]
    UserRoleNotAuthorised,

    /// <summary>
    /// The Felling Licence application could not be retrieved.
    /// </summary>
    [Description("Could not retrieve application")]
    CouldNotRetrieveApplication,

    /// <summary>
    /// The state transition requested is not valid for the Felling Licence application.
    /// </summary>
    [Description("Incorrect Felling Application status requested")]
    IncorrectFellingApplicationStatusRequested,

    /// <summary>
    /// The Felling Licence application is not in the correct state for it to be transitioned to a final state.
    /// </summary>
    [Description("Incorrect Felling Application state")]
    IncorrectFellingApplicationState,

    /// <summary>
    /// Unable to publish the Case to the decision public register.
    /// </summary>
    [Description("Could not publish to decision public register")]
    CouldNotPublishToDecisionPublicRegister,

    /// <summary>
    /// Unable to successfully send a notification to the applicant.
    /// </summary>
    [Description("Could not send notification to applicant")]
    CouldNotSendNotificationToApplicant,

    /// <summary>
    /// Successfully published the case to the decision public register, but could not subsequently store the details of
    /// it in the local system, such as the published date and expiry date.
    /// </summary>
    [Description("Could not store decision details locally")]
    CouldNotStoreDecisionDetailsLocally,

    /// <summary>
    /// Unable to store the case note associated with the finalisation of the application.
    /// </summary>
    [Description("Could not store case note")]
    CouldNotStoreCaseNote
}