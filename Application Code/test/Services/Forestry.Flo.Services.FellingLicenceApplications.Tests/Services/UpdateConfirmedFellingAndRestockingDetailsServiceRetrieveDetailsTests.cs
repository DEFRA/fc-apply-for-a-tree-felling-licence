using CSharpFunctionalExtensions;
using FluentAssertions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using System.Collections.Generic;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services;

public class UpdateConfirmedFellingAndRestockingDetailsServiceRetrieveDetailsTests
{
    private Mock<IFellingLicenceApplicationInternalRepository> _fellingLicenceApplicationRepository = null!;
    private readonly Mock<IAuditService<UpdateConfirmedFellingAndRestockingDetailsService>> _audit = new();

    public UpdateConfirmedFellingAndRestockingDetailsServiceRetrieveDetailsTests()
    {
        _fellingLicenceApplicationRepository = new Mock<IFellingLicenceApplicationInternalRepository>();
    }

    protected UpdateConfirmedFellingAndRestockingDetailsService CreateSut()
    {
        _fellingLicenceApplicationRepository.Reset();

        return new UpdateConfirmedFellingAndRestockingDetailsService(
            _fellingLicenceApplicationRepository.Object,
            new NullLogger<UpdateConfirmedFellingAndRestockingDetailsService>(),
            _audit.Object,
            new RequestContext("test", new RequestUserModel(new ClaimsPrincipal()))
        );
    }


    [Theory, AutoMoqData]

    public async Task ConfirmedDetailsCorrectlyRetrieved_WhenAllDetailsCorrect(
        FellingLicenceApplication fla)
    {
        // Set up the felling licence application with necessary details
        foreach (var proposed in fla.LinkedPropertyProfile.ProposedFellingDetails)
        {
            proposed.LinkedPropertyProfile = fla.LinkedPropertyProfile;
            proposed.LinkedPropertyProfileId = fla.LinkedPropertyProfile.PropertyProfileId;
        }
        fla.SubmittedFlaPropertyDetail.PropertyProfileId = fla.LinkedPropertyProfile.PropertyProfileId;
        foreach (var compartment in fla.SubmittedFlaPropertyDetail?.SubmittedFlaPropertyCompartments)
        {
            foreach (var detail in compartment.ConfirmedFellingDetails)
            {
                detail.SubmittedFlaPropertyCompartment = compartment;
                detail.SubmittedFlaPropertyCompartment.SubmittedFlaPropertyDetail = fla.SubmittedFlaPropertyDetail;
            }
        }

        var sut = CreateSut();

        _fellingLicenceApplicationRepository.Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(fla);

        var woodlandOfficerReview = new WoodlandOfficerReview
        {
            ConfirmedFellingAndRestockingComplete =
                fla.WoodlandOfficerReview!.ConfirmedFellingAndRestockingComplete,
            FellingLicenceApplication = fla,
            FellingLicenceApplicationId = fla.Id
        };

        _fellingLicenceApplicationRepository.Setup(r =>
                r.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(woodlandOfficerReview);

        var (_, isFailure, result) = await sut.RetrieveConfirmedFellingAndRestockingDetailModelAsync(fla.Id, CancellationToken.None);

        Assert.True(!isFailure);

        Assert.Equal(fla.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!.Count, result.ConfirmedFellingAndRestockingDetailModels.Count);

        for (var i = 0; i < fla.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!.Count; i++)
        {
            var flaValue = fla.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments[i];
            var modelValue = result.ConfirmedFellingAndRestockingDetailModels[i];
            foreach (var confirmedFellingDetail in flaValue.ConfirmedFellingDetails)
            {
                var modelFellingDetail = modelValue.ConfirmedFellingDetailModels.Where(x => x.ConfirmedFellingDetailsId == confirmedFellingDetail.Id).FirstOrDefault();

                // compare compartment values to model

                Assert.Equal(flaValue.CompartmentId, modelValue.CompartmentId);
                Assert.Equal(flaValue.CompartmentNumber, modelValue.CompartmentNumber);

                // compare confirmed felling details to model
                Assert.Equal(confirmedFellingDetail.AreaToBeFelled, modelFellingDetail!.AreaToBeFelled);
                Assert.Equal(confirmedFellingDetail.IsPartOfTreePreservationOrder, modelFellingDetail.IsPartOfTreePreservationOrder);
                Assert.Equal(confirmedFellingDetail.IsWithinConservationArea, modelFellingDetail.IsWithinConservationArea);
                Assert.Equal(confirmedFellingDetail.NumberOfTrees, modelFellingDetail.NumberOfTrees);
                Assert.Equal(confirmedFellingDetail.OperationType, modelFellingDetail.OperationType);
                Assert.Equal(confirmedFellingDetail.EstimatedTotalFellingVolume, modelFellingDetail.EstimatedTotalFellingVolume);

                for (var j = 0; j < confirmedFellingDetail.ConfirmedFellingSpecies!.Count; j++)
                {
                    // compare confirmed felling species to model

                    Assert.NotNull(modelFellingDetail.ConfirmedFellingSpecies!.ToArray()[j]);
                    Assert.Equal(confirmedFellingDetail.ConfirmedFellingSpecies[j].Species, modelFellingDetail.ConfirmedFellingSpecies.ToArray()[j].Species);
                }

                // compare confirmed restocking details to model
                foreach (var confirmedRestockingDetail in confirmedFellingDetail.ConfirmedRestockingDetails)
                {
                    var modelRestockingDetails = modelFellingDetail.ConfirmedRestockingDetailModels.Where(x => x.ConfirmedRestockingDetailsId == confirmedRestockingDetail.Id).FirstOrDefault();

                    Assert.NotNull(modelRestockingDetails);
                    Assert.Equal(confirmedRestockingDetail.Area, modelRestockingDetails!.Area);
                    Assert.Equal(confirmedRestockingDetail.PercentOpenSpace, modelRestockingDetails.PercentOpenSpace);
                    Assert.Equal(confirmedRestockingDetail.RestockingProposal, modelRestockingDetails.RestockingProposal);

                    for (var j = 0; j < confirmedRestockingDetail.ConfirmedRestockingSpecies.Count; j++)
                    {
                        // compare confirmed felling species to model

                        Assert.NotNull(modelRestockingDetails.ConfirmedRestockingSpecies![j]);
                        Assert.Equal(confirmedRestockingDetail.ConfirmedRestockingSpecies[j].Percentage, modelRestockingDetails.ConfirmedRestockingSpecies[j].Percentage);
                        Assert.Equal(confirmedRestockingDetail.ConfirmedRestockingSpecies[j].Species, modelRestockingDetails.ConfirmedRestockingSpecies[j].Species);
                    }
                }
            }
        }

        Assert.Equal(result.ConfirmedFellingAndRestockingComplete,
            fla.WoodlandOfficerReview!.ConfirmedFellingAndRestockingComplete);

        _fellingLicenceApplicationRepository.Verify(v => v.GetAsync(fla.Id, CancellationToken.None), Times.Once);
        _fellingLicenceApplicationRepository.Verify(v => v.GetWoodlandOfficerReviewAsync(fla.Id, CancellationToken.None), Times.Once);
    }

    [Theory, AutoMoqData]

    public async Task ShouldReturnFailure_WhenFLANotFound(FellingLicenceApplication fla)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository.Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.None);

        var result = await sut.RetrieveConfirmedFellingAndRestockingDetailModelAsync(fla.Id, CancellationToken.None);

        result.IsFailure.Should().BeTrue();

        _fellingLicenceApplicationRepository.Verify(v => v.GetAsync(fla.Id, CancellationToken.None), Times.Once);
    }

    [Fact]
    public void GetAmendedFellingDetailProperties_NoChanges_ReturnsEmpty()
    {
        var service = CreateSut();
        var proposed = new ProposedFellingDetail
        {
            AreaToBeFelled = 1.0,
            IsPartOfTreePreservationOrder = true,
            TreePreservationOrderReference = "TPO123",
            IsWithinConservationArea = false,
            ConservationAreaReference = "CA456",
            IsRestocking = false,
            NumberOfTrees = 10,
            OperationType = FellingOperationType.ClearFelling,
            TreeMarking = "Red",
            EstimatedTotalFellingVolume = 100.0,
            FellingSpecies = new List<FellingSpecies> { new FellingSpecies { Species = "SP1" } },
            ProposedRestockingDetails = new List<ProposedRestockingDetail> { new ProposedRestockingDetail { Area = 1.0 } }
        };
        var confirmed = new ConfirmedFellingDetail
        {
            AreaToBeFelled = 1.0,
            IsPartOfTreePreservationOrder = true,
            TreePreservationOrderReference = "TPO123",
            IsWithinConservationArea = false,
            ConservationAreaReference = "CA456",
            IsRestocking = false,
            NumberOfTrees = 10,
            OperationType = FellingOperationType.ClearFelling,
            TreeMarking = "Red",
            EstimatedTotalFellingVolume = 100.0,
            ConfirmedFellingSpecies = new List<ConfirmedFellingSpecies> { new ConfirmedFellingSpecies { Species = "SP1" } },
            ConfirmedRestockingDetails = new List<ConfirmedRestockingDetail> { new ConfirmedRestockingDetail { Area = 1.0 } }
        };
        var result = service.GetAmendedFellingDetailProperties(proposed, confirmed);
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetAmendedFellingDetailProperties_AreaChanged_ReturnsArea()
    {
        var service = CreateSut();
        var proposed = new ProposedFellingDetail { AreaToBeFelled = 2.0 };
        var confirmed = new ConfirmedFellingDetail { AreaToBeFelled = 1.0 };
        var result = service.GetAmendedFellingDetailProperties(proposed, confirmed);
        result.Should().ContainKey(nameof(proposed.AreaToBeFelled));
        result[nameof(proposed.AreaToBeFelled)].Should().Be("2");
    }

    [Fact]
    public void GetAmendedFellingDetailProperties_SpeciesChanged_ReturnsSpecies()
    {
        var service = CreateSut();
        var proposed = new ProposedFellingDetail
        {
            FellingSpecies = new List<FellingSpecies>
            {
                new FellingSpecies { Species = "SP1" },
                new FellingSpecies { Species = "SP2" }
            }
        };
        var confirmed = new ConfirmedFellingDetail
        {
            ConfirmedFellingSpecies = new List<ConfirmedFellingSpecies>
            {
                new ConfirmedFellingSpecies { Species = "SP1" }
            }
        };
        var result = service.GetAmendedFellingDetailProperties(proposed, confirmed);
        result.Should().ContainKey(nameof(proposed.FellingSpecies));
        result[nameof(proposed.FellingSpecies)].Should().Contain("SP2");
    }

    [Fact]
    public void GetAmendedFellingDetailProperties_RestockingChanged_ReturnsRestocking()
    {
        var service = CreateSut();
        var proposed = new ProposedRestockingDetail { Area = 1.0 };
        var confirmed = new ConfirmedRestockingDetail { Area = 2.0 };
        var result = service.GetAmendedRestockingProperties(proposed, confirmed, new Dictionary<Guid, string?>());
        result.Should().ContainKey(nameof(proposed.Area));
        result[nameof(proposed.Area)].Should().Be("1");
    }
}