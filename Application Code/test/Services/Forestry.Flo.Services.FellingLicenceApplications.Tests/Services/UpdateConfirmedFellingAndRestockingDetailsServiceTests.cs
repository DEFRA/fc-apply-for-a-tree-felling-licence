using FluentAssertions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.PropertyProfiles.Entities;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Forestry.Flo.Services.Common.Extensions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;
using Forestry.Flo.Services.FellingLicenceApplications.Models;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services;

public partial class UpdateConfirmedFellingAndRestockingDetailsServiceTests
{
    private IFellingLicenceApplicationInternalRepository? _fellingLicenceApplicationRepository;
    private readonly Mock<IAuditService<UpdateConfirmedFellingAndRestockingDetailsService>> _audit = new();
    private FellingLicenceApplicationsContext? _fellingLicenceApplicationsContext;

    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    protected UpdateConfirmedFellingAndRestockingDetailsService CreateSut()
    {
        _fellingLicenceApplicationsContext = TestFellingLicenceApplicationsDatabaseFactory.CreateDefaultTestContext();
        _fellingLicenceApplicationRepository = new InternalUserContextFlaRepository(_fellingLicenceApplicationsContext);
        _audit.Reset();

        return new UpdateConfirmedFellingAndRestockingDetailsService(
            _fellingLicenceApplicationRepository,
            new NullLogger<UpdateConfirmedFellingAndRestockingDetailsService>(),
            _audit.Object,
            new RequestContext("test", new RequestUserModel(new ClaimsPrincipal()))
            );
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]

    public async Task ConfirmedFellingDetailsCorrect_WhenExistingConfirmedDetails(bool existingConfirmed)
    {
        // setup

        var userId = Guid.NewGuid();

        var sut = CreateSut();

        var fla = ConstructFellingLicenceApplication(existingConfirmed, userId);

        AddRestockingCompartment(fla);

        _fellingLicenceApplicationsContext!.Add(fla);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var result = await sut.ConvertProposedFellingAndRestockingToConfirmedAsync(
            fla.Id,
            userId,
            CancellationToken.None);

        // assert 

        var storedFla = await _fellingLicenceApplicationsContext.FellingLicenceApplications.FindAsync(fla.Id);

        // assert the result is successful and the database returns the fla

        Assert.True(result.IsSuccess);
        Assert.NotNull(storedFla);

        foreach (var compartment in storedFla!.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!)
        {
            foreach (var confirmedFell in compartment.ConfirmedFellingDetails)
            {
                var proposedFell = storedFla.LinkedPropertyProfile!.ProposedFellingDetails!.FirstOrDefault(x =>
                    x.PropertyProfileCompartmentId == compartment.CompartmentId && x.AreaToBeFelled == confirmedFell.AreaToBeFelled)!;

                // assert that felling details have been successfully overriden

                Assert.Equal(proposedFell.AreaToBeFelled, confirmedFell.AreaToBeFelled);
                Assert.Equal(proposedFell.IsPartOfTreePreservationOrder, confirmedFell.IsPartOfTreePreservationOrder);
                Assert.Equal(proposedFell.TreePreservationOrderReference, confirmedFell.TreePreservationOrderReference);
                Assert.Equal(proposedFell.IsWithinConservationArea, confirmedFell.IsWithinConservationArea);
                Assert.Equal(proposedFell.ConservationAreaReference, confirmedFell.ConservationAreaReference);
                Assert.Equal(proposedFell.NumberOfTrees, confirmedFell.NumberOfTrees);
                Assert.Equal(proposedFell.TreeMarking, confirmedFell.TreeMarking);
                Assert.Equal(proposedFell.OperationType, confirmedFell.OperationType);
                Assert.Equal(proposedFell.EstimatedTotalFellingVolume, confirmedFell.EstimatedTotalFellingVolume);
                Assert.Equal(proposedFell.IsRestocking, confirmedFell.IsRestocking);
                Assert.Equal(proposedFell.NoRestockingReason, confirmedFell.NoRestockingReason);

                Assert.Equal(proposedFell.FellingSpecies!.Count, confirmedFell.ConfirmedFellingSpecies!.Count);

                foreach (var proposedSpecies in proposedFell.FellingSpecies!)
                {
                    var confirmedSpecies =
                        confirmedFell.ConfirmedFellingSpecies!.FirstOrDefault(x => x.Species == proposedSpecies.Species);

                    // assert that proposed restocking species data has been added to the confirmed restocking species list.

                    Assert.NotNull(confirmedSpecies);
                }

                foreach (var confirmedRestock in confirmedFell.ConfirmedRestockingDetails)
                {
                    var submitedCompartment = storedFla!.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments.First(x =>
                        x.Id == confirmedRestock.SubmittedFlaPropertyCompartmentId)!;

                    var proposedRestock =
                        proposedFell.ProposedRestockingDetails.FirstOrDefault(x =>
                    x.PropertyProfileCompartmentId == submitedCompartment.CompartmentId && x.Area == confirmedRestock.Area)!;

                    // assert that proposed restocking species data has been added to the confirmed restocking species list.

                    Assert.Equal(proposedRestock.Area, confirmedRestock.Area);
                    Assert.Equal(proposedRestock.PercentageOfRestockArea, confirmedRestock.PercentageOfRestockArea);
                    Assert.Equal(proposedRestock.RestockingDensity, confirmedRestock.RestockingDensity);
                    Assert.Equal(proposedRestock.RestockingProposal, confirmedRestock.RestockingProposal);
                    Assert.Equal(proposedRestock.PropertyProfileCompartmentId, submitedCompartment.CompartmentId);

                    Assert.Equal(proposedRestock.RestockingSpecies!.Count, confirmedRestock.ConfirmedRestockingSpecies.Count);

                    foreach (var proposedSpecies in proposedRestock.RestockingSpecies!)
                    {
                        var confirmedSpecies =
                            confirmedRestock.ConfirmedRestockingSpecies.FirstOrDefault(x =>
                                x.Species == proposedSpecies.Species)!;

                        // assert that proposed restocking species data has been added to the confirmed restocking species list

                        Assert.Equal(proposedSpecies.Percentage, confirmedSpecies.Percentage);
                    }
                }
            }
            Assert.Equal(
                fla.LinkedPropertyProfile!.ProposedFellingDetails!
                .Where(x=>x.PropertyProfileCompartmentId == compartment.CompartmentId).Count(), compartment.ConfirmedFellingDetails.Count);
        }

        // assert that all felling details have been copied, and that the database only contains these new details

        Assert.Equal(fla.LinkedPropertyProfile.ProposedFellingDetails.Count, _fellingLicenceApplicationsContext.ConfirmedFellingDetails.Count());

        _audit.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(e =>
                e.SourceEntityId == fla.Id
                && e.UserId == userId
                && e.EventName == AuditEvents.UpdateWoodlandOfficerReview
                && JsonSerializer.Serialize(e.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    Section = "Import Confirmed Felling/Restocking Details"
                }, _options)),
            CancellationToken.None), Times.Once);
    }

    private static void AddRestockingCompartment(FellingLicenceApplication fla)
    {
        var compartmentId = Guid.NewGuid();

        var submittedFlaPropertyCompartment = new SubmittedFlaPropertyCompartment()
        {
            CompartmentId = compartmentId,
            CompartmentNumber = "CompartmentNumber2",
            PropertyProfileId = fla.LinkedPropertyProfile!.PropertyProfileId,
            SubmittedFlaPropertyDetail = fla.SubmittedFlaPropertyDetail!,
            SubmittedFlaPropertyDetailId = fla.SubmittedFlaPropertyDetail!.Id
        };
        fla.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments!.Add(submittedFlaPropertyCompartment);

        var moreFellings = new ProposedFellingDetail()
        {
            PropertyProfileCompartmentId = fla.LinkedPropertyProfile.ProposedFellingDetails!.First().PropertyProfileCompartmentId,
            AreaToBeFelled = 252,
            FellingSpecies = new List<FellingSpecies>(),
            IsPartOfTreePreservationOrder = true,
            TreePreservationOrderReference = "TPO-Testing2",
            IsWithinConservationArea = true,
            IsRestocking = true,
            ConservationAreaReference = "CAR-Test2",
            LinkedPropertyProfile = fla.LinkedPropertyProfile,
            LinkedPropertyProfileId = fla.LinkedPropertyProfile.Id,
            OperationType = FellingOperationType.FellingIndividualTrees,
        };
        moreFellings.ProposedRestockingDetails = new List<ProposedRestockingDetail>()
                        {
                            new()
                            {
                                Area = 572,
                                RestockingSpecies = new List<RestockingSpecies>(),
                                ProposedFellingDetail = moreFellings,
                                ProposedFellingDetailsId = moreFellings.Id,
                                RestockingProposal = TypeOfProposal.ReplantTheFelledArea,
                                PropertyProfileCompartmentId = compartmentId,
                                PercentageOfRestockArea = 40,
                                RestockingDensity = 56
                            }
                        };
        fla.LinkedPropertyProfile.ProposedFellingDetails!.Add(moreFellings);
    }

    private static FellingLicenceApplication ConstructFellingLicenceApplication(bool existingConfirmed, Guid userId)
    {
        var compartmentId = Guid.NewGuid();

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
                PropertyProfileCompartmentId = compartmentId,
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
                PropertyProfileCompartmentId = compartmentId,
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

        var submittedFlaPropertyCompartment = new SubmittedFlaPropertyCompartment()
        {
            CompartmentId = compartmentId,
            CompartmentNumber = "CompartmentNumber",
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
            SubmittedFlaPropertyCompartments = new List<SubmittedFlaPropertyCompartment>() { submittedFlaPropertyCompartment }
        };
        submittedFlaPropertyCompartment.SubmittedFlaPropertyDetail = fla.SubmittedFlaPropertyDetail;
        submittedFlaPropertyCompartment.SubmittedFlaPropertyDetailId = fla.SubmittedFlaPropertyDetail.Id;

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
                SubmittedFlaPropertyCompartmentId = submittedFlaPropertyCompartment.Id,
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
                SubmittedFlaPropertyCompartmentId = submittedFlaPropertyCompartment.CompartmentId,
                SubmittedFlaPropertyCompartment = submittedFlaPropertyCompartment,
                ConfirmedRestockingDetails = { confirmedRestockingDetail },
                ConfirmedFellingSpecies = { confirmedFellingSpecie },
            };
            confirmedRestockingDetail.ConfirmedFellingDetail = confirmedFellingDetail;
            confirmedRestockingDetail.ConfirmedFellingDetailId = confirmedFellingDetail.Id;
            confirmedFellingSpecie.ConfirmedFellingDetail = confirmedFellingDetail;
            confirmedFellingSpecie.ConfirmedFellingDetailId = confirmedFellingDetail.Id;

            submittedFlaPropertyCompartment.ConfirmedFellingDetails = new List<ConfirmedFellingDetail>() { confirmedFellingDetail };
        }

        return fla;
    }

    [Fact]
    public async Task ReturnsFailure_WhenUserIsNotAssigned()
    {
        // setup

        var userId = Guid.NewGuid();

        var sut = CreateSut();

        var fla = ConstructFellingLicenceApplication(false, userId);

        _fellingLicenceApplicationsContext!.Add(fla);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var userId2 = Guid.NewGuid();

        var result = await sut.ConvertProposedFellingAndRestockingToConfirmedAsync(
            fla.Id,
            userId2,
            CancellationToken.None);

        // assert

        result.IsFailure.Should().BeTrue();

        _audit.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(e => e.SourceEntityId == fla.Id
                && e.EventName == AuditEvents.UpdateWoodlandOfficerReviewFailure
                && JsonSerializer.Serialize(e.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    Section = "Import Confirmed Felling/Restocking Details",
                    Error = $"User is not assigned Woodland Officer for application in ConvertProposedFellingAndRestockingToConfirmedAsync, application id {fla.Id}, user id {userId2}"
                }, _options)),
            CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task ReturnsFailure_WhenStatusIsNotWoodlandOfficerReview()
    {
        // setup

        var userId = Guid.NewGuid();

        var sut = CreateSut();

        var fla = ConstructFellingLicenceApplication(false, userId);

        fla.StatusHistories.Clear();
        fla.StatusHistories.Add(new StatusHistory
        {
            Created = DateTime.Now,
            FellingLicenceApplication = fla,
            FellingLicenceApplicationId = fla.Id,
            Status = FellingLicenceStatus.Draft
        });

        _fellingLicenceApplicationsContext!.Add(fla);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var result = await sut.ConvertProposedFellingAndRestockingToConfirmedAsync(
            fla.Id,
            userId,
            CancellationToken.None);

        // assert

        result.IsFailure.Should().BeTrue();

        _audit.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(e => e.SourceEntityId == fla.Id
               && e.EventName == AuditEvents.UpdateWoodlandOfficerReviewFailure
               && JsonSerializer.Serialize(e.AuditData, _options) ==
               JsonSerializer.Serialize(new
               {
                   Section = "Import Confirmed Felling/Restocking Details",
                   Error = $"Application is not in Woodland Officer review stage in ConvertProposedFellingAndRestockingToConfirmedAsync, id {fla.Id}"
               }, _options)),
            CancellationToken.None), Times.Once);
    }

    [Fact]

    public async Task ReturnsFailure_WhenPropertyProfileNotFound()
    {
        // setup

        var userId = Guid.NewGuid();

        var fla = ConstructFellingLicenceApplication(true, userId);

        var sut = CreateSut();

        fla.LinkedPropertyProfile = null;

        _fellingLicenceApplicationsContext!.Add(fla);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var result = await sut.ConvertProposedFellingAndRestockingToConfirmedAsync(
            fla.Id,
            userId,
            CancellationToken.None);

        // assert 

        result.IsFailure.Should().BeTrue();

        _audit.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(e => e.SourceEntityId == fla.Id
               && e.EventName == AuditEvents.UpdateWoodlandOfficerReviewFailure
               && e.UserId == userId
               && JsonSerializer.Serialize(e.AuditData, _options) ==
               JsonSerializer.Serialize(new
               {
                   Section = "Import Confirmed Felling/Restocking Details",
                   error = $"Unable to retrieve property profile in ConvertProposedFellingDetailsToConfirmed, id {fla.Id}"
               }, _options)),
            CancellationToken.None), Times.Once);
    }

    [Fact]

    public async Task ReturnsFailure_WhenSubmittedPropertyProfileNotFound()
    {
        // setup

        var userId = Guid.NewGuid();

        var fla = ConstructFellingLicenceApplication(true, userId);

        var sut = CreateSut();

        fla.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!.Clear();

        _fellingLicenceApplicationsContext!.Add(fla);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var result = await sut.ConvertProposedFellingAndRestockingToConfirmedAsync(
            fla.Id,
            userId,
            CancellationToken.None);

        // assert 

        result.IsFailure.Should().BeTrue();

        _audit.Verify(v => v.PublishAuditEventAsync(
            It.Is<AuditEvent>(e => e.SourceEntityId == fla.Id
               && e.EventName == AuditEvents.UpdateWoodlandOfficerReviewFailure
               && JsonSerializer.Serialize(e.AuditData, _options) ==
               JsonSerializer.Serialize(new
               {
                   Section = "Import Confirmed Felling/Restocking Details",
                   Error = $"Unable to determine submitted FLA property compartment with id {fla.LinkedPropertyProfile.ProposedFellingDetails[0].PropertyProfileCompartmentId}, FLA id {fla.Id}"
               }, _options)),
            CancellationToken.None), Times.Once);
    }

    [Fact]

    public async Task ReturnsFailure_WhenFLANotFound()
    {
        // setup

        var userId = Guid.NewGuid();

        var appId = Guid.NewGuid();

        var sut = CreateSut();

        var result = await sut.ConvertProposedFellingAndRestockingToConfirmedAsync(
            appId,
            userId,
            CancellationToken.None);

        // assert 

        result.IsFailure.Should().BeTrue();

        _audit.Verify(v => v.PublishAuditEventAsync(
        It.Is<AuditEvent>(e =>
                e.SourceEntityId == appId
                && e.UserId == userId
                && e.EventName == AuditEvents.UpdateWoodlandOfficerReviewFailure
                && JsonSerializer.Serialize(e.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    Section = "Import Confirmed Felling/Restocking Details",
                    Error = "Unable to retrieve felling licence application"
                }, _options)),
            CancellationToken.None), Times.Once);
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
        result.IsSuccess.Should().BeTrue();

        var storedFla = await _fellingLicenceApplicationsContext.FellingLicenceApplications
            .Include(fellingLicenceApplication => fellingLicenceApplication.SubmittedFlaPropertyDetail!)
            .ThenInclude(submittedFlaPropertyDetail => submittedFlaPropertyDetail.SubmittedFlaPropertyCompartments!)
            .ThenInclude(submittedFlaPropertyCompartment => submittedFlaPropertyCompartment.ConfirmedFellingDetails)
            .ThenInclude(fellingDetail => fellingDetail.ConfirmedRestockingDetails)
            .FirstAsync(x => x.Id == fla.Id);
        var storedCompartment = storedFla!.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!.First();
        storedCompartment.ConfirmedFellingDetails.Should().NotContain(x => x.Id == confirmedFellingDetail.Id);

        _fellingLicenceApplicationsContext.ConfirmedFellingDetails
            .Any(x => x.Id == confirmedFellingDetail.Id)
            .Should().BeFalse();

        foreach (var restockingDetail in confirmedFellingDetail.ConfirmedRestockingDetails)
        {
            _fellingLicenceApplicationsContext.ConfirmedRestockingDetails
                .Any(x => x.Id == restockingDetail.Id)
                .Should().BeFalse();

            foreach (var restockingSpecies in restockingDetail.ConfirmedRestockingSpecies)
            {
                _fellingLicenceApplicationsContext.ConfirmedRestockingSpecies
                    .Any(x => x.Id == restockingSpecies.Id)
                    .Should().BeFalse();
            }
        }

        _fellingLicenceApplicationsContext.ConfirmedFellingSpecies
            .NotAny(x => x.ConfirmedFellingDetailId == confirmedFellingDetail.Id)
            .Should()
            .BeTrue();

        foreach (var restock in confirmedFellingDetail.ConfirmedRestockingDetails)
        {
            _fellingLicenceApplicationsContext.ConfirmedRestockingSpecies
                .NotAny(x => x.ConfirmedRestockingDetailsId == restock.Id)
                .Should()
                .BeTrue();
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
        result.IsFailure.Should().BeTrue();
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
        result.IsFailure.Should().BeTrue();
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
        result.IsFailure.Should().BeTrue();
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

        var detailModel = new IndividualConfirmedRestockingDetailModel
        {
            CompartmentId = compartment.CompartmentId,
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
    public async Task SaveChangesToConfirmedRestockingDetailsAsync_ReturnsFailure_WhenCompartmentIsNotFound()
    {
        var sut = CreateSut();
        var userId = Guid.NewGuid();
        var fla = ConstructFellingLicenceApplication(true, userId);
        var confirmedFellingDetail = fla.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!.First().ConfirmedFellingDetails.First();
        var confirmedRestockingDetail = confirmedFellingDetail.ConfirmedRestockingDetails.First();
        var randomCompartmentId = Guid.NewGuid();
        var detailModel = new IndividualConfirmedRestockingDetailModel
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
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task SaveChangesToConfirmedRestockingDetailsAsync_ReturnsFailure_WhenUserIsNotPermitted()
    {
        var sut = CreateSut();
        var userId = Guid.NewGuid();
        var fla = ConstructFellingLicenceApplication(true, userId);
        var confirmedFellingDetail = fla.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!.First().ConfirmedFellingDetails.First();
        var confirmedRestockingDetail = confirmedFellingDetail.ConfirmedRestockingDetails.First();
        var detailModel = new IndividualConfirmedRestockingDetailModel
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
        result.IsFailure.Should().BeTrue();
    }

    [Theory, AutoMoqData]
    public async Task SaveChangesToConfirmedRestockingDetailsAsync_ReturnsFailure_WhenApplicationNotFound(
        FellingLicenceApplication fla, IndividualConfirmedRestockingDetailModel detailModel)
    {
        var userId = Guid.NewGuid();
        var sut = CreateSut();
        var result = await sut.SaveChangesToConfirmedRestockingDetailsAsync(
            fla.Id,
            userId,
            detailModel,
            new Dictionary<string, SpeciesModel>(),
            CancellationToken.None);
        result.IsFailure.Should().BeTrue();
    }
}