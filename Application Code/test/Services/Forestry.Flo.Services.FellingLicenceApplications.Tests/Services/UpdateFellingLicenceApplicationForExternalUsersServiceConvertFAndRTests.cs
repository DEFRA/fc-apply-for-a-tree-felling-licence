using AutoFixture;
using AutoFixture.AutoMoq;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Configuration;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NodaTime;
using NodaTime.Testing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services;

public class UpdateFellingLicenceApplicationForExternalUsersServiceConvertFAndRTests
{
    private IFellingLicenceApplicationExternalRepository? _fellingLicenceApplicationRepository;
    private FellingLicenceApplicationsContext? _fellingLicenceApplicationsContext;

    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private UpdateFellingLicenceApplicationForExternalUsersService CreateSut()
    {
        _fellingLicenceApplicationsContext = TestFellingLicenceApplicationsDatabaseFactory.CreateDefaultTestContext();
        _fellingLicenceApplicationRepository = new ExternalUserContextFlaRepository(
            _fellingLicenceApplicationsContext,
            new Mock<IApplicationReferenceHelper>().Object,
            new Mock<IFellingLicenceApplicationReferenceRepository>().Object);

        return new UpdateFellingLicenceApplicationForExternalUsersService(
            _fellingLicenceApplicationRepository,
            new FakeClock(Instant.FromDateTimeUtc(DateTime.Now.ToUniversalTime())),
            new OptionsWrapper<FellingLicenceApplicationOptions>(new FellingLicenceApplicationOptions()),
            new NullLogger<UpdateFellingLicenceApplicationForExternalUsersService>());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]

    public async Task ConfirmedFellingDetailsCorrect_WhenExistingConfirmedDetails(
        bool existingConfirmed)
    {
        // setup

        var sut = CreateSut();

        var user = CreateFixture().Create<UserAccessModel>();
        var fla = ConstructFellingLicenceApplication(existingConfirmed, user.UserAccountId);
        user.WoodlandOwnerIds!.Add(fla.WoodlandOwnerId);

        AddRestockingCompartment(fla);

        _fellingLicenceApplicationsContext!.Add(fla);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var result = await sut.ConvertProposedFellingAndRestockingToConfirmedAsync(
            fla.Id,
            user,
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
    public async Task ReturnsFailure_WhenUserHasNoAccessToApplication()
    {
        // setup

        var sut = CreateSut();

        var user = CreateFixture().Build<UserAccessModel>()
            .With(x => x.IsFcUser, false)
            .Create();
        
        var fla = ConstructFellingLicenceApplication(false, Guid.NewGuid());
        
        _fellingLicenceApplicationsContext!.Add(fla);
        
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var result = await sut.ConvertProposedFellingAndRestockingToConfirmedAsync(
            fla.Id,
            user,
            CancellationToken.None);

        // assert

        Assert.True(result.IsFailure);
    }

    [Fact]

    public async Task ReturnsFailure_WhenPropertyProfileNotFound()
    {
        // setup

        var sut = CreateSut();

        var user = CreateFixture().Create<UserAccessModel>();
        var fla = ConstructFellingLicenceApplication(false, user.UserAccountId);
        user.WoodlandOwnerIds!.Add(fla.WoodlandOwnerId);

        fla.LinkedPropertyProfile = null;

        _fellingLicenceApplicationsContext!.Add(fla);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var result = await sut.ConvertProposedFellingAndRestockingToConfirmedAsync(
            fla.Id,
            user,
            CancellationToken.None);

        // assert 

        Assert.True(result.IsFailure);
    }

    [Fact]

    public async Task ReturnsFailure_WhenSubmittedPropertyProfileNotFound()
    {
        // setup

        var sut = CreateSut();

        var user = CreateFixture().Create<UserAccessModel>();
        var fla = ConstructFellingLicenceApplication(false, user.UserAccountId);
        user.WoodlandOwnerIds!.Add(fla.WoodlandOwnerId);

        fla.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!.Clear();

        _fellingLicenceApplicationsContext!.Add(fla);
        await _fellingLicenceApplicationsContext.SaveChangesAsync();

        var result = await sut.ConvertProposedFellingAndRestockingToConfirmedAsync(
            fla.Id,
            user,
            CancellationToken.None);

        // assert 

        Assert.True(result.IsFailure);
    }

    [Fact]

    public async Task ReturnsFailure_WhenFLANotFound()
    {
        // setup

        var user = CreateFixture().Create<UserAccessModel>();

        var appId = Guid.NewGuid();

        var sut = CreateSut();

        var result = await sut.ConvertProposedFellingAndRestockingToConfirmedAsync(
            appId,
            user,
            CancellationToken.None);

        // assert 

        Assert.True(result.IsFailure);
    }

    private static IFixture CreateFixture()
    {
        var fixture = new Fixture().Customize(new CompositeCustomization(
            new AutoMoqCustomization(),
            new SupportMutableValueTypesCustomization()));

        fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        fixture.Customize<DateOnly>(composer => composer.FromFactory<DateTime>(DateOnly.FromDateTime));

        return fixture;
    }
}