using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Tests.Common;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services.AdminOfficerReview;

public class UpdateAdminOfficerReviewServiceStatusConfirmationTests : UpdateAdminOfficerReviewServiceTestsBase
{
    private readonly Guid _userId = Guid.NewGuid();

    [Theory]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(false, true)]
    public async Task UpdatesAdminOfficerReviewStatus_WhenSuccessful(bool isChecked, bool isAgencyApplication)
    {
        var adminOfficerReview = new Entities.AdminOfficerReview
        {
            AgentAuthorityFormChecked = !isChecked,
            MappingChecked = false,
            ConstraintsChecked = false,
            AdminOfficerReviewComplete = false
        };
        
        var application = await CreateAndSaveAdminOfficerReviewApplicationAsync(FellingLicenceStatus.AdminOfficerReview, adminOfficerReview, _userId);

        var result = await Sut.SetAgentAuthorityCheckCompletionAsync(
            application.Id,
            isAgencyApplication,
            _userId, 
            isChecked, 
            CancellationToken.None)
            .ConfigureAwait(false);

        Assert.True(result.IsSuccess);

        var updatedRecord =
            FellingLicenceApplicationsContext.FellingLicenceApplications.FirstOrDefault(x => x.Id == application.Id);

        Assert.NotNull(updatedRecord);
        Assert.Equal(isChecked, updatedRecord!.AdminOfficerReview!.AgentAuthorityFormChecked);
        Assert.Equal(adminOfficerReview.MappingChecked, updatedRecord!.AdminOfficerReview!.MappingChecked);
        Assert.Equal(adminOfficerReview.ConstraintsChecked, updatedRecord!.AdminOfficerReview!.ConstraintsChecked);
        Assert.Equal(_userId, updatedRecord!.AdminOfficerReview!.LastUpdatedById);
        Assert.Equal(Now, updatedRecord!.AdminOfficerReview!.LastUpdatedDate);
    }

    [Theory]
    [AutoMoqData]
    public async Task ReturnsFailure_WhenApplicationNotFound(FellingLicenceApplication application)
    {
        var result = await Sut.SetAgentAuthorityCheckCompletionAsync(
                application.Id,
                false,
                _userId,
                true,
                CancellationToken.None)
            .ConfigureAwait(false);

        Assert.True(result.IsFailure);

        Assert.Empty(FellingLicenceApplicationsContext.FellingLicenceApplications);
    }

    [Theory]
    [InlineData(FellingLicenceStatus.Withdrawn, true)]
    [InlineData(FellingLicenceStatus.Approved, true)]
    [InlineData(FellingLicenceStatus.Refused, true)]
    [InlineData(FellingLicenceStatus.Received, true)]
    [InlineData(FellingLicenceStatus.SentForApproval, true)]
    [InlineData(FellingLicenceStatus.WoodlandOfficerReview, true)]
    [InlineData(FellingLicenceStatus.WithApplicant, true)]
    [InlineData(FellingLicenceStatus.Draft, true)]
    [InlineData(FellingLicenceStatus.Submitted, true)]
    [InlineData(FellingLicenceStatus.ReturnedToApplicant, true)]
    [InlineData(FellingLicenceStatus.Withdrawn, false)]
    [InlineData(FellingLicenceStatus.Approved, false)]
    [InlineData(FellingLicenceStatus.Refused, false)]
    [InlineData(FellingLicenceStatus.Received, false)]
    [InlineData(FellingLicenceStatus.SentForApproval, false)]
    [InlineData(FellingLicenceStatus.WoodlandOfficerReview, false)]
    [InlineData(FellingLicenceStatus.WithApplicant, false)]
    [InlineData(FellingLicenceStatus.Draft, false)]
    [InlineData(FellingLicenceStatus.Submitted, false)]
    [InlineData(FellingLicenceStatus.ReturnedToApplicant, false)]
    public async Task ReturnsFailure_WhenApplicationNotInAdminOfficerReview(FellingLicenceStatus status, bool isAgencyApplication)
    {
        var application = await CreateAndSaveAdminOfficerReviewApplicationAsync(status, performingUserId: _userId);

        var result = await Sut.SetAgentAuthorityCheckCompletionAsync(
                application.Id,
                isAgencyApplication,
                _userId,
                true,
                CancellationToken.None)
            .ConfigureAwait(false);

        Assert.True(result.IsFailure);

        var updatedRecord =
            FellingLicenceApplicationsContext.FellingLicenceApplications.FirstOrDefault(x => x.Id == application.Id);

        Assert.NotNull(updatedRecord);
        Assert.False(updatedRecord!.AdminOfficerReview!.AgentAuthorityFormChecked);
    }

    [Theory, CombinatorialData]
    public async Task ReturnsFailure_WhenAdminOfficerReviewIsComplete(bool isAgencyApplication)
    {
        var adminOfficerReview = new Entities.AdminOfficerReview
        {
            AgentAuthorityFormChecked = true,
            MappingChecked = true,
            ConstraintsChecked = true,
            AdminOfficerReviewComplete = true
        };

        var application = await CreateAndSaveAdminOfficerReviewApplicationAsync(FellingLicenceStatus.AdminOfficerReview, adminOfficerReview, performingUserId: _userId);

        var result = await Sut.SetAgentAuthorityCheckCompletionAsync(
                application.Id,
                isAgencyApplication,
                _userId,
                true,
                CancellationToken.None)
            .ConfigureAwait(false);

        Assert.True(result.IsFailure);
    }

    [Theory, CombinatorialData]
    public async Task UpdatesTheCorrectField_WhenSuccessful(UpdateAdminOfficerReviewService.AdminOfficerReviewSections section, bool isAgencyApplication)
    {
        var application = await CreateAndSaveAdminOfficerReviewApplicationAsync(FellingLicenceStatus.AdminOfficerReview, performingUserId: _userId);

        if (section is UpdateAdminOfficerReviewService.AdminOfficerReviewSections.ConstraintsCheck)
        {
            application.AdminOfficerReview.AgentAuthorityCheckPassed = true;
            application.AdminOfficerReview.MappingCheckPassed = true;

            await FellingLicenceApplicationsContext.SaveEntitiesAsync();
        }

        var result = section switch
        {
            UpdateAdminOfficerReviewService.AdminOfficerReviewSections.AgentAuthorityForm =>
                await Sut.SetAgentAuthorityCheckCompletionAsync(
                        application.Id,
                        isAgencyApplication,
                        _userId,
                        true,
                        CancellationToken.None)
                    .ConfigureAwait(false),
            UpdateAdminOfficerReviewService.AdminOfficerReviewSections.MappingCheck =>
                await Sut.SetMappingCheckCompletionAsync(
                        application.Id,
                        isAgencyApplication,
                        _userId,
                        true,
                        CancellationToken.None)
                    .ConfigureAwait(false),
            UpdateAdminOfficerReviewService.AdminOfficerReviewSections.ConstraintsCheck =>
                await Sut.SetConstraintsCheckCompletionAsync(
                        application.Id,
                        isAgencyApplication,
                        _userId,
                        true,
                        CancellationToken.None)
                    .ConfigureAwait(false),
            UpdateAdminOfficerReviewService.AdminOfficerReviewSections.LarchCheck =>
                await Sut.SetLarchCheckCompletionAsync(
                        application.Id,
                        isAgencyApplication,
                        _userId,
                        true,
                        CancellationToken.None)
                    .ConfigureAwait(false),
            UpdateAdminOfficerReviewService.AdminOfficerReviewSections.CBWCheck =>
                await Sut.SetCBWCheckCompletionAsync(
                        application.Id,
                        isAgencyApplication,
                        _userId,
                        true,
                        CancellationToken.None)
                    .ConfigureAwait(false),
            UpdateAdminOfficerReviewService.AdminOfficerReviewSections.EiaCheck =>
                await Sut.SetEiaCheckCompletionAsync(
                        application.Id,
                        isAgencyApplication,
                        _userId,
                        true,
                        CancellationToken.None)
                    .ConfigureAwait(false),
            _ => throw new ArgumentOutOfRangeException(nameof(section), section, null)
        };
        
        Assert.True(result.IsSuccess);

        var updatedRecord =
            FellingLicenceApplicationsContext.FellingLicenceApplications.FirstOrDefault(x => x.Id == application.Id);

        Assert.NotNull(updatedRecord);
        Assert.Equal(section is UpdateAdminOfficerReviewService.AdminOfficerReviewSections.AgentAuthorityForm, updatedRecord!.AdminOfficerReview!.AgentAuthorityFormChecked);
        Assert.Equal(section is UpdateAdminOfficerReviewService.AdminOfficerReviewSections.MappingCheck, updatedRecord!.AdminOfficerReview!.MappingChecked);
        Assert.Equal(section is UpdateAdminOfficerReviewService.AdminOfficerReviewSections.ConstraintsCheck, updatedRecord!.AdminOfficerReview!.ConstraintsChecked);
        Assert.Equal(_userId, updatedRecord!.AdminOfficerReview!.LastUpdatedById);
        Assert.Equal(Now, updatedRecord!.AdminOfficerReview!.LastUpdatedDate);
    }

    [Theory, CombinatorialData]
    public async Task ReturnsFailure_WhenSettingConstraintsCheckCompletionPriorToOthers(bool isAgencyApplication)
    {
        var application = await CreateAndSaveAdminOfficerReviewApplicationAsync(FellingLicenceStatus.AdminOfficerReview, performingUserId: _userId);

        var result = await Sut.SetConstraintsCheckCompletionAsync(
                application.Id,
                isAgencyApplication,
                _userId,
                true,
                CancellationToken.None)
            .ConfigureAwait(false);

        Assert.True(result.IsFailure);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(null)]
    public async Task SetConstraintsCompleteForAgencyApplication_ReturnsFailure_WhenAgentAuthChecksNotPassedOrIsNull(bool? passedValue)
    {
        const bool isAgencyApplication = true;

        var application = await CreateAndSaveAdminOfficerReviewApplicationAsync(
            FellingLicenceStatus.AdminOfficerReview, 
            performingUserId: _userId, 
            agentAuthorityCheckPassed:passedValue, 
            mappingCheckPassed:true);

        var result = await Sut.SetConstraintsCheckCompletionAsync(
                application.Id,
                isAgencyApplication,
                _userId,
                true,
                CancellationToken.None)
            .ConfigureAwait(false);

        Assert.True(result.IsFailure);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(null)]
    public async Task SetConstraintsCompleteForAgencyApplication_ReturnsFailure_WhenMappingChecksNotPassedOrIsNull(bool? passedValue)
    {
        const bool isAgencyApplication = true;

        var application = await CreateAndSaveAdminOfficerReviewApplicationAsync(
            FellingLicenceStatus.AdminOfficerReview,
            performingUserId: _userId,
            agentAuthorityCheckPassed: true,
            mappingCheckPassed: passedValue);

        var result = await Sut.SetConstraintsCheckCompletionAsync(
                application.Id,
                isAgencyApplication,
                _userId,
                true,
                CancellationToken.None)
            .ConfigureAwait(false);

        Assert.True(result.IsFailure);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(null)]
    public async Task SetConstraintsCompleteForAgencyApplication_ReturnsFailure_WhenAgentAuthChecksNotPassedOrIsNullAndMappingChecksNotPassedOrIsNull(bool? passedValue)
    {
        const bool isAgencyApplication = true;

        var application = await CreateAndSaveAdminOfficerReviewApplicationAsync(
            FellingLicenceStatus.AdminOfficerReview,
            performingUserId: _userId,
            agentAuthorityCheckPassed: passedValue,
            mappingCheckPassed: passedValue);

        var result = await Sut.SetConstraintsCheckCompletionAsync(
                application.Id,
                isAgencyApplication,
                _userId,
                true,
                CancellationToken.None)
            .ConfigureAwait(false);

        Assert.True(result.IsFailure);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(null)]
    public async Task SetConstraintsCompleteForWoodlandOwnerApplication_ReturnsSuccess_WhenAgentAuthChecksNotPassedOrIsNull(bool? passedValue)
    {
        const bool isAgencyApplication = false;

        var application = await CreateAndSaveAdminOfficerReviewApplicationAsync(
            FellingLicenceStatus.AdminOfficerReview,
            performingUserId: _userId,
            agentAuthorityCheckPassed: passedValue,
            mappingCheckPassed: true);

        var result = await Sut.SetConstraintsCheckCompletionAsync(
                application.Id,
                isAgencyApplication,
                _userId,
                true,
                CancellationToken.None)
            .ConfigureAwait(false);

        Assert.True(result.IsSuccess);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(null)]
    public async Task SetConstraintsCompleteForWoodlandOwnerApplication_ReturnsFailure_WhenMappingChecksNotPassedOrIsNull(bool? passedValue)
    {
        const bool isAgencyApplication = false;

        var application = await CreateAndSaveAdminOfficerReviewApplicationAsync(
            FellingLicenceStatus.AdminOfficerReview,
            performingUserId: _userId,
            agentAuthorityCheckPassed: false,
            mappingCheckPassed: passedValue);

        var result = await Sut.SetConstraintsCheckCompletionAsync(
                application.Id,
                isAgencyApplication,
                _userId,
                true,
                CancellationToken.None)
            .ConfigureAwait(false);

        Assert.True(result.IsFailure);
    }
}