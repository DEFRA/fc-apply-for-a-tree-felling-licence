using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Tests.Common;
using MassTransit.Configuration;
using Moq;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services;

public partial class GetWoodlandOfficerReviewServiceTests
{

    [Theory, AutoData]
    public async Task GetDesignations_WhenGetSubmittedCompartmentsFails(Guid applicationId)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.None);

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetSubmittedFlaPropertyCompartmentsByApplicationIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<List<SubmittedFlaPropertyCompartment>>("error"));

        var result = await sut.GetCompartmentDesignationsAsync(applicationId, CancellationToken.None);

        Assert.False(result.IsSuccess);

        _fellingLicenceApplicationRepository
            .Verify(x => x.GetSubmittedFlaPropertyCompartmentsByApplicationIdAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);

        _fellingLicenceApplicationRepository
            .Verify(x => x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);

        _fellingLicenceApplicationRepository
            .VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task GetDesignations_WhenNoWoOrDesignationsYet(Guid applicationId)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.None);

        var cpts = _fixture.Build<SubmittedFlaPropertyCompartment>()
            .Without(x => x.SubmittedCompartmentDesignations)
            .Without(x => x.ConfirmedFellingDetails)
            .Without(x => x.SubmittedFlaPropertyDetail)
            .CreateMany()
            .ToList();

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetSubmittedFlaPropertyCompartmentsByApplicationIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(cpts));

        var result = await sut.GetCompartmentDesignationsAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.HasCompletedDesignations);
        Assert.All(result.Value.CompartmentDesignations, x => Assert.False(x.HasCompletedDesignations));

        _fellingLicenceApplicationRepository
            .Verify(x => x.GetSubmittedFlaPropertyCompartmentsByApplicationIdAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);

        _fellingLicenceApplicationRepository
            .Verify(x => x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);

        _fellingLicenceApplicationRepository
            .VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task GetDesignations_WhenInProgress(Guid applicationId)
    {
        var sut = CreateSut();

        var wo = _fixture.Build<WoodlandOfficerReview>()
            .Without(x => x.FellingLicenceApplication)
            .Without(x => x.SiteVisitEvidences)
            .Without(x => x.FellingAndRestockingAmendmentReviews)
            .With(x => x.DesignationsComplete, false)
            .Create();

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.From(wo));

        var cpts = _fixture.Build<SubmittedFlaPropertyCompartment>()
            .Without(x => x.SubmittedCompartmentDesignations)
            .Without(x => x.ConfirmedFellingDetails)
            .Without(x => x.SubmittedFlaPropertyDetail)
            .CreateMany()
            .ToList();

        cpts.ForEach(x => x.SubmittedCompartmentDesignations =
            _fixture.Build<SubmittedCompartmentDesignations>()
                .With(s => s.SubmittedFlaPropertyCompartment, x)
                .Create());

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetSubmittedFlaPropertyCompartmentsByApplicationIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(cpts));

        var result = await sut.GetCompartmentDesignationsAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.HasCompletedDesignations);
        Assert.All(result.Value.CompartmentDesignations, x =>
        {
            var match = cpts.Single(c => x.Id == c.SubmittedCompartmentDesignations!.Id);

            Assert.True(x.HasCompletedDesignations);
            Assert.Equal($"{match.CompartmentNumber}{match.SubCompartmentName}", x.CompartmentName);
            Assert.Equal(match.SubmittedCompartmentDesignations.Sssi, x.Sssi);
            Assert.Equal(match.SubmittedCompartmentDesignations.Sacs, x.Sacs);
            Assert.Equal(match.SubmittedCompartmentDesignations.Spa, x.Spa);
            Assert.Equal(match.SubmittedCompartmentDesignations.Ramser, x.Ramser);
            Assert.Equal(match.SubmittedCompartmentDesignations.Sbi, x.Sbi);
            Assert.Equal(match.SubmittedCompartmentDesignations.Other, x.Other);
            Assert.Equal(match.SubmittedCompartmentDesignations.OtherDesignationDetails, x.OtherDesignationDetails);
            Assert.Equal(match.SubmittedCompartmentDesignations.None, x.None);
        });

        _fellingLicenceApplicationRepository
            .Verify(x => x.GetSubmittedFlaPropertyCompartmentsByApplicationIdAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);

        _fellingLicenceApplicationRepository
            .Verify(x => x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);

        _fellingLicenceApplicationRepository
            .VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task GetDesignations_WhenCompleted(Guid applicationId)
    {
        var sut = CreateSut();

        var wo = _fixture.Build<WoodlandOfficerReview>()
            .Without(x => x.FellingLicenceApplication)
            .Without(x => x.SiteVisitEvidences)
            .Without(x => x.FellingAndRestockingAmendmentReviews)
            .With(x => x.DesignationsComplete, true)
            .Create();

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.From(wo));

        var cpts = _fixture.Build<SubmittedFlaPropertyCompartment>()
            .Without(x => x.SubmittedCompartmentDesignations)
            .Without(x => x.ConfirmedFellingDetails)
            .Without(x => x.SubmittedFlaPropertyDetail)
            .CreateMany()
            .ToList();

        cpts.ForEach(x => x.SubmittedCompartmentDesignations = 
            _fixture.Build<SubmittedCompartmentDesignations>()
                .With(s => s.SubmittedFlaPropertyCompartment, x)
                .Create());

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetSubmittedFlaPropertyCompartmentsByApplicationIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(cpts));

        var result = await sut.GetCompartmentDesignationsAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.HasCompletedDesignations);
        Assert.All(result.Value.CompartmentDesignations, x =>
        {
            var match = cpts.Single(c => x.Id == c.SubmittedCompartmentDesignations!.Id);

            Assert.True(x.HasCompletedDesignations);
            Assert.Equal($"{match.CompartmentNumber}{match.SubCompartmentName}", x.CompartmentName);
            Assert.Equal(match.SubmittedCompartmentDesignations.Sssi, x.Sssi);
            Assert.Equal(match.SubmittedCompartmentDesignations.Sacs, x.Sacs);
            Assert.Equal(match.SubmittedCompartmentDesignations.Spa, x.Spa);
            Assert.Equal(match.SubmittedCompartmentDesignations.Ramser, x.Ramser);
            Assert.Equal(match.SubmittedCompartmentDesignations.Sbi, x.Sbi);
            Assert.Equal(match.SubmittedCompartmentDesignations.Other, x.Other);
            Assert.Equal(match.SubmittedCompartmentDesignations.OtherDesignationDetails, x.OtherDesignationDetails);
            Assert.Equal(match.SubmittedCompartmentDesignations.None, x.None);
        });

        _fellingLicenceApplicationRepository
            .Verify(x => x.GetSubmittedFlaPropertyCompartmentsByApplicationIdAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);

        _fellingLicenceApplicationRepository
            .Verify(x => x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);

        _fellingLicenceApplicationRepository
            .VerifyNoOtherCalls();
    }
}