using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Services;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common; // IUnitOfWork, UserDbErrorReason
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.PropertyProfiles.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Forestry.Flo.External.Web.Tests.Services
{
    public class HabitatRestorationUseCaseTests
    {
        private static FellingLicenceApplication CreateApplication(Guid id, bool? isPriority = null)
        {
            return new FellingLicenceApplication
            {
                Id = id,
                ApplicationReference = "APP-REF",
                CreatedTimestamp = DateTime.UtcNow,
                CreatedById = Guid.NewGuid(),
                WoodlandOwnerId = Guid.NewGuid(),
                StatusHistories = [],
                FellingLicenceApplicationStepStatus = new FellingLicenceApplicationStepStatus(),
                IsPriorityOpenHabitat = isPriority,
                LinkedPropertyProfile = new LinkedPropertyProfile { FellingLicenceApplicationId = id, PropertyProfileId = Guid.NewGuid(), FellingLicenceApplication = null! }
            };
        }

        private static ExternalApplicant CreateExternalApplicant(Guid userAccountId)
        {
            var identity = new ClaimsIdentity();
            identity.AddClaim(new Claim(Flo.Services.Common.FloClaimTypes.LocalAccountId, userAccountId.ToString()));
            var principal = new ClaimsPrincipal(identity);
            return new ExternalApplicant(principal);
        }

        private static HabitatRestorationUseCase CreateUseCase(
            Mock<IFellingLicenceApplicationExternalRepository> repoMock,
            Mock<IHabitatRestorationService> habitatMock,
            out Mock<IRetrieveUserAccountsService> retrieveUserAccountsServiceMock)
        {
            var audit = new Mock<IAuditService<HabitatRestorationUseCase>>();
            var logger = new Mock<ILogger<HabitatRestorationUseCase>>();
            var retrieveWoodlandOwners = new Mock<IRetrieveWoodlandOwners>();
            retrieveUserAccountsServiceMock = new Mock<IRetrieveUserAccountsService>();
            var getFlaForExternal = new Mock<IGetFellingLicenceApplicationForExternalUsers>();
            var getPropertyProfiles = new Mock<IGetPropertyProfiles>();
            var getCompartments = new Mock<IGetCompartments>();
            var agentAuthorityService = new Mock<IAgentAuthorityService>();
            var requestContext = new RequestContext("test", new RequestUserModel(new ClaimsPrincipal(new ClaimsIdentity())));

            return new HabitatRestorationUseCase(
                retrieveUserAccountsServiceMock.Object,
                retrieveWoodlandOwners.Object,
                getFlaForExternal.Object,
                getPropertyProfiles.Object,
                getCompartments.Object,
                agentAuthorityService.Object,
                repoMock.Object,
                habitatMock.Object,
                audit.Object,
                requestContext,
                logger.Object);
        }

        private static HabitatRestorationUseCase CreateUseCase(
            Mock<IFellingLicenceApplicationExternalRepository> repoMock,
            Mock<IHabitatRestorationService> habitatMock,
            out Mock<IRetrieveUserAccountsService> retrieveUserAccountsServiceMock,
            out Mock<IAuditService<HabitatRestorationUseCase>> auditMock)
        {
            auditMock = new Mock<IAuditService<HabitatRestorationUseCase>>();
            var logger = new Mock<ILogger<HabitatRestorationUseCase>>();
            var retrieveWoodlandOwners = new Mock<IRetrieveWoodlandOwners>();
            retrieveUserAccountsServiceMock = new Mock<IRetrieveUserAccountsService>();
            var getFlaForExternal = new Mock<IGetFellingLicenceApplicationForExternalUsers>();
            var getPropertyProfiles = new Mock<IGetPropertyProfiles>();
            var getCompartments = new Mock<IGetCompartments>();
            var agentAuthorityService = new Mock<IAgentAuthorityService>();
            var requestContext = new RequestContext("test", new RequestUserModel(new ClaimsPrincipal(new ClaimsIdentity())));

            return new HabitatRestorationUseCase(
                retrieveUserAccountsServiceMock.Object,
                retrieveWoodlandOwners.Object,
                getFlaForExternal.Object,
                getPropertyProfiles.Object,
                getCompartments.Object,
                agentAuthorityService.Object,
                repoMock.Object,
                habitatMock.Object,
                auditMock.Object,
                requestContext,
                logger.Object);
        }

        private static void SetupUnitOfWork(Mock<IFellingLicenceApplicationExternalRepository> repoMock)
        {
            var uow = new Mock<IUnitOfWork>();
            uow.Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());
            repoMock.SetupGet(r => r.UnitOfWork).Returns(uow.Object);
        }

        [Fact]
        public async Task SetHabitatRestorationStatus_False_ClearsRestorations_AndSetsComplete()
        {
            var appId = Guid.NewGuid();
            var repo = new Mock<IFellingLicenceApplicationExternalRepository>();
            var habitat = new Mock<IHabitatRestorationService>();
            SetupUnitOfWork(repo);

            var app = CreateApplication(appId);
            repo.Setup(r => r.GetAsync(appId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Maybe<FellingLicenceApplication>.From(app));

            habitat.Setup(h => h.DeleteHabitatRestorationsAsync(appId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

            var sut = CreateUseCase(repo, habitat, out var retrieveUserAccountsServiceMock);
            retrieveUserAccountsServiceMock
                .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success(UserAccessModel.SystemUserAccessModel));

            var user = CreateExternalApplicant(Guid.NewGuid());
            var result = await sut.SetHabitatRestorationStatus(user, appId, false, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.True(app.FellingLicenceApplicationStepStatus.HabitatRestorationStatus);
            habitat.Verify(h => h.DeleteHabitatRestorationsAsync(appId, It.IsAny<CancellationToken>()), Times.Once);
            repo.Verify(r => r.Update(app), Times.Once);
        }

        [Fact]
        public async Task SetHabitatRestorationStatus_True_SetsStatusBasedOnCompletion()
        {
            var appId = Guid.NewGuid();
            var repo = new Mock<IFellingLicenceApplicationExternalRepository>();
            var habitat = new Mock<IHabitatRestorationService>();
            SetupUnitOfWork(repo);

            var app = CreateApplication(appId);
            repo.Setup(r => r.GetAsync(appId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Maybe<FellingLicenceApplication>.From(app));

            var restorations = new List<HabitatRestorationModel>
            {
                new HabitatRestorationModel { Completed = true },
                new HabitatRestorationModel { Completed = true }
            };
            habitat.Setup(h => h.GetHabitatRestorationModelsAsync(appId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(restorations);

            var sut = CreateUseCase(repo, habitat, out var retrieveUserAccountsServiceMock);
            retrieveUserAccountsServiceMock
                .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success(UserAccessModel.SystemUserAccessModel));
            var user = CreateExternalApplicant(Guid.NewGuid());

            var result = await sut.SetHabitatRestorationStatus(user, appId, true, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.True(app.FellingLicenceApplicationStepStatus.HabitatRestorationStatus);

            restorations[1].Completed = false;
            result = await sut.SetHabitatRestorationStatus(user, appId, true, CancellationToken.None);
            Assert.False(app.FellingLicenceApplicationStepStatus.HabitatRestorationStatus);
        }

        [Fact]
        public async Task UpdateHabitatCompartments_AddsAndDeletes_AsExpected()
        {
            var appId = Guid.NewGuid();
            var repo = new Mock<IFellingLicenceApplicationExternalRepository>();
            var habitat = new Mock<IHabitatRestorationService>();

            habitat.Setup(h => h.GetHabitatRestorationModelsAsync(appId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<HabitatRestorationModel>
                {
                    new HabitatRestorationModel { PropertyProfileCompartmentId = Guid.Parse("00000000-0000-0000-0000-000000000001") },
                    new HabitatRestorationModel { PropertyProfileCompartmentId = Guid.Parse("00000000-0000-0000-0000-000000000002") }
                });

            habitat.Setup(h => h.DeleteHabitatRestorationAsync(appId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());
            habitat.Setup(h => h.AddHabitatRestorationAsync(appId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

            var sut = CreateUseCase(repo, habitat, out _);
            var selected = new[]
            {
                Guid.Parse("00000000-0000-0000-0000-000000000002"),
                Guid.Parse("00000000-0000-0000-0000-000000000003")
            };

            var result = await sut.UpdateHabitatCompartments(appId, selected, CancellationToken.None);
            Assert.True(result.IsSuccess);
            habitat.Verify(h => h.DeleteHabitatRestorationAsync(appId, Guid.Parse("00000000-0000-0000-0000-000000000001"), It.IsAny<CancellationToken>()), Times.Once);
            habitat.Verify(h => h.AddHabitatRestorationAsync(appId, Guid.Parse("00000000-0000-0000-0000-000000000003"), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateWoodlandSpecies_ClearsNativeBroadleaf_WhenNotBroadleaf()
        {
            var appId = Guid.NewGuid();
            var compId = Guid.NewGuid();
            var repo = new Mock<IFellingLicenceApplicationExternalRepository>();
            var habitat = new Mock<IHabitatRestorationService>();
            SetupUnitOfWork(repo);

            var existing = new HabitatRestorationModel
            {
                Id = Guid.NewGuid(),
                LinkedPropertyProfileId = Guid.NewGuid(),
                PropertyProfileCompartmentId = compId,
                WoodlandSpeciesType = WoodlandSpeciesType.MixedWoodland,
                NativeBroadleaf = true
            };

            habitat.Setup(h => h.GetHabitatRestorationModelAsync(appId, compId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Maybe<HabitatRestorationModel>.From(existing));
            habitat.Setup(h => h.UpdateHabitatRestorationModelAsync(It.IsAny<HabitatRestorationModel>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

            var sut = CreateUseCase(repo, habitat, out var retrieveUserAccountsServiceMock);
            retrieveUserAccountsServiceMock
                .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success(UserAccessModel.SystemUserAccessModel));
            var user = CreateExternalApplicant(Guid.NewGuid());

            var result = await sut.UpdateWoodlandSpecies(appId, compId, WoodlandSpeciesType.ConiferWoodland, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Null(existing.NativeBroadleaf);
            Assert.Equal(WoodlandSpeciesType.ConiferWoodland, existing.WoodlandSpeciesType);
        }

        [Fact]
        public async Task UpdateNativeBroadleaf_SetsValue()
        {
            var appId = Guid.NewGuid();
            var compId = Guid.NewGuid();
            var repo = new Mock<IFellingLicenceApplicationExternalRepository>();
            var habitat = new Mock<IHabitatRestorationService>();

            var existing = new HabitatRestorationModel
            {
                Id = Guid.NewGuid(),
                PropertyProfileCompartmentId = compId,
                NativeBroadleaf = null
            };

            habitat.Setup(h => h.GetHabitatRestorationModelAsync(appId, compId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Maybe<HabitatRestorationModel>.From(existing));
            habitat.Setup(h => h.UpdateHabitatRestorationModelAsync(It.IsAny<HabitatRestorationModel>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

            var sut = CreateUseCase(repo, habitat, out var retrieveUserAccountsServiceMock);
            retrieveUserAccountsServiceMock
                .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success(UserAccessModel.SystemUserAccessModel));
            var user = CreateExternalApplicant(Guid.NewGuid());

            var result = await sut.UpdateNativeBroadleaf(appId, compId, true, CancellationToken.None);
            Assert.True(result.IsSuccess);
            Assert.True(existing.NativeBroadleaf);
        }

        [Fact]
        public async Task GetHabitatNextCompartment_ReturnsFirstIncomplete()
        {
            var appId = Guid.NewGuid();
            var repo = new Mock<IFellingLicenceApplicationExternalRepository>();
            var habitat = new Mock<IHabitatRestorationService>();

            habitat.Setup(h => h.GetHabitatRestorationModelsAsync(appId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<HabitatRestorationModel>
                {
                    new HabitatRestorationModel { PropertyProfileCompartmentId = Guid.Parse("00000000-0000-0000-0000-000000000001"), Completed = true },
                    new HabitatRestorationModel { PropertyProfileCompartmentId = Guid.Parse("00000000-0000-0000-0000-000000000002"), Completed = null },
                    new HabitatRestorationModel { PropertyProfileCompartmentId = Guid.Parse("00000000-0000-0000-0000-000000000003"), Completed = false }
                });

            var sut = CreateUseCase(repo, habitat, out _);
            var maybe = await sut.GetHabitatNextCompartment(appId, CancellationToken.None);
            Assert.True(maybe.HasValue);
            Assert.Equal(Guid.Parse("00000000-0000-0000-0000-000000000002"), maybe.Value);
        }

        [Fact]
        public async Task UpdateHabitatType_UpdatesFields()
        {
            var appId = Guid.NewGuid();
            var compId = Guid.NewGuid();
            var repo = new Mock<IFellingLicenceApplicationExternalRepository>();
            var habitat = new Mock<IHabitatRestorationService>();

            var existing = new HabitatRestorationModel
            {
                Id = Guid.NewGuid(),
                PropertyProfileCompartmentId = compId,
                HabitatType = HabitatType.Fen,
                OtherHabitatDescription = null
            };

            habitat.Setup(h => h.GetHabitatRestorationModelAsync(appId, compId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Maybe<HabitatRestorationModel>.From(existing));
            habitat.Setup(h => h.UpdateHabitatRestorationModelAsync(It.IsAny<HabitatRestorationModel>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

            var sut = CreateUseCase(repo, habitat, out var retrieveUserAccountsServiceMock);
            retrieveUserAccountsServiceMock
                .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success(UserAccessModel.SystemUserAccessModel));
            var user = CreateExternalApplicant(Guid.NewGuid());

            var result = await sut.UpdateHabitatType(appId, compId, HabitatType.LowlandHeathland, "desc", CancellationToken.None);
            Assert.True(result.IsSuccess);
            Assert.Equal(HabitatType.LowlandHeathland, existing.HabitatType);
            Assert.Equal("desc", existing.OtherHabitatDescription);
        }

        [Fact]
        public async Task UpdateFelledEarly_SetsValue()
        {
            var appId = Guid.NewGuid();
            var compId = Guid.NewGuid();
            var repo = new Mock<IFellingLicenceApplicationExternalRepository>();
            var habitat = new Mock<IHabitatRestorationService>();

            var existing = new HabitatRestorationModel
            {
                Id = Guid.NewGuid(),
                PropertyProfileCompartmentId = compId,
                FelledEarly = null
            };

            habitat.Setup(h => h.GetHabitatRestorationModelAsync(appId, compId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Maybe<HabitatRestorationModel>.From(existing));
            habitat.Setup(h => h.UpdateHabitatRestorationModelAsync(It.IsAny<HabitatRestorationModel>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

            var sut = CreateUseCase(repo, habitat, out var retrieveUserAccountsServiceMock);
            retrieveUserAccountsServiceMock
                .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success(UserAccessModel.SystemUserAccessModel));
            var user = CreateExternalApplicant(Guid.NewGuid());

            var result = await sut.UpdateFelledEarly(appId, compId, true, CancellationToken.None);
            Assert.True(result.IsSuccess);
            Assert.True(existing.FelledEarly);
        }

        [Fact]
        public async Task UpdateProductiveWoodland_ClearsFelledEarly_WhenNotTrue()
        {
            var appId = Guid.NewGuid();
            var compId = Guid.NewGuid();
            var repo = new Mock<IFellingLicenceApplicationExternalRepository>();
            var habitat = new Mock<IHabitatRestorationService>();

            var existing = new HabitatRestorationModel
            {
                Id = Guid.NewGuid(),
                LinkedPropertyProfileId = Guid.NewGuid(),
                PropertyProfileCompartmentId = compId,
                ProductiveWoodland = true,
                FelledEarly = true
            };

            habitat.Setup(h => h.GetHabitatRestorationModelAsync(appId, compId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Maybe<HabitatRestorationModel>.From(existing));
            habitat.Setup(h => h.UpdateHabitatRestorationModelAsync(It.IsAny<HabitatRestorationModel>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

            var sut = CreateUseCase(repo, habitat, out var retrieveUserAccountsServiceMock);
            retrieveUserAccountsServiceMock
                .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success(UserAccessModel.SystemUserAccessModel));
            var user = CreateExternalApplicant(Guid.NewGuid());

            var result = await sut.UpdateProductiveWoodland(appId, compId, false, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.False(existing.ProductiveWoodland);
            Assert.Null(existing.FelledEarly);
        }

        [Fact]
        public async Task UpdateCompleted_Success_PublishesAuditSuccess()
        {
            var appId = Guid.NewGuid();
            var compId = Guid.NewGuid();
            var repo = new Mock<IFellingLicenceApplicationExternalRepository>();
            var habitat = new Mock<IHabitatRestorationService>();

            var existing = new HabitatRestorationModel
            {
                Id = Guid.NewGuid(),
                PropertyProfileCompartmentId = compId,
                Completed = null
            };

            habitat.Setup(h => h.GetHabitatRestorationModelAsync(appId, compId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Maybe<HabitatRestorationModel>.From(existing));
            habitat.Setup(h => h.UpdateHabitatRestorationModelAsync(It.IsAny<HabitatRestorationModel>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

            var sut = CreateUseCase(repo, habitat, out _, out var auditMock);

            var result = await sut.UpdateCompleted(appId, compId, true, CancellationToken.None);
            Assert.True(result.IsSuccess);
            Assert.True(existing.Completed);

            auditMock.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                    a.EventName == AuditEvents.HabitatRestorationUpdate && a.SourceEntityId == appId),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateCompleted_Failure_PublishesAuditFailure()
        {
            var appId = Guid.NewGuid();
            var compId = Guid.NewGuid();
            var repo = new Mock<IFellingLicenceApplicationExternalRepository>();
            var habitat = new Mock<IHabitatRestorationService>();

            var existing = new HabitatRestorationModel
            {
                Id = Guid.NewGuid(),
                PropertyProfileCompartmentId = compId,
                Completed = null
            };

            habitat.Setup(h => h.GetHabitatRestorationModelAsync(appId, compId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Maybe<HabitatRestorationModel>.From(existing));
            habitat.Setup(h => h.UpdateHabitatRestorationModelAsync(It.IsAny<HabitatRestorationModel>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(UnitResult.Failure(UserDbErrorReason.General));

            var sut = CreateUseCase(repo, habitat, out _, out var auditMock);

            var result = await sut.UpdateCompleted(appId, compId, true, CancellationToken.None);
            Assert.True(result.IsFailure);

            auditMock.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                    a.EventName == AuditEvents.HabitatRestorationUpdateFailure && a.SourceEntityId == appId),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory, AutoMoqData]
        public async Task EnsureExistingHabitatRestorationRecordsShouldRemoveInvalidOnes(
            List<HabitatRestorationModel> existingRecords,
            UserAccessModel uam)
        {
            var appId = Guid.NewGuid();
            var repo = new Mock<IFellingLicenceApplicationExternalRepository>();
            var getFlaForExternal = new Mock<IGetFellingLicenceApplicationForExternalUsers>();
            var habitat = new Mock<IHabitatRestorationService>();
            Mock<IRetrieveUserAccountsService> userAccounts;
            Mock<IAuditService<HabitatRestorationUseCase>> auditService;

            var user = CreateExternalApplicant(Guid.NewGuid());

            SetupUnitOfWork(repo);

            var app = CreateApplication(appId);
            var existingRecordIds = existingRecords.Select(x => x.PropertyProfileCompartmentId);

            app.LinkedPropertyProfile.ProposedFellingDetails = new List<ProposedFellingDetail>
            {
                new ProposedFellingDetail
                {
                    PropertyProfileCompartmentId = existingRecordIds.FirstOrDefault(),  // first existing record - valid
                    IsRestocking = false
                },
                new ProposedFellingDetail
                {
                    PropertyProfileCompartmentId = Guid.NewGuid(),
                    IsRestocking = true
                },
                new ProposedFellingDetail
                {
                    PropertyProfileCompartmentId = existingRecordIds.LastOrDefault(),   // last existing record - now invalid
                    IsRestocking = true,
                    ProposedRestockingDetails = new List<ProposedRestockingDetail>
                    {
                        new ProposedRestockingDetail
                        {
                            PropertyProfileCompartmentId = Guid.NewGuid(),
                            RestockingProposal = TypeOfProposal.ReplantTheFelledArea
                        }
                    }
                }
            };

            app.IsPriorityOpenHabitat = true;
            app.FellingLicenceApplicationStepStatus.HabitatRestorationStatus = true;

            var stillValidId = existingRecordIds.FirstOrDefault();

            var sut = CreateUseCase(repo, habitat, out userAccounts, out auditService);

            userAccounts
                .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(uam);

            habitat
                .Setup(h => h.GetHabitatRestorationModelsAsync(appId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingRecords);

            repo
                .Setup(x => x.CheckUserCanAccessApplicationAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            repo
                .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(app);

            habitat
                .Setup(x => x.DeleteHabitatRestorationAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

            var result = await sut.EnsureExistingHabitatRestorationRecordsAsync(app.Id, user, CancellationToken.None);

            Assert.True(result.IsSuccess);

            //verify only invalid records are removed
            foreach (var existingItem in existingRecords)
            {
                var compartmentId = existingItem.PropertyProfileCompartmentId;

                if (compartmentId != stillValidId)
                {
                    habitat
                        .Verify(x => 
                                x.DeleteHabitatRestorationAsync(app.Id, compartmentId, It.IsAny<CancellationToken>()), Times.Once);
                }
                else
                {
                    habitat
                        .Verify(x =>
                            x.DeleteHabitatRestorationAsync(app.Id, compartmentId, It.IsAny<CancellationToken>()), Times.Never);
                }
            }

            //verify we didn't change the status incorrectly
            Assert.True(app.IsPriorityOpenHabitat);
            Assert.True(app.FellingLicenceApplicationStepStatus.HabitatRestorationStatus);
            
            repo.Verify(x => x.Update(app), Times.Never);
        }

        [Theory, AutoMoqData]
        public async Task EnsureExistingHabitatRestorationRecordsWhenAllRecordsNowInvalid(
            List<HabitatRestorationModel> existingRecords,
            UserAccessModel uam)
        {
            var appId = Guid.NewGuid();
            var repo = new Mock<IFellingLicenceApplicationExternalRepository>();
            var getFlaForExternal = new Mock<IGetFellingLicenceApplicationForExternalUsers>();
            var habitat = new Mock<IHabitatRestorationService>();
            Mock<IRetrieveUserAccountsService> userAccounts;
            Mock<IAuditService<HabitatRestorationUseCase>> auditService;

            var user = CreateExternalApplicant(Guid.NewGuid());

            SetupUnitOfWork(repo);

            var app = CreateApplication(appId);
            var existingRecordIds = existingRecords.Select(x => x.PropertyProfileCompartmentId);

            app.LinkedPropertyProfile.ProposedFellingDetails = new List<ProposedFellingDetail>
            {
                new ProposedFellingDetail
                {
                    PropertyProfileCompartmentId = existingRecordIds.FirstOrDefault(),  // first existing record - now invalid
                    IsRestocking = true,
                    ProposedRestockingDetails = new List<ProposedRestockingDetail>
                    {
                        new ProposedRestockingDetail
                        {
                            PropertyProfileCompartmentId = Guid.NewGuid(),
                            RestockingProposal = TypeOfProposal.ReplantTheFelledArea
                        }
                    }
                },
                new ProposedFellingDetail
                {
                    PropertyProfileCompartmentId = Guid.NewGuid(),
                    IsRestocking = true
                },
                new ProposedFellingDetail
                {
                    PropertyProfileCompartmentId = existingRecordIds.LastOrDefault(),   // last existing record - now invalid
                    IsRestocking = true,
                    ProposedRestockingDetails = new List<ProposedRestockingDetail>
                    {
                        new ProposedRestockingDetail
                        {
                            PropertyProfileCompartmentId = Guid.NewGuid(),
                            RestockingProposal = TypeOfProposal.ReplantTheFelledArea
                        }
                    }
                }
            };

            app.IsPriorityOpenHabitat = true;
            app.FellingLicenceApplicationStepStatus.HabitatRestorationStatus = true;

            var sut = CreateUseCase(repo, habitat, out userAccounts, out auditService);

            userAccounts
                .Setup(x => x.RetrieveUserAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(uam);

            habitat
                .Setup(h => h.GetHabitatRestorationModelsAsync(appId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingRecords);

            repo
                .Setup(x => x.CheckUserCanAccessApplicationAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            repo
                .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(app);

            habitat
                .Setup(x => x.DeleteHabitatRestorationAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

            var result = await sut.EnsureExistingHabitatRestorationRecordsAsync(app.Id, user, CancellationToken.None);

            Assert.True(result.IsSuccess);

            //verify only invalid records are removed
            foreach (var existingItem in existingRecords)
            {
                var compartmentId = existingItem.PropertyProfileCompartmentId;
                habitat
                    .Verify(x =>
                            x.DeleteHabitatRestorationAsync(app.Id, compartmentId, It.IsAny<CancellationToken>()), Times.Once);
            }

            //verify we didn't change the status incorrectly
            Assert.Null(app.IsPriorityOpenHabitat);
            Assert.Null(app.FellingLicenceApplicationStepStatus.HabitatRestorationStatus);

            repo.Verify(x => x.Update(app), Times.Once);
        }
    }
}
