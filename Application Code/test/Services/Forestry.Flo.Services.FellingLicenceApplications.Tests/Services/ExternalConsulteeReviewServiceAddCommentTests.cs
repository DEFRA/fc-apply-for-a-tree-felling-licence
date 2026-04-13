using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models.ExternalConsultee;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Tests.Common;
using LinqKit;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NodaTime;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services;

public class ExternalConsulteeReviewServiceAddCommentTests
{
    private readonly Mock<IClock> MockClock = new();
    private readonly Mock<IFellingLicenceApplicationInternalRepository> MockRepository = new();

    [Theory, AutoMoqData]
    public async Task WhenCommentStoredSuccessfully(
        ConsulteeCommentModel model,
        FellingLicenceApplication application)
    {
        // ensure all existing assignees are unassigned so that only the current assignees are returned in the result
        application.AssigneeHistories.ForEach(x => x.TimestampUnassigned = DateTime.UtcNow);
        var ao = new AssigneeHistory
        {
            Role = AssignedUserRole.AdminOfficer,
            AssignedUserId = Guid.NewGuid(),
            TimestampAssigned = DateTime.Today
        };
        var wo = new AssigneeHistory
        {
            Role = AssignedUserRole.WoodlandOfficer,
            AssignedUserId = Guid.NewGuid(),
            TimestampAssigned = DateTime.Today
        };
        var approver = new AssigneeHistory
        {
            Role = AssignedUserRole.FieldManager,
            AssignedUserId = Guid.NewGuid(),
            TimestampAssigned = DateTime.Today
        };
        application.AssigneeHistories.Add(ao);
        application.AssigneeHistories.Add(wo);
        application.AssigneeHistories.Add(approver);

        var sut = CreateSut();

        MockRepository
            .Setup(x => x.AddConsulteeCommentAsync(It.IsAny<ConsulteeComment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        MockRepository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(application));

        var result = await sut.AddCommentAsync(model, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(application.ApplicationReference, result.Value.ApplicationReference);
        Assert.Equal(application.AdministrativeRegion, result.Value.AdminHub);
        Assert.Equivalent(new[] {ao.AssignedUserId, wo.AssignedUserId, approver.AssignedUserId}, result.Value.AssignedFcStaff);
        Assert.Equal(application.SubmittedFlaPropertyDetail.Name, result.Value.PropertyName);

        MockRepository.Verify(x => x.AddConsulteeCommentAsync(It.Is<ConsulteeComment>(c => 
            c.CreatedTimestamp == model.CreatedTimestamp
            && c.AuthorContactEmail == model.AuthorContactEmail
            && c.AuthorName == model.AuthorName
            && c.Comment == model.Comment
            && c.FellingLicenceApplicationId == model.FellingLicenceApplicationId), It.IsAny<CancellationToken>()),
            Times.Once);
        MockRepository.Verify(x => x.GetAsync(model.FellingLicenceApplicationId, It.IsAny<CancellationToken>()), Times.Once);
        MockRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task WhenCommentNotStoredSuccessfully(ConsulteeCommentModel model)
    {
        var sut = CreateSut();

        MockRepository
            .Setup(x => x.AddConsulteeCommentAsync(It.IsAny<ConsulteeComment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Failure(UserDbErrorReason.NotUnique));

        var result = await sut.AddCommentAsync(model, CancellationToken.None);

        Assert.True(result.IsFailure);
        MockRepository.Verify(x => x.AddConsulteeCommentAsync(It.Is<ConsulteeComment>(c =>
                c.CreatedTimestamp == model.CreatedTimestamp
                && c.AuthorContactEmail == model.AuthorContactEmail
                && c.AuthorName == model.AuthorName
                && c.Comment == model.Comment
                && c.FellingLicenceApplicationId == model.FellingLicenceApplicationId), It.IsAny<CancellationToken>()),
            Times.Once);
        MockRepository.VerifyNoOtherCalls();
    }

    private ExternalConsulteeReviewService CreateSut()
    {
        MockClock.Reset();
        MockRepository.Reset();

        return new ExternalConsulteeReviewService(
            MockRepository.Object,
            MockClock.Object,
            new NullLogger<ExternalConsulteeReviewService>());
    }
}