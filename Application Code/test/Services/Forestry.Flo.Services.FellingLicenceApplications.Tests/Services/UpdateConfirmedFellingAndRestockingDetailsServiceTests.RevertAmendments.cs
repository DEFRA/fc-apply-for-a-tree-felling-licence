using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
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
        result.IsSuccess.Should().BeTrue();

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

        reloadedConfirmed.AreaToBeFelled.Should().Be(proposed.AreaToBeFelled);
        reloadedConfirmed.IsPartOfTreePreservationOrder.Should().Be(proposed.IsPartOfTreePreservationOrder);
        reloadedConfirmed.TreePreservationOrderReference.Should().Be(proposed.TreePreservationOrderReference);
        reloadedConfirmed.IsWithinConservationArea.Should().Be(proposed.IsWithinConservationArea);
        reloadedConfirmed.ConservationAreaReference.Should().Be(proposed.ConservationAreaReference);
        reloadedConfirmed.NumberOfTrees.Should().Be(proposed.NumberOfTrees);
        reloadedConfirmed.OperationType.Should().Be(proposed.OperationType);
        reloadedConfirmed.TreeMarking.Should().Be(proposed.TreeMarking);
        reloadedConfirmed.EstimatedTotalFellingVolume.Should().Be(proposed.EstimatedTotalFellingVolume);
        reloadedConfirmed.IsRestocking.Should().Be(proposed.IsRestocking ?? (proposed.ProposedRestockingDetails?.Any() == true));
        reloadedConfirmed.NoRestockingReason.Should().Be(proposed.NoRestockingReason);

        // Felling species
        reloadedConfirmed.ConfirmedFellingSpecies.Select(s => s.Species)
            .Should().BeEquivalentTo(proposed.FellingSpecies?.Select(s => s.Species) ?? Enumerable.Empty<string>());

        // Restocking details
        if (proposed.ProposedRestockingDetails != null)
        {
            reloadedConfirmed.ConfirmedRestockingDetails.Count.Should().Be(proposed.ProposedRestockingDetails.Count);
            foreach (var (confirmedRestock, proposedRestock) in reloadedConfirmed.ConfirmedRestockingDetails.Zip(proposed.ProposedRestockingDetails))
            {
                confirmedRestock.Area.Should().Be(proposedRestock.Area);
                confirmedRestock.PercentageOfRestockArea.Should().Be(proposedRestock.PercentageOfRestockArea);
                confirmedRestock.RestockingDensity.Should().Be(proposedRestock.RestockingDensity);
                confirmedRestock.NumberOfTrees.Should().Be(proposedRestock.NumberOfTrees);
                confirmedRestock.RestockingProposal.Should().Be(proposedRestock.RestockingProposal);

                // Restocking species
                confirmedRestock.ConfirmedRestockingSpecies.Select(s => s.Species)
                    .Should().BeEquivalentTo(proposedRestock.RestockingSpecies?.Select(s => s.Species) ?? Enumerable.Empty<string>());
            }
        }
        else
        {
            reloadedConfirmed.ConfirmedRestockingDetails.Should().BeEmpty();
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

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Unable to retrieve felling licence application");
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

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("is not permitted to amend felling licence application");
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
        result.IsSuccess.Should().BeTrue();

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

        reloadedConfirmed.Should().NotBeNull();
        reloadedConfirmed!.AreaToBeFelled.Should().Be(proposed.AreaToBeFelled);
        reloadedConfirmed.IsPartOfTreePreservationOrder.Should().Be(proposed.IsPartOfTreePreservationOrder);
        reloadedConfirmed.TreePreservationOrderReference.Should().Be(proposed.TreePreservationOrderReference);
        reloadedConfirmed.IsWithinConservationArea.Should().Be(proposed.IsWithinConservationArea);
        reloadedConfirmed.ConservationAreaReference.Should().Be(proposed.ConservationAreaReference);
        reloadedConfirmed.NumberOfTrees.Should().Be(proposed.NumberOfTrees);
        reloadedConfirmed.OperationType.Should().Be(proposed.OperationType);
        reloadedConfirmed.TreeMarking.Should().Be(proposed.TreeMarking);
        reloadedConfirmed.EstimatedTotalFellingVolume.Should().Be(proposed.EstimatedTotalFellingVolume);
        reloadedConfirmed.IsRestocking.Should().Be(proposed.IsRestocking ?? (proposed.ProposedRestockingDetails?.Any() == true));
        reloadedConfirmed.NoRestockingReason.Should().Be(proposed.NoRestockingReason);
        reloadedConfirmed.ConfirmedFellingSpecies.Select(s => s.Species)
            .Should().BeEquivalentTo(proposed.FellingSpecies?.Select(s => s.Species) ?? Enumerable.Empty<string>());
        reloadedConfirmed.ProposedFellingDetailId.Should().Be(proposed.Id);
        reloadedConfirmed.SubmittedFlaPropertyCompartmentId.Should().Be(reloadedCompartment.Id);
        if (proposed.ProposedRestockingDetails != null)
        {
            reloadedConfirmed.ConfirmedRestockingDetails.Count.Should().Be(proposed.ProposedRestockingDetails.Count);
            foreach (var (confirmedRestock, proposedRestock) in reloadedConfirmed.ConfirmedRestockingDetails.Zip(proposed.ProposedRestockingDetails))
            {
                confirmedRestock.Area.Should().Be(proposedRestock.Area);
                confirmedRestock.PercentageOfRestockArea.Should().Be(proposedRestock.PercentageOfRestockArea);
                confirmedRestock.RestockingDensity.Should().Be(proposedRestock.RestockingDensity);
                confirmedRestock.NumberOfTrees.Should().Be(proposedRestock.NumberOfTrees);
                confirmedRestock.RestockingProposal.Should().Be(proposedRestock.RestockingProposal);
                confirmedRestock.ConfirmedRestockingSpecies.Select(s => s.Species)
                    .Should().BeEquivalentTo(proposedRestock.RestockingSpecies?.Select(s => s.Species) ?? Enumerable.Empty<string>());
                confirmedRestock.SubmittedFlaPropertyCompartmentId.Should().Be(reloadedCompartment.Id);
                confirmedRestock.ProposedRestockingDetailId.Should().Be(proposedRestock.Id);
            }
        }
        else
        {
            reloadedConfirmed.ConfirmedRestockingDetails.Should().BeEmpty();
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
        result.IsSuccess.Should().BeTrue();
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

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Unable to find proposed felling detail");
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

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Unable to find compartment for proposed felling detail");
    }
}