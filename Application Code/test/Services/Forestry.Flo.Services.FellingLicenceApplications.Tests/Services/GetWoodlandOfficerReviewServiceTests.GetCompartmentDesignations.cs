using AutoFixture;
using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Tests.Common;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services;

public partial class GetWoodlandOfficerReviewServiceTests
{

    [Theory, AutoData]
    public async Task GetDesignations_WhenRepositoryThrows(Guid applicationId)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception());

        var result = await sut.GetCompartmentDesignationsAsync(applicationId, CancellationToken.None);

        Assert.False(result.IsSuccess);

        _fellingLicenceApplicationRepository
            .Verify(x => x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);

        _fellingLicenceApplicationRepository
            .VerifyNoOtherCalls();
    }

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
    public async Task GetDesignations_WhenNoWoOrCompletedDesignationsYet(Guid applicationId)
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

        List<ProposedCompartmentDesignations> proCpts = new();

        cpts.ForEach(x =>
        {
            x.SubmittedCompartmentDesignations =
                _fixture.Build<SubmittedCompartmentDesignations>()
                    .With(s => s.SubmittedFlaPropertyCompartment, x)
                    .With(x => x.HasBeenReviewed, false)
                    .Create();
            
            proCpts.Add(
                _fixture.Build<ProposedCompartmentDesignations>()
                    .With(p => p.PropertyProfileCompartmentId, x.CompartmentId)
                    .Without(z => z.LinkedPropertyProfile)
                    .Create());
        });

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetSubmittedFlaPropertyCompartmentsByApplicationIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(cpts));

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetProposedCompartmentDesignationsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(proCpts);

        var result = await sut.GetCompartmentDesignationsAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.HasCompletedDesignations);
        Assert.All(result.Value.CompartmentDesignations, x => Assert.False(x.HasBeenReviewed));

        _fellingLicenceApplicationRepository
            .Verify(x => x.GetSubmittedFlaPropertyCompartmentsByApplicationIdAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);

        _fellingLicenceApplicationRepository
            .Verify(x => x.GetProposedCompartmentDesignationsForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);

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

        List<ProposedCompartmentDesignations> proCpts = new();

        cpts.ForEach(x =>
        {
            x.SubmittedCompartmentDesignations =
                _fixture.Build<SubmittedCompartmentDesignations>()
                    .With(s => s.SubmittedFlaPropertyCompartment, x)
                    .Create();

            proCpts.Add(
                _fixture.Build<ProposedCompartmentDesignations>()
                    .With(p => p.PropertyProfileCompartmentId, x.CompartmentId)
                    .Without(z => z.LinkedPropertyProfile)
                    .Create());
        });

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetSubmittedFlaPropertyCompartmentsByApplicationIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(cpts));

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetProposedCompartmentDesignationsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(proCpts);

        var result = await sut.GetCompartmentDesignationsAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.HasCompletedDesignations);
        Assert.All(result.Value.CompartmentDesignations, x =>
        {
            var match = cpts.Single(c => x.Id == c.SubmittedCompartmentDesignations!.Id);

            Assert.Equal(match.SubmittedCompartmentDesignations.HasBeenReviewed, x.HasBeenReviewed);
            Assert.Equal($"{match.CompartmentNumber}{match.SubCompartmentName}", x.CompartmentName);
            Assert.Equal(match.SubmittedCompartmentDesignations.Sssi, x.Sssi);
            Assert.Equal(match.SubmittedCompartmentDesignations.Sacs, x.Sacs);
            Assert.Equal(match.SubmittedCompartmentDesignations.Spa, x.Spa);
            Assert.Equal(match.SubmittedCompartmentDesignations.Ramsar, x.Ramsar);
            Assert.Equal(match.SubmittedCompartmentDesignations.Sbi, x.Sbi);
            Assert.Equal(match.SubmittedCompartmentDesignations.Other, x.Other);
            Assert.Equal(match.SubmittedCompartmentDesignations.OtherDesignationDetails, x.OtherDesignationDetails);
            Assert.Equal(match.SubmittedCompartmentDesignations.None, x.None);
            Assert.Equal(match.SubmittedCompartmentDesignations.Paws, x.Paws);
            Assert.Equal(match.SubmittedCompartmentDesignations.ProportionBeforeFelling, x.ProportionBeforeFelling);
            Assert.Equal(match.SubmittedCompartmentDesignations.ProportionAfterFelling, x.ProportionAfterFelling);
        });

        _fellingLicenceApplicationRepository
            .Verify(x => x.GetSubmittedFlaPropertyCompartmentsByApplicationIdAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        
        _fellingLicenceApplicationRepository
            .Verify(x => x.GetProposedCompartmentDesignationsForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);

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

        List<ProposedCompartmentDesignations> proCpts = new();

        cpts.ForEach(x =>
        {
            x.SubmittedCompartmentDesignations =
                _fixture.Build<SubmittedCompartmentDesignations>()
                    .With(s => s.SubmittedFlaPropertyCompartment, x)
                    .Create();

            proCpts.Add(
                _fixture.Build<ProposedCompartmentDesignations>()
                    .With(p => p.PropertyProfileCompartmentId, x.CompartmentId)
                    .Without(z => z.LinkedPropertyProfile)
                    .Create());
        });

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetSubmittedFlaPropertyCompartmentsByApplicationIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(cpts));

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetProposedCompartmentDesignationsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(proCpts);

        var result = await sut.GetCompartmentDesignationsAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.HasCompletedDesignations);
        Assert.All(result.Value.CompartmentDesignations, x =>
        {
            var match = cpts.Single(c => x.Id == c.SubmittedCompartmentDesignations!.Id);

            Assert.Equal(match.SubmittedCompartmentDesignations.HasBeenReviewed, x.HasBeenReviewed);
            Assert.Equal($"{match.CompartmentNumber}{match.SubCompartmentName}", x.CompartmentName);
            Assert.Equal(match.SubmittedCompartmentDesignations.Sssi, x.Sssi);
            Assert.Equal(match.SubmittedCompartmentDesignations.Sacs, x.Sacs);
            Assert.Equal(match.SubmittedCompartmentDesignations.Spa, x.Spa);
            Assert.Equal(match.SubmittedCompartmentDesignations.Ramsar, x.Ramsar);
            Assert.Equal(match.SubmittedCompartmentDesignations.Sbi, x.Sbi);
            Assert.Equal(match.SubmittedCompartmentDesignations.Other, x.Other);
            Assert.Equal(match.SubmittedCompartmentDesignations.OtherDesignationDetails, x.OtherDesignationDetails);
            Assert.Equal(match.SubmittedCompartmentDesignations.None, x.None);
            Assert.Equal(match.SubmittedCompartmentDesignations.Paws, x.Paws);
            Assert.Equal(match.SubmittedCompartmentDesignations.ProportionBeforeFelling, x.ProportionBeforeFelling);
            Assert.Equal(match.SubmittedCompartmentDesignations.ProportionAfterFelling, x.ProportionAfterFelling);
        });

        Assert.All(result.Value.ProposedCompartmentDesignations, x =>
        {
            var match = proCpts.Single(c => x.PropertyProfileCompartmentId == c.PropertyProfileCompartmentId);

            Assert.Equal(match.Id, x.Id);
            Assert.Equal(match.PropertyProfileCompartmentId, x.PropertyProfileCompartmentId);
            Assert.Equal(match.CrossesPawsZones, x.CrossesPawsZones);
            Assert.Equal(match.ProportionBeforeFelling, x.ProportionBeforeFelling);
            Assert.Equal(match.ProportionAfterFelling, x.ProportionAfterFelling);
            Assert.Equal(match.IsRestoringCompartment, x.IsRestoringCompartment);
            Assert.Equal(match.RestorationDetails, x.RestorationDetails);
        });

        _fellingLicenceApplicationRepository
            .Verify(x => x.GetSubmittedFlaPropertyCompartmentsByApplicationIdAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);

        _fellingLicenceApplicationRepository
            .Verify(x => x.GetProposedCompartmentDesignationsForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);

        _fellingLicenceApplicationRepository
            .Verify(x => x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);

        _fellingLicenceApplicationRepository
            .VerifyNoOtherCalls();
    }
}