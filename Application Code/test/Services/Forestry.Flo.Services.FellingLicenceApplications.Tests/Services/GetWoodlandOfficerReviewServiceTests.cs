using AutoFixture;
using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.Gis.Interfaces;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.AutoMoq;
using FluentEmail.Core;
using Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services;

public partial class GetWoodlandOfficerReviewServiceTests
{
    private readonly Mock<IFellingLicenceApplicationInternalRepository> _fellingLicenceApplicationRepository = new();
    private readonly Mock<IForesterServices> _foresterServices = new();
    private readonly Mock<IViewCaseNotesService> _viewCaseNotesService = new();

    private readonly Mock<IUpdateConfirmedFellingAndRestockingDetailsService> _updateConfirmedFellingAndRestockingService = new();
    private readonly IFixture _fixture;

    public GetWoodlandOfficerReviewServiceTests()
    {
        var fixture = new Fixture().Customize(new CompositeCustomization(
            new AutoMoqCustomization(),
            new SupportMutableValueTypesCustomization()));

        fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        fixture.Customize<DateOnly>(composer => composer.FromFactory<DateTime>(DateOnly.FromDateTime));

        _fixture = fixture;
    }

    [Theory, AutoData]
    public async Task ShouldReturnFailureIfRepositoryThrows(Guid applicationId)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception());

        var result = await sut.GetWoodlandOfficerReviewStatusAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsFailure);

        _fellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();

        _viewCaseNotesService.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task ShouldReturnFailureIfUnableToRetrieveProposedFellingAndRestockingDetails(Guid applicationId)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetProposedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<(List<ProposedFellingDetail>, List<ProposedRestockingDetail>)>("error"));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.None);

        _fellingLicenceApplicationRepository.Setup(x => x.GetPublicRegisterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<PublicRegister>.None);

        var result = await sut.GetWoodlandOfficerReviewStatusAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsFailure);

        _fellingLicenceApplicationRepository.Verify(x => x.GetPublicRegisterAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.Verify(x => x.GetProposedFellingAndRestockingDetailsForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        
        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();

        _viewCaseNotesService.VerifyNoOtherCalls();
    }


    [Theory, AutoData]
    public async Task ShouldReturnFailureIfUnableToRetrieveConfirmedFellingAndRestockingDetails(Guid applicationId)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetProposedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((new List<ProposedFellingDetail>(0), new List<ProposedRestockingDetail>(0))));

        _updateConfirmedFellingAndRestockingService.Setup(x =>
                x.RetrieveConfirmedFellingAndRestockingDetailModelAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<CombinedConfirmedFellingAndRestockingDetailRecord>("error"));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.None);

        _fellingLicenceApplicationRepository.Setup(x => x.GetPublicRegisterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<PublicRegister>.None);

        var result = await sut.GetWoodlandOfficerReviewStatusAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsFailure);

        _fellingLicenceApplicationRepository.Verify(x => x.GetPublicRegisterAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.Verify(x => x.GetProposedFellingAndRestockingDetailsForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _updateConfirmedFellingAndRestockingService.Verify(x => x.RetrieveConfirmedFellingAndRestockingDetailModelAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);

        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();

        _viewCaseNotesService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnFailure_WhenUnableToRetrieveCompartmentDesignations(Guid applicationId, CombinedConfirmedFellingAndRestockingDetailRecord cfandr)
    {
        var sut = CreateSut();

        _updateConfirmedFellingAndRestockingService.Setup(x =>
                x.RetrieveConfirmedFellingAndRestockingDetailModelAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(cfandr));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetProposedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((new List<ProposedFellingDetail>(0), new List<ProposedRestockingDetail>(0))));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.None);

        _fellingLicenceApplicationRepository.Setup(x => x.GetPublicRegisterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<PublicRegister>.None);

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetSubmittedFlaPropertyCompartmentsByApplicationIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<List<SubmittedFlaPropertyCompartment>>("error"));

        var result = await sut.GetWoodlandOfficerReviewStatusAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsFailure);

        _fellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.Verify(x => x.GetPublicRegisterAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.Verify(x => x.GetProposedFellingAndRestockingDetailsForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _updateConfirmedFellingAndRestockingService.Verify(x => x.RetrieveConfirmedFellingAndRestockingDetailModelAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.Verify(x => x.GetSubmittedFlaPropertyCompartmentsByApplicationIdAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);

        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();

        _viewCaseNotesService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnSuccess_DesignationsNotStarted_WhenNoCompartmentsHaveDesignationsYet(Guid applicationId, CombinedConfirmedFellingAndRestockingDetailRecord cfandr)
    {
        var cpts = _fixture.Build<SubmittedFlaPropertyCompartment>()
            .Without(x => x.SubmittedCompartmentDesignations)
            .Without(x => x.ConfirmedFellingDetails)
            .Without(x => x.SubmittedFlaPropertyDetail)
            .CreateMany();

        var sut = CreateSut();

        _updateConfirmedFellingAndRestockingService.Setup(x =>
                x.RetrieveConfirmedFellingAndRestockingDetailModelAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(cfandr));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetProposedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((new List<ProposedFellingDetail>(0), new List<ProposedRestockingDetail>(0))));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.None);

        _fellingLicenceApplicationRepository.Setup(x => x.GetPublicRegisterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<PublicRegister>.None);

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetSubmittedFlaPropertyCompartmentsByApplicationIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<List<SubmittedFlaPropertyCompartment>>(cpts.ToList()));

        var result = await sut.GetWoodlandOfficerReviewStatusAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(InternalReviewStepStatus.NotStarted, result.Value.WoodlandOfficerReviewTaskListStates.CompartmentDesignationsStepStatus);

        _fellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.Verify(x => x.GetPublicRegisterAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.Verify(x => x.GetProposedFellingAndRestockingDetailsForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _updateConfirmedFellingAndRestockingService.Verify(x => x.RetrieveConfirmedFellingAndRestockingDetailModelAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.Verify(x => x.GetSubmittedFlaPropertyCompartmentsByApplicationIdAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);

        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
        
        _viewCaseNotesService.Verify(x => x.GetSpecificCaseNotesAsync(applicationId, new[] { CaseNoteType.WoodlandOfficerReviewComment }, It.IsAny<CancellationToken>()), Times.Once);
        _viewCaseNotesService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnSuccess_DesignationsInProgress_WhenAtLeastOneCompartmentHasDesignationsSet(Guid applicationId, CombinedConfirmedFellingAndRestockingDetailRecord cfandr)
    {
        var cpts = _fixture.Build<SubmittedFlaPropertyCompartment>()
            .Without(x => x.SubmittedCompartmentDesignations)
            .Without(x => x.ConfirmedFellingDetails)
            .Without(x => x.SubmittedFlaPropertyDetail)
            .CreateMany()
            .ToList();

        cpts.First().SubmittedCompartmentDesignations = new SubmittedCompartmentDesignations
        {
            None = true,
            HasBeenReviewed = true
        };

        var sut = CreateSut();

        _updateConfirmedFellingAndRestockingService.Setup(x =>
                x.RetrieveConfirmedFellingAndRestockingDetailModelAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(cfandr));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetProposedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((new List<ProposedFellingDetail>(0), new List<ProposedRestockingDetail>(0))));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.None);

        _fellingLicenceApplicationRepository.Setup(x => x.GetPublicRegisterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<PublicRegister>.None);

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetSubmittedFlaPropertyCompartmentsByApplicationIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<List<SubmittedFlaPropertyCompartment>>(cpts.ToList()));

        var result = await sut.GetWoodlandOfficerReviewStatusAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(InternalReviewStepStatus.InProgress, result.Value.WoodlandOfficerReviewTaskListStates.CompartmentDesignationsStepStatus);

        _fellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.Verify(x => x.GetPublicRegisterAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.Verify(x => x.GetProposedFellingAndRestockingDetailsForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _updateConfirmedFellingAndRestockingService.Verify(x => x.RetrieveConfirmedFellingAndRestockingDetailModelAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.Verify(x => x.GetSubmittedFlaPropertyCompartmentsByApplicationIdAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);

        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();

        _viewCaseNotesService.Verify(x => x.GetSpecificCaseNotesAsync(applicationId, new[] { CaseNoteType.WoodlandOfficerReviewComment }, It.IsAny<CancellationToken>()), Times.Once);
        _viewCaseNotesService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnSuccess_DesignationsComplete_WhenWoodlandOfficerReviewHasDesignationsComplete(Guid applicationId, CombinedConfirmedFellingAndRestockingDetailRecord cfandr)
    {
        var review = _fixture.Build<WoodlandOfficerReview>()
            .With(x => x.DesignationsComplete, true)
            .Without(x => x.FellingLicenceApplication)
            .Without(x => x.SiteVisitEvidences)
            .Without(x => x.FellingAndRestockingAmendmentReviews)
            .Create();

        var sut = CreateSut();

        _updateConfirmedFellingAndRestockingService.Setup(x =>
                x.RetrieveConfirmedFellingAndRestockingDetailModelAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(cfandr));

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetUserExternalAccessLinksByApplicationIdAndPurposeAsync(It.IsAny<Guid>(), It.IsAny<ExternalAccessLinkType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ExternalAccessLink>());

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetProposedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((new List<ProposedFellingDetail>(0), new List<ProposedRestockingDetail>(0))));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.From(review));

        _fellingLicenceApplicationRepository.Setup(x => x.GetPublicRegisterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<PublicRegister>.None);

        var result = await sut.GetWoodlandOfficerReviewStatusAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(InternalReviewStepStatus.Completed, result.Value.WoodlandOfficerReviewTaskListStates.CompartmentDesignationsStepStatus);

        _fellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.Verify(x => x.GetPublicRegisterAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.Verify(x => x.GetProposedFellingAndRestockingDetailsForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _updateConfirmedFellingAndRestockingService.Verify(x => x.RetrieveConfirmedFellingAndRestockingDetailModelAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.Verify(x => x.GetUserExternalAccessLinksByApplicationIdAndPurposeAsync(applicationId, ExternalAccessLinkType.ConsulteeInvite, It.IsAny<CancellationToken>()), Times.Once);

        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();

        _viewCaseNotesService.Verify(x => x.GetSpecificCaseNotesAsync(applicationId, new[] { CaseNoteType.WoodlandOfficerReviewComment }, It.IsAny<CancellationToken>()), Times.Once);
        _viewCaseNotesService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnSuccess_NoStepsStarted_WhenNoReviewDataRetrieved(Guid applicationId, CombinedConfirmedFellingAndRestockingDetailRecord cfandr)
    {
        var sut = CreateSut();

        cfandr.ConfirmedFellingAndRestockingDetailModels.ForEach(x =>
            x.ConfirmedFellingDetailModels.ForEach(z =>
            {
                z.AmendedProperties = [];
                z.ConfirmedRestockingDetailModels.ForEach(r => r.AmendedProperties = []);
            }));

        _updateConfirmedFellingAndRestockingService.Setup(x =>
                x.RetrieveConfirmedFellingAndRestockingDetailModelAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(cfandr));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetProposedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((new List<ProposedFellingDetail>(0), new List<ProposedRestockingDetail>(0))));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.None);

        _fellingLicenceApplicationRepository.Setup(x => x.GetPublicRegisterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<PublicRegister>.None);

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetSubmittedFlaPropertyCompartmentsByApplicationIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<List<SubmittedFlaPropertyCompartment>>([]));

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
        Assert.Equal(InternalReviewStepStatus.NotStarted, result.Value.WoodlandOfficerReviewTaskListStates.ConditionsStepStatus);
        Assert.Equal(InternalReviewStepStatus.NotStarted, result.Value.WoodlandOfficerReviewTaskListStates.ConsultationStepStatus);
        Assert.Equal(InternalReviewStepStatus.NotStarted, result.Value.WoodlandOfficerReviewTaskListStates.CompartmentDesignationsStepStatus);
        Assert.Equal(InternalReviewStepStatus.NotRequired, result.Value.WoodlandOfficerReviewTaskListStates.LarchApplicationStatus);
        Assert.Equal(InternalReviewStepStatus.NotRequired, result.Value.WoodlandOfficerReviewTaskListStates.LarchFlyoverStatus);
        Assert.Equal(InternalReviewStepStatus.NotStarted, result.Value.WoodlandOfficerReviewTaskListStates.EiaScreeningStatus);
        Assert.Equal(InternalReviewStepStatus.CannotStartYet, result.Value.WoodlandOfficerReviewTaskListStates.FinalChecksStepStatus);

        _fellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.Verify(x => x.GetPublicRegisterAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.Verify(x => x.GetProposedFellingAndRestockingDetailsForApplicationAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _updateConfirmedFellingAndRestockingService.Verify(x => x.RetrieveConfirmedFellingAndRestockingDetailModelAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.Verify(x => x.GetSubmittedFlaPropertyCompartmentsByApplicationIdAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);

        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
        
        _viewCaseNotesService.Verify(x => x.GetSpecificCaseNotesAsync(applicationId, new [] {  CaseNoteType.WoodlandOfficerReviewComment  }, It.IsAny<CancellationToken>()), Times.Once);
        _viewCaseNotesService.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnSuccess_WithComments(Guid applicationId, List<CaseNoteModel> caseNotes, CombinedConfirmedFellingAndRestockingDetailRecord cfandr)
    {
        var sut = CreateSut();

        _updateConfirmedFellingAndRestockingService.Setup(x =>
                x.RetrieveConfirmedFellingAndRestockingDetailModelAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(cfandr));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetProposedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((new List<ProposedFellingDetail>(0), new List<ProposedRestockingDetail>(0))));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.None);

        _fellingLicenceApplicationRepository.Setup(x => x.GetPublicRegisterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<PublicRegister>.None);

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetSubmittedFlaPropertyCompartmentsByApplicationIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<List<SubmittedFlaPropertyCompartment>>([]));

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

    [Theory, AutoMoqData]
    public async Task ShouldReturnSuccess_PublicRegisterCompleted_Exempt(Guid applicationId, CombinedConfirmedFellingAndRestockingDetailRecord cfandr)
    {
        var pr = new PublicRegister
        {
            WoodlandOfficerSetAsExemptFromConsultationPublicRegister = true,
            WoodlandOfficerConsultationPublicRegisterExemptionReason = "Test"
        };

        var sut = CreateSut();

        _updateConfirmedFellingAndRestockingService.Setup(x =>
                x.RetrieveConfirmedFellingAndRestockingDetailModelAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(cfandr));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetProposedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((new List<ProposedFellingDetail>(0), new List<ProposedRestockingDetail>(0))));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.None);

        _fellingLicenceApplicationRepository.Setup(x => x.GetPublicRegisterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<PublicRegister>.From(pr));

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetSubmittedFlaPropertyCompartmentsByApplicationIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<List<SubmittedFlaPropertyCompartment>>([]));

        _viewCaseNotesService.Setup(x =>
                x.GetSpecificCaseNotesAsync(It.IsAny<Guid>(), It.IsAny<CaseNoteType[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CaseNoteModel>(0));

        var result = await sut.GetWoodlandOfficerReviewStatusAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(InternalReviewStepStatus.Completed, result.Value.WoodlandOfficerReviewTaskListStates.PublicRegisterStepStatus);

        _fellingLicenceApplicationRepository.Verify(x => x.GetPublicRegisterAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnSuccess_PublicRegisterCompleted_Removed(Guid applicationId, CombinedConfirmedFellingAndRestockingDetailRecord cfandr)
    {
        var pr = new PublicRegister
        {
            ConsultationPublicRegisterPublicationTimestamp = DateTime.UtcNow.AddDays(-2),
            ConsultationPublicRegisterExpiryTimestamp = DateTime.UtcNow.AddDays(2),
            ConsultationPublicRegisterRemovedTimestamp = DateTime.UtcNow
        };

        var sut = CreateSut();

        _updateConfirmedFellingAndRestockingService.Setup(x =>
                x.RetrieveConfirmedFellingAndRestockingDetailModelAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(cfandr));

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
        _fellingLicenceApplicationRepository
            .Setup(x => x.GetSubmittedFlaPropertyCompartmentsByApplicationIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<List<SubmittedFlaPropertyCompartment>>([]));

        var result = await sut.GetWoodlandOfficerReviewStatusAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(InternalReviewStepStatus.Completed, result.Value.WoodlandOfficerReviewTaskListStates.PublicRegisterStepStatus);

        _fellingLicenceApplicationRepository.Verify(x => x.GetPublicRegisterAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnSuccess_PublicRegisterInProgress(Guid applicationId, CombinedConfirmedFellingAndRestockingDetailRecord cfandr)
    {
        var pr = new PublicRegister
        {
            ConsultationPublicRegisterPublicationTimestamp = DateTime.UtcNow.AddDays(-2),
            ConsultationPublicRegisterExpiryTimestamp = DateTime.UtcNow.AddDays(2)
        };

        var sut = CreateSut();

        _updateConfirmedFellingAndRestockingService.Setup(x =>
                x.RetrieveConfirmedFellingAndRestockingDetailModelAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(cfandr));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetProposedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((new List<ProposedFellingDetail>(0), new List<ProposedRestockingDetail>(0))));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.None);

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetSubmittedFlaPropertyCompartmentsByApplicationIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<List<SubmittedFlaPropertyCompartment>>([]));

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

    [Theory, AutoMoqData]
    public async Task ShouldReturnSuccess_SiteVisitNotStarted(Guid applicationId, CombinedConfirmedFellingAndRestockingDetailRecord cfandr)
    {
        var review = new WoodlandOfficerReview
        {
            DesignationsComplete = true
        };

        var sut = CreateSut();

        _updateConfirmedFellingAndRestockingService.Setup(x =>
                x.RetrieveConfirmedFellingAndRestockingDetailModelAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(cfandr));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetProposedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((new List<ProposedFellingDetail>(0), new List<ProposedRestockingDetail>(0))));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.From(review));

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetUserExternalAccessLinksByApplicationIdAndPurposeAsync(It.IsAny<Guid>(),
                It.IsAny<ExternalAccessLinkType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

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

    [Theory, AutoMoqData]
    public async Task ShouldReturnSuccess_SiteVisitCompleted_NotNeeded(Guid applicationId, CombinedConfirmedFellingAndRestockingDetailRecord cfandr)
    {
        var review = new WoodlandOfficerReview
        {
            SiteVisitNeeded = false,
            DesignationsComplete = true
        };

        var sut = CreateSut();

        _updateConfirmedFellingAndRestockingService.Setup(x =>
                x.RetrieveConfirmedFellingAndRestockingDetailModelAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(cfandr));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetProposedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((new List<ProposedFellingDetail>(0), new List<ProposedRestockingDetail>(0))));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.From(review));

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetUserExternalAccessLinksByApplicationIdAndPurposeAsync(It.IsAny<Guid>(),
                It.IsAny<ExternalAccessLinkType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

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

    [Theory, AutoMoqData]
    public async Task ShouldReturnSuccess_SiteVisitCompleted(Guid applicationId, [CombinatorialValues]bool? arrangementsMade, CombinedConfirmedFellingAndRestockingDetailRecord cfandr)
    {
        var review = new WoodlandOfficerReview
        {
            SiteVisitNeeded = true,
            SiteVisitArrangementsMade = arrangementsMade,
            SiteVisitComplete = true,
            DesignationsComplete = true
        };

        var sut = CreateSut();

        _updateConfirmedFellingAndRestockingService.Setup(x =>
                x.RetrieveConfirmedFellingAndRestockingDetailModelAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(cfandr));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetProposedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((new List<ProposedFellingDetail>(0), new List<ProposedRestockingDetail>(0))));

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetUserExternalAccessLinksByApplicationIdAndPurposeAsync(It.IsAny<Guid>(),
                It.IsAny<ExternalAccessLinkType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

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

    [Theory, AutoMoqData]
    public async Task ShouldReturnSuccess_SiteVisitInProgress(Guid applicationId, [CombinatorialValues]bool? arrangementsMade, CombinedConfirmedFellingAndRestockingDetailRecord cfandr)
    {
        var review = new WoodlandOfficerReview
        {
            SiteVisitNeeded = true,
            SiteVisitArrangementsMade = arrangementsMade,
            SiteVisitComplete = false,
            DesignationsComplete = true
        };

        var sut = CreateSut();

        _updateConfirmedFellingAndRestockingService.Setup(x =>
                x.RetrieveConfirmedFellingAndRestockingDetailModelAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(cfandr));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetProposedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((new List<ProposedFellingDetail>(0), new List<ProposedRestockingDetail>(0))));

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetUserExternalAccessLinksByApplicationIdAndPurposeAsync(It.IsAny<Guid>(),
                It.IsAny<ExternalAccessLinkType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

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

    [Theory, AutoMoqData]
    public async Task ShouldReturnSuccess_ConsultationsNotStarted(Guid applicationId, CombinedConfirmedFellingAndRestockingDetailRecord cfandr)
    {
        var review = new WoodlandOfficerReview
        {
            ApplicationNeedsConsultations = null,
            ConsultationsComplete = false,
            DesignationsComplete = true
        };

        var sut = CreateSut();

        _updateConfirmedFellingAndRestockingService.Setup(x =>
                x.RetrieveConfirmedFellingAndRestockingDetailModelAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(cfandr));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetProposedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((new List<ProposedFellingDetail>(0), new List<ProposedRestockingDetail>(0))));

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetUserExternalAccessLinksByApplicationIdAndPurposeAsync(It.IsAny<Guid>(),
                It.IsAny<ExternalAccessLinkType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

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

        Assert.Equal(InternalReviewStepStatus.NotStarted, result.Value.WoodlandOfficerReviewTaskListStates.ConsultationStepStatus);

        _fellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnSuccess_ConsultationsNotNeeded(Guid applicationId, CombinedConfirmedFellingAndRestockingDetailRecord cfandr)
    {
        var review = new WoodlandOfficerReview
        {
            ApplicationNeedsConsultations = false,
            ConsultationsComplete = false,
            DesignationsComplete = true
        };

        var sut = CreateSut();

        _updateConfirmedFellingAndRestockingService.Setup(x =>
                x.RetrieveConfirmedFellingAndRestockingDetailModelAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(cfandr));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetProposedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((new List<ProposedFellingDetail>(0), new List<ProposedRestockingDetail>(0))));

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetUserExternalAccessLinksByApplicationIdAndPurposeAsync(It.IsAny<Guid>(),
                It.IsAny<ExternalAccessLinkType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

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

        Assert.Equal(InternalReviewStepStatus.NotRequired, result.Value.WoodlandOfficerReviewTaskListStates.ConsultationStepStatus);

        _fellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnSuccess_ConsultationsInProgress(
        Guid applicationId,
        ExternalAccessLink accessLink,
        CombinedConfirmedFellingAndRestockingDetailRecord cfandr)
    {
        var review = new WoodlandOfficerReview
        {
            ApplicationNeedsConsultations = true,
            ConsultationsComplete = false,
            DesignationsComplete = true
        };

        var sut = CreateSut();

        _updateConfirmedFellingAndRestockingService.Setup(x =>
                x.RetrieveConfirmedFellingAndRestockingDetailModelAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(cfandr));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetProposedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((new List<ProposedFellingDetail>(0), new List<ProposedRestockingDetail>(0))));

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetUserExternalAccessLinksByApplicationIdAndPurposeAsync(It.IsAny<Guid>(),
                It.IsAny<ExternalAccessLinkType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([accessLink]);

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

        Assert.Equal(InternalReviewStepStatus.InProgress, result.Value.WoodlandOfficerReviewTaskListStates.ConsultationStepStatus);

        _fellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnSuccess_ConsultationsComplete(
        Guid applicationId,
        ExternalAccessLink accessLink,
        CombinedConfirmedFellingAndRestockingDetailRecord cfandr)
    {
        var review = new WoodlandOfficerReview
        {
            ApplicationNeedsConsultations = true,
            ConsultationsComplete = true,
            DesignationsComplete = true
        };

        var sut = CreateSut();

        _updateConfirmedFellingAndRestockingService.Setup(x =>
                x.RetrieveConfirmedFellingAndRestockingDetailModelAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(cfandr));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetProposedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((new List<ProposedFellingDetail>(0), new List<ProposedRestockingDetail>(0))));

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetUserExternalAccessLinksByApplicationIdAndPurposeAsync(It.IsAny<Guid>(),
                It.IsAny<ExternalAccessLinkType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([accessLink]);

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

        Assert.Equal(InternalReviewStepStatus.Completed, result.Value.WoodlandOfficerReviewTaskListStates.ConsultationStepStatus);

        _fellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnSuccess_PW14ChecksNotStarted(Guid applicationId, CombinedConfirmedFellingAndRestockingDetailRecord cfandr)
    {
        var review = new WoodlandOfficerReview
        {
            DesignationsComplete = true
        };

        var sut = CreateSut();

        _updateConfirmedFellingAndRestockingService.Setup(x =>
                x.RetrieveConfirmedFellingAndRestockingDetailModelAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(cfandr));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetProposedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((new List<ProposedFellingDetail>(0), new List<ProposedRestockingDetail>(0))));

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetUserExternalAccessLinksByApplicationIdAndPurposeAsync(It.IsAny<Guid>(),
                It.IsAny<ExternalAccessLinkType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

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

    [Theory, AutoMoqData]
    public async Task ShouldReturnSuccess_PW14ChecksCompleted(Guid applicationId, CombinedConfirmedFellingAndRestockingDetailRecord cfandr)
    {
        var review = new WoodlandOfficerReview
        {
            Pw14ChecksComplete = true,
            DesignationsComplete = true
        };

        var sut = CreateSut();

        _updateConfirmedFellingAndRestockingService.Setup(x =>
                x.RetrieveConfirmedFellingAndRestockingDetailModelAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(cfandr));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetProposedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((new List<ProposedFellingDetail>(0), new List<ProposedRestockingDetail>(0))));

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetUserExternalAccessLinksByApplicationIdAndPurposeAsync(It.IsAny<Guid>(),
                It.IsAny<ExternalAccessLinkType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

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
    public async Task ShouldReturnSuccess_PW14ChecksInProgress(Guid applicationId, WoodlandOfficerReview review, CombinedConfirmedFellingAndRestockingDetailRecord cfandr)
    {
        review.Pw14ChecksComplete = false;
        review.EiaTrackerCompleted = true;
        review.DesignationsComplete = true;

        var sut = CreateSut();

        _updateConfirmedFellingAndRestockingService.Setup(x =>
                x.RetrieveConfirmedFellingAndRestockingDetailModelAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(cfandr));

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

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetUserExternalAccessLinksByApplicationIdAndPurposeAsync(It.IsAny<Guid>(),
                It.IsAny<ExternalAccessLinkType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await sut.GetWoodlandOfficerReviewStatusAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(InternalReviewStepStatus.InProgress, result.Value.WoodlandOfficerReviewTaskListStates.Pw14ChecksStepStatus);

        _fellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnSuccess_ConfirmedFAndRInProgress(
        Guid applicationId, 
        CombinedConfirmedFellingAndRestockingDetailRecord cfandr,
        List<ProposedFellingDetail> proposedFelling,
        List<ProposedRestockingDetail> proposedRestocking,
        WoodlandOfficerReview review)
    {
        review.ConfirmedFellingAndRestockingComplete = false;
        review.DesignationsComplete = true;

        var sut = CreateSut();

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetProposedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((proposedFelling, proposedRestocking)));

        _updateConfirmedFellingAndRestockingService.Setup(x =>
                x.RetrieveConfirmedFellingAndRestockingDetailModelAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(cfandr));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.From(review));

        _fellingLicenceApplicationRepository.Setup(x => x.GetPublicRegisterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<PublicRegister>.None);

        _viewCaseNotesService.Setup(x =>
                x.GetSpecificCaseNotesAsync(It.IsAny<Guid>(), It.IsAny<CaseNoteType[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CaseNoteModel>(0));

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetUserExternalAccessLinksByApplicationIdAndPurposeAsync(It.IsAny<Guid>(),
                It.IsAny<ExternalAccessLinkType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await sut.GetWoodlandOfficerReviewStatusAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(InternalReviewStepStatus.InProgress, result.Value.WoodlandOfficerReviewTaskListStates.FellingAndRestockingStepStatus);

        _fellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _updateConfirmedFellingAndRestockingService.Verify(x => x.RetrieveConfirmedFellingAndRestockingDetailModelAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnSuccess_ConfirmedFAndRCompleted(
        Guid applicationId, 
        CombinedConfirmedFellingAndRestockingDetailRecord cfandr,
        List<ProposedFellingDetail> proposedFelling,
        List<ProposedRestockingDetail> proposedRestocking,
        WoodlandOfficerReview review)
    {
        review.ConfirmedFellingAndRestockingComplete = true;
        review.DesignationsComplete = true;

        var sut = CreateSut();

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetProposedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((proposedFelling, proposedRestocking)));

        _updateConfirmedFellingAndRestockingService.Setup(x =>
                x.RetrieveConfirmedFellingAndRestockingDetailModelAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(cfandr));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.From(review));

        _fellingLicenceApplicationRepository.Setup(x => x.GetPublicRegisterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<PublicRegister>.None);

        _viewCaseNotesService.Setup(x =>
                x.GetSpecificCaseNotesAsync(It.IsAny<Guid>(), It.IsAny<CaseNoteType[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CaseNoteModel>(0));

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetUserExternalAccessLinksByApplicationIdAndPurposeAsync(It.IsAny<Guid>(),
                It.IsAny<ExternalAccessLinkType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await sut.GetWoodlandOfficerReviewStatusAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(InternalReviewStepStatus.Completed, result.Value.WoodlandOfficerReviewTaskListStates.FellingAndRestockingStepStatus);

        _fellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _updateConfirmedFellingAndRestockingService.Verify(x => x.RetrieveConfirmedFellingAndRestockingDetailModelAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnSuccess_ConditionsNotStarted(Guid applicationId, CombinedConfirmedFellingAndRestockingDetailRecord cfandr)
    {
        var review = new WoodlandOfficerReview
        {
            IsConditional = null,
            DesignationsComplete = true
        };

        var sut = CreateSut();

        _updateConfirmedFellingAndRestockingService.Setup(x =>
                x.RetrieveConfirmedFellingAndRestockingDetailModelAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(cfandr));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetProposedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((new List<ProposedFellingDetail>(0), new List<ProposedRestockingDetail>(0))));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.From(review));

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetUserExternalAccessLinksByApplicationIdAndPurposeAsync(It.IsAny<Guid>(),
                It.IsAny<ExternalAccessLinkType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

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

    [Theory, AutoMoqData]
    public async Task ShouldReturnSuccess_ConditionsInProgress(Guid applicationId, CombinedConfirmedFellingAndRestockingDetailRecord cfandr)
    {
        var review = new WoodlandOfficerReview
        {
            IsConditional = true,
            ConditionsToApplicantDate = null,
            DesignationsComplete = true
        };

        var sut = CreateSut();

        _updateConfirmedFellingAndRestockingService.Setup(x =>
                x.RetrieveConfirmedFellingAndRestockingDetailModelAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(cfandr));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetProposedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((new List<ProposedFellingDetail>(0), new List<ProposedRestockingDetail>(0))));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.From(review));

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetUserExternalAccessLinksByApplicationIdAndPurposeAsync(It.IsAny<Guid>(),
                It.IsAny<ExternalAccessLinkType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

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

    [Theory, AutoMoqData]
    public async Task ShouldReturnSuccess_ConditionsCompleted_NotConditions(Guid applicationId, CombinedConfirmedFellingAndRestockingDetailRecord cfandr)
    {
        var review = new WoodlandOfficerReview
        {
            IsConditional = false,
            ConditionsToApplicantDate = null,
            DesignationsComplete = true
        };

        var sut = CreateSut();

        _updateConfirmedFellingAndRestockingService.Setup(x =>
                x.RetrieveConfirmedFellingAndRestockingDetailModelAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(cfandr));

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetUserExternalAccessLinksByApplicationIdAndPurposeAsync(It.IsAny<Guid>(),
                It.IsAny<ExternalAccessLinkType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

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

    [Theory, AutoMoqData]
    public async Task ShouldReturnSuccess_ConditionsCompleted_SentToApplicant(Guid applicationId, CombinedConfirmedFellingAndRestockingDetailRecord cfandr)
    {
        var review = new WoodlandOfficerReview
        {
            IsConditional = true,
            ConditionsToApplicantDate = DateTime.UtcNow,
            DesignationsComplete = true
        };

        var sut = CreateSut();

        _updateConfirmedFellingAndRestockingService.Setup(x =>
                x.RetrieveConfirmedFellingAndRestockingDetailModelAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(cfandr));
        
        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetProposedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((new List<ProposedFellingDetail>(0), new List<ProposedRestockingDetail>(0))));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.From(review));

        _fellingLicenceApplicationRepository.Setup(x => x.GetPublicRegisterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<PublicRegister>.None);

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetUserExternalAccessLinksByApplicationIdAndPurposeAsync(It.IsAny<Guid>(),
                It.IsAny<ExternalAccessLinkType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _viewCaseNotesService.Setup(x =>
                x.GetSpecificCaseNotesAsync(It.IsAny<Guid>(), It.IsAny<CaseNoteType[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CaseNoteModel>(0));

        var result = await sut.GetWoodlandOfficerReviewStatusAsync(applicationId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(InternalReviewStepStatus.Completed, result.Value.WoodlandOfficerReviewTaskListStates.ConditionsStepStatus);

        _fellingLicenceApplicationRepository.Verify(x => x.GetWoodlandOfficerReviewAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task ShouldReturnSuccess_LarchCheckNotRequired_WithNoLarchAndNotConfirmedFellingYet(Guid applicationId, CombinedConfirmedFellingAndRestockingDetailRecord cfandr)
    {
        var review = new WoodlandOfficerReview
        {
            IsConditional = true,
            ConditionsToApplicantDate = DateTime.UtcNow,
            DesignationsComplete = true
        };

        var sut = CreateSut();

        _updateConfirmedFellingAndRestockingService.Setup(x =>
                x.RetrieveConfirmedFellingAndRestockingDetailModelAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(cfandr));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetProposedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((new List<ProposedFellingDetail>(0), new List<ProposedRestockingDetail>(0))));

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetUserExternalAccessLinksByApplicationIdAndPurposeAsync(It.IsAny<Guid>(),
                It.IsAny<ExternalAccessLinkType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

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

    [Theory, AutoMoqData]
    public async Task ShouldReturnSuccess_LarchCheckCannotStartYet_WithLarchAndNotConfirmedFellingYet(
        Guid applicationId, CombinedConfirmedFellingAndRestockingDetailRecord cfandr)
    {
        var review = new WoodlandOfficerReview
        {
            IsConditional = true,
            ConditionsToApplicantDate = DateTime.UtcNow,
            DesignationsComplete = true,
            ConfirmedFellingAndRestockingComplete = false
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

        _updateConfirmedFellingAndRestockingService.Setup(x =>
                x.RetrieveConfirmedFellingAndRestockingDetailModelAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(cfandr));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetProposedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((new List<ProposedFellingDetail> { felling }, new List<ProposedRestockingDetail>(0))));

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetUserExternalAccessLinksByApplicationIdAndPurposeAsync(It.IsAny<Guid>(),
                It.IsAny<ExternalAccessLinkType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

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

    [Theory, AutoMoqData]
    public async Task ShouldReturnSuccess_LarchCheckNoStarted(
        Guid applicationId, CombinedConfirmedFellingAndRestockingDetailRecord cfandr)
    {
        var review = new WoodlandOfficerReview
        {
            ConfirmedFellingAndRestockingComplete = true,
            DesignationsComplete = true
        };

        var species = _fixture.Build<ConfirmedFellingSpecies>()
            .With(x => x.Species, "EL")
            .Without(x => x.ConfirmedFellingDetail)
            .Create();

        cfandr.ConfirmedFellingAndRestockingDetailModels.ForEach(x =>
                x.ConfirmedFellingDetailModels.ForEach(y => y.ConfirmedFellingSpecies = [species]));

        var sut = CreateSut();

        _updateConfirmedFellingAndRestockingService.Setup(x =>
                x.RetrieveConfirmedFellingAndRestockingDetailModelAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(cfandr));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetProposedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((new List<ProposedFellingDetail>(0), new List<ProposedRestockingDetail>(0))));

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetUserExternalAccessLinksByApplicationIdAndPurposeAsync(It.IsAny<Guid>(),
                It.IsAny<ExternalAccessLinkType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

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

    [Theory, AutoMoqData]
    public async Task ShouldReturnSuccess_LarchCheckInProgress(Guid applicationId, CombinedConfirmedFellingAndRestockingDetailRecord cfandr)
    {
        var review = new WoodlandOfficerReview
        {
            ConfirmedFellingAndRestockingComplete = true,
            LarchCheckComplete = false,
            DesignationsComplete = true
        };

        var species = _fixture.Build<ConfirmedFellingSpecies>()
            .With(x => x.Species, "EL")
            .Without(x => x.ConfirmedFellingDetail)
            .Create();

        cfandr.ConfirmedFellingAndRestockingDetailModels.ForEach(x =>
                x.ConfirmedFellingDetailModels.ForEach(y => y.ConfirmedFellingSpecies = [species]));

        var sut = CreateSut();

        _updateConfirmedFellingAndRestockingService.Setup(x =>
                x.RetrieveConfirmedFellingAndRestockingDetailModelAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(cfandr));

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetUserExternalAccessLinksByApplicationIdAndPurposeAsync(It.IsAny<Guid>(),
                It.IsAny<ExternalAccessLinkType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

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

    [Theory, AutoMoqData]
    public async Task ShouldReturnSuccess_LarchCheckComplete(Guid applicationId, CombinedConfirmedFellingAndRestockingDetailRecord cfandr)
    {
        var review = new WoodlandOfficerReview
        {
            ConfirmedFellingAndRestockingComplete = true,
            LarchCheckComplete = true,
            DesignationsComplete = true
        };

        var species = _fixture.Build<ConfirmedFellingSpecies>()
            .With(x => x.Species, "EL")
            .Without(x => x.ConfirmedFellingDetail)
            .Create();

        cfandr.ConfirmedFellingAndRestockingDetailModels.ForEach(x =>
            x.ConfirmedFellingDetailModels.ForEach(y => y.ConfirmedFellingSpecies = [species]));

        var sut = CreateSut();

        _updateConfirmedFellingAndRestockingService.Setup(x =>
                x.RetrieveConfirmedFellingAndRestockingDetailModelAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(cfandr));

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetUserExternalAccessLinksByApplicationIdAndPurposeAsync(It.IsAny<Guid>(),
                It.IsAny<ExternalAccessLinkType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

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
    public async Task ShouldReturnSuccess_LarchFlyoverNotRequired_WhenConfirmInspectionLogIsFalse(Guid applicationId, CombinedConfirmedFellingAndRestockingDetailRecord cfandr)
    {
        var sut = CreateSut();

        // Arrange larch species in confirmed details so it's a larch application
        var species = _fixture.Build<ConfirmedFellingSpecies>()
            .With(x => x.Species, "EL")
            .Without(x => x.ConfirmedFellingDetail)
            .Create();

        cfandr.ConfirmedFellingAndRestockingDetailModels.ForEach(x =>
            x.ConfirmedFellingDetailModels.ForEach(y => y.ConfirmedFellingSpecies = [species]));

        _fellingLicenceApplicationRepository
            .Setup(x => x.GetUserExternalAccessLinksByApplicationIdAndPurposeAsync(It.IsAny<Guid>(),
                It.IsAny<ExternalAccessLinkType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var review = new WoodlandOfficerReview
        {
            ConfirmedFellingAndRestockingComplete = true,
            LarchCheckComplete = true,
            DesignationsComplete = true,
            FellingLicenceApplication = new FellingLicenceApplication
            {
                LarchCheckDetails = new LarchCheckDetails
                {
                    ConfirmInspectionLog = false // not true => flyover is not required
                }
            }
        };

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetProposedFellingAndRestockingDetailsForApplicationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success((new List<ProposedFellingDetail>(0), new List<ProposedRestockingDetail>(0))));

        _updateConfirmedFellingAndRestockingService.Setup(x =>
                x.RetrieveConfirmedFellingAndRestockingDetailModelAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(cfandr));

        _fellingLicenceApplicationRepository.Setup(x =>
                x.GetWoodlandOfficerReviewAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<WoodlandOfficerReview>.From(review));

        _fellingLicenceApplicationRepository.Setup(x => x.GetPublicRegisterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<PublicRegister>.None);

        _viewCaseNotesService.Setup(x =>
                x.GetSpecificCaseNotesAsync(It.IsAny<Guid>(), It.IsAny<CaseNoteType[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CaseNoteModel>(0));

        // Act
        var result = await sut.GetWoodlandOfficerReviewStatusAsync(applicationId, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(InternalReviewStepStatus.NotRequired, result.Value.WoodlandOfficerReviewTaskListStates.LarchFlyoverStatus);
    }

    private GetWoodlandOfficerReviewService CreateSut()
    {
        _fellingLicenceApplicationRepository.Reset();
        _viewCaseNotesService.Reset();
        _foresterServices.Reset();
        _updateConfirmedFellingAndRestockingService.Reset();

        return new GetWoodlandOfficerReviewService(
            _fellingLicenceApplicationRepository.Object,
            _foresterServices.Object,
            _viewCaseNotesService.Object,
            _updateConfirmedFellingAndRestockingService.Object,
            new NullLogger<GetWoodlandOfficerReviewService>());
    }
}