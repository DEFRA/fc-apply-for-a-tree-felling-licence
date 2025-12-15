using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;

namespace Forestry.Flo.Services.FellingLicenceApplications.Extensions;

public static class EntityExtensions
{
    public static InternalReviewStepStatus GetPublicRegisterStatus(this Maybe<PublicRegister> publicRegister)
    {
        if (publicRegister.HasNoValue)
            return InternalReviewStepStatus.NotStarted;

        if (publicRegister.Value.WoodlandOfficerSetAsExemptFromConsultationPublicRegister || publicRegister.Value.ConsultationPublicRegisterRemovedTimestamp.HasValue)
            return InternalReviewStepStatus.Completed;

        return InternalReviewStepStatus.InProgress;
    }

    public static InternalReviewStepStatus GetSiteVisitStatus(this Maybe<WoodlandOfficerReview> review)
    {
        if (review.HasNoValue || review.Value.SiteVisitNeeded.HasNoValue())
            return InternalReviewStepStatus.NotStarted;

        if (review.Value.SiteVisitNeeded is false || review.Value.SiteVisitComplete)
            return InternalReviewStepStatus.Completed;

        return InternalReviewStepStatus.InProgress;
    }

    public static InternalReviewStepStatus GetPw14ChecksStatus(this Maybe<WoodlandOfficerReview> review)
    {
        if (review.HasNoValue)
            return InternalReviewStepStatus.NotStarted;

        if (review.Value.Pw14ChecksComplete)
            return InternalReviewStepStatus.Completed;

        if (review.Value.LandInformationSearchChecked.HasValue
            || review.Value.TpoOrCaDeclared.HasValue
            || review.Value.AreProposalsUkfsCompliant.HasValue
            || review.Value.IsApplicationValid.HasValue
            || review.Value.EiaThresholdExceeded.HasValue
            || review.Value.EiaTrackerCompleted.HasValue
            || review.Value.EiaChecklistDone.HasValue)
            return InternalReviewStepStatus.InProgress;

        return InternalReviewStepStatus.NotStarted;
    }


    /// <summary>
    /// Determines the EIA screening status for a woodland officer review.
    /// </summary>
    /// <param name="review">The woodland officer review, wrapped in a <see cref="Maybe{T}"/>.</param>
    /// <param name="requiresEia">A boolean indicating whether an Environmental Impact Assessment (EIA) is required.</param>
    /// <returns>
    /// <see cref="InternalReviewStepStatus.NotStarted"/> if the review is not present;
    /// <see cref="InternalReviewStepStatus.NotRequired"/> if EIA is not required;
    /// <see cref="InternalReviewStepStatus.Completed"/> if EIA screening is complete;
    /// otherwise, <see cref="InternalReviewStepStatus.NotStarted"/>.
    /// </returns>
    public static InternalReviewStepStatus GetEiaScreeningStatus(this Maybe<WoodlandOfficerReview> review, bool requiresEia)
    {
        if (review.HasNoValue)
            return InternalReviewStepStatus.NotStarted;

        if (requiresEia is false)
        {
            return InternalReviewStepStatus.NotRequired;
        }

        return review.Value.EiaScreeningComplete is true 
            ? InternalReviewStepStatus.Completed
            : InternalReviewStepStatus.NotStarted;
    }

    public static bool AllAreComplete(this WoodlandOfficerReviewTaskListStates woodlandOfficerReviewTaskListStates)
    {
        return woodlandOfficerReviewTaskListStates.PublicRegisterStepStatus == InternalReviewStepStatus.Completed
               && woodlandOfficerReviewTaskListStates.SiteVisitStepStatus == InternalReviewStepStatus.Completed
               && woodlandOfficerReviewTaskListStates.Pw14ChecksStepStatus == InternalReviewStepStatus.Completed
               && woodlandOfficerReviewTaskListStates.FellingAndRestockingStepStatus == InternalReviewStepStatus.Completed
               && woodlandOfficerReviewTaskListStates.ConditionsStepStatus == InternalReviewStepStatus.Completed
               && woodlandOfficerReviewTaskListStates.LarchApplicationStatus is InternalReviewStepStatus.NotRequired or InternalReviewStepStatus.Completed
               && woodlandOfficerReviewTaskListStates.LarchFlyoverStatus is InternalReviewStepStatus.NotRequired or InternalReviewStepStatus.Completed
               && woodlandOfficerReviewTaskListStates.ConsultationStepStatus is InternalReviewStepStatus.NotRequired or InternalReviewStepStatus.Completed;
    }

    public static TypeOfProposal[] AllowedRestockingForFellingType(this FellingOperationType fellingType, bool includeNone = true)
    {
        switch (fellingType)
        {
            case FellingOperationType.ClearFelling:
                return new[]
                {   TypeOfProposal.CreateDesignedOpenGround,
                TypeOfProposal.DoNotIntendToRestock,
                TypeOfProposal.ReplantTheFelledArea,
                TypeOfProposal.RestockByNaturalRegeneration,
                TypeOfProposal.RestockWithCoppiceRegrowth,
                TypeOfProposal.PlantAnAlternativeArea,
                TypeOfProposal.NaturalColonisation
            };
            case FellingOperationType.FellingOfCoppice:
                return new[]
                {
                TypeOfProposal.CreateDesignedOpenGround,
                TypeOfProposal.DoNotIntendToRestock,
                TypeOfProposal.RestockWithCoppiceRegrowth
            };
            case FellingOperationType.FellingIndividualTrees:
                return new[]
                {
                TypeOfProposal.CreateDesignedOpenGround,
                TypeOfProposal.DoNotIntendToRestock,
                TypeOfProposal.RestockByNaturalRegeneration,
                TypeOfProposal.RestockWithCoppiceRegrowth,
                TypeOfProposal.RestockWithIndividualTrees,
                TypeOfProposal.PlantAnAlternativeAreaWithIndividualTrees
            };
            case FellingOperationType.RegenerationFelling:
                return new[]
                {
                TypeOfProposal.CreateDesignedOpenGround,
                TypeOfProposal.DoNotIntendToRestock,
                TypeOfProposal.ReplantTheFelledArea,
                TypeOfProposal.RestockByNaturalRegeneration,
                TypeOfProposal.RestockWithCoppiceRegrowth
            };
            default:
                return includeNone ? new[] { TypeOfProposal.None } : Array.Empty<TypeOfProposal>();
        }
    }

    public static bool SupportsAlternativeCompartmentRestocking(this FellingOperationType fellingOperationType)
    {
        var allowedTypes = fellingOperationType.AllowedRestockingForFellingType();

        var alternateTypes = new List<TypeOfProposal> { TypeOfProposal.PlantAnAlternativeArea, TypeOfProposal.PlantAnAlternativeAreaWithIndividualTrees, TypeOfProposal.NaturalColonisation };

        return allowedTypes.Any(alternateTypes.Contains);
    }

    public static bool IsAlternativeCompartmentRestockingType(this TypeOfProposal typeOfProposal)
    {
        return typeOfProposal is TypeOfProposal.PlantAnAlternativeArea or TypeOfProposal.PlantAnAlternativeAreaWithIndividualTrees or TypeOfProposal.NaturalColonisation;
    }

    public static bool IsNumberOfTreesRestockingType(this TypeOfProposal typeOfProposal)
    {
        return typeOfProposal is TypeOfProposal.RestockWithIndividualTrees or TypeOfProposal.PlantAnAlternativeAreaWithIndividualTrees;
    }

    /// <summary>
    /// Retrieves the current status of the specified felling licence application.
    /// </summary>
    /// <param name="fellingLicenceApplication">The felling licence application for which the current status is to be retrieved.</param>
    /// <returns>The most recent <see cref="FellingLicenceStatus"/> based on the application's status history.</returns>
    public static FellingLicenceStatus GetCurrentStatus(this FellingLicenceApplication fellingLicenceApplication)
        => fellingLicenceApplication.StatusHistories.MaxBy(x => x.Created)?.Status ?? FellingLicenceStatus.Received;

    /// <summary>
    /// Retrieves the nth most recent status of the specified felling licence application.
    /// </summary>
    /// <param name="fellingLicenceApplication">The felling licence application to evaluate.</param>
    /// <param name="n">The index of the status to retrieve, where 0 is the most recent.</param>
    /// <returns>The nth most recent <see cref="FellingLicenceStatus"/>, or <see cref="Maybe{T}.None"/> if not available.</returns>
    public static Maybe<FellingLicenceStatus> GetNthStatus(this FellingLicenceApplication fellingLicenceApplication, int n) =>
        fellingLicenceApplication.StatusHistories.Count < n + 1 
            ? Maybe<FellingLicenceStatus>.None 
            : fellingLicenceApplication.StatusHistories.OrderByDescending(x => x.Created).ElementAt(n).Status;

    /// <summary>
    /// Determines whether the specified felling licence application should be removed from the consultation public register.
    /// </summary>
    /// <param name="publicRegister">The public register associated with the felling licence application.</param>
    /// <returns>
    /// <c>true</c> if the application should be removed from the consultation public register; otherwise, <c>false</c>.
    /// </returns>
    public static bool ShouldApplicationBeRemovedFromConsultationPublicRegister(
        this PublicRegister? publicRegister) =>
        publicRegister?.EsriId is not null &&
        publicRegister?.ConsultationPublicRegisterPublicationTimestamp.HasValue is true &&
        publicRegister.ConsultationPublicRegisterRemovedTimestamp.HasNoValue();

    /// <summary>
    /// Determines whether the specified felling details require an Environmental Impact Assessment (EIA).
    /// </summary>
    /// <param name="fellingDetails">The collection of proposed felling details to evaluate.</param>
    /// <returns>
    /// <c>true</c> if any of the proposed felling details (excluding thinning operations) either do not intend to restock,
    /// have no restocking details, or include restocking proposals that are considered deforestation or afforestation;
    /// otherwise, <c>false</c>.
    /// </returns>
    public static bool ShouldProposedFellingDetailsRequireEia(
        this IEnumerable<ProposedFellingDetail> fellingDetails)
    {
        TypeOfProposal[] deforestationOrAfforestationProposals =
        [
            TypeOfProposal.DoNotIntendToRestock,
            TypeOfProposal.None,
            TypeOfProposal.PlantAnAlternativeArea,
            TypeOfProposal.PlantAnAlternativeAreaWithIndividualTrees,
        ];

        return fellingDetails
            .Where(f => f.OperationType is not FellingOperationType.Thinning)
            .Any(f =>
                f.IsRestocking is false ||
                f.ProposedRestockingDetails is null ||
                f.ProposedRestockingDetails.Count == 0 ||
                f.ProposedRestockingDetails.Any(restocking =>
                    deforestationOrAfforestationProposals.Contains(restocking.RestockingProposal)));
    }

    /// <summary>
    /// Determines whether the specified felling licence application requires an Environmental Impact Assessment (EIA).
    /// </summary>
    /// <param name="fellingLicenceApplication">The felling licence application to evaluate.</param>
    /// <returns>
    /// <c>true</c> if the application requires an EIA based on its linked property profile and proposed felling details; otherwise, <c>false</c>.
    /// </returns>
    public static bool ShouldApplicationRequireEia(this FellingLicenceApplication fellingLicenceApplication) =>
        fellingLicenceApplication.LinkedPropertyProfile?.ProposedFellingDetails?.ShouldProposedFellingDetailsRequireEia() ?? false;

    /// <summary>
    /// Determines whether the specified felling licence application requires PAWS data input.
    /// </summary>
    /// <param name="fellingLicenceApplication">The felling licence application to evaluate.</param>
    /// <returns><c>true</c> if the application requires PAWS data input, otherwise <c>false</c>.</returns>
    public static bool DoesApplicationRequirePawsDataInput(this FellingLicenceApplication fellingLicenceApplication) =>
        (fellingLicenceApplication.FellingLicenceApplicationStepStatus.CompartmentDesignationsStatuses ?? []).Count != 0;

    /// <summary>
    /// Determines whether the specified felling licence application is in a state that allows the applicant to edit it.
    /// </summary>
    /// <param name="fellingLicenceApplication">The felling licence application to test.</param>
    /// <returns>True if the application is in a state for the applicant to be able to edit, otherwise false.</returns>
    public static bool IsInApplicantEditableState(this FellingLicenceApplication fellingLicenceApplication) =>
        FellingLicenceStatusConstants.SubmitStatuses.Any(x => x == fellingLicenceApplication.GetCurrentStatus());


    /// <summary>
    /// Gets a distinct list of all property profile compartment ids for the proposed felling and restocking
    /// on the given felling licence application.
    /// </summary>
    /// <param name="application">The application to return the compartment ids for.</param>
    /// <returns>A <see cref="IEnumerable{T}"/> of compartment ids.</returns>
    public static IEnumerable<Guid> GetAllCompartmentIdsInApplication(this FellingLicenceApplication application)
    {
        if (application?.LinkedPropertyProfile?.ProposedFellingDetails is null)
            return [];

        var fellingCompartmentIds = application.LinkedPropertyProfile.ProposedFellingDetails
            .Select(d => d.PropertyProfileCompartmentId);

        var restockingIds = application.LinkedPropertyProfile.ProposedFellingDetails
            .Where(d => d.ProposedRestockingDetails is not null)
            .SelectMany(d => d.ProposedRestockingDetails!)
            .Select(r => r.PropertyProfileCompartmentId);

        return fellingCompartmentIds.Concat(restockingIds).Distinct();
    }
}

