using FluentValidation.TestHelper;
using Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;
using Forestry.Flo.Internal.Web.Services.Validation;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;

namespace Forestry.Flo.Internal.Web.Tests.Services.Validation;

public class AmendConfirmedFellingDetailsViewModelValidatorTests : CompartmentConfirmedFellingRestockingDetailsModelValidatorTestsBase
{
    private readonly AmendConfirmedFellingDetailsViewModelValidator _modelValidator = new();

    private static AmendConfirmedFellingDetailsViewModel CreateViewModel(
        FellingOperationType operationType,
        TypeOfProposal restockingProposal,
        bool? isRestocking = true,
        string? noRestockingReason = null,
        double? areaToBeFelled = null,
        int? numberOfTrees = null,
        double? confirmedTotalHectares = null)
    {
        var compartmentModel = CreateValidModel(operationType, restockingProposal);

        // Amend ConfirmedFellingDetails with test-specific values
        var fellingDetail = compartmentModel.ConfirmedFellingDetails.First();
        var species = TreeSpeciesFactory.SpeciesDictionary;
        fellingDetail.ConfirmedFellingSpecies = [new ConfirmedFellingSpeciesModel
            {
                Id = Guid.NewGuid(),
                Species = species.First().Key,
                SpeciesType = SpeciesType.Broadleaf,
                Deleted = false
            }
        ];
        if (areaToBeFelled != null) fellingDetail.AreaToBeFelled = areaToBeFelled;
        if (numberOfTrees != null) fellingDetail.NumberOfTrees = numberOfTrees;
        if (isRestocking != null) fellingDetail.IsRestocking = isRestocking;
        if (noRestockingReason != null) fellingDetail.NoRestockingReason = noRestockingReason;

        var individualModel = new IndividualConfirmedFellingRestockingDetailModel
        {
            CompartmentId = compartmentModel.CompartmentId,
            ConfirmedFellingDetails = fellingDetail,
            TotalHectares = confirmedTotalHectares
        };

        return new AmendConfirmedFellingDetailsViewModel
        {
            ApplicationId = Guid.NewGuid(),
            ConfirmedFellingRestockingDetails = individualModel,
            ConfirmedFellingAndRestockingComplete = true,
        };
    }

    [Fact]
    public void ValidModel_PassesValidation()
    {
        var model = CreateViewModel(FellingOperationType.ClearFelling, TypeOfProposal.ReplantTheFelledArea, confirmedTotalHectares: 0.4, areaToBeFelled: 0.2);
        var result = _modelValidator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void AreaToBeFelled_Required_WhenFelling()
    {
        var model = CreateViewModel(FellingOperationType.ClearFelling, TypeOfProposal.ReplantTheFelledArea, areaToBeFelled: null);
        var result = _modelValidator.TestValidate(model);
        result.ShouldHaveValidationErrorFor("ConfirmedFellingRestockingDetails.ConfirmedFellingDetails.AreaToBeFelled");
    }

    [Fact]
    public void AreaToBeFelled_MustBePositive_WhenFelling()
    {
        var model = CreateViewModel(FellingOperationType.ClearFelling, TypeOfProposal.ReplantTheFelledArea, areaToBeFelled: 0);
        var result = _modelValidator.TestValidate(model);
        result.ShouldHaveValidationErrorFor("ConfirmedFellingRestockingDetails.ConfirmedFellingDetails.AreaToBeFelled");
    }

    [Fact]
    public void AreaToBeFelled_MustNotExceedConfirmedTotalHectares()
    {
        var model = CreateViewModel(FellingOperationType.ClearFelling, TypeOfProposal.ReplantTheFelledArea, areaToBeFelled: 1000, confirmedTotalHectares: 10);
        var result = _modelValidator.TestValidate(model);
        result.ShouldHaveValidationErrorFor("ConfirmedFellingRestockingDetails.ConfirmedFellingDetails.AreaToBeFelled");
    }

    [Fact]
    public void AreaToBeFelledNoError_WhenEqual()
    {
        var model = CreateViewModel(FellingOperationType.ClearFelling, TypeOfProposal.ReplantTheFelledArea, areaToBeFelled: 1000, confirmedTotalHectares: 1000);
        var result = _modelValidator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor("ConfirmedFellingRestockingDetails.ConfirmedFellingDetails.AreaToBeFelled");
    }

    [Fact]
    public void NumberOfTrees_MustBePositive_IfProvided()
    {
        var model = CreateViewModel(FellingOperationType.ClearFelling, TypeOfProposal.ReplantTheFelledArea, numberOfTrees: 0);
        var result = _modelValidator.TestValidate(model);
        result.ShouldHaveValidationErrorFor("ConfirmedFellingRestockingDetails.ConfirmedFellingDetails.NumberOfTrees");
    }

    [Fact]
    public void IsRestocking_Required_WhenFelling()
    {
        var model = CreateViewModel(FellingOperationType.ClearFelling, TypeOfProposal.ReplantTheFelledArea, isRestocking: null);
        var result = _modelValidator.TestValidate(model);
        result.ShouldHaveValidationErrorFor("ConfirmedFellingRestockingDetails.ConfirmedFellingDetails.IsRestocking");
    }

    [Fact]
    public void NoRestockingReason_Required_WhenNotRestocking()
    {
        var model = CreateViewModel(FellingOperationType.ClearFelling, TypeOfProposal.DoNotIntendToRestock, isRestocking: false, noRestockingReason: null);
        var result = _modelValidator.TestValidate(model);
        result.ShouldHaveValidationErrorFor("ConfirmedFellingRestockingDetails.ConfirmedFellingDetails.NoRestockingReason");
    }

    [Fact]
    public void TpoNumberRequired_WhenTpoYes()
    {
        var model = CreateViewModel(FellingOperationType.ClearFelling, TypeOfProposal.DoNotIntendToRestock, isRestocking: false);

        model.ConfirmedFellingRestockingDetails.ConfirmedFellingDetails.IsPartOfTreePreservationOrder = true;
        model.ConfirmedFellingRestockingDetails.ConfirmedFellingDetails.TreePreservationOrderReference = null;

        var result = _modelValidator.TestValidate(model);
        result.ShouldHaveValidationErrorFor("ConfirmedFellingRestockingDetails.ConfirmedFellingDetails.NoRestockingReason");
    }

    [Fact]
    public void TpoNumberNotRequired_WhenTpoNo()
    {
        var model = CreateViewModel(FellingOperationType.ClearFelling, TypeOfProposal.DoNotIntendToRestock);

        model.ConfirmedFellingRestockingDetails.ConfirmedFellingDetails.IsPartOfTreePreservationOrder = false;
        model.ConfirmedFellingRestockingDetails.ConfirmedFellingDetails.TreePreservationOrderReference = null;

        var result = _modelValidator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor("ConfirmedFellingRestockingDetails.ConfirmedFellingDetails.NoRestockingReason");
    }

    [Fact]
    public void TreeMarkingRequired_WhenTreeMarkingYes()
    {
        var model = CreateViewModel(FellingOperationType.ClearFelling, TypeOfProposal.DoNotIntendToRestock, isRestocking: false);

        model.ConfirmedFellingRestockingDetails.ConfirmedFellingDetails.IsTreeMarkingUsed = true;
        model.ConfirmedFellingRestockingDetails.ConfirmedFellingDetails.TreeMarking = null;

        var result = _modelValidator.TestValidate(model);
        result.ShouldHaveValidationErrorFor("ConfirmedFellingRestockingDetails.ConfirmedFellingDetails.TreeMarking");
    }

    [Fact]
    public void TreeMarkingNotRequired_WhenTreeMarkingNo()
    {
        var model = CreateViewModel(FellingOperationType.ClearFelling, TypeOfProposal.DoNotIntendToRestock);

        model.ConfirmedFellingRestockingDetails.ConfirmedFellingDetails.IsTreeMarkingUsed = false;
        model.ConfirmedFellingRestockingDetails.ConfirmedFellingDetails.TreeMarking = null;

        var result = _modelValidator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor("ConfirmedFellingRestockingDetails.ConfirmedFellingDetails.TreeMarking");
    }

    [Fact]
    public void CaReferenceRequired_WhenCaYes()
    {
        var model = CreateViewModel(FellingOperationType.ClearFelling, TypeOfProposal.DoNotIntendToRestock, isRestocking: false);

        model.ConfirmedFellingRestockingDetails.ConfirmedFellingDetails.IsWithinConservationArea = true;
        model.ConfirmedFellingRestockingDetails.ConfirmedFellingDetails.ConservationAreaReference = null;

        var result = _modelValidator.TestValidate(model);
        result.ShouldHaveValidationErrorFor("ConfirmedFellingRestockingDetails.ConfirmedFellingDetails.ConservationAreaReference");
    }

    [Fact]
    public void CaReferenceNotRequired_WhenCaNo()
    {
        var model = CreateViewModel(FellingOperationType.ClearFelling, TypeOfProposal.DoNotIntendToRestock, isRestocking: false);

        model.ConfirmedFellingRestockingDetails.ConfirmedFellingDetails.IsWithinConservationArea = false;
        model.ConfirmedFellingRestockingDetails.ConfirmedFellingDetails.ConservationAreaReference = null;

        var result = _modelValidator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor("ConfirmedFellingRestockingDetails.ConfirmedFellingDetails.ConservationAreaReference");
    }

    [Fact]
    public void Species_MustNotBeEmpty_WhenFelling()
    {
        var model = CreateViewModel(FellingOperationType.ClearFelling, TypeOfProposal.ReplantTheFelledArea);
        model.Species.Clear();
        var result = _modelValidator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(m => m.Species);
    }

    [Fact]
    public void NoValidation_WhenOperationTypeIsNone()
    {
        var model = CreateViewModel(FellingOperationType.None, TypeOfProposal.None);
        model.Species.Clear();
        var result = _modelValidator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(m => m.Species);
        result.ShouldNotHaveValidationErrorFor("ConfirmedFellingRestockingDetails.ConfirmedTotalHectares");
        result.ShouldNotHaveValidationErrorFor("ConfirmedFellingRestockingDetails.ConfirmedFellingDetails.AreaToBeFelled");
    }
}