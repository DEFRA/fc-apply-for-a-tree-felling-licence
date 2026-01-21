using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Tests.Common;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services.AdminOfficerReview;

public class UpdateAdminOfficerReviewServiceConfirmTreeHealthCheckTests : UpdateAdminOfficerReviewServiceTestsBase
{
    [Theory, AutoMoqData]
    public async Task ReturnsFailure_WhenApplicationNotFound(Guid applicationId, Guid userId)
    {
        var result = await Sut.ConfirmTreeHealthCheckAsync(
                applicationId,
                userId,
                CancellationToken.None);

        Assert.True(result.IsFailure);

        Assert.Empty(FellingLicenceApplicationsContext.FellingLicenceApplications);
    }

    [Theory]
    [InlineData(FellingLicenceStatus.Withdrawn)]
    [InlineData(FellingLicenceStatus.Approved)]
    [InlineData(FellingLicenceStatus.Refused)]
    [InlineData(FellingLicenceStatus.Received)]
    [InlineData(FellingLicenceStatus.SentForApproval)]
    [InlineData(FellingLicenceStatus.WoodlandOfficerReview)]
    [InlineData(FellingLicenceStatus.WithApplicant)]
    [InlineData(FellingLicenceStatus.Draft)]
    [InlineData(FellingLicenceStatus.Submitted)]
    [InlineData(FellingLicenceStatus.ReturnedToApplicant)]
    [InlineData(FellingLicenceStatus.ApprovedInError)]
    [InlineData(FellingLicenceStatus.ReferredToLocalAuthority)]
    public async Task ReturnsFailure_WhenApplicationNotInAdminOfficerReview(FellingLicenceStatus status)
    {
        var userId = Guid.NewGuid();

        var application = await CreateAndSaveAdminOfficerReviewApplicationAsync(status, performingUserId: userId);

        var result = await Sut.ConfirmTreeHealthCheckAsync(
                application.Id,
                userId,
                CancellationToken.None);

        Assert.True(result.IsFailure);

        var updatedRecord =
            FellingLicenceApplicationsContext.FellingLicenceApplications.FirstOrDefault(x => x.Id == application.Id);

        Assert.NotNull(updatedRecord);
        Assert.Null(updatedRecord!.AdminOfficerReview!.IsTreeHealthAnswersChecked);
    }

    [Fact]
    public async Task ReturnsFailure_WhenAdminOfficerReviewIsComplete()
    {
        var userId = Guid.NewGuid();

        var adminOfficerReview = new Entities.AdminOfficerReview
        {
            AgentAuthorityFormChecked = true,
            MappingChecked = true,
            ConstraintsChecked = true,
            AdminOfficerReviewComplete = true,
            IsTreeHealthAnswersChecked = true
        };

        var application = await CreateAndSaveAdminOfficerReviewApplicationAsync(FellingLicenceStatus.AdminOfficerReview, adminOfficerReview, performingUserId: userId);

        var result = await Sut.ConfirmTreeHealthCheckAsync(
                application.Id,
                userId,
                CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task ReturnsSuccessWhenUpdateIsSuccessful()
    {
        var userId = Guid.NewGuid();

        var adminOfficerReview = new Entities.AdminOfficerReview
        {
            AgentAuthorityFormChecked = true,
            MappingChecked = true,
            ConstraintsChecked = true,
            IsTreeHealthAnswersChecked = null
        };

        var application = await CreateAndSaveAdminOfficerReviewApplicationAsync(FellingLicenceStatus.AdminOfficerReview, adminOfficerReview, performingUserId: userId);

        var result = await Sut.ConfirmTreeHealthCheckAsync(
            application.Id,
            userId,
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var updatedRecord =
            FellingLicenceApplicationsContext.FellingLicenceApplications.FirstOrDefault(x => x.Id == application.Id);

        Assert.NotNull(updatedRecord);
        Assert.True(updatedRecord!.AdminOfficerReview!.IsTreeHealthAnswersChecked);
    }
}