using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services
{
    public class HabitatRestorationServiceTests
    {
        private static FellingLicenceApplicationsContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<FellingLicenceApplicationsContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new FellingLicenceApplicationsContext(options);
        }

        private static HabitatRestorationService CreateService(FellingLicenceApplicationsContext ctx)
        {
            var referenceRepo = new FellingLicenceApplicationReferenceRepository(ctx);
            var appRefHelper = new ApplicationReferenceHelper();
            var externalRepo = new ExternalUserContextFlaRepository(ctx, appRefHelper, referenceRepo);
            return new HabitatRestorationService(externalRepo);
        }

        private static FellingLicenceApplication CreateMinimalApplication(Guid id)
        {
            return new FellingLicenceApplication
            {
                Id = id,
                ApplicationReference = "APP-REF",
                CreatedTimestamp = DateTime.UtcNow,
                CreatedById = Guid.NewGuid(),
                WoodlandOwnerId = Guid.NewGuid(),
                StatusHistories = []
            };
        }

        [Fact]
        public async Task GetHabitatRestorationsAsync_ReturnsOnlyForApplication()
        {
            using var ctx = CreateContext();
            var service = CreateService(ctx);

            var appA = Guid.NewGuid();
            var appB = Guid.NewGuid();

            var applicationA = CreateMinimalApplication(appA);
            var applicationB = CreateMinimalApplication(appB);
            ctx.FellingLicenceApplications.AddRange(applicationA, applicationB);

            var lppA = new LinkedPropertyProfile { FellingLicenceApplicationId = appA, PropertyProfileId = Guid.NewGuid(), FellingLicenceApplication = applicationA };
            var lppB = new LinkedPropertyProfile { FellingLicenceApplicationId = appB, PropertyProfileId = Guid.NewGuid(), FellingLicenceApplication = applicationB };
            ctx.LinkedPropertyProfiles.AddRange(lppA, lppB);
            await ctx.SaveChangesAsync();

            var hrA1 = new HabitatRestoration { LinkedPropertyProfileId = lppA.Id, PropertyProfileCompartmentId = Guid.NewGuid() };
            var hrA2 = new HabitatRestoration { LinkedPropertyProfileId = lppA.Id, PropertyProfileCompartmentId = Guid.NewGuid() };
            var hrB1 = new HabitatRestoration { LinkedPropertyProfileId = lppB.Id, PropertyProfileCompartmentId = Guid.NewGuid() };
            ctx.HabitatRestorations.AddRange(hrA1, hrA2, hrB1);
            await ctx.SaveChangesAsync();

            var listA = await service.GetHabitatRestorationModelsAsync(appA, CancellationToken.None);
            var listB = await service.GetHabitatRestorationModelsAsync(appB, CancellationToken.None);

            Assert.Equal(2, listA.Count);
            Assert.Single(listB);
            Assert.All(listA, r => Assert.Equal(lppA.Id, r.LinkedPropertyProfileId));
        }

        [Fact]
        public async Task AddHabitatRestorationAsync_Fails_WhenNoMatchingLinkedProfile()
        {
            using var ctx = CreateContext();
            var service = CreateService(ctx);

            var appId = Guid.NewGuid();
            var compartmentId = Guid.NewGuid();

            var result = await service.AddHabitatRestorationAsync(appId, compartmentId);
            Assert.True(result.IsFailure);
            Assert.Equal(UserDbErrorReason.NotFound, result.Error);
        }

        [Fact]
        public async Task AddHabitatRestorationAsync_Adds_WhenMatchingLinkedProfileAndPfdExists()
        {
            using var ctx = CreateContext();
            var service = CreateService(ctx);

            var appId = Guid.NewGuid();
            var application = CreateMinimalApplication(appId);
            ctx.FellingLicenceApplications.Add(application);

            var lpp = new LinkedPropertyProfile { FellingLicenceApplicationId = appId, PropertyProfileId = Guid.NewGuid(), FellingLicenceApplication = application };
            ctx.LinkedPropertyProfiles.Add(lpp);
            await ctx.SaveChangesAsync();

            var compartmentId = Guid.NewGuid();
            var pfd = new ProposedFellingDetail { LinkedPropertyProfileId = lpp.Id, PropertyProfileCompartmentId = compartmentId };
            ctx.ProposedFellingDetails.Add(pfd);
            await ctx.SaveChangesAsync();

            var result = await service.AddHabitatRestorationAsync(appId, compartmentId);
            Assert.True(result.IsSuccess);

            var restorations = await service.GetHabitatRestorationModelsAsync(appId, CancellationToken.None);
            Assert.Single(restorations);
            Assert.Equal(compartmentId, restorations[0].PropertyProfileCompartmentId);
        }

        [Fact]
        public async Task UpdateHabitatRestorationAsync_ModelUpdatesTrackedEntity()
        {
            using var ctx = CreateContext();
            var service = CreateService(ctx);

            var appId = Guid.NewGuid();
            var application = CreateMinimalApplication(appId);
            ctx.FellingLicenceApplications.Add(application);

            var lpp = new LinkedPropertyProfile { FellingLicenceApplicationId = appId, PropertyProfileId = Guid.NewGuid(), FellingLicenceApplication = application };
            ctx.LinkedPropertyProfiles.Add(lpp);
            await ctx.SaveChangesAsync();

            var hr = new HabitatRestoration
            {
                LinkedPropertyProfileId = lpp.Id,
                PropertyProfileCompartmentId = Guid.NewGuid(),
                HabitatType = HabitatType.BlanketBog,
                WoodlandSpeciesType = Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandSpeciesType.MixedWoodland,
                NativeBroadleaf = false,
                ProductiveWoodland = false,
                FelledEarly = false,
                Completed = false,
                OtherHabitatDescription = "old"
            };
            ctx.HabitatRestorations.Add(hr);
            await ctx.SaveChangesAsync();

            // load into tracking
            var tracked = await ctx.HabitatRestorations.FirstAsync(x => x.Id == hr.Id);

            var updateModel = new HabitatRestoration
            {
                Id = hr.Id,
                LinkedPropertyProfileId = lpp.Id,
                PropertyProfileCompartmentId = hr.PropertyProfileCompartmentId,
                HabitatType = HabitatType.LowlandHeathland,
                WoodlandSpeciesType = Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandSpeciesType.BroadleafWoodland,
                NativeBroadleaf = null,
                ProductiveWoodland = true,
                FelledEarly = true,
                Completed = true,
                OtherHabitatDescription = "new"
            };

            var result = await service.UpdateHabitatRestorationAsync(updateModel);
            Assert.True(result.IsSuccess);

            // Updated via repository (attached or tracked instance), re-query to verify changes persisted
            var updated = await ctx.HabitatRestorations.AsNoTracking().FirstAsync(x => x.Id == hr.Id);
            Assert.Equal(updateModel.HabitatType, updated.HabitatType);
            Assert.Equal(updateModel.WoodlandSpeciesType, updated.WoodlandSpeciesType);
            Assert.Equal(updateModel.NativeBroadleaf, updated.NativeBroadleaf);
            Assert.Equal(updateModel.ProductiveWoodland, updated.ProductiveWoodland);
            Assert.Equal(updateModel.FelledEarly, updated.FelledEarly);
            Assert.Equal(updateModel.Completed, updated.Completed);
            Assert.Equal(updateModel.OtherHabitatDescription, updated.OtherHabitatDescription);
        }

        [Fact]
        public async Task DeleteHabitatRestorationsAsync_RemovesAllForApplication()
        {
            using var ctx = CreateContext();
            var service = CreateService(ctx);

            var appId = Guid.NewGuid();
            var otherAppId = Guid.NewGuid();

            var applicationA = CreateMinimalApplication(appId);
            var applicationB = CreateMinimalApplication(otherAppId);
            ctx.FellingLicenceApplications.AddRange(applicationA, applicationB);

            var lppA = new LinkedPropertyProfile { FellingLicenceApplicationId = appId, PropertyProfileId = Guid.NewGuid(), FellingLicenceApplication = applicationA };
            var lppB = new LinkedPropertyProfile { FellingLicenceApplicationId = otherAppId, PropertyProfileId = Guid.NewGuid(), FellingLicenceApplication = applicationB };
            ctx.LinkedPropertyProfiles.AddRange(lppA, lppB);
            await ctx.SaveChangesAsync();

            ctx.HabitatRestorations.AddRange(
                new HabitatRestoration { LinkedPropertyProfileId = lppA.Id, PropertyProfileCompartmentId = Guid.NewGuid() },
                new HabitatRestoration { LinkedPropertyProfileId = lppA.Id, PropertyProfileCompartmentId = Guid.NewGuid() },
                new HabitatRestoration { LinkedPropertyProfileId = lppB.Id, PropertyProfileCompartmentId = Guid.NewGuid() }
            );
            await ctx.SaveChangesAsync();

            var result = await service.DeleteHabitatRestorationsAsync(appId, CancellationToken.None);
            Assert.True(result.IsSuccess);

            var remaining = await service.GetHabitatRestorationModelsAsync(appId, CancellationToken.None);
            Assert.Empty(remaining);

            var otherRemaining = await service.GetHabitatRestorationModelsAsync(otherAppId, CancellationToken.None);
            Assert.Single(otherRemaining);
        }
    }
}
