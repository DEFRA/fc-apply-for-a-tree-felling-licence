using AutoFixture;
using AutoFixture.Xunit2;
using Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;
using Forestry.Flo.Internal.Web.Services.Validation;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Extensions;

namespace Forestry.Flo.Internal.Web.Tests.Services.Validation;

public class ConfirmedFellingAndRestockingCrossValidatorTests
{
    private static readonly Fixture Fixture = new Fixture();

    [Theory, AutoData]
    public async Task WithMultipleSameFellingOperationInOneCompartment([CombinatorialValues]FellingOperationType fellingOperationType)
    {
        if (fellingOperationType == FellingOperationType.None)
        {
            return; // Skip invalid operation type
        }

        // Arrange
        Guid compartmentId = Fixture.Create<Guid>();
        var felling1 = CreateValidFellingWithoutRestocking(fellingOperationType);
        var felling2 = CreateValidFellingWithoutRestocking(fellingOperationType);  // two valid felling operations but with the same type

        var altType = Fixture.Create<FellingOperationType>();
        while (altType == fellingOperationType || altType == FellingOperationType.None)
        {
            altType = Fixture.Create<FellingOperationType>();
        }

        var felling3 = CreateValidFellingWithRestocking(compartmentId, altType);  // and a felling of a different type that is ok

        var cpt = Fixture.Build<CompartmentConfirmedFellingRestockingDetailsModel>()
            .With(x => x.ConfirmedFellingDetails, [felling1, felling2, felling3])
            .With(x => x.SubmittedFlaPropertyCompartmentId, compartmentId)
            .Create();

        var testData = Fixture.Build<ConfirmedFellingRestockingDetailsModel>()
            .With(x => x.Compartments, [cpt])
            .Create();

        var validator = new ConfirmedFellingAndRestockingCrossValidator();

        // Act

        var results = await validator.ValidateAsync(testData);

        // Assert

        Assert.False(results.IsValid);
        Assert.Equal(2, results.Errors.Count);

        Assert.Contains(results.Errors, x => 
            x.FormattedMessagePlaceholderValues["PropertyName"].ToString() == $"amend-link-felling-{felling1.ConfirmedFellingDetailsId}"
            && x.ErrorMessage == $"There is more than one {fellingOperationType.GetDisplayName()} operation in compartment {cpt.CompartmentName}");

        Assert.Contains(results.Errors, x =>
            x.FormattedMessagePlaceholderValues["PropertyName"].ToString() == $"amend-link-felling-{felling2.ConfirmedFellingDetailsId}"
            && x.ErrorMessage == $"There is more than one {fellingOperationType.GetDisplayName()} operation in compartment {cpt.CompartmentName}");

    }

    [Theory, AutoData]
    public async Task WithFellingSetToNotRestockingButHasRestocking([CombinatorialValues] FellingOperationType fellingOperationType)
    {
        if (fellingOperationType is FellingOperationType.None or FellingOperationType.Thinning)
        {
            return; // Skip operation types not covered by this test
        }

        // Arrange
        var compartmentId = Fixture.Create<Guid>();
        var felling1 = CreateValidFellingWithRestocking(compartmentId, fellingOperationType);
        felling1.IsRestocking = false;  // Set felling to not restocking, but it has restocking details
        felling1.NoRestockingReason = "No restocking required";  // Set reason for no restocking

        var altType = Fixture.Create<FellingOperationType>();
        while (altType == fellingOperationType || altType == FellingOperationType.None)
        {
            altType = Fixture.Create<FellingOperationType>();
        }

        var felling2 = CreateValidFellingWithRestocking(compartmentId, altType);  // and a felling of a different type that is ok

        var cpt = Fixture.Build<CompartmentConfirmedFellingRestockingDetailsModel>()
            .With(x => x.ConfirmedFellingDetails, [felling1, felling2])
            .With(x => x.SubmittedFlaPropertyCompartmentId, compartmentId)
            .Create();

        var testData = Fixture.Build<ConfirmedFellingRestockingDetailsModel>()
            .With(x => x.Compartments, [cpt])
            .Create();

        var validator = new ConfirmedFellingAndRestockingCrossValidator();

        // Act

        var results = await validator.ValidateAsync(testData);

        // Assert

        Assert.False(results.IsValid);
        Assert.Single(results.Errors);

        Assert.Contains(results.Errors, x =>
            x.FormattedMessagePlaceholderValues["PropertyName"].ToString() == $"amend-link-felling-{felling1.ConfirmedFellingDetailsId}"
            && x.ErrorMessage == $"{fellingOperationType.GetDisplayName()} operation in compartment {cpt.CompartmentName} has 'Is Restocking' set to 'No' but has linked restocking operations");
    }

    [Fact]
    public async Task WithThinningButHasRestocking()
    {
        // Arrange
        var compartmentId = Fixture.Create<Guid>();
        var felling1 = CreateValidThinningFellingWithoutRestocking();
        var restocking = Fixture.Build<ConfirmedRestockingDetailViewModel>()
            .With(x => x.RestockingProposal, TypeOfProposal.ReplantTheFelledArea)
            .With(x => x.RestockingCompartmentId, compartmentId)
            .Without(x => x.AmendedProperties)
            .Create();
        felling1.ConfirmedRestockingDetails = [restocking];  // Thinning should not have restocking details

        var altType = Fixture.Create<FellingOperationType>();
        while (altType == FellingOperationType.Thinning || altType == FellingOperationType.None)
        {
            altType = Fixture.Create<FellingOperationType>();
        }

        var felling2 = CreateValidFellingWithRestocking(compartmentId, altType);  // and a felling of a different type that is ok

        var cpt = Fixture.Build<CompartmentConfirmedFellingRestockingDetailsModel>()
            .With(x => x.ConfirmedFellingDetails, [felling1, felling2])
            .With(x => x.SubmittedFlaPropertyCompartmentId, compartmentId)
            .Create();

        var testData = Fixture.Build<ConfirmedFellingRestockingDetailsModel>()
            .With(x => x.Compartments, [cpt])
            .Create();

        var validator = new ConfirmedFellingAndRestockingCrossValidator();

        // Act

        var results = await validator.ValidateAsync(testData);

        // Assert

        Assert.False(results.IsValid);
        Assert.Equal(2, results.Errors.Count);

        Assert.Contains(results.Errors, x =>
                x.FormattedMessagePlaceholderValues["PropertyName"].ToString() == $"amend-link-felling-{felling1.ConfirmedFellingDetailsId}"
                && x.ErrorMessage == $"{FellingOperationType.Thinning.GetDisplayName()} operation in compartment {cpt.CompartmentName} has linked restocking operations");

        Assert.Contains(results.Errors, x =>
                x.FormattedMessagePlaceholderValues["PropertyName"].ToString() == $"amend-link-restocking-{felling1.ConfirmedRestockingDetails[0].ConfirmedRestockingDetailsId}"
                && x.ErrorMessage == $"{TypeOfProposal.ReplantTheFelledArea.GetDisplayName()} restocking is not allowed for the {FellingOperationType.Thinning.GetDisplayName()} felling operation in compartment {cpt.CompartmentName}");
    }

    [Theory, AutoData]
    public async Task WithFellingSetToIsRestockingButHasNoRestocking([CombinatorialValues] FellingOperationType fellingOperationType)
    {
        if (fellingOperationType is FellingOperationType.None or FellingOperationType.Thinning)
        {
            return; // Skip operation types not covered by this test
        }

        // Arrange
        var compartmentId = Fixture.Create<Guid>();
        var felling1 = CreateValidFellingWithRestocking(compartmentId, fellingOperationType);
        felling1.ConfirmedRestockingDetails = [];  // Set felling to have no restocking details

        var altType = Fixture.Create<FellingOperationType>();
        while (altType == fellingOperationType || altType == FellingOperationType.None)
        {
            altType = Fixture.Create<FellingOperationType>();
        }

        var felling2 = CreateValidFellingWithRestocking(compartmentId, altType);  // and a felling of a different type that is ok

        var cpt = Fixture.Build<CompartmentConfirmedFellingRestockingDetailsModel>()
            .With(x => x.ConfirmedFellingDetails, [felling1, felling2])
            .With(x => x.SubmittedFlaPropertyCompartmentId, compartmentId)
            .Create();

        var testData = Fixture.Build<ConfirmedFellingRestockingDetailsModel>()
            .With(x => x.Compartments, [cpt])
            .Create();

        var validator = new ConfirmedFellingAndRestockingCrossValidator();

        // Act

        var results = await validator.ValidateAsync(testData);

        // Assert

        Assert.False(results.IsValid);
        Assert.Single(results.Errors);

        Assert.Contains(results.Errors, x =>
            x.FormattedMessagePlaceholderValues["PropertyName"].ToString() == $"amend-link-felling-{felling1.ConfirmedFellingDetailsId}"
            && x.ErrorMessage == $"{fellingOperationType.GetDisplayName()} operation in compartment {cpt.CompartmentName} has 'Is Restocking' set to 'Yes' but has no linked restocking operations");
    }

    [Theory, AutoData]
    public async Task WithASameCompartmentRestockingNotInFellingCompartment([CombinatorialValues] FellingOperationType fellingOperationType)
    {
        if (fellingOperationType is FellingOperationType.None or FellingOperationType.Thinning)
        {
            return; // Skip operation types not covered by this test
        }

        // Arrange
        var compartmentId = Fixture.Create<Guid>();
        var felling1 = CreateValidFellingWithRestocking(compartmentId, fellingOperationType);

        var sameCompartmentRestockingOp = felling1.ConfirmedRestockingDetails.FirstOrDefault(r => !r.RestockingProposal!.Value.IsAlternativeCompartmentRestockingType() == false);
        if (sameCompartmentRestockingOp == null)
        {
            return; // No same-compartment restocking operations to validate
        }
        sameCompartmentRestockingOp.RestockingCompartmentId = Guid.NewGuid(); // Set the same-compartment restocking to another compartment

        var altType = Fixture.Create<FellingOperationType>();
        while (altType == fellingOperationType || altType == FellingOperationType.None)
        {
            altType = Fixture.Create<FellingOperationType>();
        }

        var felling2 = CreateValidFellingWithRestocking(compartmentId, altType);  // and a felling of a different type that is ok

        var cpt = Fixture.Build<CompartmentConfirmedFellingRestockingDetailsModel>()
            .With(x => x.ConfirmedFellingDetails, [felling1, felling2])
            .With(x => x.SubmittedFlaPropertyCompartmentId, compartmentId)
            .Create();

        var testData = Fixture.Build<ConfirmedFellingRestockingDetailsModel>()
            .With(x => x.Compartments, [cpt])
            .Create();

        var validator = new ConfirmedFellingAndRestockingCrossValidator();

        // Act

        var results = await validator.ValidateAsync(testData);

        // Assert

        Assert.False(results.IsValid);
        Assert.Single(results.Errors);

        Assert.Contains(results.Errors, x =>
            x.FormattedMessagePlaceholderValues["PropertyName"].ToString() == $"amend-link-restocking-{sameCompartmentRestockingOp.ConfirmedRestockingDetailsId}"
            && x.ErrorMessage == $"{sameCompartmentRestockingOp.RestockingProposal.GetDisplayName()} restocking must be in the same compartment as the {felling1.OperationType.GetDisplayName()} felling operation in compartment {cpt.CompartmentName}");
    }

    [Theory, AutoData]
    public async Task WithAllSameCompartmentRestockingNotInFellingCompartment([CombinatorialValues] FellingOperationType fellingOperationType)
    {
        if (fellingOperationType is FellingOperationType.None or FellingOperationType.Thinning)
        {
            return; // Skip operation types not covered by this test
        }

        // Arrange
        var compartmentId = Fixture.Create<Guid>();
        var felling1 = CreateValidFellingWithRestocking(compartmentId, fellingOperationType);

        var sameCompartmentRestockingOps = felling1.ConfirmedRestockingDetails.Where(r => !r.RestockingProposal!.Value.IsAlternativeCompartmentRestockingType() == false).ToList();
        if (!sameCompartmentRestockingOps.Any())
        {
            return; // No same-compartment restocking operations to validate
        }
        sameCompartmentRestockingOps.ForEach(r => r.RestockingCompartmentId = Guid.NewGuid()); // Set all same-compartment restockings to another compartment

        var altType = Fixture.Create<FellingOperationType>();
        while (altType == fellingOperationType || altType == FellingOperationType.None)
        {
            altType = Fixture.Create<FellingOperationType>();
        }

        var felling2 = CreateValidFellingWithRestocking(compartmentId, altType);  // and a felling of a different type that is ok

        var cpt = Fixture.Build<CompartmentConfirmedFellingRestockingDetailsModel>()
            .With(x => x.ConfirmedFellingDetails, [felling1, felling2])
            .With(x => x.SubmittedFlaPropertyCompartmentId, compartmentId)
            .Create();

        var testData = Fixture.Build<ConfirmedFellingRestockingDetailsModel>()
            .With(x => x.Compartments, [cpt])
            .Create();

        var validator = new ConfirmedFellingAndRestockingCrossValidator();

        // Act

        var results = await validator.ValidateAsync(testData);

        // Assert

        Assert.False(results.IsValid);
        Assert.Equal(sameCompartmentRestockingOps.Count, results.Errors.Count);

        foreach (var restocking in sameCompartmentRestockingOps)
        {
            Assert.Contains(results.Errors, x =>
                x.FormattedMessagePlaceholderValues["PropertyName"].ToString() == $"amend-link-restocking-{restocking.ConfirmedRestockingDetailsId}"
                && x.ErrorMessage == $"{restocking.RestockingProposal.GetDisplayName()} restocking must be in the same compartment as the {felling1.OperationType.GetDisplayName()} felling operation in compartment {cpt.CompartmentName}");

        }
    }

    [Theory, AutoData]
    public async Task WithAnAltCompartmentRestockingInFellingCompartment([CombinatorialValues] FellingOperationType fellingOperationType)
    {
        if (fellingOperationType is FellingOperationType.None or FellingOperationType.Thinning)
        {
            return; // Skip operation types not covered by this test
        }

        // Arrange
        var compartmentId = Fixture.Create<Guid>();
        var felling1 = CreateValidFellingWithRestocking(compartmentId, fellingOperationType);

        var altCompartmentRestockingOp = felling1.ConfirmedRestockingDetails.FirstOrDefault(r => !r.RestockingProposal!.Value.IsAlternativeCompartmentRestockingType());
        if (altCompartmentRestockingOp == null)
        {
            return; // No alt-compartment restocking operations to validate
        }
        altCompartmentRestockingOp.RestockingCompartmentId = compartmentId; // Set the alt-compartment restocking to the felling compartment

        var altType = Fixture.Create<FellingOperationType>();
        while (altType == fellingOperationType || altType == FellingOperationType.None)
        {
            altType = Fixture.Create<FellingOperationType>();
        }

        var felling2 = CreateValidFellingWithRestocking(compartmentId, altType);  // and a felling of a different type that is ok

        var cpt = Fixture.Build<CompartmentConfirmedFellingRestockingDetailsModel>()
            .With(x => x.ConfirmedFellingDetails, [felling1, felling2])
            .With(x => x.SubmittedFlaPropertyCompartmentId, compartmentId)
            .Create();

        var testData = Fixture.Build<ConfirmedFellingRestockingDetailsModel>()
            .With(x => x.Compartments, [cpt])
            .Create();

        var validator = new ConfirmedFellingAndRestockingCrossValidator();

        // Act

        var results = await validator.ValidateAsync(testData);

        // Assert

        Assert.False(results.IsValid);
        Assert.Single(results.Errors);

        Assert.Contains(results.Errors, x =>
            x.FormattedMessagePlaceholderValues["PropertyName"].ToString() == $"amend-link-restocking-{altCompartmentRestockingOp.ConfirmedRestockingDetailsId}"
            && x.ErrorMessage == $"{altCompartmentRestockingOp.RestockingProposal.GetDisplayName()} restocking must be in a different compartment than the {felling1.OperationType.GetDisplayName()} felling operation in compartment {cpt.CompartmentName}");
    }

    [Theory, AutoData]
    public async Task WithMultipleSameRestockingTypeInFellingCompartment([CombinatorialValues] FellingOperationType fellingOperationType)
    {
        if (fellingOperationType is FellingOperationType.None or FellingOperationType.Thinning)
        {
            return; // Skip operation types not covered by this test
        }

        // Arrange
        var compartmentId = Fixture.Create<Guid>();
        var felling1 = CreateValidFellingWithRestocking(compartmentId, fellingOperationType);

        var sameCompartmentRestockingOp = felling1.ConfirmedRestockingDetails.FirstOrDefault(r => !r.RestockingProposal!.Value.IsAlternativeCompartmentRestockingType() == false);
        if (sameCompartmentRestockingOp == null)
        {
            return; // No same-compartment restocking operations to validate
        }


        var duplicateRestocking = Fixture.Build<ConfirmedRestockingDetailViewModel>()
            .With(x => x.OperationType, sameCompartmentRestockingOp.OperationType)
            .With(x => x.RestockingCompartmentId, sameCompartmentRestockingOp.RestockingCompartmentId)
            .Create();

        felling1.ConfirmedRestockingDetails = [ ..felling1.ConfirmedRestockingDetails, duplicateRestocking]; // Add a duplicate restocking of the same type in the same compartment

        var altType = Fixture.Create<FellingOperationType>();
        while (altType == fellingOperationType || altType == FellingOperationType.None)
        {
            altType = Fixture.Create<FellingOperationType>();
        }

        var felling2 = CreateValidFellingWithRestocking(compartmentId, altType);  // and a felling of a different type that is ok

        var cpt = Fixture.Build<CompartmentConfirmedFellingRestockingDetailsModel>()
            .With(x => x.ConfirmedFellingDetails, [felling1, felling2])
            .With(x => x.SubmittedFlaPropertyCompartmentId, compartmentId)
            .Create();

        var testData = Fixture.Build<ConfirmedFellingRestockingDetailsModel>()
            .With(x => x.Compartments, [cpt])
            .Create();

        var validator = new ConfirmedFellingAndRestockingCrossValidator();

        // Act

        var results = await validator.ValidateAsync(testData);

        // Assert

        Assert.False(results.IsValid);
        Assert.Equal(2, results.Errors.Count);

        Assert.Contains(results.Errors, x =>
            x.FormattedMessagePlaceholderValues["PropertyName"].ToString() == $"amend-link-restocking-{sameCompartmentRestockingOp.ConfirmedRestockingDetailsId}"
            && x.ErrorMessage == $"{sameCompartmentRestockingOp.RestockingProposal.GetDisplayName()} restocking occurs multiple times for the {felling1.OperationType.GetDisplayName()} felling operation in compartment {cpt.CompartmentName}");
        Assert.Contains(results.Errors, x =>
            x.FormattedMessagePlaceholderValues["PropertyName"].ToString() == $"amend-link-restocking-{duplicateRestocking.ConfirmedRestockingDetailsId}"
            && x.ErrorMessage == $"{duplicateRestocking.RestockingProposal.GetDisplayName()} restocking occurs multiple times for the {felling1.OperationType.GetDisplayName()} felling operation in compartment {cpt.CompartmentName}");
    }

    [Theory, AutoData]
    public async Task WithAllAltCompartmentRestockingInFellingCompartment([CombinatorialValues] FellingOperationType fellingOperationType)
    {
        if (fellingOperationType is FellingOperationType.None or FellingOperationType.Thinning)
        {
            return; // Skip operation types not covered by this test
        }

        // Arrange
        var compartmentId = Fixture.Create<Guid>();
        var felling1 = CreateValidFellingWithRestocking(compartmentId, fellingOperationType);

        var altCompartmentRestockingOps = felling1.ConfirmedRestockingDetails.Where(r => !r.RestockingProposal!.Value.IsAlternativeCompartmentRestockingType()).ToList();
        if (!altCompartmentRestockingOps.Any())
        {
            return; // No alt-compartment restocking operations to validate
        }
        altCompartmentRestockingOps.ForEach(r => r.RestockingCompartmentId = compartmentId); // Set the alt-compartment restockings to the felling compartment

        var altType = Fixture.Create<FellingOperationType>();
        while (altType == fellingOperationType || altType == FellingOperationType.None)
        {
            altType = Fixture.Create<FellingOperationType>();
        }

        var felling2 = CreateValidFellingWithRestocking(compartmentId, altType);  // and a felling of a different type that is ok

        var cpt = Fixture.Build<CompartmentConfirmedFellingRestockingDetailsModel>()
            .With(x => x.ConfirmedFellingDetails, [felling1, felling2])
            .With(x => x.SubmittedFlaPropertyCompartmentId, compartmentId)
            .Create();

        var testData = Fixture.Build<ConfirmedFellingRestockingDetailsModel>()
            .With(x => x.Compartments, [cpt])
            .Create();

        var validator = new ConfirmedFellingAndRestockingCrossValidator();

        // Act

        var results = await validator.ValidateAsync(testData);

        // Assert

        Assert.False(results.IsValid);
        Assert.Equal(altCompartmentRestockingOps.Count, results.Errors.Count);

        foreach (var altCompartmentRestockingOp in altCompartmentRestockingOps)
        {
            Assert.Contains(results.Errors, x =>
                x.FormattedMessagePlaceholderValues["PropertyName"].ToString() == $"amend-link-restocking-{altCompartmentRestockingOp.ConfirmedRestockingDetailsId}"
                && x.ErrorMessage == $"{altCompartmentRestockingOp.RestockingProposal.GetDisplayName()} restocking must be in a different compartment than the {felling1.OperationType.GetDisplayName()} felling operation in compartment {cpt.CompartmentName}");
        }
    }

    [Theory, AutoData]
    public async Task WithMultipleSameRestockingTypeInSameAltCompartment([CombinatorialValues] FellingOperationType fellingOperationType)
    {
        if (fellingOperationType is FellingOperationType.None or FellingOperationType.Thinning)
        {
            return; // Skip operation types not covered by this test
        }

        // Arrange
        var compartmentId = Fixture.Create<Guid>();
        var felling1 = CreateValidFellingWithRestocking(compartmentId, fellingOperationType);

        var altCompartmentRestockingOp = felling1.ConfirmedRestockingDetails.FirstOrDefault(r => !r.RestockingProposal!.Value.IsAlternativeCompartmentRestockingType());
        if (altCompartmentRestockingOp == null)
        {
            return; // No same-compartment restocking operations to validate
        }

        var duplicateRestocking = Fixture.Build<ConfirmedRestockingDetailViewModel>()
            .With(x => x.OperationType, altCompartmentRestockingOp.OperationType)
            .With(x => x.RestockingCompartmentId, altCompartmentRestockingOp.RestockingCompartmentId)
            .Create();

        felling1.ConfirmedRestockingDetails = [.. felling1.ConfirmedRestockingDetails, duplicateRestocking]; // Add a duplicate restocking of the same type in the same compartment

        var altType = Fixture.Create<FellingOperationType>();
        while (altType == fellingOperationType || altType == FellingOperationType.None)
        {
            altType = Fixture.Create<FellingOperationType>();
        }

        var felling2 = CreateValidFellingWithRestocking(compartmentId, altType);  // and a felling of a different type that is ok

        var cpt = Fixture.Build<CompartmentConfirmedFellingRestockingDetailsModel>()
            .With(x => x.ConfirmedFellingDetails, [felling1, felling2])
            .With(x => x.SubmittedFlaPropertyCompartmentId, compartmentId)
            .Create();

        var testData = Fixture.Build<ConfirmedFellingRestockingDetailsModel>()
            .With(x => x.Compartments, [cpt])
            .Create();

        var validator = new ConfirmedFellingAndRestockingCrossValidator();

        // Act

        var results = await validator.ValidateAsync(testData);

        // Assert

        Assert.False(results.IsValid);
        Assert.Equal(2, results.Errors.Count);

        Assert.Contains(results.Errors, x =>
            x.FormattedMessagePlaceholderValues["PropertyName"].ToString() == $"amend-link-restocking-{altCompartmentRestockingOp.ConfirmedRestockingDetailsId}"
            && x.ErrorMessage == $"{altCompartmentRestockingOp.RestockingProposal.GetDisplayName()} restocking occurs multiple times in the same alternative compartment for the {felling1.OperationType.GetDisplayName()} felling operation in compartment {cpt.CompartmentName}");
        Assert.Contains(results.Errors, x =>
            x.FormattedMessagePlaceholderValues["PropertyName"].ToString() == $"amend-link-restocking-{duplicateRestocking.ConfirmedRestockingDetailsId}"
            && x.ErrorMessage == $"{duplicateRestocking.RestockingProposal.GetDisplayName()} restocking occurs multiple times in the same alternative compartment for the {felling1.OperationType.GetDisplayName()} felling operation in compartment {cpt.CompartmentName}");
    }

    [Theory, AutoData]
    public async Task CanHaveMultipleSameAltAreaRestockingTypeInDifferentAltCompartments([CombinatorialValues] FellingOperationType fellingOperationType)
    {
        if (fellingOperationType is FellingOperationType.None or FellingOperationType.Thinning)
        {
            return; // Skip operation types not covered by this test
        }

        // Arrange
        var compartmentId = Fixture.Create<Guid>();
        var felling1 = CreateValidFellingWithRestocking(compartmentId, fellingOperationType);

        var altCompartmentRestockingOp = felling1.ConfirmedRestockingDetails.FirstOrDefault(r => !r.RestockingProposal!.Value.IsAlternativeCompartmentRestockingType());
        if (altCompartmentRestockingOp == null)
        {
            return; // No same-compartment restocking operations to validate
        }


        var duplicateRestocking = Fixture.Build<ConfirmedRestockingDetailViewModel>()
            .With(x => x.OperationType, altCompartmentRestockingOp.OperationType)
            .Create();

        felling1.ConfirmedRestockingDetails = [.. felling1.ConfirmedRestockingDetails, duplicateRestocking]; // Add a duplicate restocking of the same alt-area type but in another alt compartment

        var altType = Fixture.Create<FellingOperationType>();
        while (altType == fellingOperationType || altType == FellingOperationType.None)
        {
            altType = Fixture.Create<FellingOperationType>();
        }

        var felling2 = CreateValidFellingWithRestocking(compartmentId, altType);  // and a felling of a different type that is ok

        var cpt = Fixture.Build<CompartmentConfirmedFellingRestockingDetailsModel>()
            .With(x => x.ConfirmedFellingDetails, [felling1, felling2])
            .With(x => x.SubmittedFlaPropertyCompartmentId, compartmentId)
            .Create();

        var testData = Fixture.Build<ConfirmedFellingRestockingDetailsModel>()
            .With(x => x.Compartments, [cpt])
            .Create();

        var validator = new ConfirmedFellingAndRestockingCrossValidator();

        // Act

        var results = await validator.ValidateAsync(testData);

        // Assert

        Assert.True(results.IsValid);
        Assert.Empty(results.Errors);
    }

    [Theory, AutoData]
    public async Task WithInvalidRestockingTypeForFellingType([CombinatorialValues] FellingOperationType fellingOperationType)
    {
        if (fellingOperationType is FellingOperationType.None or FellingOperationType.Thinning)
        {
            return; // Skip operation types not covered by this test
        }

        // Arrange
        var compartmentId = Fixture.Create<Guid>();
        var felling1 = CreateValidFellingWithRestocking(compartmentId, fellingOperationType);

        var invalidRestockingTypeForFellingType = Enum
            .GetValues<TypeOfProposal>()
            .FirstOrDefault(r => fellingOperationType.AllowedRestockingForFellingType(false).Contains(r) == false);

        if (invalidRestockingTypeForFellingType == TypeOfProposal.None)
        {
            return; // No invalid restocking types to validate
        }

        var restockingCptId = invalidRestockingTypeForFellingType.IsAlternativeCompartmentRestockingType() ? Guid.NewGuid() : compartmentId;
        var invalidRestocking = Fixture.Build<ConfirmedRestockingDetailViewModel>()
            .With(x => x.RestockingProposal, invalidRestockingTypeForFellingType)
            .With(x => x.RestockingCompartmentId, restockingCptId)
            .Without(x => x.AmendedProperties)
            .Create();

        felling1.ConfirmedRestockingDetails = [.. felling1.ConfirmedRestockingDetails, invalidRestocking]; // Add a duplicate restocking of the same type in the same compartment

        var altType = Fixture.Create<FellingOperationType>();
        while (altType == fellingOperationType || altType == FellingOperationType.None)
        {
            altType = Fixture.Create<FellingOperationType>();
        }

        var felling2 = CreateValidFellingWithRestocking(compartmentId, altType);  // and a felling of a different type that is ok

        var cpt = Fixture.Build<CompartmentConfirmedFellingRestockingDetailsModel>()
            .With(x => x.ConfirmedFellingDetails, [felling1, felling2])
            .With(x => x.SubmittedFlaPropertyCompartmentId, compartmentId)
            .Create();

        var testData = Fixture.Build<ConfirmedFellingRestockingDetailsModel>()
            .With(x => x.Compartments, [cpt])
            .Create();

        var validator = new ConfirmedFellingAndRestockingCrossValidator();

        // Act

        var results = await validator.ValidateAsync(testData);

        // Assert

        Assert.False(results.IsValid);
        Assert.Single(results.Errors);

        Assert.Contains(results.Errors, x =>
            x.FormattedMessagePlaceholderValues["PropertyName"].ToString() == $"amend-link-restocking-{invalidRestocking.ConfirmedRestockingDetailsId}"
            && x.ErrorMessage == $"{invalidRestocking.RestockingProposal.GetDisplayName()} restocking is not allowed for the {felling1.OperationType.GetDisplayName()} felling operation in compartment {cpt.CompartmentName}");
    }

    [Theory, AutoData]
    public async Task WithValidModel([CombinatorialValues] FellingOperationType fellingOperationType)
    {
        if (fellingOperationType is FellingOperationType.None or FellingOperationType.Thinning)
        {
            return; // Skip operation types not covered by this test
        }

        // Arrange
        var compartmentId = Fixture.Create<Guid>();
        var felling1 = CreateValidFellingWithRestocking(compartmentId, fellingOperationType);

        var altType = Fixture.Create<FellingOperationType>();
        while (altType == fellingOperationType || altType == FellingOperationType.None)
        {
            altType = Fixture.Create<FellingOperationType>();
        }

        var felling2 = CreateValidFellingWithoutRestocking(altType);  // and a felling of a different type that has no restocking

        var cpt = Fixture.Build<CompartmentConfirmedFellingRestockingDetailsModel>()
            .With(x => x.ConfirmedFellingDetails, [felling1, felling2])
            .With(x => x.SubmittedFlaPropertyCompartmentId, compartmentId)
            .Create();

        var testData = Fixture.Build<ConfirmedFellingRestockingDetailsModel>()
            .With(x => x.Compartments, [cpt])
            .Create();

        var validator = new ConfirmedFellingAndRestockingCrossValidator();

        // Act

        var results = await validator.ValidateAsync(testData);

        // Assert

        Assert.True(results.IsValid);
        Assert.Empty(results.Errors);
    }

    private ConfirmedFellingDetailViewModel CreateValidFellingWithoutRestocking(
        FellingOperationType operation = FellingOperationType.ClearFelling)
    {
        return Fixture.Build<ConfirmedFellingDetailViewModel>()
            .With(x => x.OperationType, operation)
            .With(x => x.IsRestocking, operation == FellingOperationType.Thinning ? null : false)
            .With(x => x.NoRestockingReason, operation == FellingOperationType.Thinning ? null : "No restocking required")
            .With(x => x.ConfirmedRestockingDetails, [])
            .Create();
    }

    private ConfirmedFellingDetailViewModel CreateValidFellingWithRestocking(
        Guid compartmentId,
        FellingOperationType operation = FellingOperationType.ClearFelling)
    {
        var validRestockingTypes = operation.AllowedRestockingForFellingType(false);

        var restockingModels = new List<ConfirmedRestockingDetailViewModel>();

        foreach (var validRestockingType in validRestockingTypes)
        {
            restockingModels.Add(Fixture.Build<ConfirmedRestockingDetailViewModel>()
                .With(x => x.RestockingProposal, validRestockingType)
                .With(x => x.RestockingCompartmentId, validRestockingType.IsAlternativeCompartmentRestockingType()
                    ? Fixture.Create<Guid>()  // Use a different compartment ID for alternative compartment restocking
                    : compartmentId)
                .Without(x => x.AmendedProperties)
                .Create());
        }

        return Fixture.Build<ConfirmedFellingDetailViewModel>()
            .With(x => x.OperationType, operation)
            .With(x => x.IsRestocking, true)
            .With(x => x.ConfirmedRestockingDetails, restockingModels.ToArray)
            .Create();
    }

    private ConfirmedFellingDetailViewModel CreateValidThinningFellingWithoutRestocking()
    {
        return Fixture.Build<ConfirmedFellingDetailViewModel>()
            .With(x => x.OperationType, FellingOperationType.Thinning)
            .Without(x => x.IsRestocking)
            .Without(x => x.NoRestockingReason)
            .With(x => x.ConfirmedRestockingDetails, [])
            .Without(x => x.AmendedProperties)
            .Create();
    }
}