using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.FileStorage.ResultModels;
using Forestry.Flo.Services.Gis.Interfaces;
using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services;

public class GetWoodlandOfficerReviewServiceTests
{
    private readonly Mock<IFellingLicenceApplicationInternalRepository> _fellingLicenceApplicationRepository = new();
    private readonly Mock<IForesterServices> _foresterServices = new();
    private readonly Mock<IViewCaseNotesService> _viewCaseNotesService = new();
    private readonly Fixture _fixture = new();

    [Theory, AutoData]
    public async Task ShouldReturnFailureIfUnableToRetrieveFellingAndRestockingDetails(Guid applicationId)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetConfirmedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<(List<ConfirmedFellingDetail>, List<ConfirmedRestockingDetail>)>("error"));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.None);

        _fellingLicenceApplicationRepository.Setup(x => x.GetPublicRegisterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<PublicRegister>.None);

        var result = await sut.GetWoodlandOfficerReviewStatusAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsFailure);

        _fellingLicenceApplicationRepository.Verify(x => x.GetPublicRegisterAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.Verify(x => x.GetConfirmedFellingAndRestockingDetailsForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _viewCaseNotesService.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task ShouldReturnSuccess_NoStepsStarted_WhenNoReviewDataRetrieved(Guid applicationId)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetConfirmedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((new List<ConfirmedFellingDetail>(0), new List<ConfirmedRestockingDetail>(0))));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetProposedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((new List<ProposedFellingDetail>(0), new List<ProposedRestockingDetail>(0))));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.None);

        _fellingLicenceApplicationRepository.Setup(x => x.GetPublicRegisterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<PublicRegister>.None);

        _viewCaseNotesService.Setup(x =>
                x.GetSpecificCaseNotesAsync(It.IsAny<Guid>(), It.IsAny<CaseNoteType[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CaseNoteModel>(0));

        var result = await sut.GetWoodlandOfficerReviewStatusAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Empty(result.Value.WoodlandOfficerReviewComments);
        Assert.Null(result.Value.RecommendedLicenceDuration);
        Assert.Equal(InternalReviewStepStatus.NotStarted, result.Value.WoodlandOfficerReviewTaskListStates.PublicRegisterStepStatus);
        Assert.Equal(InternalReviewStepStatus.NotStarted, result.Value.WoodlandOfficerReviewTaskListStates.SiteVisitStepStatus);
        Assert.Equal(InternalReviewStepStatus.NotStarted, result.Value.WoodlandOfficerReviewTaskListStates.Pw14ChecksStepStatus);
        Assert.Equal(InternalReviewStepStatus.NotStarted, result.Value.WoodlandOfficerReviewTaskListStates.FellingAndRestockingStepStatus);

        _fellingLicenceApplicationRepository.Verify(x => x.GetPublicRegisterAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.Verify(x => x.GetConfirmedFellingAndRestockingDetailsForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _viewCaseNotesService.Verify(x => x.GetSpecificCaseNotesAsync(applicationId, new [] {  CaseNoteType.WoodlandOfficerReviewComment  }, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoData]
    public async Task ShouldReturnSuccess_WithComments(Guid applicationId, List<CaseNoteModel> caseNotes)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetConfirmedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((new List<ConfirmedFellingDetail>(0), new List<ConfirmedRestockingDetail>(0))));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetProposedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((new List<ProposedFellingDetail>(0), new List<ProposedRestockingDetail>(0))));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.None);

        _fellingLicenceApplicationRepository.Setup(x => x.GetPublicRegisterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<PublicRegister>.None);

        _viewCaseNotesService.Setup(x =>
                x.GetSpecificCaseNotesAsync(It.IsAny<Guid>(), It.IsAny<CaseNoteType[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(caseNotes);

        var result = await sut.GetWoodlandOfficerReviewStatusAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value?.WoodlandOfficerReviewComments);

        caseNotes.ForEach(expected =>
        {
            Assert.Contains(result.Value!.WoodlandOfficerReviewComments, y => y == expected);
        });

        _viewCaseNotesService.Verify(x => x.GetSpecificCaseNotesAsync(applicationId, new[] { CaseNoteType.WoodlandOfficerReviewComment }, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoData]
    public async Task ShouldReturnSuccess_PublicRegisterCompleted_Exempt(Guid applicationId)
    {
        var pr = new PublicRegister
        {
            WoodlandOfficerSetAsExemptFromConsultationPublicRegister = true,
            WoodlandOfficerConsultationPublicRegisterExemptionReason = "Test"
        };

        var sut = CreateSut();

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetConfirmedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((new List<ConfirmedFellingDetail>(0), new List<ConfirmedRestockingDetail>(0))));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetProposedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((new List<ProposedFellingDetail>(0), new List<ProposedRestockingDetail>(0))));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.None);

        _fellingLicenceApplicationRepository.Setup(x => x.GetPublicRegisterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<PublicRegister>.From(pr));

        _viewCaseNotesService.Setup(x =>
                x.GetSpecificCaseNotesAsync(It.IsAny<Guid>(), It.IsAny<CaseNoteType[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CaseNoteModel>(0));

        var result = await sut.GetWoodlandOfficerReviewStatusAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(InternalReviewStepStatus.Completed, result.Value.WoodlandOfficerReviewTaskListStates.PublicRegisterStepStatus);

        _fellingLicenceApplicationRepository.Verify(x => x.GetPublicRegisterAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoData]
    public async Task ShouldReturnSuccess_PublicRegisterCompleted_Removed(Guid applicationId)
    {
        var pr = new PublicRegister
        {
            ConsultationPublicRegisterPublicationTimestamp = DateTime.UtcNow.AddDays(-2),
            ConsultationPublicRegisterExpiryTimestamp = DateTime.UtcNow.AddDays(2),
            ConsultationPublicRegisterRemovedTimestamp = DateTime.UtcNow
        };

        var sut = CreateSut();

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetConfirmedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((new List<ConfirmedFellingDetail>(0), new List<ConfirmedRestockingDetail>(0))));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetProposedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((new List<ProposedFellingDetail>(0), new List<ProposedRestockingDetail>(0))));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.None);

        _fellingLicenceApplicationRepository.Setup(x => x.GetPublicRegisterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<PublicRegister>.From(pr));

        _viewCaseNotesService.Setup(x =>
                x.GetSpecificCaseNotesAsync(It.IsAny<Guid>(), It.IsAny<CaseNoteType[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CaseNoteModel>(0));

        var result = await sut.GetWoodlandOfficerReviewStatusAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(InternalReviewStepStatus.Completed, result.Value.WoodlandOfficerReviewTaskListStates.PublicRegisterStepStatus);

        _fellingLicenceApplicationRepository.Verify(x => x.GetPublicRegisterAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoData]
    public async Task ShouldReturnSuccess_PublicRegisterInProgress(Guid applicationId)
    {
        var pr = new PublicRegister
        {
            ConsultationPublicRegisterPublicationTimestamp = DateTime.UtcNow.AddDays(-2),
            ConsultationPublicRegisterExpiryTimestamp = DateTime.UtcNow.AddDays(2)
        };

        var sut = CreateSut();

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetConfirmedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((new List<ConfirmedFellingDetail>(0), new List<ConfirmedRestockingDetail>(0))));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetProposedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((new List<ProposedFellingDetail>(0), new List<ProposedRestockingDetail>(0))));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.None);

        _fellingLicenceApplicationRepository.Setup(x => x.GetPublicRegisterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<PublicRegister>.From(pr));

        _viewCaseNotesService.Setup(x =>
                x.GetSpecificCaseNotesAsync(It.IsAny<Guid>(), It.IsAny<CaseNoteType[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CaseNoteModel>(0));

        var result = await sut.GetWoodlandOfficerReviewStatusAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(InternalReviewStepStatus.InProgress, result.Value.WoodlandOfficerReviewTaskListStates.PublicRegisterStepStatus);

        _fellingLicenceApplicationRepository.Verify(x => x.GetPublicRegisterAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoData]
    public async Task ShouldReturnSuccess_SiteVisitNotStarted(Guid applicationId)
    {
        var review = new WoodlandOfficerReview();

        var sut = CreateSut();

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetConfirmedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((new List<ConfirmedFellingDetail>(0), new List<ConfirmedRestockingDetail>(0))));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetProposedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((new List<ProposedFellingDetail>(0), new List<ProposedRestockingDetail>(0))));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.From(review));

        _fellingLicenceApplicationRepository.Setup(x => x.GetPublicRegisterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<PublicRegister>.None);

        _viewCaseNotesService.Setup(x =>
                x.GetSpecificCaseNotesAsync(It.IsAny<Guid>(), It.IsAny<CaseNoteType[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CaseNoteModel>(0));

        var result = await sut.GetWoodlandOfficerReviewStatusAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(InternalReviewStepStatus.NotStarted, result.Value.WoodlandOfficerReviewTaskListStates.SiteVisitStepStatus);

        _fellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoData]
    public async Task ShouldReturnSuccess_SiteVisitCompleted_NotNeeded(Guid applicationId)
    {
        var review = new WoodlandOfficerReview
        {
            SiteVisitNotNeeded = true
        };

        var sut = CreateSut();

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetConfirmedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((new List<ConfirmedFellingDetail>(0), new List<ConfirmedRestockingDetail>(0))));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetProposedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((new List<ProposedFellingDetail>(0), new List<ProposedRestockingDetail>(0))));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.From(review));

        _fellingLicenceApplicationRepository.Setup(x => x.GetPublicRegisterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<PublicRegister>.None);

        _viewCaseNotesService.Setup(x =>
                x.GetSpecificCaseNotesAsync(It.IsAny<Guid>(), It.IsAny<CaseNoteType[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CaseNoteModel>(0));

        var result = await sut.GetWoodlandOfficerReviewStatusAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(InternalReviewStepStatus.Completed, result.Value.WoodlandOfficerReviewTaskListStates.SiteVisitStepStatus);

        _fellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoData]
    public async Task ShouldReturnSuccess_SiteVisitCompleted_NotesRetrieved(Guid applicationId)
    {
        var review = new WoodlandOfficerReview
        {
            SiteVisitArtefactsCreated = DateTime.UtcNow.AddDays(-3),
            SiteVisitNotesRetrieved = DateTime.UtcNow.AddDays(-1)
        };

        var sut = CreateSut();

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetConfirmedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((new List<ConfirmedFellingDetail>(0), new List<ConfirmedRestockingDetail>(0))));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetProposedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((new List<ProposedFellingDetail>(0), new List<ProposedRestockingDetail>(0))));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.From(review));

        _fellingLicenceApplicationRepository.Setup(x => x.GetPublicRegisterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<PublicRegister>.None);

        _viewCaseNotesService.Setup(x =>
                x.GetSpecificCaseNotesAsync(It.IsAny<Guid>(), It.IsAny<CaseNoteType[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CaseNoteModel>(0));

        var result = await sut.GetWoodlandOfficerReviewStatusAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(InternalReviewStepStatus.Completed, result.Value.WoodlandOfficerReviewTaskListStates.SiteVisitStepStatus);

        _fellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoData]
    public async Task ShouldReturnSuccess_SiteVisitInProgress(Guid applicationId)
    {
        var review = new WoodlandOfficerReview
        {
            SiteVisitArtefactsCreated = DateTime.UtcNow.AddDays(-3)
        };

        var sut = CreateSut();

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetConfirmedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((new List<ConfirmedFellingDetail>(0), new List<ConfirmedRestockingDetail>(0))));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetProposedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((new List<ProposedFellingDetail>(0), new List<ProposedRestockingDetail>(0))));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.From(review));

        _fellingLicenceApplicationRepository.Setup(x => x.GetPublicRegisterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<PublicRegister>.None);

        _viewCaseNotesService.Setup(x =>
                x.GetSpecificCaseNotesAsync(It.IsAny<Guid>(), It.IsAny<CaseNoteType[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CaseNoteModel>(0));

        var result = await sut.GetWoodlandOfficerReviewStatusAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(InternalReviewStepStatus.InProgress, result.Value.WoodlandOfficerReviewTaskListStates.SiteVisitStepStatus);

        _fellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoData]
    public async Task ShouldReturnSuccess_PW14ChecksNotStarted(Guid applicationId)
    {
        var review = new WoodlandOfficerReview();

        var sut = CreateSut();

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetConfirmedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((new List<ConfirmedFellingDetail>(0), new List<ConfirmedRestockingDetail>(0))));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetProposedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((new List<ProposedFellingDetail>(0), new List<ProposedRestockingDetail>(0))));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.From(review));

        _fellingLicenceApplicationRepository.Setup(x => x.GetPublicRegisterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<PublicRegister>.None);

        _viewCaseNotesService.Setup(x =>
                x.GetSpecificCaseNotesAsync(It.IsAny<Guid>(), It.IsAny<CaseNoteType[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CaseNoteModel>(0));

        var result = await sut.GetWoodlandOfficerReviewStatusAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(InternalReviewStepStatus.NotStarted, result.Value.WoodlandOfficerReviewTaskListStates.Pw14ChecksStepStatus);

        _fellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoData]
    public async Task ShouldReturnSuccess_PW14ChecksCompleted(Guid applicationId)
    {
        var review = new WoodlandOfficerReview
        {
            Pw14ChecksComplete = true
        };

        var sut = CreateSut();

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetConfirmedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((new List<ConfirmedFellingDetail>(0), new List<ConfirmedRestockingDetail>(0))));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetProposedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((new List<ProposedFellingDetail>(0), new List<ProposedRestockingDetail>(0))));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.From(review));

        _fellingLicenceApplicationRepository.Setup(x => x.GetPublicRegisterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<PublicRegister>.None);

        _viewCaseNotesService.Setup(x =>
                x.GetSpecificCaseNotesAsync(It.IsAny<Guid>(), It.IsAny<CaseNoteType[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CaseNoteModel>(0));

        var result = await sut.GetWoodlandOfficerReviewStatusAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(InternalReviewStepStatus.Completed, result.Value.WoodlandOfficerReviewTaskListStates.Pw14ChecksStepStatus);

        _fellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
    }
    
    [Theory, AutoMoqData]
    public async Task ShouldReturnSuccess_PW14ChecksInProgress(Guid applicationId, WoodlandOfficerReview review)
    {
        review.Pw14ChecksComplete = false;
        review.EiaTrackerCompleted = true;

        var sut = CreateSut();

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetConfirmedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((new List<ConfirmedFellingDetail>(0), new List<ConfirmedRestockingDetail>(0))));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetProposedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((new List<ProposedFellingDetail>(0), new List<ProposedRestockingDetail>(0))));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.From(review));

        _fellingLicenceApplicationRepository.Setup(x => x.GetPublicRegisterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<PublicRegister>.None);

        _viewCaseNotesService.Setup(x =>
                x.GetSpecificCaseNotesAsync(It.IsAny<Guid>(), It.IsAny<CaseNoteType[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CaseNoteModel>(0));

        var result = await sut.GetWoodlandOfficerReviewStatusAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(InternalReviewStepStatus.InProgress, result.Value.WoodlandOfficerReviewTaskListStates.Pw14ChecksStepStatus);

        _fellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnSuccess_ConfirmedFAndRInProgress(Guid applicationId, List<ConfirmedFellingDetail> felling, List<ConfirmedRestockingDetail> restocking, WoodlandOfficerReview review)
    {
        review.ConfirmedFellingAndRestockingComplete = false;

        var sut = CreateSut();

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetConfirmedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((felling, restocking)));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.From(review));

        _fellingLicenceApplicationRepository.Setup(x => x.GetPublicRegisterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<PublicRegister>.None);

        _viewCaseNotesService.Setup(x =>
                x.GetSpecificCaseNotesAsync(It.IsAny<Guid>(), It.IsAny<CaseNoteType[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CaseNoteModel>(0));

        var result = await sut.GetWoodlandOfficerReviewStatusAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(InternalReviewStepStatus.InProgress, result.Value.WoodlandOfficerReviewTaskListStates.FellingAndRestockingStepStatus);

        _fellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.Verify(x => x.GetConfirmedFellingAndRestockingDetailsForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnSuccess_ConfirmedFAndRCompleted(Guid applicationId, List<ConfirmedFellingDetail> felling, List<ConfirmedRestockingDetail> restocking, WoodlandOfficerReview review)
    {
        review.ConfirmedFellingAndRestockingComplete = true;

        var sut = CreateSut();

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetConfirmedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((felling, restocking)));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.From(review));

        _fellingLicenceApplicationRepository.Setup(x => x.GetPublicRegisterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<PublicRegister>.None);

        _viewCaseNotesService.Setup(x =>
                x.GetSpecificCaseNotesAsync(It.IsAny<Guid>(), It.IsAny<CaseNoteType[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CaseNoteModel>(0));

        var result = await sut.GetWoodlandOfficerReviewStatusAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(InternalReviewStepStatus.Completed, result.Value.WoodlandOfficerReviewTaskListStates.FellingAndRestockingStepStatus);

        _fellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.Verify(x => x.GetConfirmedFellingAndRestockingDetailsForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoData]
    public async Task ShouldReturnSuccess_ConditionsNotStarted(Guid applicationId)
    {
        var review = new WoodlandOfficerReview
        {
            IsConditional = null
        };

        var sut = CreateSut();

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetConfirmedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((new List<ConfirmedFellingDetail>(0), new List<ConfirmedRestockingDetail>(0))));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetProposedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((new List<ProposedFellingDetail>(0), new List<ProposedRestockingDetail>(0))));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.From(review));

        _fellingLicenceApplicationRepository.Setup(x => x.GetPublicRegisterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<PublicRegister>.None);

        _viewCaseNotesService.Setup(x =>
                x.GetSpecificCaseNotesAsync(It.IsAny<Guid>(), It.IsAny<CaseNoteType[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CaseNoteModel>(0));

        var result = await sut.GetWoodlandOfficerReviewStatusAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(InternalReviewStepStatus.NotStarted, result.Value.WoodlandOfficerReviewTaskListStates.ConditionsStepStatus);

        _fellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoData]
    public async Task ShouldReturnSuccess_ConditionsInProgress(Guid applicationId)
    {
        var review = new WoodlandOfficerReview
        {
            IsConditional = true,
            ConditionsToApplicantDate = null
        };

        var sut = CreateSut();

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetConfirmedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((new List<ConfirmedFellingDetail>(0), new List<ConfirmedRestockingDetail>(0))));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetProposedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((new List<ProposedFellingDetail>(0), new List<ProposedRestockingDetail>(0))));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.From(review));

        _fellingLicenceApplicationRepository.Setup(x => x.GetPublicRegisterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<PublicRegister>.None);

        _viewCaseNotesService.Setup(x =>
                x.GetSpecificCaseNotesAsync(It.IsAny<Guid>(), It.IsAny<CaseNoteType[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CaseNoteModel>(0));

        var result = await sut.GetWoodlandOfficerReviewStatusAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(InternalReviewStepStatus.InProgress, result.Value.WoodlandOfficerReviewTaskListStates.ConditionsStepStatus);

        _fellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoData]
    public async Task ShouldReturnSuccess_ConditionsCompleted_NotConditions(Guid applicationId)
    {
        var review = new WoodlandOfficerReview
        {
            IsConditional = false,
            ConditionsToApplicantDate = null
        };

        var sut = CreateSut();

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetConfirmedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((new List<ConfirmedFellingDetail>(0), new List<ConfirmedRestockingDetail>(0))));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetProposedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((new List<ProposedFellingDetail>(0), new List<ProposedRestockingDetail>(0))));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.From(review));

        _fellingLicenceApplicationRepository.Setup(x => x.GetPublicRegisterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<PublicRegister>.None);

        _viewCaseNotesService.Setup(x =>
                x.GetSpecificCaseNotesAsync(It.IsAny<Guid>(), It.IsAny<CaseNoteType[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CaseNoteModel>(0));

        var result = await sut.GetWoodlandOfficerReviewStatusAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(InternalReviewStepStatus.Completed, result.Value.WoodlandOfficerReviewTaskListStates.ConditionsStepStatus);

        _fellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoData]
    public async Task ShouldReturnSuccess_ConditionsCompleted_SentToApplicant(Guid applicationId)
    {
        var review = new WoodlandOfficerReview
        {
            IsConditional = true,
            ConditionsToApplicantDate = DateTime.UtcNow
        };

        var sut = CreateSut();

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetConfirmedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((new List<ConfirmedFellingDetail>(0), new List<ConfirmedRestockingDetail>(0))));
        
        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetProposedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((new List<ProposedFellingDetail>(0), new List<ProposedRestockingDetail>(0))));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.From(review));

        _fellingLicenceApplicationRepository.Setup(x => x.GetPublicRegisterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<PublicRegister>.None);

        _viewCaseNotesService.Setup(x =>
                x.GetSpecificCaseNotesAsync(It.IsAny<Guid>(), It.IsAny<CaseNoteType[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CaseNoteModel>(0));

        var result = await sut.GetWoodlandOfficerReviewStatusAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(InternalReviewStepStatus.Completed, result.Value.WoodlandOfficerReviewTaskListStates.ConditionsStepStatus);

        _fellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoData]
    public async Task ShouldReturnSuccess_LarchCheckNotRequired_WithNoLarchAndNotConfirmedFellingYet(Guid applicationId)
    {
        var review = new WoodlandOfficerReview
        {
            IsConditional = true,
            ConditionsToApplicantDate = DateTime.UtcNow
        };

        var sut = CreateSut();

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetConfirmedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((new List<ConfirmedFellingDetail>(0), new List<ConfirmedRestockingDetail>(0))));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetProposedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((new List<ProposedFellingDetail>(0), new List<ProposedRestockingDetail>(0))));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.From(review));

        _fellingLicenceApplicationRepository.Setup(x => x.GetPublicRegisterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<PublicRegister>.None);

        _viewCaseNotesService.Setup(x =>
                x.GetSpecificCaseNotesAsync(It.IsAny<Guid>(), It.IsAny<CaseNoteType[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CaseNoteModel>(0));

        var result = await sut.GetWoodlandOfficerReviewStatusAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(InternalReviewStepStatus.NotRequired, result.Value.WoodlandOfficerReviewTaskListStates.LarchApplicationStatus);

        _fellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoData]
    public async Task ShouldReturnSuccess_LarchCheckCannotStartYet_WithLarchAndNotConfirmedFellingYet(
        Guid applicationId)
    {
        var review = new WoodlandOfficerReview
        {
            IsConditional = true,
            ConditionsToApplicantDate = DateTime.UtcNow
        };

        var species = _fixture.Build<FellingSpecies>()
            .With(x => x.Species, "EL")
            .Without(x => x.ProposedFellingDetail)
            .Create();

        var felling = _fixture.Build<ProposedFellingDetail>()
            .With(x => x.FellingSpecies, [species])
            .Without(x => x.LinkedPropertyProfile)
            .Without(x => x.FellingOutcomes)
            .With(x => x.ProposedRestockingDetails, [])
            .Create();

        var sut = CreateSut();

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetConfirmedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((new List<ConfirmedFellingDetail>(0), new List<ConfirmedRestockingDetail>(0))));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetProposedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((new List<ProposedFellingDetail> { felling }, new List<ProposedRestockingDetail>(0))));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.From(review));

        _fellingLicenceApplicationRepository.Setup(x => x.GetPublicRegisterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<PublicRegister>.None);

        _viewCaseNotesService.Setup(x =>
                x.GetSpecificCaseNotesAsync(It.IsAny<Guid>(), It.IsAny<CaseNoteType[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CaseNoteModel>(0));

        var result = await sut.GetWoodlandOfficerReviewStatusAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(InternalReviewStepStatus.CannotStartYet, result.Value.WoodlandOfficerReviewTaskListStates.LarchApplicationStatus);

        _fellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoData]
    public async Task ShouldReturnSuccess_LarchCheckNoStarted(
        Guid applicationId)
    {
        var review = new WoodlandOfficerReview
        {
            ConfirmedFellingAndRestockingComplete = true
        };

        var species = _fixture.Build<ConfirmedFellingSpecies>()
            .With(x => x.Species, "EL")
            .Without(x => x.ConfirmedFellingDetail)
            .Create();

        var felling = _fixture.Build<ConfirmedFellingDetail>()
            .With(x => x.ConfirmedFellingSpecies, [species])
            .Without(x => x.SubmittedFlaPropertyCompartment)
            .With(x => x.ConfirmedRestockingDetails, [])
            .Create();

        var sut = CreateSut();

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetConfirmedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((new List<ConfirmedFellingDetail> { felling }, new List<ConfirmedRestockingDetail>(0))));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetProposedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((new List<ProposedFellingDetail>(0), new List<ProposedRestockingDetail>(0))));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.From(review));

        _fellingLicenceApplicationRepository.Setup(x => x.GetPublicRegisterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<PublicRegister>.None);

        _viewCaseNotesService.Setup(x =>
                x.GetSpecificCaseNotesAsync(It.IsAny<Guid>(), It.IsAny<CaseNoteType[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CaseNoteModel>(0));

        var result = await sut.GetWoodlandOfficerReviewStatusAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(InternalReviewStepStatus.NotStarted, result.Value.WoodlandOfficerReviewTaskListStates.LarchApplicationStatus);

        _fellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoData]
    public async Task ShouldReturnSuccess_LarchCheckInProgress(Guid applicationId)
    {
        var review = new WoodlandOfficerReview
        {
            ConfirmedFellingAndRestockingComplete = true,
            LarchCheckComplete = false
        };

        var species = _fixture.Build<ConfirmedFellingSpecies>()
            .With(x => x.Species, "EL")
            .Without(x => x.ConfirmedFellingDetail)
            .Create();

        var felling = _fixture.Build<ConfirmedFellingDetail>()
            .With(x => x.ConfirmedFellingSpecies, [species])
            .Without(x => x.SubmittedFlaPropertyCompartment)
            .With(x => x.ConfirmedRestockingDetails, [])
            .Create();

        var sut = CreateSut();

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetConfirmedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((new List<ConfirmedFellingDetail> { felling }, new List<ConfirmedRestockingDetail>(0))));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetProposedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((new List<ProposedFellingDetail>(0), new List<ProposedRestockingDetail>(0))));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.From(review));

        _fellingLicenceApplicationRepository.Setup(x => x.GetPublicRegisterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<PublicRegister>.None);

        _viewCaseNotesService.Setup(x =>
                x.GetSpecificCaseNotesAsync(It.IsAny<Guid>(), It.IsAny<CaseNoteType[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CaseNoteModel>(0));

        var result = await sut.GetWoodlandOfficerReviewStatusAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(InternalReviewStepStatus.InProgress, result.Value.WoodlandOfficerReviewTaskListStates.LarchApplicationStatus);

        _fellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoData]
    public async Task ShouldReturnSuccess_LarchCheckComplete(Guid applicationId)
    {
        var review = new WoodlandOfficerReview
        {
            ConfirmedFellingAndRestockingComplete = true,
            LarchCheckComplete = true
        };

        var species = _fixture.Build<ConfirmedFellingSpecies>()
            .With(x => x.Species, "EL")
            .Without(x => x.ConfirmedFellingDetail)
            .Create();

        var felling = _fixture.Build<ConfirmedFellingDetail>()
            .With(x => x.ConfirmedFellingSpecies, [species])
            .Without(x => x.SubmittedFlaPropertyCompartment)
            .With(x => x.ConfirmedRestockingDetails, [])
            .Create();

        var sut = CreateSut();

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetConfirmedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((new List<ConfirmedFellingDetail> { felling }, new List<ConfirmedRestockingDetail>(0))));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetProposedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((new List<ProposedFellingDetail>(0), new List<ProposedRestockingDetail>(0))));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.From(review));

        _fellingLicenceApplicationRepository.Setup(x => x.GetPublicRegisterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<PublicRegister>.None);

        _viewCaseNotesService.Setup(x =>
                x.GetSpecificCaseNotesAsync(It.IsAny<Guid>(), It.IsAny<CaseNoteType[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CaseNoteModel>(0));

        var result = await sut.GetWoodlandOfficerReviewStatusAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(InternalReviewStepStatus.Completed, result.Value.WoodlandOfficerReviewTaskListStates.LarchApplicationStatus);

        _fellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task GetPublicRegisterShouldReturnMaybeNoneIfNoEntryExists(Guid applicationId)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository.Setup(x => x.GetPublicRegisterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<PublicRegister>.None);

        var result = await sut.GetPublicRegisterDetailsAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.HasNoValue);

        _fellingLicenceApplicationRepository.Verify(x => x.GetPublicRegisterAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task GetPublicRegisterShouldReturnExpectedModelIfEntryExists(
        Guid applicationId, 
        PublicRegister publicRegister)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository.Setup(x => x.GetPublicRegisterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<PublicRegister>.From(publicRegister));

        var result = await sut.GetPublicRegisterDetailsAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.HasValue);

        var actual = result.Value.Value;
        Assert.Equal(publicRegister.WoodlandOfficerSetAsExemptFromConsultationPublicRegister, actual.WoodlandOfficerSetAsExemptFromConsultationPublicRegister);
        Assert.Equal(publicRegister.WoodlandOfficerConsultationPublicRegisterExemptionReason, actual.WoodlandOfficerConsultationPublicRegisterExemptionReason);
        Assert.Equal(publicRegister.ConsultationPublicRegisterPublicationTimestamp, actual.ConsultationPublicRegisterPublicationTimestamp);
        Assert.Equal(publicRegister.ConsultationPublicRegisterExpiryTimestamp, actual.ConsultationPublicRegisterExpiryTimestamp);
        Assert.Equal(publicRegister.ConsultationPublicRegisterRemovedTimestamp, actual.ConsultationPublicRegisterRemovedTimestamp);

        _fellingLicenceApplicationRepository.Verify(x => x.GetPublicRegisterAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task GetSiteVisitShouldReturnMaybeNoneIfNoEntryOrCommentsExist(Guid applicationId)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository.Setup(x => x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.None);

        _viewCaseNotesService
            .Setup(x => x.GetSpecificCaseNotesAsync(It.IsAny<Guid>(), It.IsAny<CaseNoteType[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CaseNoteModel>(0));

        var result = await sut.GetSiteVisitDetailsAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.HasNoValue);

        _fellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _viewCaseNotesService.Verify(x => x.GetSpecificCaseNotesAsync(applicationId, new[] { CaseNoteType.SiteVisitComment }, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task GetSiteVisitShouldReturnDefaultEntryAndCommentsIfJustCommentsExist(
        Guid applicationId,
        IList<CaseNoteModel> comments)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository.Setup(x => x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.None);

        _viewCaseNotesService
            .Setup(x => x.GetSpecificCaseNotesAsync(It.IsAny<Guid>(), It.IsAny<CaseNoteType[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(comments);

        var result = await sut.GetSiteVisitDetailsAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.HasValue);

        var model = result.Value.Value;

        Assert.Null(model.SiteVisitArtefactsCreated);
        Assert.Null(model.SiteVisitNotesRetrieved);
        Assert.False(model.SiteVisitNotNeeded);
        Assert.Equal(comments, model.SiteVisitComments);

        _fellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _viewCaseNotesService.Verify(x => x.GetSpecificCaseNotesAsync(applicationId, new[] { CaseNoteType.SiteVisitComment }, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task GetSiteVisitShouldReturnExpectedValuesIfDataExists(
        Guid applicationId,
        WoodlandOfficerReview reviewEntity,
        IList<CaseNoteModel> comments)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository.Setup(x => x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.From(reviewEntity));

        _viewCaseNotesService
            .Setup(x => x.GetSpecificCaseNotesAsync(It.IsAny<Guid>(), It.IsAny<CaseNoteType[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(comments);

        var result = await sut.GetSiteVisitDetailsAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.HasValue);

        var model = result.Value.Value;

        Assert.Equal(reviewEntity.SiteVisitArtefactsCreated, model.SiteVisitArtefactsCreated);
        Assert.Equal(reviewEntity.SiteVisitNotesRetrieved, model.SiteVisitNotesRetrieved);
        Assert.Equal(reviewEntity.SiteVisitNotNeeded, model.SiteVisitNotNeeded);
        Assert.Equal(comments, model.SiteVisitComments);

        _fellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _viewCaseNotesService.Verify(x => x.GetSpecificCaseNotesAsync(applicationId, new[] { CaseNoteType.SiteVisitComment }, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task GetPw14ChecksShouldReturnMaybeNoneIfNoEntryExists(Guid applicationId)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository.Setup(x => x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.None);

        var result = await sut.GetPw14ChecksAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.HasNoValue);

        _fellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task GetPw14ChecksShouldReturnExpectedValuesIfDataExists(
        Guid applicationId,
        WoodlandOfficerReview reviewEntity)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository.Setup(x => x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.From(reviewEntity));

        var result = await sut.GetPw14ChecksAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.HasValue);

        var model = result.Value.Value;

        Assert.Equal(reviewEntity.LandInformationSearchChecked, model.LandInformationSearchChecked);
        Assert.Equal(reviewEntity.AreProposalsUkfsCompliant, model.AreProposalsUkfsCompliant);
        Assert.Equal(reviewEntity.TpoOrCaDeclared, model.TpoOrCaDeclared);
        Assert.Equal(reviewEntity.IsApplicationValid, model.IsApplicationValid);
        Assert.Equal(reviewEntity.EiaThresholdExceeded, model.EiaThresholdExceeded);
        Assert.Equal(reviewEntity.EiaTrackerCompleted, model.EiaTrackerCompleted);
        Assert.Equal(reviewEntity.EiaChecklistDone, model.EiaChecklistDone);
        
        _fellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoData]
    public async Task GetForMobileAppsShouldReturnFailureIfApplicationNotFound(Guid applicationId)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository.Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.None);

        var result = await sut.GetApplicationDetailsForSiteVisitMobileLayersAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsFailure);

        _fellingLicenceApplicationRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task GetForMobileAppsShouldReturnFailureIfApplicationHasNoSubmittedPropertyDetail(
        Guid applicationId,
        FellingLicenceApplication application)
    {
        var sut = CreateSut();

        application.SubmittedFlaPropertyDetail = null;
        _fellingLicenceApplicationRepository.Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));

        var result = await sut.GetApplicationDetailsForSiteVisitMobileLayersAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsFailure);

        _fellingLicenceApplicationRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task GetForMobileAppsShouldReturnFailureIfApplicationHasNoLinkedPropertyProfile(
        Guid applicationId,
        FellingLicenceApplication application)
    {
        var sut = CreateSut();

        application.LinkedPropertyProfile = null;
        _fellingLicenceApplicationRepository.Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));

        var result = await sut.GetApplicationDetailsForSiteVisitMobileLayersAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsFailure);

        _fellingLicenceApplicationRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task GetForMobileAppsShouldReturnExpectedDetails(
        Guid applicationId,
        FellingLicenceApplication application,
        Polygon gisData)
    {
        var sut = CreateSut();

        foreach (var submittedFlaPropertyCompartment in application.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments)
        {
            submittedFlaPropertyCompartment.GISData = JsonConvert.SerializeObject(gisData);
        }

        _fellingLicenceApplicationRepository.Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));

        var result = await sut.GetApplicationDetailsForSiteVisitMobileLayersAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(application.ApplicationReference, result.Value.CaseReference);

        var expectedCompartmentDetails = application.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments
            .Select(x => x.ToInternalFullCompartmentDetails())
            .ToList();
        Assert.Equal(expectedCompartmentDetails.Count, result.Value.Compartments.Count);
        expectedCompartmentDetails.ForEach(x =>
        {
            Assert.Contains(result.Value.Compartments, y => 
                y.CompartmentNumber == x.CompartmentNumber
                && y.WoodlandName == x.WoodlandName
                && y.Designation == x.Designation
                && y.GISData == x.GISData);
        });

        _fellingLicenceApplicationRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task GetDetailsForConditionsNotification_WhenApplicationNotFound(
        Guid applicationId)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository.Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.None);

        var result = await sut.GetDetailsForConditionsNotificationAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsFailure);

        _fellingLicenceApplicationRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task GetDetailsForConditionsNotification_WhenApplicationFound(
        Guid applicationId,
        FellingLicenceApplication application)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository.Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));

        var result = await sut.GetDetailsForConditionsNotificationAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(application.ApplicationReference, result.Value.ApplicationReference);
        Assert.Equal(application.CreatedById, result.Value.ApplicationAuthorId);

        _fellingLicenceApplicationRepository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
    }

    private GetWoodlandOfficerReviewService CreateSut()
    {
        _fellingLicenceApplicationRepository.Reset();
        _viewCaseNotesService.Reset();
        _foresterServices.Reset();

        return new GetWoodlandOfficerReviewService(
            _fellingLicenceApplicationRepository.Object,
            _foresterServices.Object,
            _viewCaseNotesService.Object,
            new NullLogger<GetWoodlandOfficerReviewService>());
    }
}