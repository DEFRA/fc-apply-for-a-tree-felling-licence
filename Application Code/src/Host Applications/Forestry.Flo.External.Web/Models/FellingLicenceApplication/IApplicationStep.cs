namespace Forestry.Flo.External.Web.Models.FellingLicenceApplication;

public interface IApplicationStep
{
    ApplicationStepStatus Status { get; }

    public bool? StepComplete { get; set; }
}