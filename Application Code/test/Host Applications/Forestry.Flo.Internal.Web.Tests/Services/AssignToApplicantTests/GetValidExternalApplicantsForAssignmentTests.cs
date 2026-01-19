using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Tests.Common;
using LinqKit;
using Moq;

namespace Forestry.Flo.Internal.Web.Tests.Services.AssignToApplicantTests;

public class GetValidExternalApplicantsForAssignmentTests : AssignToApplicantUseCaseTestsBase
{
    [Theory, AutoData]
    public async Task ShouldReturnFailureWhenFellingLicenceApplicationCouldNotBeFound(
        Guid applicationId,
        string returnUrl)
    {
        var sut = CreateSut();

        await RetrieveFlaSummaryShouldReturnFailureWhenFlaCouldNotBeFound(
            async () => await sut.GetValidExternalApplicantsForAssignmentAsync(TestUser, applicationId, returnUrl, CancellationToken.None),
            applicationId);
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnFailureWhenWoodlandOwnerCouldNotBeFound(
        FellingLicenceApplication fla,
        string returnUrl)
    {
        fla.StatusHistories =
        [
            new StatusHistory
            {
                FellingLicenceApplication = fla,
                Status = FellingLicenceStatus.AdminOfficerReview
            }
        ];

        var sut = CreateSut();

        TestUtils.SetProtectedProperty(fla, nameof(fla.Id), Guid.NewGuid());

        await RetrieveFlaSummaryShouldReturnFailureWhenWoodlandOwnerForFlaCouldNotBeFound(
            async () => await sut.GetValidExternalApplicantsForAssignmentAsync(TestUser, fla.Id, returnUrl, CancellationToken.None),
            fla);
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnFailureWhenApplicantAccountCouldNotBeFound(
        FellingLicenceApplication fla,
        WoodlandOwnerModel woodlandOwner,
        AssigneeHistory assigneeHistory,
        UserAccount authorAccount,
        string returnUrl)
    {
        fla.StatusHistories =
        [
            new StatusHistory
            {
                FellingLicenceApplication = fla,
                Status = FellingLicenceStatus.AdminOfficerReview
            }
        ];

        var sut = CreateSut();

        TestUtils.SetProtectedProperty(fla, nameof(fla.Id), Guid.NewGuid());

        await RetrieveFlaSummaryShouldReturnFailureWhenExternalApplicantAccountForAssigneeOnFlaCouldNotBeFound(
            async () => await sut.GetValidExternalApplicantsForAssignmentAsync(TestUser, fla.Id, returnUrl, CancellationToken.None),
            fla,
            woodlandOwner,
            assigneeHistory,
            authorAccount);
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnFailureWhenInternalUserAccountCouldNotBeFound(
        FellingLicenceApplication fla,
        WoodlandOwnerModel woodlandOwner,
        AssigneeHistory assigneeHistory,
        UserAccount authorAccount,
        string returnUrl)
    {
        fla.StatusHistories =
        [
            new StatusHistory
            {
                FellingLicenceApplication = fla,
                Status = FellingLicenceStatus.AdminOfficerReview
            }
        ];

        var sut = CreateSut();

        TestUtils.SetProtectedProperty(fla, nameof(fla.Id), Guid.NewGuid());

        await RetrieveFlaSummaryShouldReturnFailureWhenInternalUserAccountForAssigneeOnFlaCouldNotBeFound(
            async () => await sut.GetValidExternalApplicantsForAssignmentAsync(TestUser, fla.Id, returnUrl, CancellationToken.None),
            fla,
            woodlandOwner,
            assigneeHistory,
            authorAccount);
    }


    [Theory, AutoMoqData]
    public async Task ShouldReturnFailureWhenFlaInWrongState(
        FellingLicenceApplication fla,
        WoodlandOwnerModel woodlandOwner,
        UserAccount externalApplicantAccount,
        Flo.Services.InternalUsers.Entities.UserAccount.UserAccount internalUserAccount,
        List<UserAccountModel> externalApplicantAccountModels,
        string returnUrl)
    {
        FellingLicenceStatus[] errorStates =
        [
            FellingLicenceStatus.Draft, FellingLicenceStatus.Approved, FellingLicenceStatus.SentForApproval,
            FellingLicenceStatus.Received, FellingLicenceStatus.WithApplicant, FellingLicenceStatus.ReturnedToApplicant,
            FellingLicenceStatus.ApprovedInError, FellingLicenceStatus.ReferredToLocalAuthority,
            FellingLicenceStatus.Refused, FellingLicenceStatus.Withdrawn
        ];

        TestUtils.SetProtectedProperty(fla, nameof(fla.Id), Guid.NewGuid());
        TestUtils.SetProtectedProperty(externalApplicantAccount, nameof(externalApplicantAccount.Id), Guid.NewGuid());
        TestUtils.SetProtectedProperty(woodlandOwner, nameof(woodlandOwner.Id), fla.WoodlandOwnerId);

        fla.LinkedPropertyProfile.ProposedFellingDetails = new List<ProposedFellingDetail>();

        externalApplicantAccountModels.First().UserAccountId = externalApplicantAccount.Id;
        fla.CreatedById = externalApplicantAccount.Id;

        foreach (var currentStatus in errorStates)
        {
            MockFlaRepository.Reset();
            MockAuditService.Reset();

            fla.StatusHistories =
            [
                new StatusHistory
                {
                    FellingLicenceApplication = fla,
                    Status = currentStatus
                }
            ];

            var sut = CreateSut();

            MockFlaRepository.Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Maybe<FellingLicenceApplication>.From(fla));

            MockWoodlandOwnerService.Setup(x =>
                    x.RetrieveWoodlandOwnerByIdAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success(woodlandOwner));

            MockRetrieveUserAccountsService.Setup(x =>
                    x.RetrieveUserAccountEntityByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success(externalApplicantAccount));

            MockInternalUserAccountService
                .Setup(x => x.GetUserAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Maybe<Flo.Services.InternalUsers.Entities.UserAccount.UserAccount>.From(internalUserAccount));

            MockRetrieveUserAccountsService
                .Setup(x => x.IsUserAccountLinkedToFcAgencyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success(false));

            MockRetrieveUserAccountsService
                .Setup(x => x.RetrieveUserAccountsForFcAgencyAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success(externalApplicantAccountModels));

            var result = await sut.GetValidExternalApplicantsForAssignmentAsync(TestUser, fla.Id, returnUrl,
                CancellationToken.None);


            Assert.False(result.IsSuccess);

            MockFlaRepository.Verify(x => x.GetAsync(fla.Id, It.IsAny<CancellationToken>()), Times.Once);
            MockAuditService.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(y => y.EventName == AuditEvents.AssignFellingLicenceApplicationToApplicantFailure), It.IsAny<CancellationToken>()), Times.Once);
        }

    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnSuccessWithFcAuthoredApplication_WhenUserIsStillActive(
        FellingLicenceApplication fla,
        WoodlandOwnerModel woodlandOwner,
        UserAccount externalApplicantAccount,
        Flo.Services.InternalUsers.Entities.UserAccount.UserAccount internalUserAccount,
        AssigneeHistory externalAssigneeHistory,
        AssigneeHistory internalAssigneeHistory,
        List<UserAccountModel> externalApplicantAccountModels,
        string returnUrl)
    {
        var sut = CreateSut();

        TestUtils.SetProtectedProperty(fla, nameof(fla.Id), Guid.NewGuid());
        TestUtils.SetProtectedProperty(externalApplicantAccount, nameof(externalApplicantAccount.Id), Guid.NewGuid());
        TestUtils.SetProtectedProperty(woodlandOwner, nameof(woodlandOwner.Id), fla.WoodlandOwnerId);

        fla.LinkedPropertyProfile.ProposedFellingDetails = new List<ProposedFellingDetail>();

        externalApplicantAccountModels.First().UserAccountId = externalApplicantAccount.Id;
        fla.CreatedById = externalApplicantAccount.Id;

        fla.StatusHistories =
        [
            new StatusHistory
            {
                FellingLicenceApplication = fla,
                Status = FellingLicenceStatus.AdminOfficerReview
            }
        ];

        await RetrieveFlaSummaryShouldReturnSuccessWhenDetailsRetrieved(
            async () =>
            {
                var result = await sut.GetValidExternalApplicantsForAssignmentAsync(TestUser, fla.Id, returnUrl,
                    CancellationToken.None);

                Assert.NotNull(result.Value);
                Assert.False(result.Value.ShowListOfUsers);
                Assert.Equal(fla.CreatedById, result.Value.ExternalApplicantId);
                Assert.Equal(externalApplicantAccountModels.Count, result.Value.ExternalApplicants.Count);
                externalApplicantAccountModels.ForEach(m => Assert.True(result.Value.ExternalApplicants.Contains(m)));

                return result;
            },
            fla,
            woodlandOwner,
            externalApplicantAccount,
            internalUserAccount,
            externalAssigneeHistory,
            internalAssigneeHistory,
            true,
            externalApplicantAccountModels);
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnSuccessWithFcAuthoredApplication_WhenUserIsNotStillActive(
        FellingLicenceApplication fla,
        WoodlandOwnerModel woodlandOwner,
        UserAccount externalApplicantAccount,
        Flo.Services.InternalUsers.Entities.UserAccount.UserAccount internalUserAccount,
        AssigneeHistory externalAssigneeHistory,
        AssigneeHistory internalAssigneeHistory,
        List<UserAccountModel> externalApplicantAccountModels,
        string returnUrl)
    {
        var sut = CreateSut();

        TestUtils.SetProtectedProperty(fla, nameof(fla.Id), Guid.NewGuid());
        TestUtils.SetProtectedProperty(externalApplicantAccount, nameof(externalApplicantAccount.Id), Guid.NewGuid());
        TestUtils.SetProtectedProperty(woodlandOwner, nameof(woodlandOwner.Id), fla.WoodlandOwnerId);

        fla.LinkedPropertyProfile.ProposedFellingDetails = new List<ProposedFellingDetail>();
        fla.CreatedById = Guid.NewGuid();

        fla.StatusHistories =
        [
            new StatusHistory
            {
                FellingLicenceApplication = fla,
                Status = FellingLicenceStatus.AdminOfficerReview
            }
        ];

        await RetrieveFlaSummaryShouldReturnSuccessWhenDetailsRetrieved(
            async () =>
            {
                var result = await sut.GetValidExternalApplicantsForAssignmentAsync(TestUser, fla.Id, returnUrl,
                    CancellationToken.None);

                Assert.NotNull(result.Value);
                Assert.True(result.Value.ShowListOfUsers);
                Assert.Null(result.Value.ExternalApplicantId);
                Assert.Equal(externalApplicantAccountModels.Count, result.Value.ExternalApplicants.Count);
                externalApplicantAccountModels.ForEach(m => Assert.True(result.Value.ExternalApplicants.Contains(m)));

                return result;
            },
            fla,
            woodlandOwner,
            externalApplicantAccount,
            internalUserAccount,
            externalAssigneeHistory,
            internalAssigneeHistory,
            true,
            externalApplicantAccountModels);
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnSuccessWithApplicantAuthoredApplication_WhenUserIsNotStillActive_MultipleApplicants(
        FellingLicenceApplication fla,
        WoodlandOwnerModel woodlandOwner,
        UserAccount externalApplicantAccount,
        Flo.Services.InternalUsers.Entities.UserAccount.UserAccount internalUserAccount,
        AssigneeHistory externalAssigneeHistory,
        AssigneeHistory internalAssigneeHistory,
        List<UserAccountModel> externalApplicantAccountModels,
        string returnUrl)
    {
        var sut = CreateSut();

        TestUtils.SetProtectedProperty(fla, nameof(fla.Id), Guid.NewGuid());
        TestUtils.SetProtectedProperty(externalApplicantAccount, nameof(externalApplicantAccount.Id), Guid.NewGuid());
        TestUtils.SetProtectedProperty(woodlandOwner, nameof(woodlandOwner.Id), fla.WoodlandOwnerId);

        fla.LinkedPropertyProfile.ProposedFellingDetails = new List<ProposedFellingDetail>();
        fla.CreatedById = Guid.NewGuid();

        fla.StatusHistories =
        [
            new StatusHistory
            {
                FellingLicenceApplication = fla,
                Status = FellingLicenceStatus.AdminOfficerReview
            }
        ];

        await RetrieveFlaSummaryShouldReturnSuccessWhenDetailsRetrieved(
            async () =>
            {
                var result = await sut.GetValidExternalApplicantsForAssignmentAsync(TestUser, fla.Id, returnUrl,
                    CancellationToken.None);

                Assert.NotNull(result.Value);
                Assert.True(result.Value.ShowListOfUsers);
                Assert.Null(result.Value.ExternalApplicantId);
                Assert.Equal(externalApplicantAccountModels.Count, result.Value.ExternalApplicants.Count);
                externalApplicantAccountModels.ForEach(m => Assert.True(result.Value.ExternalApplicants.Contains(m)));

                return result;
            },
            fla,
            woodlandOwner,
            externalApplicantAccount,
            internalUserAccount,
            externalAssigneeHistory,
            internalAssigneeHistory,
            false,
            externalApplicantAccountModels);
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnSuccessWithApplicantAuthoredApplication_WhenUserIsNotStillActive_SingleApplicant(
        FellingLicenceApplication fla,
        WoodlandOwnerModel woodlandOwner,
        UserAccount externalApplicantAccount,
        Flo.Services.InternalUsers.Entities.UserAccount.UserAccount internalUserAccount,
        AssigneeHistory externalAssigneeHistory,
        AssigneeHistory internalAssigneeHistory,
        UserAccountModel externalApplicantAccountModel,
        string returnUrl)
    {
        var sut = CreateSut();

        TestUtils.SetProtectedProperty(fla, nameof(fla.Id), Guid.NewGuid());
        TestUtils.SetProtectedProperty(externalApplicantAccount, nameof(externalApplicantAccount.Id), Guid.NewGuid());
        TestUtils.SetProtectedProperty(woodlandOwner, nameof(woodlandOwner.Id), fla.WoodlandOwnerId);

        fla.LinkedPropertyProfile.ProposedFellingDetails = new List<ProposedFellingDetail>();
        fla.CreatedById = Guid.NewGuid();

        fla.StatusHistories =
        [
            new StatusHistory
            {
                FellingLicenceApplication = fla,
                Status = FellingLicenceStatus.AdminOfficerReview
            }
        ];

        await RetrieveFlaSummaryShouldReturnSuccessWhenDetailsRetrieved(
            async () =>
            {
                var result = await sut.GetValidExternalApplicantsForAssignmentAsync(TestUser, fla.Id, returnUrl,
                    CancellationToken.None);

                Assert.NotNull(result.Value);
                Assert.False(result.Value.ShowListOfUsers);
                Assert.Equal(externalApplicantAccountModel.UserAccountId, result.Value.ExternalApplicantId);
                Assert.Equal(externalApplicantAccountModel, result.Value.ExternalApplicants.Single());

                return result;
            },
            fla,
            woodlandOwner,
            externalApplicantAccount,
            internalUserAccount,
            externalAssigneeHistory,
            internalAssigneeHistory,
            false,
            new List<UserAccountModel> {externalApplicantAccountModel});
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnSuccessWithApplicantAuthoredApplication_WhenUserIsStillActive_MultipleApplicants(
        FellingLicenceApplication fla,
        WoodlandOwnerModel woodlandOwner,
        UserAccount externalApplicantAccount,
        Flo.Services.InternalUsers.Entities.UserAccount.UserAccount internalUserAccount,
        AssigneeHistory externalAssigneeHistory,
        AssigneeHistory internalAssigneeHistory,
        List<UserAccountModel> externalApplicantAccountModels,
        string returnUrl)
    {
        var sut = CreateSut();

        TestUtils.SetProtectedProperty(fla, nameof(fla.Id), Guid.NewGuid());
        TestUtils.SetProtectedProperty(externalApplicantAccount, nameof(externalApplicantAccount.Id), Guid.NewGuid());
        TestUtils.SetProtectedProperty(woodlandOwner, nameof(woodlandOwner.Id), fla.WoodlandOwnerId);

        fla.LinkedPropertyProfile.ProposedFellingDetails = new List<ProposedFellingDetail>();
        fla.CreatedById = externalApplicantAccount.Id;
        externalApplicantAccountModels.First().UserAccountId = externalApplicantAccount.Id;

        fla.StatusHistories =
        [
            new StatusHistory
            {
                FellingLicenceApplication = fla,
                Status = FellingLicenceStatus.AdminOfficerReview
            }
        ];

        await RetrieveFlaSummaryShouldReturnSuccessWhenDetailsRetrieved(
            async () =>
            {
                var result = await sut.GetValidExternalApplicantsForAssignmentAsync(TestUser, fla.Id, returnUrl,
                    CancellationToken.None);

                Assert.NotNull(result.Value);
                Assert.True(result.Value.ShowListOfUsers);
                Assert.Equal(fla.CreatedById, result.Value.ExternalApplicantId);
                Assert.Equal(externalApplicantAccountModels.Count, result.Value.ExternalApplicants.Count);
                externalApplicantAccountModels.ForEach(m => Assert.True(result.Value.ExternalApplicants.Contains(m)));

                return result;
            },
            fla,
            woodlandOwner,
            externalApplicantAccount,
            internalUserAccount,
            externalAssigneeHistory,
            internalAssigneeHistory,
            false,
            externalApplicantAccountModels);
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnSuccessWithPawsOption_WhenApplicationHasPaws(
        FellingLicenceApplication fla,
        WoodlandOwnerModel woodlandOwner,
        UserAccount externalApplicantAccount,
        Flo.Services.InternalUsers.Entities.UserAccount.UserAccount internalUserAccount,
        AssigneeHistory externalAssigneeHistory,
        AssigneeHistory internalAssigneeHistory,
        List<UserAccountModel> externalApplicantAccountModels,
        string returnUrl)
    {
        var sut = CreateSut();

        TestUtils.SetProtectedProperty(fla, nameof(fla.Id), Guid.NewGuid());
        TestUtils.SetProtectedProperty(externalApplicantAccount, nameof(externalApplicantAccount.Id), Guid.NewGuid());
        TestUtils.SetProtectedProperty(woodlandOwner, nameof(woodlandOwner.Id), fla.WoodlandOwnerId);

        fla.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments
            .ForEach(x => x.SubmittedCompartmentDesignations.Paws = true);

        fla.LinkedPropertyProfile.ProposedFellingDetails = new List<ProposedFellingDetail>();
        fla.CreatedById = externalApplicantAccount.Id;
        externalApplicantAccountModels.First().UserAccountId = externalApplicantAccount.Id;
        
        fla.StatusHistories =
        [
            new StatusHistory
            {
                FellingLicenceApplication = fla,
                Status = FellingLicenceStatus.AdminOfficerReview
            }
        ];

        await RetrieveFlaSummaryShouldReturnSuccessWhenDetailsRetrieved(
            async () =>
            {
                var result = await sut.GetValidExternalApplicantsForAssignmentAsync(TestUser, fla.Id, returnUrl,
                    CancellationToken.None);

                Assert.NotNull(result.Value);
                Assert.Contains(result.Value.SectionsToReview.Keys,
                    x => x == FellingLicenceApplicationSection.PawsAndIawp);

                return result;
            },
            fla,
            woodlandOwner,
            externalApplicantAccount,
            internalUserAccount,
            externalAssigneeHistory,
            internalAssigneeHistory,
            false,
            externalApplicantAccountModels);
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnSuccessWithoutPawsOption_WhenApplicationHasNoPaws(
        FellingLicenceApplication fla,
        WoodlandOwnerModel woodlandOwner,
        UserAccount externalApplicantAccount,
        Flo.Services.InternalUsers.Entities.UserAccount.UserAccount internalUserAccount,
        AssigneeHistory externalAssigneeHistory,
        AssigneeHistory internalAssigneeHistory,
        List<UserAccountModel> externalApplicantAccountModels,
        string returnUrl)
    {
        var sut = CreateSut();

        TestUtils.SetProtectedProperty(fla, nameof(fla.Id), Guid.NewGuid());
        TestUtils.SetProtectedProperty(externalApplicantAccount, nameof(externalApplicantAccount.Id), Guid.NewGuid());
        TestUtils.SetProtectedProperty(woodlandOwner, nameof(woodlandOwner.Id), fla.WoodlandOwnerId);

        fla.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments
            .ForEach(x => x.SubmittedCompartmentDesignations.Paws = false);

        fla.LinkedPropertyProfile.ProposedFellingDetails = new List<ProposedFellingDetail>();
        fla.CreatedById = externalApplicantAccount.Id;
        externalApplicantAccountModels.First().UserAccountId = externalApplicantAccount.Id;

        fla.StatusHistories =
        [
            new StatusHistory
            {
                FellingLicenceApplication = fla,
                Status = FellingLicenceStatus.AdminOfficerReview
            }
        ];

        await RetrieveFlaSummaryShouldReturnSuccessWhenDetailsRetrieved(
            async () =>
            {
                var result = await sut.GetValidExternalApplicantsForAssignmentAsync(TestUser, fla.Id, returnUrl,
                    CancellationToken.None);

                Assert.NotNull(result.Value);
                Assert.DoesNotContain(result.Value.SectionsToReview.Keys,
                    x => x == FellingLicenceApplicationSection.PawsAndIawp);

                return result;
            },
            fla,
            woodlandOwner,
            externalApplicantAccount,
            internalUserAccount,
            externalAssigneeHistory,
            internalAssigneeHistory,
            false,
            externalApplicantAccountModels);
    }
}