namespace Forestry.Flo.External.Web.Models.FellingLicenceApplication;

public interface IApplicationStep
{
    ApplicationStepStatus Status { get; }

    public bool? StepComplete { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this step is required for the application to be submitted.
    /// </summary>
    public bool StepRequiredForApplication { get; set; }
}