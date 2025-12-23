using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.External.Web.Models.FellingLicenceApplication;

public class ApplicationStepBase : IApplicationStep
{
    public virtual Guid ApplicationId { get; set; }

    public ApplicationStepStatus Status
    {
        get
        {
            ApplicationStepStatus applicationStepStatus;

            if (!StepComplete.HasValue)
            {
                applicationStepStatus = IsWithApplicant
                    ? ApplicationStepStatus.AmendmentRequired
                    : ApplicationStepStatus.NotStarted;
            }
            else
            {
                applicationStepStatus = StepComplete.Value 
                    ? ApplicationStepStatus.Completed 
                    : IsWithApplicant
                        ? ApplicationStepStatus.AmendmentRequired 
                        : ApplicationStepStatus.InProgress;
            }

            return applicationStepStatus;
        }

    }

    public virtual bool? StepComplete { get; set; }

    public virtual bool StepRequiredForApplication { get; set; } = true;

    public bool ReturnToApplicationSummary { get; set; }

    public bool ReturnToPlayback { get; set; }

    public FellingLicenceStatus FellingLicenceStatus { get; set; } = FellingLicenceStatus.Draft;

    public bool AllowEditing => FellingLicenceStatus is FellingLicenceStatus.Draft or FellingLicenceStatus.WithApplicant or FellingLicenceStatus.ReturnedToApplicant;

    public bool IsWithApplicant => FellingLicenceStatus is FellingLicenceStatus.WithApplicant or FellingLicenceStatus.ReturnedToApplicant;
}