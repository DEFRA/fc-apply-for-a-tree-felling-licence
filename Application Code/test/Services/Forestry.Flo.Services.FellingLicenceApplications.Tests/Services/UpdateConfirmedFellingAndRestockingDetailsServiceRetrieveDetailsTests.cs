using CSharpFunctionalExtensions;
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
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services;

public class UpdateConfirmedFellingAndRestockingDetailsServiceRetrieveDetailsTests
{
    private readonly Mock<IFellingLicenceApplicationInternalRepository> _fellingLicenceApplicationRepository = null!;
    private readonly Mock<IFellingLicenceApplicationExternalRepository> _externalRepository = null!;
    private readonly Mock<IAuditService<UpdateConfirmedFellingAndRestockingDetailsService>> _audit = new();

    public UpdateConfirmedFellingAndRestockingDetailsServiceRetrieveDetailsTests()
    {
        _fellingLicenceApplicationRepository = new Mock<IFellingLicenceApplicationInternalRepository>();
        _externalRepository = new Mock<IFellingLicenceApplicationExternalRepository>();
    }

    protected UpdateConfirmedFellingAndRestockingDetailsService CreateSut()
    {
        _fellingLicenceApplicationRepository.Reset();
        _externalRepository.Reset();

        return new UpdateConfirmedFellingAndRestockingDetailsService(
            _fellingLicenceApplicationRepository.Object,
            _externalRepository.Object,
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

        Assert.True(result.IsFailure);

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
        Assert.Empty(result);
    }

    [Fact]
    public void GetAmendedFellingDetailProperties_AreaChanged_ReturnsArea()
    {
        var service = CreateSut();
        var proposed = new ProposedFellingDetail { AreaToBeFelled = 2.0 };
        var confirmed = new ConfirmedFellingDetail { AreaToBeFelled = 1.0 };
        var result = service.GetAmendedFellingDetailProperties(proposed, confirmed);
        Assert.Contains(nameof(proposed.AreaToBeFelled), result.Keys);
        Assert.Equal("2", result[nameof(proposed.AreaToBeFelled)]);
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
        Assert.Contains(nameof(proposed.FellingSpecies), result.Keys);
        Assert.Contains("SP2", result[nameof(proposed.FellingSpecies)]);
    }

    [Fact]
    public void GetAmendedFellingDetailProperties_RestockingChanged_ReturnsRestocking()
    {
        var service = CreateSut();
        var proposed = new ProposedRestockingDetail { Area = 1.0 };
        var confirmed = new ConfirmedRestockingDetail { Area = 2.0 };
        var result = service.GetAmendedRestockingProperties(proposed, confirmed, new Dictionary<Guid, string?>());
        Assert.Contains(nameof(proposed.Area), result.Keys);
        Assert.Equal("1", result[nameof(proposed.Area)]);
    }

    [Theory, AutoMoqData]
    public async Task ConfirmedDetailsCorrectlyRetrieved_WithCompartmentOnlyUsedForRestocking(
        FellingLicenceApplication fla)
    {
        // Arrange: Set up a compartment only used for restocking
        var restockingCompartment = new SubmittedFlaPropertyCompartment
        {
            Id = Guid.NewGuid(),
            CompartmentId = Guid.NewGuid(),
            CompartmentNumber = "R1",
            PropertyProfileId = fla.SubmittedFlaPropertyDetail.PropertyProfileId,
            SubmittedFlaPropertyDetail = fla.SubmittedFlaPropertyDetail,
            ConfirmedFellingDetails = []
        };


        var fellingCompartment = new SubmittedFlaPropertyCompartment
        {
            Id = Guid.NewGuid(),
            CompartmentId = Guid.NewGuid(),
            CompartmentNumber = "R2",
            PropertyProfileId = fla.SubmittedFlaPropertyDetail.PropertyProfileId,
            SubmittedFlaPropertyDetail = fla.SubmittedFlaPropertyDetail,
            ConfirmedFellingDetails = [],
        };


        // Add a ConfirmedFellingDetail with IsRestocking = true, but no felling details
        var confirmedFellingDetail = new ConfirmedFellingDetail
        {
            SubmittedFlaPropertyCompartmentId = fellingCompartment.Id,
            SubmittedFlaPropertyCompartment = restockingCompartment,
            IsRestocking = true,
            ConfirmedFellingSpecies = [],
            ConfirmedRestockingDetails = new List<ConfirmedRestockingDetail>
            {
                new ConfirmedRestockingDetail
                {
                    SubmittedFlaPropertyCompartmentId = restockingCompartment.Id,
                    RestockingDensity = 2.5
                }
            }
        };
        restockingCompartment.ConfirmedFellingDetails.Add(confirmedFellingDetail);

        // Clear any existing compartments and add only the restocking compartment
        fla.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments = new List<SubmittedFlaPropertyCompartment>
        {
            restockingCompartment,
            fellingCompartment
        };

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

        // Act
        var (isSuccess, _, result) = await sut.RetrieveConfirmedFellingAndRestockingDetailModelAsync(fla.Id, CancellationToken.None);

        // Assert
        Assert.True(isSuccess);

        var modelValue = result.ConfirmedFellingAndRestockingDetailModels.First();
        Assert.Equal(restockingCompartment.CompartmentId, modelValue.CompartmentId);
        Assert.Equal(restockingCompartment.CompartmentNumber, modelValue.CompartmentNumber);

        var modelFellingDetail = modelValue.ConfirmedFellingDetailModels.First();
        Assert.True(modelFellingDetail.IsRestocking);

        var modelRestockingDetails = modelFellingDetail.ConfirmedRestockingDetailModels.First();
        Assert.Equal(2.5, modelRestockingDetails.RestockingDensity);

        Assert.Equal(result.ConfirmedFellingAndRestockingComplete,
            fla.WoodlandOfficerReview!.ConfirmedFellingAndRestockingComplete);

        Assert.Contains(fellingCompartment, result.SubmittedFlaPropertyCompartments);
        Assert.Contains(restockingCompartment, result.SubmittedFlaPropertyCompartments);

        _fellingLicenceApplicationRepository.Verify(v => v.GetAsync(fla.Id, CancellationToken.None), Times.Once);
        _fellingLicenceApplicationRepository.Verify(v => v.GetWoodlandOfficerReviewAsync(fla.Id, CancellationToken.None), Times.Once);
    }
}