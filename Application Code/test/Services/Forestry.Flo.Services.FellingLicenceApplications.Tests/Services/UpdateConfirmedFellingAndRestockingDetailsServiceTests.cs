using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Extensions;
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services;

public partial class UpdateConfirmedFellingAndRestockingDetailsServiceTests
{
    private IFellingLicenceApplicationInternalRepository? _fellingLicenceApplicationRepository;
    private IFellingLicenceApplicationExternalRepository _fellingLicenceApplicationExternalRepository;
    private readonly Mock<IAuditService<UpdateConfirmedFellingAndRestockingDetailsService>> _audit = new();
    private FellingLicenceApplicationsContext? _fellingLicenceApplicationsContext;

    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    protected UpdateConfirmedFellingAndRestockingDetailsService CreateSut()
    {
        var referenceGenerator = new Mock<IApplicationReferenceHelper>();
        referenceGenerator.Setup(r => r.GenerateReferenceNumber(It.IsAny<FellingLicenceApplication>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>()))
            .Returns("test");

        var mockReferenceRepository = new Mock<IFellingLicenceApplicationReferenceRepository>();
        mockReferenceRepository
            .Setup(x => x.GetNextApplicationReferenceIdValueAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _fellingLicenceApplicationsContext = TestFellingLicenceApplicationsDatabaseFactory.CreateDefaultTestContext();
        _fellingLicenceApplicationRepository = new InternalUserContextFlaRepository(_fellingLicenceApplicationsContext);
        _fellingLicenceApplicationExternalRepository = new ExternalUserContextFlaRepository(
            _fellingLicenceApplicationsContext, referenceGenerator.Object, mockReferenceRepository.Object);
        _audit.Reset();

        return new UpdateConfirmedFellingAndRestockingDetailsService(
            _fellingLicenceApplicationRepository,
            _fellingLicenceApplicationExternalRepository,
            new NullLogger<UpdateConfirmedFellingAndRestockingDetailsService>(),
            _audit.Object,
            new RequestContext("test", new RequestUserModel(new ClaimsPrincipal()))
            );
    }
    
    [Fact]
    public async Task DeleteConfirmedFellingDetailAsync_RemovesConfirmedFellingDetailAndRestockingDetails()
    {
        // setup
        var userId = Guid.NewGuid();
        var sut = CreateSut();
        var fla = ConstructFellingLicenceApplication(true, userId);

        _fellingLicenceApplicationsContext!.Add(fla);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var compartment = fla.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!.First();
        var confirmedFellingDetail = compartment.ConfirmedFellingDetails.First();

        // Act
        var result = await sut.DeleteConfirmedFellingDetailAsync(
            fla.Id,
            confirmedFellingDetail.Id,
            userId,
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);

        var storedFla = await _fellingLicenceApplicationsContext.FellingLicenceApplications
            .Include(fellingLicenceApplication => fellingLicenceApplication.SubmittedFlaPropertyDetail!)
            .ThenInclude(submittedFlaPropertyDetail => submittedFlaPropertyDetail.SubmittedFlaPropertyCompartments!)
            .ThenInclude(submittedFlaPropertyCompartment => submittedFlaPropertyCompartment.ConfirmedFellingDetails)
            .ThenInclude(fellingDetail => fellingDetail.ConfirmedRestockingDetails)
            .FirstAsync(x => x.Id == fla.Id);
        var storedCompartment = storedFla!.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!.First();
        Assert.DoesNotContain(confirmedFellingDetail.Id, storedCompartment.ConfirmedFellingDetails.Select(x => x.Id));
        Assert.DoesNotContain(confirmedFellingDetail.Id, _fellingLicenceApplicationsContext.ConfirmedFellingDetails.Select(x => x.Id));

        foreach (var restockingDetail in confirmedFellingDetail.ConfirmedRestockingDetails)
        {
            Assert.DoesNotContain(restockingDetail.Id, _fellingLicenceApplicationsContext.ConfirmedRestockingDetails.Select(x => x.Id));

            foreach (var restockingSpecies in restockingDetail.ConfirmedRestockingSpecies)
            {
                Assert.DoesNotContain(restockingSpecies.Id, _fellingLicenceApplicationsContext.ConfirmedRestockingSpecies.Select(x => x.Id));
            }
        }

        Assert.DoesNotContain(confirmedFellingDetail.Id, _fellingLicenceApplicationsContext.ConfirmedFellingSpecies.Select(x => x.ConfirmedFellingDetailId));

        foreach (var restock in confirmedFellingDetail.ConfirmedRestockingDetails)
        {
            Assert.DoesNotContain(restock.Id, _fellingLicenceApplicationsContext.ConfirmedRestockingSpecies.Select(x => x.ConfirmedRestockingDetailsId));
        }
    }

    [Fact]
    public async Task DeleteConfirmedFellingDetailAsync_ReturnsFailure_WhenDetailNotFound()
    {
        // setup
        var userId = Guid.NewGuid();
        var sut = CreateSut();
        var fla = ConstructFellingLicenceApplication(true, userId);

        _fellingLicenceApplicationsContext!.Add(fla);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var nonExistentDetailId = Guid.NewGuid();

        // Act
        var result = await sut.DeleteConfirmedFellingDetailAsync(
            fla.Id,
            nonExistentDetailId,
            userId,
            CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task DeleteConfirmedFellingDetailAsync_ReturnsFailure_WhenUserIsNotAssigned()
    {
        // setup
        var userId = Guid.NewGuid();
        var fla = ConstructFellingLicenceApplication(true, userId);
        var sut = CreateSut();

        _fellingLicenceApplicationsContext!.Add(fla);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var compartment = fla.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!.First();
        var confirmedFellingDetail = compartment.ConfirmedFellingDetails.First();
        var otherUserId = Guid.NewGuid();

        // Act
        var result = await sut.DeleteConfirmedFellingDetailAsync(
            fla.Id,
            confirmedFellingDetail.Id,
            otherUserId,
            CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task DeleteConfirmedFellingDetailAsync_ReturnsFailure_WhenApplicationNotFound()
    {
        // setup
        var userId = Guid.NewGuid();
        var sut = CreateSut();
        var nonExistentAppId = Guid.NewGuid();
        var confirmedFellingDetailId = Guid.NewGuid();

        // Act
        var result = await sut.DeleteConfirmedFellingDetailAsync(
            nonExistentAppId,
            confirmedFellingDetailId,
            userId,
            CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task SaveChangesToConfirmedRestockingDetailsAsync_AmendsDetails_WhenAllDataRetrieved()
    {
        var sut = CreateSut();
        var userId = Guid.NewGuid();
        var fla = ConstructFellingLicenceApplication(true, userId);
        var compartment = fla.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!.First();
        var confirmedFellingDetail = compartment.ConfirmedFellingDetails.First();
        var confirmedRestockingDetail = confirmedFellingDetail.ConfirmedRestockingDetails.First();

        _fellingLicenceApplicationsContext!.Add(fla);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var restockingSpecies = new List<ConfirmedRestockingSpecies>
        {
            new ConfirmedRestockingSpecies { Species = "speciesA", Percentage = 50 },
            new ConfirmedRestockingSpecies { Species = "speciesB", Percentage = 50 }
        };

        var detailModel = new IndividualRestockingDetailModel
        {
            CompartmentId = compartment.CompartmentId,
            SubmittedFlaPropertyCompartmentId = compartment.Id,
            ConfirmedRestockingDetailModel = new ConfirmedRestockingDetailModel
            {
                ConfirmedRestockingDetailsId = confirmedRestockingDetail.Id,
                ConfirmedFellingDetailsId = confirmedFellingDetail.Id,
                Area = 123,
                RestockingProposal = TypeOfProposal.ReplantTheFelledArea,
                NumberOfTrees = 10,
                PercentOpenSpace = 5,
                PercentNaturalRegeneration = 10,
                PercentageOfRestockArea = 100,
                RestockingDensity = 200,
                ConfirmedRestockingSpecies = restockingSpecies
            }
        };

        var speciesModel = new Dictionary<string, SpeciesModel>
        {
            { "speciesA", new SpeciesModel { Id = Guid.NewGuid(), Species = "speciesA", Percentage = 50 } },
            { "speciesB", new SpeciesModel { Id = Guid.NewGuid(), Species = "speciesB", Percentage = 50 } }
        };

        var result = await sut.SaveChangesToConfirmedRestockingDetailsAsync(
            fla.Id,
            userId,
            detailModel,
            speciesModel,
            CancellationToken.None);

        var storedFla = await _fellingLicenceApplicationsContext.FellingLicenceApplications
            .Include(x => x.SubmittedFlaPropertyDetail)
            .ThenInclude(d => d.SubmittedFlaPropertyCompartments!)
            .ThenInclude(c => c.ConfirmedFellingDetails)
            .ThenInclude(fd => fd.ConfirmedRestockingDetails)
            .ThenInclude(rd => rd.ConfirmedRestockingSpecies)
            .FirstOrDefaultAsync(x => x.Id == fla.Id);

        Assert.True(result.IsSuccess);
        Assert.NotNull(storedFla);
        var storedCompartment = storedFla!.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!.First(x => x.CompartmentId == compartment.CompartmentId);
        var storedFellingDetail = storedCompartment.ConfirmedFellingDetails.First(x => x.Id == confirmedFellingDetail.Id);
        var storedRestockingDetail = storedFellingDetail.ConfirmedRestockingDetails.First(x => x.Id == confirmedRestockingDetail.Id);
        Assert.Equal(123, storedRestockingDetail.Area);
        Assert.Equal(TypeOfProposal.ReplantTheFelledArea, storedRestockingDetail.RestockingProposal);
        Assert.Equal(10, storedRestockingDetail.NumberOfTrees);
        Assert.Equal(5, storedRestockingDetail.PercentOpenSpace);
        Assert.Equal(10, storedRestockingDetail.PercentNaturalRegeneration);
        Assert.Equal(100, storedRestockingDetail.PercentageOfRestockArea);
        Assert.Equal(200, storedRestockingDetail.RestockingDensity);
        Assert.Equal(2, storedRestockingDetail.ConfirmedRestockingSpecies.Count);
        Assert.Contains(storedRestockingDetail.ConfirmedRestockingSpecies, x => x.Species == "speciesA" && x.Percentage == 50);
        Assert.Contains(storedRestockingDetail.ConfirmedRestockingSpecies, x => x.Species == "speciesB" && x.Percentage == 50);
    }

    [Fact]
    public async Task SaveChangesToConfirmedRestockingDetailsAsync_AmendsDetails_WhenAllDataRetrieved_AddingRestockingInAltCompartment()
    {
        var sut = CreateSut();
        var userId = Guid.NewGuid();
        var fla = ConstructFellingLicenceApplication(true, userId);
        var fellingCompartment = fla.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!.First();
        var restockingCompartment = fla.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!.Last();
        var confirmedFellingDetail = fellingCompartment.ConfirmedFellingDetails.First();

        _fellingLicenceApplicationsContext!.Add(fla);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var restockingSpecies = new List<ConfirmedRestockingSpecies>
        {
            new ConfirmedRestockingSpecies { Species = "speciesA", Percentage = 50 },
            new ConfirmedRestockingSpecies { Species = "speciesB", Percentage = 50 }
        };

        var detailModel = new IndividualRestockingDetailModel
        {
            CompartmentId = restockingCompartment.CompartmentId,
            SubmittedFlaPropertyCompartmentId = restockingCompartment.Id,
            ConfirmedRestockingDetailModel = new ConfirmedRestockingDetailModel
            {
                ConfirmedRestockingDetailsId = Guid.NewGuid(),
                ConfirmedFellingDetailsId = confirmedFellingDetail.Id,
                Area = 123,
                RestockingProposal = TypeOfProposal.PlantAnAlternativeAreaWithIndividualTrees,
                NumberOfTrees = 10,
                PercentOpenSpace = 5,
                PercentNaturalRegeneration = 10,
                PercentageOfRestockArea = 100,
                RestockingDensity = 200,
                ConfirmedRestockingSpecies = restockingSpecies
            }
        };

        var speciesModel = new Dictionary<string, SpeciesModel>
        {
            { "speciesA", new SpeciesModel { Id = Guid.NewGuid(), Species = "speciesA", Percentage = 50 } },
            { "speciesB", new SpeciesModel { Id = Guid.NewGuid(), Species = "speciesB", Percentage = 50 } }
        };

        var result = await sut.SaveChangesToConfirmedRestockingDetailsAsync(
            fla.Id,
            userId,
            detailModel,
            speciesModel,
            CancellationToken.None);

        var storedFla = await _fellingLicenceApplicationsContext.FellingLicenceApplications
            .Include(x => x.SubmittedFlaPropertyDetail)
            .ThenInclude(d => d.SubmittedFlaPropertyCompartments!)
            .ThenInclude(c => c.ConfirmedFellingDetails)
            .ThenInclude(fd => fd.ConfirmedRestockingDetails)
            .ThenInclude(rd => rd.ConfirmedRestockingSpecies)
            .FirstOrDefaultAsync(x => x.Id == fla.Id);

        Assert.True(result.IsSuccess);
        Assert.NotNull(storedFla);
        var storedCompartment = storedFla!.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!.First(x => x.CompartmentId == fellingCompartment.CompartmentId);
        var storedFellingDetail = storedCompartment.ConfirmedFellingDetails.First(x => x.Id == confirmedFellingDetail.Id);
        var storedNewRestockingDetail = storedFellingDetail.ConfirmedRestockingDetails.First(x => x.RestockingProposal == TypeOfProposal.PlantAnAlternativeAreaWithIndividualTrees);
        Assert.Equal(123, storedNewRestockingDetail.Area);
        Assert.Equal(TypeOfProposal.PlantAnAlternativeAreaWithIndividualTrees, storedNewRestockingDetail.RestockingProposal);
        Assert.Equal(10, storedNewRestockingDetail.NumberOfTrees);
        Assert.Equal(5, storedNewRestockingDetail.PercentOpenSpace);
        Assert.Equal(10, storedNewRestockingDetail.PercentNaturalRegeneration);
        Assert.Equal(100, storedNewRestockingDetail.PercentageOfRestockArea);
        Assert.Equal(200, storedNewRestockingDetail.RestockingDensity);
        Assert.Equal(2, storedNewRestockingDetail.ConfirmedRestockingSpecies.Count);
        Assert.Contains(storedNewRestockingDetail.ConfirmedRestockingSpecies, x => x.Species == "speciesA" && x.Percentage == 50);
        Assert.Contains(storedNewRestockingDetail.ConfirmedRestockingSpecies, x => x.Species == "speciesB" && x.Percentage == 50);
    }

    [Fact]
    public async Task SaveChangesToConfirmedRestockingDetailsAsync_ReturnsFailure_WhenCompartmentIsNotFound()
    {
        var sut = CreateSut();
        var userId = Guid.NewGuid();
        var fla = ConstructFellingLicenceApplication(true, userId);
        var confirmedFellingDetail = fla.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!.First().ConfirmedFellingDetails.First();
        var confirmedRestockingDetail = confirmedFellingDetail.ConfirmedRestockingDetails.First();
        var randomCompartmentId = Guid.NewGuid();
        var detailModel = new IndividualRestockingDetailModel
        {
            CompartmentId = randomCompartmentId,
            ConfirmedRestockingDetailModel = new ConfirmedRestockingDetailModel
            {
                ConfirmedRestockingDetailsId = confirmedRestockingDetail.Id,
                ConfirmedFellingDetailsId = confirmedFellingDetail.Id,
                Area = 123,
                RestockingProposal = TypeOfProposal.ReplantTheFelledArea,
                ConfirmedRestockingSpecies = new List<ConfirmedRestockingSpecies>()
            }
        };
        var result = await sut.SaveChangesToConfirmedRestockingDetailsAsync(
            fla.Id,
            userId,
            detailModel,
            new Dictionary<string, SpeciesModel>(),
            CancellationToken.None);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task SaveChangesToConfirmedRestockingDetailsAsync_ReturnsFailure_WhenUserIsNotPermitted()
    {
        var sut = CreateSut();
        var userId = Guid.NewGuid();
        var fla = ConstructFellingLicenceApplication(true, userId);
        var confirmedFellingDetail = fla.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!.First().ConfirmedFellingDetails.First();
        var confirmedRestockingDetail = confirmedFellingDetail.ConfirmedRestockingDetails.First();
        var detailModel = new IndividualRestockingDetailModel
        {
            CompartmentId = fla.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!.First().CompartmentId,
            ConfirmedRestockingDetailModel = new ConfirmedRestockingDetailModel
            {
                ConfirmedRestockingDetailsId = confirmedRestockingDetail.Id,
                ConfirmedFellingDetailsId = confirmedFellingDetail.Id,
                Area = 123,
                RestockingProposal = TypeOfProposal.ReplantTheFelledArea,
                ConfirmedRestockingSpecies = new List<ConfirmedRestockingSpecies>()
            }
        };
        var otherUserId = Guid.NewGuid();
        var result = await sut.SaveChangesToConfirmedRestockingDetailsAsync(
            fla.Id,
            otherUserId,
            detailModel,
            new Dictionary<string, SpeciesModel>(),
            CancellationToken.None);
        Assert.True(result.IsFailure);
    }

    [Theory, AutoMoqData]
    public async Task SaveChangesToConfirmedRestockingDetailsAsync_ReturnsFailure_WhenApplicationNotFound(
        FellingLicenceApplication fla, IndividualRestockingDetailModel detailModel)
    {
        var userId = Guid.NewGuid();
        var sut = CreateSut();
        var result = await sut.SaveChangesToConfirmedRestockingDetailsAsync(
            fla.Id,
            userId,
            detailModel,
            new Dictionary<string, SpeciesModel>(),
            CancellationToken.None);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task DeleteConfirmedRestockingDetailAsync_RemovesRestockingDetailAndSpecies()
    {
        // setup
        var userId = Guid.NewGuid();
        var sut = CreateSut();
        var fla = ConstructFellingLicenceApplication(true, userId);
        _fellingLicenceApplicationsContext!.Add(fla);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var compartment = fla.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!.First();
        var confirmedFellingDetail = compartment.ConfirmedFellingDetails.First();
        var confirmedRestockingDetail = confirmedFellingDetail.ConfirmedRestockingDetails.First();
        var restockingSpeciesIds = confirmedRestockingDetail.ConfirmedRestockingSpecies.Select(x => x.Id).ToList();

        // Act
        var result = await sut.DeleteConfirmedRestockingDetailAsync(
            fla.Id,
            confirmedRestockingDetail.Id,
            userId,
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        var storedFla = await _fellingLicenceApplicationsContext.FellingLicenceApplications
            .Include(x => x.SubmittedFlaPropertyDetail)
            .ThenInclude(d => d.SubmittedFlaPropertyCompartments!)
            .ThenInclude(c => c.ConfirmedFellingDetails)
            .ThenInclude(fd => fd.ConfirmedRestockingDetails)
            .ThenInclude(rd => rd.ConfirmedRestockingSpecies)
            .FirstAsync(x => x.Id == fla.Id);
        var storedCompartment = storedFla!.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!.First();
        var storedFellingDetail = storedCompartment.ConfirmedFellingDetails.First(x => x.Id == confirmedFellingDetail.Id);
        Assert.DoesNotContain(confirmedRestockingDetail.Id, storedFellingDetail.ConfirmedRestockingDetails.Select(x => x.Id));
        foreach (var speciesId in restockingSpeciesIds)
        {
            Assert.DoesNotContain(speciesId, _fellingLicenceApplicationsContext.ConfirmedRestockingSpecies.Select(x => x.Id));
        }
    }

    [Fact]
    public async Task DeleteConfirmedRestockingDetailAsync_ReturnsFailure_WhenApplicationNotFound()
    {
        var userId = Guid.NewGuid();
        var sut = CreateSut();
        var nonExistentAppId = Guid.NewGuid();
        var restockingDetailId = Guid.NewGuid();
        var result = await sut.DeleteConfirmedRestockingDetailAsync(
            nonExistentAppId,
            restockingDetailId,
            userId,
            CancellationToken.None);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task DeleteConfirmedRestockingDetailAsync_ReturnsFailure_WhenUserIsNotPermitted()
    {
        var userId = Guid.NewGuid();
        var fla = ConstructFellingLicenceApplication(true, userId);
        var sut = CreateSut();
        _fellingLicenceApplicationsContext!.Add(fla);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();
        var compartment = fla.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!.First();
        var confirmedFellingDetail = compartment.ConfirmedFellingDetails.First();
        var confirmedRestockingDetail = confirmedFellingDetail.ConfirmedRestockingDetails.First();
        var otherUserId = Guid.NewGuid();
        var result = await sut.DeleteConfirmedRestockingDetailAsync(
            fla.Id,
            confirmedRestockingDetail.Id,
            otherUserId,
            CancellationToken.None);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task DeleteConfirmedRestockingDetailAsync_ReturnsFailure_WhenFellingDetailNotFound()
    {
        var userId = Guid.NewGuid();
        var sut = CreateSut();
        var fla = ConstructFellingLicenceApplication(true, userId);
        _fellingLicenceApplicationsContext!.Add(fla);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();
        var nonExistentRestockingDetailId = Guid.NewGuid();
        var result = await sut.DeleteConfirmedRestockingDetailAsync(
            fla.Id,
            nonExistentRestockingDetailId,
            userId,
            CancellationToken.None);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task DeleteConfirmedRestockingDetailAsync_ReturnsFailure_WhenRestockingDetailNotFound()
    {
        var userId = Guid.NewGuid();
        var sut = CreateSut();
        var fla = ConstructFellingLicenceApplication(true, userId);
        _fellingLicenceApplicationsContext!.Add(fla);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();
        var compartment = fla.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!.First();
        var confirmedFellingDetail = compartment.ConfirmedFellingDetails.First();
        // Remove all restocking details to simulate not found
        confirmedFellingDetail.ConfirmedRestockingDetails.Clear();
        await _fellingLicenceApplicationsContext.SaveChangesAsync();
        var randomRestockingDetailId = Guid.NewGuid();
        var result = await sut.DeleteConfirmedRestockingDetailAsync(
            fla.Id,
            randomRestockingDetailId,
            userId,
            CancellationToken.None);
        Assert.True(result.IsFailure);
    }

    private static FellingLicenceApplication ConstructFellingLicenceApplication(bool existingConfirmed, Guid userId)
    {
        var compartmentId1 = Guid.NewGuid();
        var compartmentId2 = Guid.NewGuid();
        var submittedFlaCompartmentId1 = Guid.NewGuid();
        var submittedFlaCompartmentId2 = Guid.NewGuid();

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

        fla.LinkedPropertyProfile = new LinkedPropertyProfile()
        {
            FellingLicenceApplicationId = fla.Id,
            FellingLicenceApplication = fla,
            PropertyProfileId = Guid.NewGuid(),
        };

        fla.LinkedPropertyProfile.ProposedFellingDetails = new List<ProposedFellingDetail>()
        {
            new()
            {
                AreaToBeFelled = 25,
                FellingSpecies = new List<FellingSpecies>(),
                IsPartOfTreePreservationOrder = true,
                TreePreservationOrderReference = "TPO-Testing",
                IsWithinConservationArea = true,
                ConservationAreaReference = "CAR-Test",
                IsRestocking = true,
                LinkedPropertyProfile = fla.LinkedPropertyProfile,
                LinkedPropertyProfileId = fla.LinkedPropertyProfile.Id,
                OperationType = FellingOperationType.ClearFelling,
                PropertyProfileCompartmentId = compartmentId1,
                EstimatedTotalFellingVolume = 24
            }
        };

        fla.LinkedPropertyProfile.ProposedFellingDetails[0].FellingSpecies!.Add(new FellingSpecies()
        {
            Id = Guid.NewGuid(),
            ProposedFellingDetail = fla.LinkedPropertyProfile.ProposedFellingDetails[0],
            ProposedFellingDetailsId = fla.LinkedPropertyProfile.ProposedFellingDetails[0].Id,
            Species = "test"
        });

        fla.LinkedPropertyProfile.ProposedFellingDetails[0].ProposedRestockingDetails = new List<ProposedRestockingDetail>()
        {
            new()
            {
                Area = 57,
                RestockingSpecies = new List<RestockingSpecies>(),
                ProposedFellingDetail = fla.LinkedPropertyProfile.ProposedFellingDetails[0],
                ProposedFellingDetailsId = fla.LinkedPropertyProfile.ProposedFellingDetails[0].Id,
                RestockingProposal = TypeOfProposal.ReplantTheFelledArea,
                PropertyProfileCompartmentId = compartmentId1,
                PercentageOfRestockArea = 40,
                RestockingDensity = 56
            }
        };

        fla.LinkedPropertyProfile.ProposedFellingDetails[0].ProposedRestockingDetails![0].RestockingSpecies!.Add(new RestockingSpecies()
        {
            Id = Guid.NewGuid(),
            Percentage = 100,
            ProposedRestockingDetail = fla.LinkedPropertyProfile.ProposedFellingDetails[0].ProposedRestockingDetails![0],
            ProposedRestockingDetailsId = fla.LinkedPropertyProfile.ProposedFellingDetails[0].ProposedRestockingDetails![0].Id,
            Species = "test"
        });

        var submittedFlaPropertyCompartment1 = new SubmittedFlaPropertyCompartment()
        {
            Id = submittedFlaCompartmentId1,
            CompartmentId = compartmentId1,
            CompartmentNumber = "CompartmentNumber1",
            PropertyProfileId = fla.LinkedPropertyProfile.PropertyProfileId
        };

        var submittedFlaPropertyCompartment2 = new SubmittedFlaPropertyCompartment()
        {
            Id = submittedFlaCompartmentId2,
            CompartmentId = compartmentId2,
            CompartmentNumber = "CompartmentNumber2",
            PropertyProfileId = fla.LinkedPropertyProfile.PropertyProfileId
        };

        fla.SubmittedFlaPropertyDetail = new()
        {
            FellingLicenceApplication = fla,
            FellingLicenceApplicationId = fla.Id,
            HasWoodlandManagementPlan = true,
            Id = Guid.NewGuid(),
            Name = String.Empty,
            PropertyProfileId = fla.LinkedPropertyProfile.PropertyProfileId,
            SubmittedFlaPropertyCompartments = new List<SubmittedFlaPropertyCompartment>() { submittedFlaPropertyCompartment1, submittedFlaPropertyCompartment2 }
        };
        submittedFlaPropertyCompartment1.SubmittedFlaPropertyDetail = fla.SubmittedFlaPropertyDetail;
        submittedFlaPropertyCompartment1.SubmittedFlaPropertyDetailId = fla.SubmittedFlaPropertyDetail.Id;

        if (existingConfirmed)
        {
            var confirmedRestockingSpecie = new ConfirmedRestockingSpecies()
            {
                Id = Guid.NewGuid(),
                Percentage = 44,
                Species = "test2"
            };

            var confirmedRestockingDetail = new ConfirmedRestockingDetail()
            {
                Area = 66,
                RestockingProposal = TypeOfProposal.CreateDesignedOpenGround,
                PercentageOfRestockArea = 12,
                RestockingDensity = 89,
                SubmittedFlaPropertyCompartmentId = submittedFlaCompartmentId1,
                ConfirmedRestockingSpecies = { confirmedRestockingSpecie }
            };
            confirmedRestockingSpecie.ConfirmedRestockingDetail = confirmedRestockingDetail;
            confirmedRestockingSpecie.ConfirmedRestockingDetailsId = confirmedRestockingDetail.Id;

            var confirmedFellingSpecie = new ConfirmedFellingSpecies()
            {
                Id = Guid.NewGuid(),
                Species = "test2"
            };

            var confirmedFellingDetail = new ConfirmedFellingDetail()
            {
                AreaToBeFelled = 66,
                EstimatedTotalFellingVolume = 65,
                IsPartOfTreePreservationOrder = false,
                TreePreservationOrderReference = null,
                IsWithinConservationArea = true,
                IsRestocking = true,
                ConservationAreaReference = "Testing",
                OperationType = FellingOperationType.ClearFelling,
                SubmittedFlaPropertyCompartmentId = submittedFlaCompartmentId1,
                SubmittedFlaPropertyCompartment = submittedFlaPropertyCompartment1,
                ConfirmedRestockingDetails = { confirmedRestockingDetail },
                ConfirmedFellingSpecies = { confirmedFellingSpecie },
            };
            confirmedRestockingDetail.ConfirmedFellingDetail = confirmedFellingDetail;
            confirmedRestockingDetail.ConfirmedFellingDetailId = confirmedFellingDetail.Id;
            confirmedFellingSpecie.ConfirmedFellingDetail = confirmedFellingDetail;
            confirmedFellingSpecie.ConfirmedFellingDetailId = confirmedFellingDetail.Id;

            submittedFlaPropertyCompartment1.ConfirmedFellingDetails = new List<ConfirmedFellingDetail>() { confirmedFellingDetail };
        }

        return fla;
    }

    [Fact]
    public void HasMissingProposedFellingOrRestockingLink_ReturnsTrue_WhenConfirmedRestockingReferencesMissingProposed()
    {
        var sut = CreateSut();

        var proposedFell = new Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview.ProposedFellingDetailModel
        {
            Id = Guid.NewGuid(),
            ProposedRestockingDetails = new List<Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview.ProposedRestockingDetailModel>()
        };

        var confirmedRestockMissing = new Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview.ConfirmedRestockingDetailModel
        {
            ConfirmedRestockingDetailsId = Guid.NewGuid(),
            ProposedRestockingDetailsId = Guid.NewGuid(), // not present in proposed
            ConfirmedRestockingSpecies = new List<ConfirmedRestockingSpecies>()
        };

        var confirmedFell = new Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview.ConfirmedFellingDetailModel
        {
            ConfirmedFellingDetailsId = Guid.NewGuid(),
            ProposedFellingDetailsId = proposedFell.Id,
            ConfirmedRestockingDetailModels = new List<Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview.ConfirmedRestockingDetailModel> { confirmedRestockMissing }
        };

        var compartment = new Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview.FellingAndRestockingDetailModel
        {
            CompartmentId = Guid.NewGuid(),
            SubmittedFlaPropertyCompartmentId = Guid.NewGuid(),
            ProposedFellingDetailModels = new List<Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview.ProposedFellingDetailModel> { proposedFell },
            ConfirmedFellingDetailModels = new List<Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview.ConfirmedFellingDetailModel> { confirmedFell }
        };

        Assert.True(sut.HasMissingProposedFellingOrRestockingLink(compartment));
    }

    [Fact]
    public void HasMissingProposedFellingOrRestockingLink_ReturnsTrue_WhenConfirmedFellingReferencesMissingProposed()
    {
        var sut = CreateSut();

        var proposedFell = new Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview.ProposedFellingDetailModel { Id = Guid.NewGuid() };
        var confirmedFell = new Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview.ConfirmedFellingDetailModel
        {
            ConfirmedFellingDetailsId = Guid.NewGuid(),
            ProposedFellingDetailsId = Guid.NewGuid(), // not present
            ConfirmedRestockingDetailModels = new List<Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview.ConfirmedRestockingDetailModel>()
        };

        var compartment = new Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview.FellingAndRestockingDetailModel
        {
            CompartmentId = Guid.NewGuid(),
            SubmittedFlaPropertyCompartmentId = Guid.NewGuid(),
            ProposedFellingDetailModels = new List<Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview.ProposedFellingDetailModel> { proposedFell },
            ConfirmedFellingDetailModels = new List<Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview.ConfirmedFellingDetailModel> { confirmedFell }
        };

        Assert.True(sut.HasMissingProposedFellingOrRestockingLink(compartment));
    }

    [Fact]
    public void HasMissingProposedFellingOrRestockingLink_ReturnsFalse_WhenAllConfirmedLinkedToProposed()
    {
        var sut = CreateSut();

        var proposedRestockId = Guid.NewGuid();
        var proposedFell = new Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview.ProposedFellingDetailModel
        {
            Id = Guid.NewGuid(),
            ProposedRestockingDetails = new List<Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview.ProposedRestockingDetailModel>
            {
                new Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview.ProposedRestockingDetailModel { Id = proposedRestockId }
            }
        };

        var confirmedRestock = new Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview.ConfirmedRestockingDetailModel
        {
            ConfirmedRestockingDetailsId = Guid.NewGuid(),
            ProposedRestockingDetailsId = proposedRestockId,
            ConfirmedRestockingSpecies = new List<ConfirmedRestockingSpecies>()
        };

        var confirmedFell = new Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview.ConfirmedFellingDetailModel
        {
            ConfirmedFellingDetailsId = Guid.NewGuid(),
            ProposedFellingDetailsId = proposedFell.Id,
            ConfirmedRestockingDetailModels = new List<Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview.ConfirmedRestockingDetailModel> { confirmedRestock }
        };

        var compartment = new Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview.FellingAndRestockingDetailModel
        {
            CompartmentId = Guid.NewGuid(),
            SubmittedFlaPropertyCompartmentId = Guid.NewGuid(),
            ProposedFellingDetailModels = new List<Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview.ProposedFellingDetailModel> { proposedFell },
            ConfirmedFellingDetailModels = new List<Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview.ConfirmedFellingDetailModel> { confirmedFell }
        };

        Assert.False(sut.HasMissingProposedFellingOrRestockingLink(compartment));
    }

    [Fact]
    public void HasUnmatchedProposedFellingOrRestocking_ReturnsTrue_WhenProposedFellingHasNoConfirmed()
    {
        var sut = CreateSut();

        var proposedFell = new Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview.ProposedFellingDetailModel
        {
            Id = Guid.NewGuid(),
            ProposedRestockingDetails = new List<Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview.ProposedRestockingDetailModel>()
        };

        var compartment = new Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview.FellingAndRestockingDetailModel
        {
            CompartmentId = Guid.NewGuid(),
            SubmittedFlaPropertyCompartmentId = Guid.NewGuid(),
            ProposedFellingDetailModels = new List<Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview.ProposedFellingDetailModel> { proposedFell },
            ConfirmedFellingDetailModels = new List<Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview.ConfirmedFellingDetailModel>()
        };

        Assert.True(sut.HasUnmatchedProposedFellingOrRestocking(compartment));
    }

    [Fact]
    public void HasUnmatchedProposedFellingOrRestocking_ReturnsTrue_WhenProposedRestockingHasNoConfirmed()
    {
        var sut = CreateSut();

        var proposedRestockId = Guid.NewGuid();
        var proposedFell = new Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview.ProposedFellingDetailModel
        {
            Id = Guid.NewGuid(),
            ProposedRestockingDetails = new List<Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview.ProposedRestockingDetailModel>
            {
                new Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview.ProposedRestockingDetailModel { Id = proposedRestockId }
            }
        };

        var confirmedFell = new Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview.ConfirmedFellingDetailModel
        {
            ConfirmedFellingDetailsId = Guid.NewGuid(),
            ProposedFellingDetailsId = proposedFell.Id,
            ConfirmedRestockingDetailModels = new List<Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview.ConfirmedRestockingDetailModel>() // none
        };

        var compartment = new Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview.FellingAndRestockingDetailModel
        {
            CompartmentId = Guid.NewGuid(),
            SubmittedFlaPropertyCompartmentId = Guid.NewGuid(),
            ProposedFellingDetailModels = new List<Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview.ProposedFellingDetailModel> { proposedFell },
            ConfirmedFellingDetailModels = new List<Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview.ConfirmedFellingDetailModel> { confirmedFell }
        };

        Assert.True(sut.HasUnmatchedProposedFellingOrRestocking(compartment));
    }

    [Fact]
    public void HasUnmatchedProposedFellingOrRestocking_ReturnsFalse_WhenAllProposedHaveConfirmed()
    {
        var sut = CreateSut();

        var proposedRestockId = Guid.NewGuid();
        var proposedFell = new Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview.ProposedFellingDetailModel
        {
            Id = Guid.NewGuid(),
            ProposedRestockingDetails = new List<Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview.ProposedRestockingDetailModel>
            {
                new Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview.ProposedRestockingDetailModel { Id = proposedRestockId }
            }
        };

        var confirmedRestock = new Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview.ConfirmedRestockingDetailModel
        {
            ConfirmedRestockingDetailsId = Guid.NewGuid(),
            ProposedRestockingDetailsId = proposedRestockId,
            ConfirmedRestockingSpecies = new List<ConfirmedRestockingSpecies>()
        };

        var confirmedFell = new Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview.ConfirmedFellingDetailModel
        {
            ConfirmedFellingDetailsId = Guid.NewGuid(),
            ProposedFellingDetailsId = proposedFell.Id,
            ConfirmedRestockingDetailModels = new List<Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview.ConfirmedRestockingDetailModel> { confirmedRestock }
        };

        var compartment = new Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview.FellingAndRestockingDetailModel
        {
            CompartmentId = Guid.NewGuid(),
            SubmittedFlaPropertyCompartmentId = Guid.NewGuid(),
            ProposedFellingDetailModels = new List<Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview.ProposedFellingDetailModel> { proposedFell },
            ConfirmedFellingDetailModels = new List<Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview.ConfirmedFellingDetailModel> { confirmedFell }
        };

        Assert.False(sut.HasUnmatchedProposedFellingOrRestocking(compartment));
    }
}