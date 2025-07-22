using AutoFixture.Xunit2;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Entities.WoodlandOwner;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Tests.Common;

namespace Forestry.Flo.Internal.Web.Tests.Services.AssignToUserUseCaseTests;

public class ConfirmReassignApplicationForRoleTests : AssignToUserUseCaseTestsBase
{
    [Theory, AutoData]
    public async Task ShouldReturnFailureWhenFellingLicenceApplicationCouldNotBeFound(
        Guid applicationId, 
        AssignedUserRole role,
        string returnUrl)
    {
        var sut = CreateSut();

        await RetrieveFlaSummaryShouldReturnFailureWhenFlaCouldNotBeFound(
            async () => await sut.ConfirmReassignApplicationForRole(applicationId, role, returnUrl, _testUser, CancellationToken.None),
            applicationId);
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnFailureWhenWoodlandOwnerForFlaCouldNotBeFound(
        FellingLicenceApplication fla, 
        AssignedUserRole role,
        string returnUrl)
    {
        var sut = CreateSut();

        await RetrieveFlaSummaryShouldReturnFailureWhenWoodlandOwnerForFlaCouldNotBeFound(
            async () => await sut.ConfirmReassignApplicationForRole(fla.Id, role, returnUrl, _testUser, CancellationToken.None),
            fla);
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnFailureWhenInternalUserAccountForAssigneeOnFlaCouldNotBeFound(
        FellingLicenceApplication fla,
        WoodlandOwnerModel woodlandOwner,
        AssignedUserRole role,
        AssigneeHistory assigneeHistory,
        string returnUrl,
        UserAccount createdByUser)
    {
        var sut = CreateSut();
        
        await RetrieveFlaSummaryShouldReturnFailureWhenInternalUserAccountForAssigneeOnFlaCouldNotBeFound(
            async () => await sut.ConfirmReassignApplicationForRole(fla.Id, role, returnUrl, _testUser, CancellationToken.None),
            fla,
            woodlandOwner,
            createdByUser,
            assigneeHistory);
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnSuccessWhenDetailsRetrieved(
        FellingLicenceApplication fla,
        WoodlandOwnerModel woodlandOwner,
        UserAccount externalApplicantAccount,
        Flo.Services.InternalUsers.Entities.UserAccount.UserAccount internalUserAccount,
        AssigneeHistory externalAssigneeHistory,
        AssigneeHistory internalAssigneeHistory,
        AssignedUserRole role,
        string returnUrl)
    {
        var sut = CreateSut();

        fla.LinkedPropertyProfile.ProposedFellingDetails = new List<ProposedFellingDetail>();

        var result = await RetrieveFlaSummaryShouldReturnSuccessWhenDetailsRetrieved(
            async () => await sut.ConfirmReassignApplicationForRole(fla.Id, role, returnUrl, _testUser, CancellationToken.None),
            fla,
            woodlandOwner,
            externalApplicantAccount,
            internalUserAccount,
            externalAssigneeHistory,
            internalAssigneeHistory);

        Assert.Equal(role, result.Value.SelectedRole);
        Assert.Equal(returnUrl, result.Value.ReturnUrl);
    }
}