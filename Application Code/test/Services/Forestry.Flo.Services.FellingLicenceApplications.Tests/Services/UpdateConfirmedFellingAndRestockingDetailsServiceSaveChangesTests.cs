using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Newtonsoft.Json;
using Xunit;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services;

public class UpdateConfirmedFellingAndRestockingDetailsServiceSaveChangesTests
{
    private IFellingLicenceApplicationInternalRepository _fellingLicenceApplicationRepository = null!;
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IAuditService<UpdateConfirmedFellingAndRestockingDetailsService>> _audit = new();
    private FellingLicenceApplicationsContext? _fellingLicenceApplicationsContext;
    private const string Species1 = "AR";
    private const string Species2 = "PAR";
    private const string Species3 = "AH";


    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    protected UpdateConfirmedFellingAndRestockingDetailsService CreateSut()
    {
        _unitOfWork.Reset();

        _fellingLicenceApplicationsContext = TestFellingLicenceApplicationsDatabaseFactory.CreateDefaultTestContext();
        _fellingLicenceApplicationRepository = new InternalUserContextFlaRepository(_fellingLicenceApplicationsContext);

        return new UpdateConfirmedFellingAndRestockingDetailsService(
            _fellingLicenceApplicationRepository,
            new NullLogger<UpdateConfirmedFellingAndRestockingDetailsService>(),
            _audit.Object,
            new RequestContext("test", new RequestUserModel(new ClaimsPrincipal()))
        );
    }

    private static FellingLicenceApplication ConstructFellingLicenceApplication(
        bool emptyFellRestock,
        Guid userId)
    {
        var compartmentId = Guid.NewGuid();
        var propertyProfileId = Guid.NewGuid();
        var submittedFlaPropertyCompartmentId = Guid.NewGuid();

        var fla = new FellingLicenceApplication()
        {
            ApplicationReference = "test_ref",
            CreatedById = Guid.NewGuid(),
            WoodlandOwnerId = Guid.NewGuid(),
        };

        fla.AssigneeHistories.Add(new AssigneeHistory
        {
            AssignedUserId = userId,
            FellingLicenceApplication = fla,
            FellingLicenceApplicationId = fla.Id,
            Role = AssignedUserRole.WoodlandOfficer,
            TimestampAssigned = DateTime.Now
        });

        fla.StatusHistories.Add(new StatusHistory
        {
            Created = DateTime.Now,
            FellingLicenceApplication = fla,
            FellingLicenceApplicationId = fla.Id,
            Status = FellingLicenceStatus.WoodlandOfficerReview
        });

        fla.SubmittedFlaPropertyDetail = new SubmittedFlaPropertyDetail
        {
            FellingLicenceApplication = fla,
            FellingLicenceApplicationId = fla.Id,
            HasWoodlandManagementPlan = true,
            Id = Guid.NewGuid(),
            Name = String.Empty,
            PropertyProfileId = propertyProfileId,
            SubmittedFlaPropertyCompartments = new List<SubmittedFlaPropertyCompartment>()
            {
                new()
                {
                    CompartmentId = compartmentId,
                    CompartmentNumber = "CompartmentNumber",
                    ConfirmedFellingDetails = emptyFellRestock
                        ? new List<ConfirmedFellingDetail>()
                        : new List<ConfirmedFellingDetail>(){
                            new() {
                                SubmittedFlaPropertyCompartmentId = submittedFlaPropertyCompartmentId,
                                AreaToBeFelled = 66,
                                ConfirmedFellingSpecies = new List<ConfirmedFellingSpecies>(),
                                IsPartOfTreePreservationOrder = false,
                                IsWithinConservationArea = true,
                                OperationType = FellingOperationType.ClearFelling,
                                ConfirmedRestockingDetails = emptyFellRestock
                                    ? new List<ConfirmedRestockingDetail>()
                                    : new List<ConfirmedRestockingDetail>() {
                                        new ConfirmedRestockingDetail() {
                                            SubmittedFlaPropertyCompartmentId= submittedFlaPropertyCompartmentId,
                                            Area = 52,
                                            ConfirmedRestockingSpecies = new List<ConfirmedRestockingSpecies>(),
                                            PercentageOfRestockArea = 12,
                                            PercentNaturalRegeneration = 47,
                                            PercentOpenSpace = 83,
                                            RestockingDensity = 45,
                                            RestockingProposal = TypeOfProposal.RestockWithIndividualTrees,
                                        }
                                    },
                            }},
                    PropertyProfileId = propertyProfileId
                }
            }
        };

        return fla;
    }

    [Fact]
    public async Task SaveChangesToConfirmedFellingDetailsAsync_AmendsDetails_WhenAllDataRetrieved()
    {
        var sut = CreateSut();
        var userId = Guid.NewGuid();
        var fla = ConstructFellingLicenceApplication(false, userId);

        _fellingLicenceApplicationsContext!.Add(fla);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var detailModel = ConstructIndividualConfirmedFellingAndRestockingDetailModels(fla, 3);

        detailModel.ConfirmedFellingDetailModel.ConfirmedFellingDetailsId =
            fla.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!.First().ConfirmedFellingDetails.First().Id;

        var result = await sut.SaveChangesToConfirmedFellingDetailsAsync(
            fla.Id,
            userId,
            detailModel,
            [],
            CancellationToken.None);

        var storedFla = await _fellingLicenceApplicationsContext.FellingLicenceApplications
            .Include(x => x.SubmittedFlaPropertyDetail)
            .ThenInclude(submittedFlaPropertyDetail => submittedFlaPropertyDetail.SubmittedFlaPropertyCompartments!)
            .ThenInclude(submittedFlaPropertyCompartment => submittedFlaPropertyCompartment.ConfirmedFellingDetails)
            .ThenInclude(confirmedFellingDetail => confirmedFellingDetail.ConfirmedFellingSpecies)
            .FirstOrDefaultAsync(x => x.Id == fla.Id);
        Assert.True(result.IsSuccess);
        Assert.NotNull(storedFla);

        var storedCompartment = storedFla!.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!
            .First(x => x.CompartmentId == detailModel.CompartmentId);

        foreach (var updatedFellingDetail in storedCompartment.ConfirmedFellingDetails)
        {
            var newFelling = detailModel.ConfirmedFellingDetailModel;
            Assert.NotNull(newFelling);
            Assert.Empty(updatedFellingDetail.ConfirmedFellingSpecies);
            Assert.Equal(newFelling?.AreaToBeFelled, updatedFellingDetail.AreaToBeFelled);
            Assert.Equal(newFelling?.IsPartOfTreePreservationOrder, updatedFellingDetail.IsPartOfTreePreservationOrder);
            Assert.Equal(newFelling?.IsWithinConservationArea, updatedFellingDetail.IsWithinConservationArea);
            Assert.Equal(newFelling?.NumberOfTrees, updatedFellingDetail.NumberOfTrees);
            Assert.Equal(newFelling?.TreeMarking, updatedFellingDetail.TreeMarking);
            Assert.Equal(newFelling?.OperationType, updatedFellingDetail.OperationType);
            Assert.Equal(newFelling?.EstimatedTotalFellingVolume, updatedFellingDetail.EstimatedTotalFellingVolume);
            Assert.Equal(newFelling?.IsRestocking, updatedFellingDetail.IsRestocking);
            Assert.Equal(newFelling?.NoRestockingReason, updatedFellingDetail.NoRestockingReason);
        }
    }

    [Fact]
    public async Task SaveChangesToConfirmedFellingDetailsAsync_ClearsRestocking_WhenNotRestocking()
    {
        var sut = CreateSut();
        var userId = Guid.NewGuid();
        var fla = ConstructFellingLicenceApplication(false, userId);

        _fellingLicenceApplicationsContext!.Add(fla);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var detailModel = ConstructIndividualConfirmedFellingAndRestockingDetailModels(fla, 3);

        detailModel.ConfirmedFellingDetailModel.ConfirmedFellingDetailsId =
            fla.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!.First().ConfirmedFellingDetails.First().Id;

        detailModel.ConfirmedFellingDetailModel.IsRestocking = false;

        var result = await sut.SaveChangesToConfirmedFellingDetailsAsync(
            fla.Id,
            userId,
            detailModel,
            [],
            CancellationToken.None);

        var storedFla = await _fellingLicenceApplicationsContext.FellingLicenceApplications
            .Include(x => x.SubmittedFlaPropertyDetail)
            .ThenInclude(submittedFlaPropertyDetail => submittedFlaPropertyDetail.SubmittedFlaPropertyCompartments!)
            .ThenInclude(submittedFlaPropertyCompartment => submittedFlaPropertyCompartment.ConfirmedFellingDetails)
            .ThenInclude(confirmedFellingDetail => confirmedFellingDetail.ConfirmedFellingSpecies)
            .Include(fellingLicenceApplication => fellingLicenceApplication.SubmittedFlaPropertyDetail!)
            .ThenInclude(submittedFlaPropertyDetail => submittedFlaPropertyDetail.SubmittedFlaPropertyCompartments!)
            .ThenInclude(submittedFlaPropertyCompartment => submittedFlaPropertyCompartment.ConfirmedFellingDetails)
            .ThenInclude(confirmedFellingDetail => confirmedFellingDetail.ConfirmedRestockingDetails)
            .FirstOrDefaultAsync(x => x.Id == fla.Id);
        Assert.True(result.IsSuccess);
        Assert.NotNull(storedFla);

        var storedCompartment = storedFla!.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!
            .First(x => x.CompartmentId == detailModel.CompartmentId);

        foreach (var updatedFellingDetail in storedCompartment.ConfirmedFellingDetails)
        {
            var newFelling = detailModel.ConfirmedFellingDetailModel;
            Assert.NotNull(newFelling);
            Assert.Empty(updatedFellingDetail.ConfirmedFellingSpecies);
            Assert.Equal(newFelling?.AreaToBeFelled, updatedFellingDetail.AreaToBeFelled);
            Assert.Equal(newFelling?.IsPartOfTreePreservationOrder, updatedFellingDetail.IsPartOfTreePreservationOrder);
            Assert.Equal(newFelling?.IsWithinConservationArea, updatedFellingDetail.IsWithinConservationArea);
            Assert.Equal(newFelling?.NumberOfTrees, updatedFellingDetail.NumberOfTrees);
            Assert.Equal(newFelling?.TreeMarking, updatedFellingDetail.TreeMarking);
            Assert.Equal(newFelling?.OperationType, updatedFellingDetail.OperationType);
            Assert.Equal(newFelling?.EstimatedTotalFellingVolume, updatedFellingDetail.EstimatedTotalFellingVolume);
            Assert.Equal(newFelling?.IsRestocking, updatedFellingDetail.IsRestocking);
            Assert.Equal(newFelling?.NoRestockingReason, updatedFellingDetail.NoRestockingReason);
            Assert.Empty(updatedFellingDetail.ConfirmedRestockingDetails);
        }
    }

    [Theory]
    [InlineData(3, Species1, Species2, Species3)]
    [InlineData(1, Species1)]
    [InlineData(2, Species2, Species3)]
    [InlineData(0)]
    public async Task SaveChangesToConfirmedFellingDetailsAsync_UpdatesConfirmedFellingSpecies(int expectedCount, params string[] speciesList)
    {
        var sut = CreateSut();
        var userId = Guid.NewGuid();
        var fla = ConstructFellingLicenceApplication(false, userId);

        var fd = fla.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!.First().ConfirmedFellingDetails.First();

        fd.ConfirmedFellingSpecies.Clear();
        fd.ConfirmedFellingSpecies.Add(new ConfirmedFellingSpecies
        {
            ConfirmedFellingDetailId = fd.Id,
            ConfirmedFellingDetail = fd,
            Species = Species1
        });

        _fellingLicenceApplicationsContext!.Add(fla);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var detailModel = ConstructIndividualConfirmedFellingAndRestockingDetailModels(fla, 0);

        var speciesModel = new Dictionary<string, SpeciesModel>();
        foreach (var species in speciesList)
        {
            speciesModel.Add(species, new SpeciesModel
            {
                Id = Guid.NewGuid(),
                Species = species,
                SpeciesName = species + "Name",
            });
        }
        detailModel.ConfirmedFellingDetailModel.ConfirmedFellingDetailsId = fd.Id;

        var result = await sut.SaveChangesToConfirmedFellingDetailsAsync(
            fla.Id,
            userId,
            detailModel,
            speciesModel,
            CancellationToken.None);

        var storedFla = await _fellingLicenceApplicationsContext.FellingLicenceApplications
            .Include(x => x.SubmittedFlaPropertyDetail)
            .ThenInclude(submittedFlaPropertyDetail => submittedFlaPropertyDetail.SubmittedFlaPropertyCompartments!)
            .ThenInclude(submittedFlaPropertyCompartment => submittedFlaPropertyCompartment.ConfirmedFellingDetails)
            .ThenInclude(confirmedFellingDetail => confirmedFellingDetail.ConfirmedFellingSpecies)
            .FirstOrDefaultAsync(x => x.Id == fla.Id);
        Assert.True(result.IsSuccess);
        Assert.NotNull(storedFla);

        var storedFd = storedFla!.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!
            .First(x => x.CompartmentId == detailModel.CompartmentId).ConfirmedFellingDetails
            .First(x => x.Id == fd.Id);

        Assert.Equal(expectedCount, storedFd.ConfirmedFellingSpecies.Count);

        foreach (var species in speciesList)
        {
            storedFd.ConfirmedFellingSpecies.Should().ContainSingle(x => x.Species == species);
        }
    }

    [Fact]
    public async Task SaveChangesToConfirmedFellingDetailsAsync_ReturnsFailure_WhenCompartmentIsNotFound()
    {
        var sut = CreateSut();
        var userId = Guid.NewGuid();
        var fla = ConstructFellingLicenceApplication(false, userId);
        var detailModel = ConstructIndividualConfirmedFellingAndRestockingDetailModels(fla, 2);
        var randomId = Guid.NewGuid();
        detailModel.CompartmentId = randomId;

        _fellingLicenceApplicationsContext!.Add(fla);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        detailModel.ConfirmedFellingDetailModel.ConfirmedFellingDetailsId =
            fla.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!.First().ConfirmedFellingDetails.First().Id;

        var result = await sut.SaveChangesToConfirmedFellingDetailsAsync(
            fla.Id,
            userId,
            detailModel,
            [],
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task SaveChangesToConfirmedFellingDetailsAsync_ReturnsFailure_WhenUserIsNotPermitted()
    {
        var sut = CreateSut();
        var userId = Guid.NewGuid();
        var fla = ConstructFellingLicenceApplication(false, userId);
        var detailModel = ConstructIndividualConfirmedFellingAndRestockingDetailModels(fla, 2);

        _fellingLicenceApplicationsContext!.Add(fla);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        detailModel.ConfirmedFellingDetailModel.ConfirmedFellingDetailsId =
            fla.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!.First().ConfirmedFellingDetails.First().Id;

        var userId2 = Guid.NewGuid();

        var result = await sut.SaveChangesToConfirmedFellingDetailsAsync(
            fla.Id,
            userId2,
            detailModel,
            [],
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Theory, AutoMoqData]
    public async Task SaveChangesToConfirmedFellingDetailsAsync_ReturnsFailure_WhenApplicationNotFound(
        FellingLicenceApplication fla, IndividualFellingRestockingDetailModel detailModel)
    {
        var userId = Guid.NewGuid();
        var sut = CreateSut();

        var result = await sut.SaveChangesToConfirmedFellingDetailsAsync(
            fla.Id,
            userId,
            detailModel,
            [],
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }



    private static List<FellingAndRestockingDetailModel> ConstructConfirmedFellingAndRestockingDetailModels(
        FellingLicenceApplication fla, int speciesCount)
    {
        var detailModels = new List<FellingAndRestockingDetailModel>();

        for (var i = 0; i < fla.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!.Count; i++)
        {

            var fellingSpecies = new List<ConfirmedFellingSpecies>();
            var restockingSpecies = new List<ConfirmedRestockingSpecies>();
            for (int j = 0; j < speciesCount; j++)
            {
                fellingSpecies.Add(new ConfirmedFellingSpecies
                {
                    Species = "species" + j
                });

                restockingSpecies.Add(new ConfirmedRestockingSpecies
                {
                    Percentage = 24,
                    Species = "species" + j
                });
            }

            var felling = fla.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments![i].ConfirmedFellingDetails!.FirstOrDefault();
            var restocking = felling?.ConfirmedRestockingDetails.FirstOrDefault();
            var item = new ConfirmedFellingDetailModel
            {
                ConfirmedFellingDetailsId = felling?.Id ?? new Guid(),
                AreaToBeFelled = 98,
                ConfirmedFellingSpecies = fellingSpecies,
                IsPartOfTreePreservationOrder = false,
                IsWithinConservationArea = true,
                NumberOfTrees = 163,
                OperationType = FellingOperationType.Thinning,
                IsTreeMarkingUsed = true,
                TreeMarking = "NewMarking",
                EstimatedTotalFellingVolume = 97,
                ConfirmedRestockingDetailModels = new List<ConfirmedRestockingDetailModel>
                        {
                            new ConfirmedRestockingDetailModel
                            {
                                ConfirmedRestockingDetailsId = restocking?.Id ?? new Guid(),
                                Area = 31,
                                ConfirmedRestockingSpecies = restockingSpecies,
                                PercentageOfRestockArea = 12,
                                PercentNaturalRegeneration = 99,
                                PercentOpenSpace = 44,
                                RestockingDensity = 11,
                                RestockingProposal = TypeOfProposal.ReplantTheFelledArea
                            }
                        },
                IsRestocking = true,
            };

            detailModels.Add(new FellingAndRestockingDetailModel()
            {
                ConfirmedFellingDetailModels = new List<ConfirmedFellingDetailModel>
                {
                    item
                },
                CompartmentId = fla.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments[i].CompartmentId,
            });
        }

        return detailModels;
    }



    private static IndividualFellingRestockingDetailModel ConstructIndividualConfirmedFellingAndRestockingDetailModels(
        FellingLicenceApplication fla, int speciesCount)
    {
        var fellingSpecies = new List<ConfirmedFellingSpecies>();
        var restockingSpecies = new List<ConfirmedRestockingSpecies>();
        for (int j = 0; j < speciesCount; j++)
        {
            fellingSpecies.Add(new ConfirmedFellingSpecies
            {
                Species = "species" + j
            });

            restockingSpecies.Add(new ConfirmedRestockingSpecies
            {
                Percentage = 24,
                Species = "species" + j
            });
        }

        var submittedPropertyCompartment = fla.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!.First();
        var felling = submittedPropertyCompartment.ConfirmedFellingDetails!.FirstOrDefault();
        Assert.NotNull(felling);
        var restocking = felling?.ConfirmedRestockingDetails.FirstOrDefault();

        return new IndividualFellingRestockingDetailModel
        {
            CompartmentId = submittedPropertyCompartment.CompartmentId,
            ConfirmedFellingDetailModel = new ConfirmedFellingDetailModel
            {
                ConfirmedFellingDetailsId = felling?.Id ?? Guid.NewGuid(),
                AreaToBeFelled = 98,
                ConfirmedFellingSpecies = fellingSpecies,
                IsPartOfTreePreservationOrder = false,
                IsWithinConservationArea = true,
                NumberOfTrees = 163,
                OperationType = FellingOperationType.Thinning,
                TreeMarking = "NewMarking",
                EstimatedTotalFellingVolume = 97,
                ConfirmedRestockingDetailModels = new List<ConfirmedRestockingDetailModel>
                {
                    new()
                    {
                        ConfirmedRestockingDetailsId = restocking?.Id ?? Guid.Empty,
                        Area = 31,
                        ConfirmedRestockingSpecies = restockingSpecies,
                        PercentageOfRestockArea = 12,
                        PercentNaturalRegeneration = 99,
                        PercentOpenSpace = 44,
                        RestockingDensity = 11,
                        RestockingProposal = TypeOfProposal.ReplantTheFelledArea,
                        CompartmentId = submittedPropertyCompartment.CompartmentId
                    }
                },
                IsRestocking = true
            }
        };
    }

    [Fact]
    public async Task AddNewConfirmedFellingDetailsAsync_Succeeds_WhenAllDataIsValid()
    {
        // Arrange
        var sut = CreateSut();
        var userId = Guid.NewGuid();
        var fla = ConstructFellingLicenceApplication(true, userId);
        var compartment = fla.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!.First();
        _fellingLicenceApplicationsContext!.Add(fla);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var newFellingDetailModel = new NewConfirmedFellingDetailModel
        {
            AreaToBeFelled = 10,
            IsPartOfTreePreservationOrder = false,
            TreePreservationOrderReference = null,
            IsWithinConservationArea = false,
            ConservationAreaReference = null,
            NumberOfTrees = 5,
            OperationType = FellingOperationType.ClearFelling,
            TreeMarking = "Marking",
            EstimatedTotalFellingVolume = 100,
            IsRestocking = true,
            NoRestockingReason = null
        };
        var newDetail = new NewConfirmedFellingDetailWithCompartmentId(compartment.CompartmentId, newFellingDetailModel);

        var speciesModel = new Dictionary<string, SpeciesModel>
        {
            { "SP1", new SpeciesModel { Id = Guid.NewGuid(), Species = "SP1", SpeciesName = "Species 1" } },
            { "SP2", new SpeciesModel { Id = Guid.NewGuid(), Species = "SP2", SpeciesName = "Species 2" } }
        };

        // Act
        var result = await sut.AddNewConfirmedFellingDetailsAsync(
            fla.Id,
            userId,
            newDetail,
            speciesModel,
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);

        var storedFla = await _fellingLicenceApplicationsContext.FellingLicenceApplications
            .Include(x => x.SubmittedFlaPropertyDetail)
            .ThenInclude(d => d.SubmittedFlaPropertyCompartments!)
            .ThenInclude(c => c.ConfirmedFellingDetails)
            .ThenInclude(fd => fd.ConfirmedFellingSpecies)
            .FirstOrDefaultAsync(x => x.Id == fla.Id);

        var storedCompartment = storedFla!.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!
            .First(x => x.CompartmentId == compartment.CompartmentId);

        var addedDetail = storedCompartment.ConfirmedFellingDetails.Last();
        Assert.Equal(newFellingDetailModel.AreaToBeFelled, addedDetail.AreaToBeFelled);
        Assert.Equal(newFellingDetailModel.OperationType, addedDetail.OperationType);
        Assert.Equal(2, addedDetail.ConfirmedFellingSpecies.Count);
        Assert.All(addedDetail.ConfirmedFellingSpecies, s => speciesModel.Keys.Contains(s.Species));
    }

    [Fact]
    public async Task AddNewConfirmedFellingDetailsAsync_ReturnsFailure_WhenApplicationNotFound()
    {
        // Arrange
        var sut = CreateSut();
        var userId = Guid.NewGuid();
        var newFellingDetailModel = new NewConfirmedFellingDetailModel
        {
            AreaToBeFelled = 10,
            IsPartOfTreePreservationOrder = false,
            TreePreservationOrderReference = null,
            IsWithinConservationArea = false,
            ConservationAreaReference = null,
            NumberOfTrees = 5,
            OperationType = FellingOperationType.ClearFelling,
            TreeMarking = "Marking",
            EstimatedTotalFellingVolume = 100,
            IsRestocking = true,
            NoRestockingReason = null
        };
        var newDetail = new NewConfirmedFellingDetailWithCompartmentId(Guid.NewGuid(), newFellingDetailModel);

        // Act
        var result = await sut.AddNewConfirmedFellingDetailsAsync(
            Guid.NewGuid(),
            userId,
            newDetail,
            new(),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task AddNewConfirmedFellingDetailsAsync_ReturnsFailure_WhenUserNotPermitted()
    {
        // Arrange
        var sut = CreateSut();
        var userId = Guid.NewGuid();
        var fla = ConstructFellingLicenceApplication(true, userId);
        var compartment = fla.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!.First();
        _fellingLicenceApplicationsContext!.Add(fla);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var newFellingDetailModel = new NewConfirmedFellingDetailModel
        {
            AreaToBeFelled = 10,
            IsPartOfTreePreservationOrder = false,
            TreePreservationOrderReference = null,
            IsWithinConservationArea = false,
            ConservationAreaReference = null,
            NumberOfTrees = 5,
            OperationType = FellingOperationType.ClearFelling,
            TreeMarking = "Marking",
            EstimatedTotalFellingVolume = 100,
            IsRestocking = true,
            NoRestockingReason = null
        };
        var newDetail = new NewConfirmedFellingDetailWithCompartmentId(compartment.CompartmentId, newFellingDetailModel);

        var otherUserId = Guid.NewGuid();

        // Act
        var result = await sut.AddNewConfirmedFellingDetailsAsync(
            fla.Id,
            otherUserId,
            newDetail,
            new(),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task AddNewConfirmedFellingDetailsAsync_ReturnsFailure_WhenCompartmentNotFound()
    {
        // Arrange
        var sut = CreateSut();
        var userId = Guid.NewGuid();
        var fla = ConstructFellingLicenceApplication(true, userId);
        _fellingLicenceApplicationsContext!.Add(fla);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var newFellingDetailModel = new NewConfirmedFellingDetailModel
        {
            AreaToBeFelled = 10,
            IsPartOfTreePreservationOrder = false,
            TreePreservationOrderReference = null,
            IsWithinConservationArea = false,
            ConservationAreaReference = null,
            NumberOfTrees = 5,
            OperationType = FellingOperationType.ClearFelling,
            TreeMarking = "Marking",
            EstimatedTotalFellingVolume = 100,
            IsRestocking = true,
            NoRestockingReason = null
        };
        var newDetail = new NewConfirmedFellingDetailWithCompartmentId(Guid.NewGuid(), newFellingDetailModel);

        // Act
        var result = await sut.AddNewConfirmedFellingDetailsAsync(
            fla.Id,
            userId,
            newDetail,
            new(),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
    }
}