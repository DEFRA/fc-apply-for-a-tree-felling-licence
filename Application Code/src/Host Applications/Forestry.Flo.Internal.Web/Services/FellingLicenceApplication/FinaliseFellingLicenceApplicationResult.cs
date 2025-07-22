using CSharpFunctionalExtensions;

namespace Forestry.Flo.Internal.Web.Services.FellingLicenceApplication;

/// <summary>
/// Class encapsulating the <see cref="Result"/> object and a list of <see cref="FinaliseFellingLicenceApplicationProcessOutcomes"/>
/// to indicate the process(es) that failed, which may or may not be blockers to the final state transition.
/// </summary>
public class FinaliseFellingLicenceApplicationResult
{
    private Result OverallResult { get; }

    /// <summary>
    /// Was the final state transition successful.
    /// </summary>
    public bool IsSuccess => OverallResult.IsSuccess;

    /// <summary>
    /// Was a failure result encountered during the final state transition.
    /// </summary>
    public bool IsFailure => OverallResult.IsFailure;

    /// <summary>
    /// A list containing processes which may have failed during the final state transition,
    /// but may not necessarily be blockers to the overall success of the process.
    /// </summary>
    public List<FinaliseFellingLicenceApplicationProcessOutcomes> SubProcessFailures { get; private set; }

    private FinaliseFellingLicenceApplicationResult(
        Result overallResult, 
        List<FinaliseFellingLicenceApplicationProcessOutcomes>? subProcessFailures = null)
    {
        OverallResult = overallResult;
        SubProcessFailures = subProcessFailures ?? new List<FinaliseFellingLicenceApplicationProcessOutcomes>();
    }

    /// <summary>
    /// Static method to create an instance of the <see cref="FinaliseFellingLicenceApplicationResult"/> class with a successful result.
    /// </summary>
    /// <param name="subProcessFailures">A nullable list of possible failures which may occur. Each of those process failures must not be a blocker to a success outcome.</param>
    /// <returns>A <see cref="FinaliseFellingLicenceApplicationResult"/> instance with a nullable list of <see cref="FinaliseFellingLicenceApplicationProcessOutcomes"/> indicating the non-blocking process(es) that failed.</returns>
    public static FinaliseFellingLicenceApplicationResult CreateSuccess(
        List<FinaliseFellingLicenceApplicationProcessOutcomes>? subProcessFailures)
    {
        return new FinaliseFellingLicenceApplicationResult(Result.Success(), subProcessFailures);
    }

    /// <summary>
    /// Static method to create an instance of the <see cref="FinaliseFellingLicenceApplicationResult"/> class with a failure result.
    /// </summary>
    /// <param name="failureMessage">The error message to accompany the <see cref="Result.Failure"/>> result.</param>
    /// <param name="subProcessFailure">The sub process failure that occured causing the overall process to be a Failure.</param>
    /// <returns>A <see cref="FinaliseFellingLicenceApplicationResult"/> instance with a list containing a <see cref="FinaliseFellingLicenceApplicationProcessOutcomes"/> to indicate the process that failed.</returns>
    public static FinaliseFellingLicenceApplicationResult CreateFailure(
        string failureMessage, 
        FinaliseFellingLicenceApplicationProcessOutcomes subProcessFailure)
    {
        return new FinaliseFellingLicenceApplicationResult(Result.Failure(failureMessage), new List<FinaliseFellingLicenceApplicationProcessOutcomes>{ subProcessFailure });
    }
}