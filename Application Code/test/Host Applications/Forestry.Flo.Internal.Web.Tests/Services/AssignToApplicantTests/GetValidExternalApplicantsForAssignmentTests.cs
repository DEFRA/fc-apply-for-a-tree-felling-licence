using AutoFixture.Xunit2;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Entities.WoodlandOwner;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Tests.Common;

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

        fla.SubmittedFlaPropertyDetail = null;
        fla.LinkedPropertyProfile.ProposedFellingDetails = new List<ProposedFellingDetail>();

        externalApplicantAccountModels.First().UserAccountId = externalApplicantAccount.Id;
        fla.CreatedById = externalApplicantAccount.Id;

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

        fla.SubmittedFlaPropertyDetail = null;
        fla.LinkedPropertyProfile.ProposedFellingDetails = new List<ProposedFellingDetail>();
        fla.CreatedById = Guid.NewGuid();

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

        fla.SubmittedFlaPropertyDetail = null;
        fla.LinkedPropertyProfile.ProposedFellingDetails = new List<ProposedFellingDetail>();
        fla.CreatedById = Guid.NewGuid();

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

        fla.SubmittedFlaPropertyDetail = null;
        fla.LinkedPropertyProfile.ProposedFellingDetails = new List<ProposedFellingDetail>();
        fla.CreatedById = Guid.NewGuid();

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

        fla.SubmittedFlaPropertyDetail = null;
        fla.LinkedPropertyProfile.ProposedFellingDetails = new List<ProposedFellingDetail>();
        fla.CreatedById = externalApplicantAccount.Id;
        externalApplicantAccountModels.First().UserAccountId = externalApplicantAccount.Id;

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
}