using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services;

public partial class UpdateConfirmedFellingAndRestockingDetailsServiceTests
{
    [Fact]
    public async Task RevertConfirmedFellingDetailAmendmentsAsync_ReturnsSuccess_AndRevertsChangesToProposedValues()
    {
        // setup
        var userId = Guid.NewGuid();
        var sut = CreateSut();
        var fla = ConstructFellingLicenceApplication(false, userId);


        _fellingLicenceApplicationsContext!.Add(fla);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var proposed = fla.LinkedPropertyProfile!.ProposedFellingDetails!.First();
        var submittedFlaCompartment = fla.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!
            .First();

        submittedFlaCompartment.ConfirmedFellingDetails.Clear();
        var confirmedFellingDetail = new ConfirmedFellingDetail
        {
            SubmittedFlaPropertyCompartmentId = submittedFlaCompartment.Id,
            SubmittedFlaPropertyCompartment = submittedFlaCompartment,
            OperationType = FellingOperationType.FellingOfCoppice,
            AreaToBeFelled = 100d,
            NumberOfTrees = 45,
            TreeMarking = "newydd",
            IsPartOfTreePreservationOrder = false,
            TreePreservationOrderReference = null,
            IsWithinConservationArea = false,
            ConservationAreaReference = null,
            ConfirmedFellingSpecies = [],
            ConfirmedRestockingDetails = [],
            EstimatedTotalFellingVolume = 321,
            IsRestocking = false,
            NoRestockingReason = "test",
            ProposedFellingDetailId = proposed.Id
        };
        submittedFlaCompartment.ConfirmedFellingDetails.Add(confirmedFellingDetail);

        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        // act
        var result = await sut.RevertConfirmedFellingDetailAmendmentsAsync(
                fla.Id,
                confirmedFellingDetail.ProposedFellingDetailId.Value,
                userId,
                CancellationToken.None);

        // assert
        Assert.True(result.IsSuccess);

        var reloadedFla = await _fellingLicenceApplicationsContext.FellingLicenceApplications
            .Include(fellingLicenceApplication => fellingLicenceApplication.SubmittedFlaPropertyDetail!)
            .ThenInclude(submittedFlaPropertyDetail => submittedFlaPropertyDetail.SubmittedFlaPropertyCompartments!)
            .ThenInclude(submittedFlaPropertyCompartment => submittedFlaPropertyCompartment.ConfirmedFellingDetails)
            .ThenInclude(confirmedFellingDetail => confirmedFellingDetail.ConfirmedFellingSpecies)
            .Include(fellingLicenceApplication => fellingLicenceApplication.SubmittedFlaPropertyDetail!)
            .ThenInclude(submittedFlaPropertyDetail => submittedFlaPropertyDetail.SubmittedFlaPropertyCompartments!)
            .ThenInclude(submittedFlaPropertyCompartment => submittedFlaPropertyCompartment.ConfirmedFellingDetails)
            .ThenInclude(confirmedFellingDetail => confirmedFellingDetail.ConfirmedRestockingDetails)
            .FirstAsync(x => x.Id == fla.Id);
        var reloadedCompartment = reloadedFla!.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!
            .First(x => x.Id == submittedFlaCompartment.Id);
        var reloadedConfirmed = reloadedCompartment.ConfirmedFellingDetails.First(x => x.Id == confirmedFellingDetail.Id);

        Assert.Equal(proposed.AreaToBeFelled, reloadedConfirmed.AreaToBeFelled);
        Assert.Equal(proposed.IsPartOfTreePreservationOrder, reloadedConfirmed.IsPartOfTreePreservationOrder);
        Assert.Equal(proposed.TreePreservationOrderReference, reloadedConfirmed.TreePreservationOrderReference);
        Assert.Equal(proposed.IsWithinConservationArea, reloadedConfirmed.IsWithinConservationArea);
        Assert.Equal(proposed.ConservationAreaReference, reloadedConfirmed.ConservationAreaReference);
        Assert.Equal(proposed.NumberOfTrees, reloadedConfirmed.NumberOfTrees);
        Assert.Equal(proposed.OperationType, reloadedConfirmed.OperationType);
        Assert.Equal(proposed.TreeMarking, reloadedConfirmed.TreeMarking);
        Assert.Equal(proposed.EstimatedTotalFellingVolume, reloadedConfirmed.EstimatedTotalFellingVolume);
        Assert.Equal(proposed.IsRestocking ?? (proposed.ProposedRestockingDetails?.Any() == true), reloadedConfirmed.IsRestocking);
        Assert.Equal(proposed.NoRestockingReason, reloadedConfirmed.NoRestockingReason);

        // Felling species
        Assert.Equivalent(proposed.FellingSpecies?.Select(s => s.Species) ?? Enumerable.Empty<string>(),
            reloadedConfirmed.ConfirmedFellingSpecies.Select(s => s.Species));

        // Restocking details
        if (proposed.ProposedRestockingDetails != null)
        {
            Assert.Equal(proposed.ProposedRestockingDetails.Count, reloadedConfirmed.ConfirmedRestockingDetails.Count);
            foreach (var (confirmedRestock, proposedRestock) in reloadedConfirmed.ConfirmedRestockingDetails.Zip(proposed.ProposedRestockingDetails))
            {
                Assert.Equal(proposedRestock.Area, confirmedRestock.Area);
                Assert.Equal(proposedRestock.PercentageOfRestockArea, confirmedRestock.PercentageOfRestockArea);
                Assert.Equal(proposedRestock.RestockingDensity, confirmedRestock.RestockingDensity);
                Assert.Equal(proposedRestock.NumberOfTrees, confirmedRestock.NumberOfTrees);
                Assert.Equal(proposedRestock.RestockingProposal, confirmedRestock.RestockingProposal);

                // Restocking species
                Assert.Equivalent(proposedRestock.RestockingSpecies?.Select(s => s.Species) ?? Enumerable.Empty<string>(),
                    confirmedRestock.ConfirmedRestockingSpecies.Select(s => s.Species));
            }
        }
        else
        {
            Assert.Empty(reloadedConfirmed.ConfirmedRestockingDetails);
        }
    }


    [Fact]
    public async Task RevertConfirmedFellingDetailAmendmentsAsync_ReturnsFailure_WhenFlaNotFound()
    {
        var userId = Guid.NewGuid();
        var sut = CreateSut();

        var result = await sut.RevertConfirmedFellingDetailAmendmentsAsync(
            Guid.NewGuid(), // non-existent applicationId
            Guid.NewGuid(),
            userId,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("Unable to retrieve felling licence application", result.Error);
    }

    [Fact]
    public async Task RevertConfirmedFellingDetailAmendmentsAsync_ReturnsFailure_WhenUserNotPermitted()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var sut = CreateSut();
        var fla = ConstructFellingLicenceApplication(false, userId);

        _fellingLicenceApplicationsContext!.Add(fla);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var proposed = fla.LinkedPropertyProfile!.ProposedFellingDetails!.First();
        var submittedFlaCompartment = fla.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!.First();

        submittedFlaCompartment.ConfirmedFellingDetails.Clear();
        var confirmedFellingDetail = new ConfirmedFellingDetail
        {
            SubmittedFlaPropertyCompartmentId = submittedFlaCompartment.Id,
            SubmittedFlaPropertyCompartment = submittedFlaCompartment,
            OperationType = FellingOperationType.FellingOfCoppice,
            AreaToBeFelled = 100d,
            NumberOfTrees = 45,
            TreeMarking = "newydd",
            IsPartOfTreePreservationOrder = false,
            TreePreservationOrderReference = null,
            IsWithinConservationArea = false,
            ConservationAreaReference = null,
            ConfirmedFellingSpecies = [],
            ConfirmedRestockingDetails = [],
            EstimatedTotalFellingVolume = 321,
            IsRestocking = false,
            NoRestockingReason = "test",
            ProposedFellingDetailId = proposed.Id
        };
        submittedFlaCompartment.ConfirmedFellingDetails.Add(confirmedFellingDetail);

        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var result = await sut.RevertConfirmedFellingDetailAmendmentsAsync(
            fla.Id,
            confirmedFellingDetail.Id,
            otherUserId, // not permitted
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("is not permitted to amend felling licence application", result.Error);
    }

    [Fact]
    public async Task RevertConfirmedFellingDetailAmendmentsAsync_ReturnsSuccess_WhenConfirmedFellingDetailRecreated()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var sut = CreateSut();
        var fla = ConstructFellingLicenceApplication(false, userId);
        _fellingLicenceApplicationsContext!.Add(fla);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var proposed = fla.LinkedPropertyProfile!.ProposedFellingDetails!.First();
        var submittedFlaCompartment = fla.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!.First();

        // Remove all confirmed felling details to simulate deletion
        submittedFlaCompartment.ConfirmedFellingDetails.Clear();
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        // Act
        var result = await sut.RevertConfirmedFellingDetailAmendmentsAsync(
            fla.Id,
            proposed.Id,
            userId,
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);

        var reloadedFla = await _fellingLicenceApplicationsContext.FellingLicenceApplications
            .Include(x => x.SubmittedFlaPropertyDetail!)
                .ThenInclude(d => d.SubmittedFlaPropertyCompartments!)
                    .ThenInclude(c => c.ConfirmedFellingDetails)
                        .ThenInclude(f => f.ConfirmedFellingSpecies)
            .Include(x => x.SubmittedFlaPropertyDetail!)
                .ThenInclude(d => d.SubmittedFlaPropertyCompartments!)
                    .ThenInclude(c => c.ConfirmedFellingDetails)
                        .ThenInclude(f => f.ConfirmedRestockingDetails)
            .FirstAsync(x => x.Id == fla.Id);

        var reloadedCompartment = reloadedFla.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!
            .First(x => x.Id == submittedFlaCompartment.Id);
        var reloadedConfirmed = reloadedCompartment.ConfirmedFellingDetails
            .FirstOrDefault(x => x.ProposedFellingDetailId == proposed.Id);

        Assert.NotNull(reloadedConfirmed);
        Assert.Equal(proposed.AreaToBeFelled, reloadedConfirmed!.AreaToBeFelled);
        Assert.Equal(proposed.IsPartOfTreePreservationOrder, reloadedConfirmed.IsPartOfTreePreservationOrder);
        Assert.Equal(proposed.TreePreservationOrderReference, reloadedConfirmed.TreePreservationOrderReference);
        Assert.Equal(proposed.IsWithinConservationArea, reloadedConfirmed.IsWithinConservationArea);
        Assert.Equal(proposed.ConservationAreaReference, reloadedConfirmed.ConservationAreaReference);
        Assert.Equal(proposed.NumberOfTrees, reloadedConfirmed.NumberOfTrees);
        Assert.Equal(proposed.OperationType, reloadedConfirmed.OperationType);
        Assert.Equal(proposed.TreeMarking, reloadedConfirmed.TreeMarking);
        Assert.Equal(proposed.EstimatedTotalFellingVolume, reloadedConfirmed.EstimatedTotalFellingVolume);
        Assert.Equal(proposed.IsRestocking ?? (proposed.ProposedRestockingDetails?.Any() == true), reloadedConfirmed.IsRestocking);
        Assert.Equal(proposed.NoRestockingReason, reloadedConfirmed.NoRestockingReason);
        Assert.Equivalent(proposed.FellingSpecies?.Select(s => s.Species) ?? Enumerable.Empty<string>(),
            reloadedConfirmed.ConfirmedFellingSpecies.Select(s => s.Species));
        Assert.Equal(proposed.Id, reloadedConfirmed.ProposedFellingDetailId);
        Assert.Equal(reloadedCompartment.Id, reloadedConfirmed.SubmittedFlaPropertyCompartmentId);
        if (proposed.ProposedRestockingDetails != null)
        {
            Assert.Equal(proposed.ProposedRestockingDetails.Count, reloadedConfirmed.ConfirmedRestockingDetails.Count);
            foreach (var (confirmedRestock, proposedRestock) in reloadedConfirmed.ConfirmedRestockingDetails.Zip(proposed.ProposedRestockingDetails))
            {
                Assert.Equal(proposedRestock.Area, confirmedRestock.Area);
                Assert.Equal(proposedRestock.PercentageOfRestockArea, confirmedRestock.PercentageOfRestockArea);
                Assert.Equal(proposedRestock.RestockingDensity, confirmedRestock.RestockingDensity);
                Assert.Equal(proposedRestock.NumberOfTrees, confirmedRestock.NumberOfTrees);
                Assert.Equal(proposedRestock.RestockingProposal, confirmedRestock.RestockingProposal);
                Assert.Equivalent(proposedRestock.RestockingSpecies?.Select(s => s.Species) ?? Enumerable.Empty<string>(),
                    confirmedRestock.ConfirmedRestockingSpecies.Select(s => s.Species));
                Assert.Equal(reloadedCompartment.Id, confirmedRestock.SubmittedFlaPropertyCompartmentId);
                Assert.Equal(proposedRestock.Id, confirmedRestock.ProposedRestockingDetailId);
            }
        }
        else
        {
            Assert.Empty(reloadedConfirmed.ConfirmedRestockingDetails);
        }
    }

    [Fact]
    public async Task RevertConfirmedFellingDetailAmendmentsAsync_ReturnsSuccess_WhenConfirmedFellingDetailAlreadyExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var sut = CreateSut();
        var fla = ConstructFellingLicenceApplication(false, userId);
        _fellingLicenceApplicationsContext!.Add(fla);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var proposed = fla.LinkedPropertyProfile!.ProposedFellingDetails!.First();
        var submittedFlaCompartment = fla.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!.First();

        submittedFlaCompartment.ConfirmedFellingDetails.Clear();
        var confirmedFellingDetail = new ConfirmedFellingDetail
        {
            SubmittedFlaPropertyCompartmentId = submittedFlaCompartment.Id,
            SubmittedFlaPropertyCompartment = submittedFlaCompartment,
            OperationType = FellingOperationType.FellingOfCoppice,
            AreaToBeFelled = 100d,
            NumberOfTrees = 45,
            TreeMarking = "newydd",
            IsPartOfTreePreservationOrder = false,
            TreePreservationOrderReference = null,
            IsWithinConservationArea = false,
            ConservationAreaReference = null,
            ConfirmedFellingSpecies = [],
            ConfirmedRestockingDetails = [],
            EstimatedTotalFellingVolume = 321,
            IsRestocking = false,
            NoRestockingReason = "test",
            ProposedFellingDetailId = proposed.Id
        };
        submittedFlaCompartment.ConfirmedFellingDetails.Add(confirmedFellingDetail);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        // Act
        var result = await sut.RevertConfirmedFellingDetailAmendmentsAsync(
            fla.Id,
            proposed.Id,
            userId,
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task RevertConfirmedFellingDetailAmendmentsAsync_ReturnsFailure_WhenProposedFellingDetailNotFound()
    {
        var userId = Guid.NewGuid();
        var sut = CreateSut();
        var fla = ConstructFellingLicenceApplication(false, userId);
        _fellingLicenceApplicationsContext!.Add(fla);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var result = await sut.RevertConfirmedFellingDetailAmendmentsAsync(
            fla.Id,
            Guid.NewGuid(),
            userId,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("Unable to find proposed felling detail", result.Error);
    }

    [Fact]
    public async Task RevertConfirmedFellingDetailAmendmentsAsync_ReturnsFailure_WhenCompartmentNotFound()
    {
        var userId = Guid.NewGuid();
        var sut = CreateSut();
        var fla = ConstructFellingLicenceApplication(false, userId);
        _fellingLicenceApplicationsContext!.Add(fla);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var proposed = fla.LinkedPropertyProfile!.ProposedFellingDetails!.First();
        // Remove all compartments to simulate missing compartment
        fla.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!.Clear();
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var result = await sut.RevertConfirmedFellingDetailAmendmentsAsync(
            fla.Id,
            proposed.Id,
            userId,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("Unable to find compartment for proposed felling detail", result.Error);
    }
}