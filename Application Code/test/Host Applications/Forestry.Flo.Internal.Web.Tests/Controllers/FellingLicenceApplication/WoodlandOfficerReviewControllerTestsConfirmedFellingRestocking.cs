using AutoFixture;
using CSharpFunctionalExtensions;
using CSharpFunctionalExtensions.ValueTasks;
using FluentValidation;
using FluentValidation.Results;
using Forestry.Flo.Internal.Web.Controllers.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication.WoodlandOfficerReview;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.PropertyProfiles.DataImports;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Threading;
using SubmittedFlaPropertyCompartment = Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview.SubmittedFlaPropertyCompartment;

namespace Forestry.Flo.Internal.Web.Tests.Controllers.FellingLicenceApplication;

public class WoodlandOfficerReviewControllerTestsConfirmedFellingRestocking
{
    private readonly Mock<IConfirmedFellingAndRestockingDetailsUseCase> _cfrUseCaseMock;
    private readonly Mock<IValidator<AmendConfirmedFellingDetailsViewModel>> _amendFellingValidatorMock;
    private readonly Mock<IValidator<AddNewConfirmedFellingDetailsViewModel>> _addFellingValidatorMock;
    private readonly Mock<IValidator<AmendConfirmedRestockingDetailsViewModel>> _amendRestockingValidatorMock;
    private readonly WoodlandOfficerReviewController _controller;
    private readonly Guid _applicationId = Guid.NewGuid();
    private readonly Guid _compartmentId = Guid.NewGuid();
    private readonly Guid _confirmedFellingDetailsId = Guid.NewGuid();
    private readonly Guid _confirmedRestockingDetailsId = Guid.NewGuid();
    private readonly Guid _fellingDetailsId = Guid.NewGuid();
    private readonly Guid _amendmentReviewId = Guid.NewGuid();
    private readonly Guid _restockingDetailsId = Guid.NewGuid();
    private readonly CancellationToken _cancellationToken = CancellationToken.None;
    private readonly Fixture _fixture = new();

    public WoodlandOfficerReviewControllerTestsConfirmedFellingRestocking()
    {
        var validatorMock = new Mock<IValidator<CompartmentConfirmedFellingRestockingDetailsModel>>();
        var eiaUseCase = new Mock<IEnvironmentalImpactAssessmentAdminOfficerUseCase>();
        var woMock = new Mock<IWoodlandOfficerReviewUseCase>();

        _cfrUseCaseMock = new Mock<IConfirmedFellingAndRestockingDetailsUseCase>();
        _amendFellingValidatorMock = new Mock<IValidator<AmendConfirmedFellingDetailsViewModel>>();
        _addFellingValidatorMock = new Mock<IValidator<AddNewConfirmedFellingDetailsViewModel>>();
        _amendRestockingValidatorMock = new Mock<IValidator<AmendConfirmedRestockingDetailsViewModel>>();
        _controller = new WoodlandOfficerReviewController(validatorMock.Object, eiaUseCase.Object, woMock.Object);
        _controller.PrepareControllerForTest(Guid.NewGuid());
    }

    [Fact]
    public async Task AmendConfirmedFellingDetails_Get_ReturnsView_WhenSuccess()
    {
        var model = new ConfirmedFellingRestockingDetailsModel
        {
            ApplicationId = _applicationId,
            Compartments = new[]
            {
                new CompartmentConfirmedFellingRestockingDetailsModel
                {
                    CompartmentId = _compartmentId,
                    CompartmentNumber = "1",
                    SubCompartmentName = "A",
                    SubmittedFlaPropertyCompartmentId = Guid.NewGuid(),
                    TotalHectares = 1.5,
                    ConfirmedFellingDetails = new[]
                    {
                        new ConfirmedFellingDetailViewModel
                        {
                            ConfirmedFellingDetailsId = _confirmedFellingDetailsId,
                            ConfirmedFellingSpecies =
                            [
                            ]
                        }
                    }
                }
            },
            Amendment = new AmendmentReview             {
                CanCurrentUserAmend = false,
                AmendmentState = AmendmentStateEnum.NoAmendment
            },
        };
        _cfrUseCaseMock.Setup(x => x.GetConfirmedFellingAndRestockingDetailsAsync(
            _applicationId, It.IsAny<InternalUser>(), _cancellationToken, It.IsAny<string>()))
            .ReturnsAsync(Result.Success(model));

        var result = await _controller.AmendConfirmedFellingDetails(
            _applicationId, _confirmedFellingDetailsId, _cfrUseCaseMock.Object, _cancellationToken);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<AmendConfirmedFellingDetailsViewModel>(viewResult.Model);
    }

    [Fact]
    public async Task AmendConfirmedFellingDetails_Get_RedirectsToIndex_WhenFailure()
    {
        _cfrUseCaseMock.Setup(x => x.GetConfirmedFellingAndRestockingDetailsAsync(
            _applicationId, It.IsAny<InternalUser>(), _cancellationToken, It.IsAny<string>()))
            .ReturnsAsync(Result.Failure<ConfirmedFellingRestockingDetailsModel>("fail"));

        var result = await _controller.AmendConfirmedFellingDetails(
            _applicationId, _confirmedFellingDetailsId, _cfrUseCaseMock.Object, _cancellationToken);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal(_applicationId, redirect.RouteValues["applicationId"]);
    }

    [Fact]
    public async Task AmendConfirmedFellingDetails_Post_RedirectsToConfirmedFellingAndRestocking_WhenSuccess()
    {
        var speciesDictionary = TreeSpeciesFactory.SpeciesDictionary;

        var deletedSpecies =
            _fixture.Build<ConfirmedFellingSpeciesModel>()
                .With(x => x.Species, speciesDictionary.Skip(2).First().Key)
                .With(x => x.Deleted, true).Create();
        var viewModel = new AmendConfirmedFellingDetailsViewModel
        {
            ApplicationId = _applicationId,
            ConfirmedFellingRestockingDetails = new IndividualConfirmedFellingRestockingDetailModel
            {
                CompartmentId = _compartmentId,
                ConfirmedFellingDetails = new ConfirmedFellingDetailViewModel
                {
                    ConfirmedFellingDetailsId = _confirmedFellingDetailsId,
                    ConfirmedFellingSpecies = [
                        _fixture.Build<ConfirmedFellingSpeciesModel>().With(x => x.Species, speciesDictionary.First().Key).Create(),
                        deletedSpecies
                    ]
                }
            },
            ConfirmedFellingAndRestockingComplete = false
        };

        var newSpecies = _fixture.Build<ConfirmedFellingSpeciesModel>()
            .With(x => x.Species, speciesDictionary.Skip(1).First().Key)
            .Create();

        viewModel.Species.Add(newSpecies.Species!, new SpeciesModel
        {
            Id = newSpecies.Id!.Value,
            Species = newSpecies.Species!,
            SpeciesName = newSpecies.Species!,
        });

        viewModel.Species.Remove(deletedSpecies.Species!);

        _amendFellingValidatorMock.Setup(s => s.Validate(It.IsAny<AmendConfirmedFellingDetailsViewModel>()))
            .Returns(new ValidationResult());
        _cfrUseCaseMock.Setup(x => x.SaveConfirmedFellingDetailsAsync(
            viewModel, It.IsAny<InternalUser>(), _cancellationToken))
            .ReturnsAsync(Result.Success(true));

        var result = await _controller.AmendConfirmedFellingDetails(
            viewModel, _cfrUseCaseMock.Object, _amendFellingValidatorMock.Object, _cancellationToken);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ConfirmedFellingAndRestocking", redirect.ActionName);
        Assert.Equal(_applicationId, redirect.RouteValues["id"]);
    }

    [Fact]
    public async Task AddConfirmedFellingDetails_Get_ReturnsView_WhenSuccess()
    {
        var model = new ConfirmedFellingRestockingDetailsModel
        {
            ApplicationId = _applicationId,
            Compartments = new[]
            {
                new CompartmentConfirmedFellingRestockingDetailsModel
                {
                    CompartmentId = _compartmentId,
                    CompartmentNumber = "1",
                    SubCompartmentName = "A",
                    TotalHectares = 1.5,
                    ConfirmedFellingDetails = Array.Empty<ConfirmedFellingDetailViewModel>()
                }
            },
            Amendment = new AmendmentReview             {
                CanCurrentUserAmend = false,
                AmendmentState = AmendmentStateEnum.NoAmendment
            },
        };
        _cfrUseCaseMock.Setup(x => x.GetConfirmedFellingAndRestockingDetailsAsync(
            _applicationId, It.IsAny<InternalUser>(), _cancellationToken, It.IsAny<string>()))
            .ReturnsAsync(Result.Success(model));

        var result = await _controller.AddConfirmedFellingDetails(
            _applicationId, _compartmentId, _cfrUseCaseMock.Object, _cancellationToken);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<AddNewConfirmedFellingDetailsViewModel>(viewResult.Model);
    }

    [Fact]
    public async Task AddConfirmedFellingDetails_Post_RedirectsToConfirmedFellingAndRestocking_WhenSuccess()
    {
        var speciesDictionary = TreeSpeciesFactory.SpeciesDictionary;

        var deletedSpecies =
            _fixture.Build<ConfirmedFellingSpeciesModel>()
                .With(x => x.Species, speciesDictionary.Skip(2).First().Key)
                .With(x => x.Deleted, true).Create();

        var viewModel = new AddNewConfirmedFellingDetailsViewModel
        {
            ApplicationId = _applicationId,
            ConfirmedFellingRestockingDetails = new NewConfirmedFellingDetailModel
            {
                CompartmentId = _compartmentId,
                ConfirmedFellingDetails = new ConfirmedFellingDetailViewModel
                {
                    ConfirmedFellingDetailsId = _confirmedFellingDetailsId,
                    ConfirmedFellingSpecies = [ deletedSpecies ]
                }
            },
        };

        var newSpecies = _fixture.Build<ConfirmedFellingSpeciesModel>()
            .With(x => x.Species, speciesDictionary.Skip(1).First().Key)
            .Create();

        viewModel.Species.Add(newSpecies.Species!, new SpeciesModel
        {
            Id = newSpecies.Id!.Value,
            Species = newSpecies.Species!,
            SpeciesName = newSpecies.Species!,
        });

        viewModel.Species.Remove(deletedSpecies.Species!);

        _addFellingValidatorMock.Setup(s => s.Validate(It.IsAny<AddNewConfirmedFellingDetailsViewModel>()))
            .Returns(new ValidationResult());
        _cfrUseCaseMock.Setup(x => x.SaveConfirmedFellingDetailsAsync(
            viewModel, It.IsAny<InternalUser>(), _cancellationToken))
            .ReturnsAsync(Result.Success(true));

        var result = await _controller.AddConfirmedFellingDetails(
            viewModel, _cfrUseCaseMock.Object, _addFellingValidatorMock.Object, _cancellationToken);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ConfirmedFellingAndRestocking", redirect.ActionName);
        Assert.Equal(_applicationId, redirect.RouteValues["id"]);
    }


    [Theory]
    [InlineData(TypeOfProposal.CreateDesignedOpenGround)]
    [InlineData(TypeOfProposal.RestockWithIndividualTrees)]
    [InlineData(TypeOfProposal.PlantAnAlternativeAreaWithIndividualTrees)]
    [InlineData((TypeOfProposal)999)] // Unknown/other
    public async Task AmendConfirmedRestockingDetails_Post_CoversAllRestockingProposalBranches(TypeOfProposal proposal)
    {
        var viewModel = new AmendConfirmedRestockingDetailsViewModel
        {
            ApplicationId = _applicationId,
            ConfirmedFellingRestockingDetails = new IndividualConfirmedRestockingDetailModel
            {
                CompartmentId = _compartmentId,
                ConfirmedRestockingDetails = new ConfirmedRestockingDetailViewModel
                {
                    ConfirmedFellingDetailsId = _fellingDetailsId,
                    RestockingProposal = proposal,
                    ConfirmedRestockingSpecies = new[]
                    {
                    new ConfirmedRestockingSpeciesModel { Species = "A" }
                }
                }
            },
            SubmittedFlaPropertyCompartments = new List<SubmittedFlaPropertyCompartment>(),
        };
        // Add a species to model.Species not in ConfirmedRestockingSpecies to test add logic
        viewModel.Species.Add("B", new SpeciesModel { Species = "B" });

        _amendRestockingValidatorMock.Setup(s => s.Validate(It.IsAny<AmendConfirmedRestockingDetailsViewModel>()))
            .Returns(new ValidationResult());
        _cfrUseCaseMock.Setup(x => x.SaveConfirmedRestockingDetailsAsync(
            viewModel, It.IsAny<InternalUser>(), _cancellationToken))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Success(true));

        var result = await _controller.AmendConfirmedRestockingDetails(
            viewModel, _cfrUseCaseMock.Object, _amendRestockingValidatorMock.Object, _cancellationToken);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ConfirmedFellingAndRestocking", redirect.ActionName);
        Assert.Equal(_applicationId, redirect.RouteValues["id"]);
    }

    [Fact]
    public async Task AmendConfirmedRestockingDetails_Post_Redirects_WhenRetrievalFails()
    {
        var viewModel = new AmendConfirmedRestockingDetailsViewModel
        {
            ApplicationId = _applicationId,
            ConfirmedFellingRestockingDetails = new IndividualConfirmedRestockingDetailModel
            {
                CompartmentId = _compartmentId,
                ConfirmedRestockingDetails = new ConfirmedRestockingDetailViewModel
                {
                    ConfirmedFellingDetailsId = _fellingDetailsId,
                    RestockingProposal = TypeOfProposal.CreateDesignedOpenGround
                }
            },
            SubmittedFlaPropertyCompartments = new List<SubmittedFlaPropertyCompartment>(),
        };
        _controller.ModelState.AddModelError("Test", "Error");
        _amendRestockingValidatorMock.Setup(s => s.Validate(It.IsAny<AmendConfirmedRestockingDetailsViewModel>()))
            .Returns(new ValidationResult(new[] { new ValidationFailure("Test", "Error") }));

        _cfrUseCaseMock.Setup(x => x.GetConfirmedFellingAndRestockingDetailsAsync(
            _applicationId, It.IsAny<InternalUser>(), _cancellationToken, It.IsAny<string>()))
            .ReturnsAsync(Result.Failure<ConfirmedFellingRestockingDetailsModel>("fail"));

        var result = await _controller.AmendConfirmedRestockingDetails(
            viewModel, _cfrUseCaseMock.Object, _amendRestockingValidatorMock.Object, _cancellationToken);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ConfirmedFellingAndRestocking", redirect.ActionName);
        Assert.Equal(_applicationId, redirect.RouteValues["id"]);
    }


    [Fact]
    public async Task AmendConfirmedRestockingDetails_Post_Redirects_WhenSpecificDetailNull()
    {
        var viewModel = new AmendConfirmedRestockingDetailsViewModel
        {
            ApplicationId = _applicationId,
            ConfirmedFellingRestockingDetails = new IndividualConfirmedRestockingDetailModel
            {
                CompartmentId = _compartmentId,
                ConfirmedRestockingDetails = new ConfirmedRestockingDetailViewModel
                {
                    ConfirmedFellingDetailsId = _fellingDetailsId,
                    RestockingProposal = TypeOfProposal.CreateDesignedOpenGround
                }
            },
            SubmittedFlaPropertyCompartments = new List<SubmittedFlaPropertyCompartment>(),
        };
        _controller.ModelState.AddModelError("Test", "Error");
        _amendRestockingValidatorMock.Setup(s => s.Validate(It.IsAny<AmendConfirmedRestockingDetailsViewModel>()))
            .Returns(new ValidationResult(new[] { new ValidationFailure("Test", "Error") }));

        _cfrUseCaseMock.Setup(x => x.GetConfirmedFellingAndRestockingDetailsAsync(
                _applicationId, It.IsAny<InternalUser>(), _cancellationToken, It.IsAny<string>()))
            .ReturnsAsync(Result.Success<ConfirmedFellingRestockingDetailsModel>(_fixture.Build<ConfirmedFellingRestockingDetailsModel>().With(x => x.Compartments, []).Create()));

        var result = await _controller.AmendConfirmedRestockingDetails(
            viewModel, _cfrUseCaseMock.Object, _amendRestockingValidatorMock.Object, _cancellationToken);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ConfirmedFellingAndRestocking", redirect.ActionName);
        Assert.Equal(_applicationId, redirect.RouteValues["id"]);
    }

    [Fact]
    public async Task AddConfirmedRestockingDetails_Post_AddsConfirmationMessage_WhenIsAdd()
    {
        var viewModel = new AmendConfirmedRestockingDetailsViewModel
        {
            ApplicationId = _applicationId,
            ConfirmedFellingRestockingDetails = new IndividualConfirmedRestockingDetailModel
            {
                CompartmentId = _compartmentId,
                ConfirmedRestockingDetails = new ConfirmedRestockingDetailViewModel
                {
                    ConfirmedFellingDetailsId = _fellingDetailsId,
                    RestockingProposal = TypeOfProposal.CreateDesignedOpenGround
                }
            },
            SubmittedFlaPropertyCompartments = new List<SubmittedFlaPropertyCompartment>(),
        };
        _amendRestockingValidatorMock.Setup(s => s.Validate(It.IsAny<AmendConfirmedRestockingDetailsViewModel>()))
            .Returns(new ValidationResult());
        _cfrUseCaseMock.Setup(x => x.SaveConfirmedRestockingDetailsAsync(
            viewModel, It.IsAny<InternalUser>(), _cancellationToken))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Success(true));

        // This will hit the isAdd branch and should add a confirmation message
        var result = await _controller.AddConfirmedRestockingDetails(
            viewModel, _cfrUseCaseMock.Object, _amendRestockingValidatorMock.Object, _cancellationToken);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ConfirmedFellingAndRestocking", redirect.ActionName);
        Assert.Equal(_applicationId, redirect.RouteValues["id"]);
    }

    [Fact]
    public async Task SelectFellingCompartment_Get_ReturnsView_WhenSuccess()
    {
        var model = new SelectFellingCompartmentModel
        {
            ApplicationId = _applicationId,
            SelectableCompartments = new List<SelectableCompartment>()
        };
        _cfrUseCaseMock.Setup(x => x.GetSelectableFellingCompartmentsAsync(
            _applicationId, _cancellationToken))
            .ReturnsAsync(Result.Success(model));

        var result = await _controller.SelectFellingCompartment(
            _applicationId, _cfrUseCaseMock.Object, _cancellationToken);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<SelectFellingCompartmentModel>(viewResult.Model);
    }

    [Fact]
    public async Task SelectFellingCompartment_Get_RedirectsToConfirmedFellingAndRestocking_OnSelectableCompartmentRetrievalFailure()
    {
        var model = new SelectFellingCompartmentModel
        {
            ApplicationId = _applicationId,
            SelectableCompartments = new List<SelectableCompartment>()
        };
        _cfrUseCaseMock.Setup(x => x.GetSelectableFellingCompartmentsAsync(
                _applicationId, _cancellationToken))
            .ReturnsAsync(Result.Failure<SelectFellingCompartmentModel>("fail"));

        var result = await _controller.SelectFellingCompartment(
            _applicationId, _cfrUseCaseMock.Object, _cancellationToken);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ConfirmedFellingAndRestocking", redirect.ActionName);
        Assert.Equal(_applicationId, redirect.RouteValues["id"]);
    }

    [Fact]
    public async Task DeleteConfirmedFellingDetails_RedirectsToConfirmedFellingAndRestocking_WhenSuccess()
    {
        _cfrUseCaseMock.Setup(x => x.DeleteConfirmedFellingDetailAsync(
            _applicationId, _confirmedFellingDetailsId, It.IsAny<InternalUser>(), _cancellationToken))
            .ReturnsAsync(Result.Success(true));

        var result = await _controller.DeleteConfirmedFellingDetails(
            _applicationId, _confirmedFellingDetailsId, _cfrUseCaseMock.Object, _cancellationToken);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ConfirmedFellingAndRestocking", redirect.ActionName);
        Assert.Equal(_applicationId, redirect.RouteValues["id"]);
    }

    [Fact]
    public async Task DeleteConfirmedRestockingDetails_RedirectsToConfirmedFellingAndRestocking_WhenSuccess()
    {
        _cfrUseCaseMock.Setup(x => x.DeleteConfirmedRestockingDetailAsync(
            _applicationId, _confirmedRestockingDetailsId, It.IsAny<InternalUser>(), _cancellationToken))
            .ReturnsAsync(Result.Success(true));

        var result = await _controller.DeleteConfirmedRestockingDetails(
            _applicationId, _confirmedRestockingDetailsId, _cfrUseCaseMock.Object, _cancellationToken);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ConfirmedFellingAndRestocking", redirect.ActionName);
        Assert.Equal(_applicationId, redirect.RouteValues["id"]);
    }

    [Fact]
    public async Task RevertAmendedConfirmedFellingDetails_RedirectsToConfirmedFellingAndRestocking_WhenSuccess()
    {
        var proposedFellingDetailsId = Guid.NewGuid();
        _cfrUseCaseMock.Setup(x => x.RevertConfirmedFellingDetailAmendmentsAsync(
            _applicationId, proposedFellingDetailsId, It.IsAny<InternalUser>(), _cancellationToken))
            .ReturnsAsync(Result.Success(true));

        var result = await _controller.RevertAmendedConfirmedFellingDetails(
            _applicationId, proposedFellingDetailsId, _cfrUseCaseMock.Object, _cancellationToken);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ConfirmedFellingAndRestocking", redirect.ActionName);
        Assert.Equal(_applicationId, redirect.RouteValues["id"]);
    }

    [Fact]
    public async Task ConfirmedFellingAndRestocking_Get_RedirectsToError_WhenFailure()
    {
        _cfrUseCaseMock.Setup(x => x.GetConfirmedFellingAndRestockingDetailsAsync(
            _applicationId, It.IsAny<InternalUser>(), _cancellationToken, null))
            .ReturnsAsync(Result.Failure<ConfirmedFellingRestockingDetailsModel>("fail"));

        var result = await _controller.ConfirmedFellingAndRestocking(
            _applicationId, _cfrUseCaseMock.Object, _cancellationToken);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Error", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
    }

    [Fact]
    public async Task ConfirmFellingAndRestocking_Post_RedirectsToConfirmedFellingAndRestocking_WhenFailure()
    {
        var woReviewUseCaseMock = new Mock<IWoodlandOfficerReviewUseCase>();

        var validator = new Mock<IValidator<ConfirmedFellingRestockingDetailsModel>>();
        validator.Setup(v => v.ValidateAsync(It.IsAny<ConfirmedFellingRestockingDetailsModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _cfrUseCaseMock.Setup(x => x.GetConfirmedFellingAndRestockingDetailsAsync(
            _applicationId, It.IsAny<InternalUser>(), _cancellationToken, null))
            .ReturnsAsync(Result.Success(_fixture.Create<ConfirmedFellingRestockingDetailsModel>()));
        woReviewUseCaseMock.Setup(x => x.CompleteConfirmedFellingAndRestockingDetailsAsync(
            _applicationId, It.IsAny<InternalUser>(), _cancellationToken))
            .ReturnsAsync(Result.Failure("fail"));

        var result = await _controller.ConfirmFellingAndRestocking(
            _applicationId, _cfrUseCaseMock.Object, woReviewUseCaseMock.Object, validator.Object, _cancellationToken);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ConfirmedFellingAndRestocking", redirect.ActionName);
        Assert.Equal(_applicationId, redirect.RouteValues["id"]);
    }

    [Fact]
    public async Task AmendConfirmedFellingDetails_Post_RedirectsToConfirmedFellingAndRestocking_WhenSaveFails()
    {
        var viewModel = new AmendConfirmedFellingDetailsViewModel
        {
            ApplicationId = _applicationId,
            ConfirmedFellingRestockingDetails = new IndividualConfirmedFellingRestockingDetailModel
            {
                CompartmentId = _compartmentId,
                ConfirmedFellingDetails = new ConfirmedFellingDetailViewModel
                {
                    ConfirmedFellingDetailsId = _confirmedFellingDetailsId,
                }
            },
            ConfirmedFellingAndRestockingComplete = false
        };
        _amendFellingValidatorMock.Setup(s => s.Validate(It.IsAny<AmendConfirmedFellingDetailsViewModel>()))
            .Returns(new ValidationResult());
        _cfrUseCaseMock.Setup(x => x.SaveConfirmedFellingDetailsAsync(
            viewModel, It.IsAny<InternalUser>(), _cancellationToken))
            .ReturnsAsync(Result.Failure("fail"));

        var result = await _controller.AmendConfirmedFellingDetails(
            viewModel, _cfrUseCaseMock.Object, _amendFellingValidatorMock.Object, _cancellationToken);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal(_applicationId, redirect.RouteValues["id"]);
    }

    [Fact]
    public async Task AddConfirmedFellingDetails_Get_RedirectsToConfirmedFellingAndRestocking_WhenFailure()
    {
        _cfrUseCaseMock.Setup(x => x.GetConfirmedFellingAndRestockingDetailsAsync(
            _applicationId, It.IsAny<InternalUser>(), _cancellationToken, It.IsAny<string>()))
            .ReturnsAsync(Result.Failure<ConfirmedFellingRestockingDetailsModel>("fail"));

        var result = await _controller.AddConfirmedFellingDetails(
            _applicationId, _compartmentId, _cfrUseCaseMock.Object, _cancellationToken);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ConfirmedFellingAndRestocking", redirect.ActionName);
        Assert.Equal(_applicationId, redirect.RouteValues["applicationId"]);
    }

    [Fact]
    public async Task SelectFellingCompartment_Post_RedirectsToAddConfirmedFellingDetails_WhenModelStateIsValid()
    {
        var model = new SelectFellingCompartmentModel
        {
            ApplicationId = _applicationId,
            SelectedCompartmentId = _compartmentId
        };
        _controller.ModelState.Clear(); // ModelState is valid

        var result = await _controller.SelectFellingCompartment(
            model, _cfrUseCaseMock.Object, _cancellationToken);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("AddConfirmedFellingDetails", redirect.ActionName);
        Assert.Equal(_applicationId, redirect.RouteValues["applicationId"]);
        Assert.Equal(_compartmentId, redirect.RouteValues["compartmentId"]);
    }

    [Fact]
    public async Task AmendConfirmedFellingDetails_Get_RedirectsToIndex_WhenCompartmentNotFound()
    {
        var model = new ConfirmedFellingRestockingDetailsModel
        {
            ApplicationId = _applicationId,
            Compartments =
            [
            ],
            Amendment = new AmendmentReview
            {
                CanCurrentUserAmend = false,
                AmendmentState = AmendmentStateEnum.NoAmendment
            },
        };
        _cfrUseCaseMock.Setup(x => x.GetConfirmedFellingAndRestockingDetailsAsync(
            _applicationId, It.IsAny<InternalUser>(), _cancellationToken, It.IsAny<string>()))
            .ReturnsAsync(Result.Success(model));

        var result = await _controller.AmendConfirmedFellingDetails(
            _applicationId, _confirmedFellingDetailsId, _cfrUseCaseMock.Object, _cancellationToken);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal(_applicationId, redirect.RouteValues["applicationId"]);
    }

    [Fact]
    public async Task AmendConfirmedFellingDetails_Post_ReturnsView_WhenModelStateInvalid()
    {
        const string propertyName = "Species[0].Value";

        var viewModel = new AmendConfirmedFellingDetailsViewModel
        {
            ApplicationId = _applicationId,
            ConfirmedFellingRestockingDetails = new IndividualConfirmedFellingRestockingDetailModel
            {
                CompartmentId = _compartmentId,
                ConfirmedFellingDetails = new ConfirmedFellingDetailViewModel
                {
                    ConfirmedFellingDetailsId = _confirmedFellingDetailsId,
                }
            },
            ConfirmedFellingAndRestockingComplete = false
        };

        var validationFailures = _fixture.Create<List<ValidationFailure>>();

        var speciesListFailure = new ValidationFailure("Species[0].Value", "error");
        speciesListFailure.FormattedMessagePlaceholderValues = new Dictionary<string, object>
        {
            { "PropertyName", propertyName }
        };
        validationFailures.Add(speciesListFailure);

        _amendFellingValidatorMock.Setup(s => s.Validate(It.IsAny<AmendConfirmedFellingDetailsViewModel>()))
            .Returns(new ValidationResult(validationFailures));
        var model = new ConfirmedFellingRestockingDetailsModel
        {
            ApplicationId = _applicationId,
            FellingLicenceApplicationSummary = new(),
            Breadcrumbs = new(),
            Compartments = new[]
            {
                new CompartmentConfirmedFellingRestockingDetailsModel
                {
                    CompartmentId = _compartmentId,
                    CompartmentNumber = "1",
                    SubCompartmentName = "A",
                    SubmittedFlaPropertyCompartmentId = Guid.NewGuid(),
                    TotalHectares = 1.5,
                    ConfirmedFellingDetails = new[]
                    {
                        new ConfirmedFellingDetailViewModel
                        {
                            ConfirmedFellingDetailsId = _confirmedFellingDetailsId,
                            ConfirmedFellingSpecies =
                            [
                            ]
                        }
                    }
                }
            },
            Amendment = new AmendmentReview
            {
                CanCurrentUserAmend = false,
                AmendmentState = AmendmentStateEnum.NoAmendment
            },
        };
        _cfrUseCaseMock.Setup(x => x.GetConfirmedFellingAndRestockingDetailsAsync(
            _applicationId, It.IsAny<InternalUser>(), _cancellationToken, It.IsAny<string>()))
            .ReturnsAsync(Result.Success(model));

        var result = await _controller.AmendConfirmedFellingDetails(
            viewModel, _cfrUseCaseMock.Object, _amendFellingValidatorMock.Object, _cancellationToken);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<AmendConfirmedFellingDetailsViewModel>(viewResult.Model);

        Assert.True(_controller.ModelState.ContainsKey(propertyName));
        var modelState = _controller.ModelState[propertyName];
        var error = Assert.Single(modelState!.Errors);
        Assert.Equal("error", error.ErrorMessage);
    }


    [Fact]
    public async Task AmendConfirmedFellingDetails_Post_RedirectsToIndex_WhenModelReretrievalFails()
    {
        const string propertyName = "Species[0].Value";

        var viewModel = new AmendConfirmedFellingDetailsViewModel
        {
            ApplicationId = _applicationId,
            ConfirmedFellingRestockingDetails = new IndividualConfirmedFellingRestockingDetailModel
            {
                CompartmentId = _compartmentId,
                ConfirmedFellingDetails = new ConfirmedFellingDetailViewModel
                {
                    ConfirmedFellingDetailsId = _confirmedFellingDetailsId,
                }
            },
            ConfirmedFellingAndRestockingComplete = false
        };

        var validationFailures = _fixture.Create<List<ValidationFailure>>();

        var speciesListFailure = new ValidationFailure("Species[0].Value", "error");
        speciesListFailure.FormattedMessagePlaceholderValues = new Dictionary<string, object>
        {
            { "PropertyName", propertyName }
        };
        validationFailures.Add(speciesListFailure);

        _amendFellingValidatorMock.Setup(s => s.Validate(It.IsAny<AmendConfirmedFellingDetailsViewModel>()))
            .Returns(new ValidationResult(validationFailures));
        var model = new ConfirmedFellingRestockingDetailsModel
        {
            ApplicationId = _applicationId,
            FellingLicenceApplicationSummary = new(),
            Breadcrumbs = new(),
            Compartments = new[]
            {
                new CompartmentConfirmedFellingRestockingDetailsModel
                {
                    CompartmentId = _compartmentId,
                    CompartmentNumber = "1",
                    SubCompartmentName = "A",
                    SubmittedFlaPropertyCompartmentId = Guid.NewGuid(),
                    TotalHectares = 1.5,
                    ConfirmedFellingDetails = new[]
                    {
                        new ConfirmedFellingDetailViewModel
                        {
                            ConfirmedFellingDetailsId = _confirmedFellingDetailsId,
                            ConfirmedFellingSpecies =
                            [
                            ]
                        }
                    }
                }
            },
            Amendment = new AmendmentReview
            {
                CanCurrentUserAmend = false,
                AmendmentState = AmendmentStateEnum.NoAmendment
            },
        };
        _cfrUseCaseMock.Setup(x => x.GetConfirmedFellingAndRestockingDetailsAsync(
            _applicationId, It.IsAny<InternalUser>(), _cancellationToken, It.IsAny<string>()))
            .ReturnsAsync(Result.Failure<ConfirmedFellingRestockingDetailsModel>("fail"));

        var result = await _controller.AmendConfirmedFellingDetails(
            viewModel, _cfrUseCaseMock.Object, _amendFellingValidatorMock.Object, _cancellationToken);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal(_applicationId, redirect.RouteValues["id"]);
    }

    [Fact]
    public async Task AddConfirmedFellingDetails_Get_RedirectsToConfirmedFellingAndRestocking_WhenCompartmentNotFound()
    {
        var model = new ConfirmedFellingRestockingDetailsModel
        {
            ApplicationId = _applicationId,
            Compartments =
            [
            ],
            Amendment = new AmendmentReview
            {
                CanCurrentUserAmend = false,
                AmendmentState = AmendmentStateEnum.NoAmendment
            },
        };
        _cfrUseCaseMock.Setup(x => x.GetConfirmedFellingAndRestockingDetailsAsync(
            _applicationId, It.IsAny<InternalUser>(), _cancellationToken, It.IsAny<string>()))
            .ReturnsAsync(Result.Success(model));

        var result = await _controller.AddConfirmedFellingDetails(
            _applicationId, _compartmentId, _cfrUseCaseMock.Object, _cancellationToken);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ConfirmedFellingAndRestocking", redirect.ActionName);
        Assert.Equal(_applicationId, redirect.RouteValues["applicationId"]);
    }

    [Fact]
    public async Task AddConfirmedFellingDetails_Post_RedirectsToConfirmedFellingAndRestocking_WhenSaveFails()
    {
        var viewModel = new AddNewConfirmedFellingDetailsViewModel
        {
            ApplicationId = _applicationId,
            ConfirmedFellingRestockingDetails = new NewConfirmedFellingDetailModel
            {
                CompartmentId = _compartmentId,
                ConfirmedFellingDetails = new ConfirmedFellingDetailViewModel
                {
                    ConfirmedFellingDetailsId = _confirmedFellingDetailsId,
                    ConfirmedFellingSpecies = []
                }
            },
        };
        _addFellingValidatorMock.Setup(s => s.Validate(It.IsAny<AddNewConfirmedFellingDetailsViewModel>()))
            .Returns(new ValidationResult());
        _cfrUseCaseMock.Setup(x => x.SaveConfirmedFellingDetailsAsync(
            viewModel, It.IsAny<InternalUser>(), _cancellationToken))
            .ReturnsAsync(Result.Failure("fail"));

        var result = await _controller.AddConfirmedFellingDetails(
            viewModel, _cfrUseCaseMock.Object, _addFellingValidatorMock.Object, _cancellationToken);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ConfirmedFellingAndRestocking", redirect.ActionName);
        Assert.Equal(_applicationId, redirect.RouteValues["id"]);
    }

    [Fact]
    public async Task AddConfirmedFellingDetails_Post_ReturnsView_WhenModelStateInvalid()
    {
        var viewModel = new AddNewConfirmedFellingDetailsViewModel
        {
            ApplicationId = _applicationId,
            ConfirmedFellingRestockingDetails = new NewConfirmedFellingDetailModel
            {
                CompartmentId = _compartmentId,
                ConfirmedFellingDetails = new ConfirmedFellingDetailViewModel
                {
                    ConfirmedFellingDetailsId = _confirmedFellingDetailsId,
                    ConfirmedFellingSpecies = []
                }
            },
        };
        _addFellingValidatorMock.Setup(s => s.Validate(It.IsAny<AddNewConfirmedFellingDetailsViewModel>()))
            .Returns(new ValidationResult(_fixture.Create<List<ValidationFailure>>()));
        _controller.ModelState.AddModelError("Test", "Error");
        var model = new ConfirmedFellingRestockingDetailsModel
        {
            ApplicationId = _applicationId,
            FellingLicenceApplicationSummary = new(),
            Breadcrumbs = new(),
            Compartments = new[]
            {
                new CompartmentConfirmedFellingRestockingDetailsModel
                {
                    CompartmentId = _compartmentId,
                    CompartmentNumber = "1",
                    SubCompartmentName = "A",
                    TotalHectares = 1.5,
                    ConfirmedFellingDetails = Array.Empty<ConfirmedFellingDetailViewModel>()
                }
            },
            Amendment = new AmendmentReview
            {
                CanCurrentUserAmend = false,
                AmendmentState = AmendmentStateEnum.NoAmendment
            },
        };
        _cfrUseCaseMock.Setup(x => x.GetConfirmedFellingAndRestockingDetailsAsync(
            _applicationId, It.IsAny<InternalUser>(), _cancellationToken, It.IsAny<string>()))
            .ReturnsAsync(Result.Success(model));

        var result = await _controller.AddConfirmedFellingDetails(
            viewModel, _cfrUseCaseMock.Object, _addFellingValidatorMock.Object, _cancellationToken);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<AddNewConfirmedFellingDetailsViewModel>(viewResult.Model);
    }

    [Fact]
    public async Task AddConfirmedFellingDetails_Post_ReturnsConfirmedFellingAndRestocking_WhenModelRetrievalFails()
    {
        var viewModel = new AddNewConfirmedFellingDetailsViewModel
        {
            ApplicationId = _applicationId,
            ConfirmedFellingRestockingDetails = new NewConfirmedFellingDetailModel
            {
                CompartmentId = _compartmentId,
                ConfirmedFellingDetails = new ConfirmedFellingDetailViewModel
                {
                    ConfirmedFellingDetailsId = _confirmedFellingDetailsId,
                    ConfirmedFellingSpecies = []
                }
            },
        };
        _addFellingValidatorMock.Setup(s => s.Validate(It.IsAny<AddNewConfirmedFellingDetailsViewModel>()))
            .Returns(new ValidationResult(_fixture.Create<List<ValidationFailure>>()));
        _controller.ModelState.AddModelError("Test", "Error");
        var model = new ConfirmedFellingRestockingDetailsModel
        {
            ApplicationId = _applicationId,
            FellingLicenceApplicationSummary = new(),
            Breadcrumbs = new(),
            Compartments = new[]
            {
                new CompartmentConfirmedFellingRestockingDetailsModel
                {
                    CompartmentId = _compartmentId,
                    CompartmentNumber = "1",
                    SubCompartmentName = "A",
                    TotalHectares = 1.5,
                    ConfirmedFellingDetails = Array.Empty<ConfirmedFellingDetailViewModel>()
                }
            },
            Amendment = new AmendmentReview
            {
                CanCurrentUserAmend = false,
                AmendmentState = AmendmentStateEnum.Completed
            },
        };
        _cfrUseCaseMock.Setup(x => x.GetConfirmedFellingAndRestockingDetailsAsync(
            _applicationId, It.IsAny<InternalUser>(), _cancellationToken, It.IsAny<string>()))
            .ReturnsAsync(Result.Failure<ConfirmedFellingRestockingDetailsModel>("fail"));

        var result = await _controller.AddConfirmedFellingDetails(
            viewModel, _cfrUseCaseMock.Object, _addFellingValidatorMock.Object, _cancellationToken);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ConfirmedFellingAndRestocking", redirect.ActionName);
        Assert.Equal(_applicationId, redirect.RouteValues["id"]);
    }

    [Fact]
    public async Task SelectFellingCompartment_Post_RedirectsToConfirmedFellingAndRestocking_WhenFailure()
    {
        var model = new SelectFellingCompartmentModel
        {
            ApplicationId = _applicationId,
            SelectedCompartmentId = _compartmentId
        };
        _controller.ModelState.AddModelError("Test", "Error");
        _cfrUseCaseMock.Setup(x => x.GetSelectableFellingCompartmentsAsync(
            _applicationId, _cancellationToken))
            .ReturnsAsync(Result.Failure<SelectFellingCompartmentModel>("fail"));

        var result = await _controller.SelectFellingCompartment(
            model, _cfrUseCaseMock.Object, _cancellationToken);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ConfirmedFellingAndRestocking", redirect.ActionName);
        Assert.Equal(_applicationId, redirect.RouteValues["id"]);
    }

    [Fact]
    public async Task SelectFellingCompartment_Post_ReturnsView_WhenModelStateInvalidAndSuccess()
    {
        var model = new SelectFellingCompartmentModel
        {
            ApplicationId = _applicationId,
            SelectedCompartmentId = _compartmentId
        };
        _controller.ModelState.AddModelError("Test", "Error");
        var resultModel = new SelectFellingCompartmentModel
        {
            ApplicationId = _applicationId,
            SelectableCompartments = new List<SelectableCompartment>(),
            FellingLicenceApplicationSummary = new(),
            Breadcrumbs = new()
        };
        _cfrUseCaseMock.Setup(x => x.GetSelectableFellingCompartmentsAsync(
            _applicationId, _cancellationToken))
            .ReturnsAsync(Result.Success(resultModel));

        var result = await _controller.SelectFellingCompartment(
            model, _cfrUseCaseMock.Object, _cancellationToken);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<SelectFellingCompartmentModel>(viewResult.Model);
    }

    [Fact]
    public async Task ConfirmedFellingAndRestocking_Get_ReturnsView_WhenSuccess()
    {
        var model = new ConfirmedFellingRestockingDetailsModel
        {
            ApplicationId = _applicationId,
            Compartments =
            [
            ],
            FellingLicenceApplicationSummary = new(),
            Breadcrumbs = new(),
            Amendment = new AmendmentReview
            {
                CanCurrentUserAmend = false,
                AmendmentState = AmendmentStateEnum.Completed
            },
        };
        _cfrUseCaseMock.Setup(x => x.GetConfirmedFellingAndRestockingDetailsAsync(
            _applicationId, It.IsAny<InternalUser>(), _cancellationToken, null))
            .ReturnsAsync(Result.Success(model));

        var result = await _controller.ConfirmedFellingAndRestocking(
            _applicationId, _cfrUseCaseMock.Object, _cancellationToken);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<ConfirmedFellingRestockingDetailsModel>(viewResult.Model);
    }

    [Fact]
    public async Task ConfirmFellingAndRestocking_Post_RedirectsToIndex_WhenSuccess()
    {
        var woReviewUseCaseMock = new Mock<IWoodlandOfficerReviewUseCase>();

        var model = _fixture.Create<ConfirmedFellingRestockingDetailsModel>();
        var validator = new Mock<IValidator<ConfirmedFellingRestockingDetailsModel>>();
        validator.Setup(v => v.ValidateAsync(It.IsAny<ConfirmedFellingRestockingDetailsModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _cfrUseCaseMock.Setup(x => x.GetConfirmedFellingAndRestockingDetailsAsync(
            _applicationId, It.IsAny<InternalUser>(), _cancellationToken, null))
            .ReturnsAsync(Result.Success(model));
        woReviewUseCaseMock.Setup(x => x.CompleteConfirmedFellingAndRestockingDetailsAsync(
            _applicationId, It.IsAny<InternalUser>(), _cancellationToken))
            .ReturnsAsync(Result.Success(true));

        var result = await _controller.ConfirmFellingAndRestocking(
            _applicationId, _cfrUseCaseMock.Object, woReviewUseCaseMock.Object, validator.Object, _cancellationToken);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("WoodlandOfficerReview", redirect.ControllerName);
        Assert.Equal(_applicationId, redirect.RouteValues["id"]);
    }


    [Fact]
    public async Task ConfirmFellingAndRestocking_Post_RedirectsToErrorWhenModelRetrievalFails()
    {
        var woReviewUseCaseMock = new Mock<IWoodlandOfficerReviewUseCase>();

        var model = _fixture.Create<ConfirmedFellingRestockingDetailsModel>();
        var validator = new Mock<IValidator<ConfirmedFellingRestockingDetailsModel>>();
        validator.Setup(v => v.ValidateAsync(It.IsAny<ConfirmedFellingRestockingDetailsModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _cfrUseCaseMock.Setup(x => x.GetConfirmedFellingAndRestockingDetailsAsync(
                _applicationId, It.IsAny<InternalUser>(), _cancellationToken, null))
            .ReturnsAsync(Result.Failure<ConfirmedFellingRestockingDetailsModel>("fail"));

        var result = await _controller.ConfirmFellingAndRestocking(
            _applicationId, _cfrUseCaseMock.Object, woReviewUseCaseMock.Object, validator.Object, _cancellationToken);

        var redirect = Assert.IsType<RedirectToActionResult>(result);

        Assert.Equal("Error", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
    }

    [Fact]
    public async Task ConfirmFellingAndRestocking_Post_RedirectsToConfirmedFellingAndRestocking_WhenValidationFails()
    {
        var woReviewUseCaseMock = new Mock<IWoodlandOfficerReviewUseCase>();
        var validator = new Mock<IValidator<ConfirmedFellingRestockingDetailsModel>>();
        validator.Setup(v => v.ValidateAsync(It.IsAny<ConfirmedFellingRestockingDetailsModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult([
                new ValidationFailure("SomeProperty", "Some error message")
            ]));
        var model = new ConfirmedFellingRestockingDetailsModel
        {
            ApplicationId = _applicationId,
            Compartments =
            [
            ],
            FellingLicenceApplicationSummary = new(),
            Breadcrumbs = new(),
            Amendment = new AmendmentReview
            {
                CanCurrentUserAmend = false,
                AmendmentState = AmendmentStateEnum.Completed
            },
        };
        _cfrUseCaseMock.Setup(x => x.GetConfirmedFellingAndRestockingDetailsAsync(
            _applicationId, It.IsAny<InternalUser>(), _cancellationToken, null))
            .ReturnsAsync(Result.Success(model));

        // Simulate validation failure by returning a validator with errors
        // You may need to mock ConfirmedFellingAndRestockingCrossValidator if used

        var result = await _controller.ConfirmFellingAndRestocking(
            _applicationId, _cfrUseCaseMock.Object, woReviewUseCaseMock.Object, validator.Object, _cancellationToken);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ConfirmedFellingAndRestocking", redirect.ActionName);
        Assert.Equal(_applicationId, redirect.RouteValues["id"]);
    }

    [Fact]
    public async Task DeleteConfirmedFellingDetails_RedirectsToConfirmedFellingAndRestocking_WhenFailure()
    {
        _cfrUseCaseMock.Setup(x => x.DeleteConfirmedFellingDetailAsync(
            _applicationId, _confirmedFellingDetailsId, It.IsAny<InternalUser>(), _cancellationToken))
            .ReturnsAsync(Result.Failure("fail"));

        var result = await _controller.DeleteConfirmedFellingDetails(
            _applicationId, _confirmedFellingDetailsId, _cfrUseCaseMock.Object, _cancellationToken);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ConfirmedFellingAndRestocking", redirect.ActionName);
        Assert.Equal(_applicationId, redirect.RouteValues["id"]);
    }

    [Fact]
    public async Task DeleteConfirmedRestockingDetails_RedirectsToConfirmedFellingAndRestocking_WhenFailure()
    {
        _cfrUseCaseMock.Setup(x => x.DeleteConfirmedRestockingDetailAsync(
            _applicationId, _confirmedRestockingDetailsId, It.IsAny<InternalUser>(), _cancellationToken))
            .ReturnsAsync(Result.Failure("fail"));

        var result = await _controller.DeleteConfirmedRestockingDetails(
            _applicationId, _confirmedRestockingDetailsId, _cfrUseCaseMock.Object, _cancellationToken);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ConfirmedFellingAndRestocking", redirect.ActionName);
        Assert.Equal(_applicationId, redirect.RouteValues["id"]);
    }

    [Fact]
    public async Task RevertAmendedConfirmedFellingDetails_RedirectsToConfirmedFellingAndRestocking_WhenFailure()
    {
        var proposedFellingDetailsId = Guid.NewGuid();
        _cfrUseCaseMock.Setup(x => x.RevertConfirmedFellingDetailAmendmentsAsync(
            _applicationId, proposedFellingDetailsId, It.IsAny<InternalUser>(), _cancellationToken))
            .ReturnsAsync(Result.Failure("fail"));

        var result = await _controller.RevertAmendedConfirmedFellingDetails(
            _applicationId, proposedFellingDetailsId, _cfrUseCaseMock.Object, _cancellationToken);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ConfirmedFellingAndRestocking", redirect.ActionName);
        Assert.Equal(_applicationId, redirect.RouteValues["id"]);
    }

    [Fact]
    public async Task AmendConfirmedRestockingDetails_Get_ReturnsView_WhenSuccess()
    {
        var compartment = new CompartmentConfirmedFellingRestockingDetailsModel
        {
            CompartmentId = _compartmentId,
            ConfirmedFellingDetails = new[]
            {
                new ConfirmedFellingDetailViewModel
                {
                    ConfirmedFellingDetailsId = _fellingDetailsId,
                    ConfirmedRestockingDetails = new[]
                    {
                        new ConfirmedRestockingDetailViewModel
                        {
                            ConfirmedRestockingDetailsId = _restockingDetailsId
                        }
                    }
                }
            }
        };
        var model = new ConfirmedFellingRestockingDetailsModel
        {
            ApplicationId = _applicationId,
            Compartments = new[]
            {
                compartment
            },
            SubmittedFlaPropertyCompartments = new List<SubmittedFlaPropertyCompartment>(),
            FellingLicenceApplicationSummary = new(),
            Breadcrumbs = new(),
            Amendment = new AmendmentReview
            {
                CanCurrentUserAmend = false,
                AmendmentState = AmendmentStateEnum.Completed
            },
        };
        _cfrUseCaseMock.Setup(x => x.GetConfirmedFellingAndRestockingDetailsAsync(
            _applicationId, It.IsAny<InternalUser>(), _cancellationToken, It.IsAny<string>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Success(model));

        var result = await _controller.AmendConfirmedRestockingDetails(
            _applicationId, _fellingDetailsId, _restockingDetailsId, _cfrUseCaseMock.Object, _cancellationToken);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<AmendConfirmedRestockingDetailsViewModel>(viewResult.Model);
    }

    [Fact]
    public async Task AmendConfirmedRestockingDetails_Get_Redirects_WhenFailure()
    {
        _cfrUseCaseMock.Setup(x => x.GetConfirmedFellingAndRestockingDetailsAsync(
            _applicationId, It.IsAny<InternalUser>(), _cancellationToken, It.IsAny<string>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<ConfirmedFellingRestockingDetailsModel>("fail"));

        var result = await _controller.AmendConfirmedRestockingDetails(
            _applicationId, _fellingDetailsId, _restockingDetailsId, _cfrUseCaseMock.Object, _cancellationToken);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ConfirmedFellingAndRestocking", redirect.ActionName);
        Assert.Equal(_applicationId, redirect.RouteValues["applicationId"]);
    }

    [Fact]
    public async Task AmendConfirmedRestockingDetails_Get_Redirects_WhenCompartmentNotFound()
    {
        var model = new ConfirmedFellingRestockingDetailsModel
        {
            ApplicationId = _applicationId,
            Compartments = Array.Empty<CompartmentConfirmedFellingRestockingDetailsModel>(),
            SubmittedFlaPropertyCompartments = new List<SubmittedFlaPropertyCompartment>(),
            FellingLicenceApplicationSummary = new(),
            Breadcrumbs = new(),
            Amendment = new AmendmentReview
            {
                CanCurrentUserAmend = false,
                AmendmentState = AmendmentStateEnum.Completed
            },
        };
        _cfrUseCaseMock.Setup(x => x.GetConfirmedFellingAndRestockingDetailsAsync(
            _applicationId, It.IsAny<InternalUser>(), _cancellationToken, It.IsAny<string>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Success(model));

        var result = await _controller.AmendConfirmedRestockingDetails(
            _applicationId, _fellingDetailsId, _restockingDetailsId, _cfrUseCaseMock.Object, _cancellationToken);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ConfirmedFellingAndRestocking", redirect.ActionName);
        Assert.Equal(_applicationId, redirect.RouteValues["applicationId"]);
    }

    [Fact]
    public async Task AddConfirmedRestockingDetails_Get_ReturnsView_WhenSuccess()
    {
        var compartment = new CompartmentConfirmedFellingRestockingDetailsModel
        {
            CompartmentId = _compartmentId,
            ConfirmedFellingDetails = new[]
            {
                new ConfirmedFellingDetailViewModel
                {
                    ConfirmedFellingDetailsId = _fellingDetailsId,
                    OperationType = FellingOperationType.None
                }
            }
        };
        var model = new ConfirmedFellingRestockingDetailsModel
        {
            ApplicationId = _applicationId,
            Compartments = new[]
            {
                compartment
            },
            SubmittedFlaPropertyCompartments = new List<SubmittedFlaPropertyCompartment>(),
            FellingLicenceApplicationSummary = new(),
            Breadcrumbs = new(),
            Amendment = new AmendmentReview
            {
                CanCurrentUserAmend = false,
                AmendmentState = AmendmentStateEnum.NoAmendment
            },
        };
        _cfrUseCaseMock.Setup(x => x.GetConfirmedFellingAndRestockingDetailsAsync(
            _applicationId, It.IsAny<InternalUser>(), _cancellationToken, It.IsAny<string>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Success(model));

        var result = await _controller.AddConfirmedRestockingDetails(
            _applicationId, _compartmentId, _fellingDetailsId, _cfrUseCaseMock.Object, _cancellationToken);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<AmendConfirmedRestockingDetailsViewModel>(viewResult.Model);
    }

    [Fact]
    public async Task AddConfirmedRestockingDetails_Get_Redirects_WhenFailure()
    {
        _cfrUseCaseMock.Setup(x => x.GetConfirmedFellingAndRestockingDetailsAsync(
            _applicationId, It.IsAny<InternalUser>(), _cancellationToken, It.IsAny<string>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure<ConfirmedFellingRestockingDetailsModel>("fail"));

        var result = await _controller.AddConfirmedRestockingDetails(
            _applicationId, _compartmentId, _fellingDetailsId, _cfrUseCaseMock.Object, _cancellationToken);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ConfirmedFellingAndRestocking", redirect.ActionName);
        Assert.Equal(_applicationId, redirect.RouteValues["applicationId"]);
    }

    [Fact]
    public async Task AddConfirmedRestockingDetails_Get_Redirects_WhenCompartmentNotFound()
    {
        var model = new ConfirmedFellingRestockingDetailsModel
        {
            ApplicationId = _applicationId,
            Compartments = Array.Empty<CompartmentConfirmedFellingRestockingDetailsModel>(),
            SubmittedFlaPropertyCompartments = new List<SubmittedFlaPropertyCompartment>(),
            FellingLicenceApplicationSummary = new(),
            Breadcrumbs = new(),
            Amendment = new AmendmentReview
            {
                CanCurrentUserAmend = false,
                AmendmentState = AmendmentStateEnum.NoAmendment
            },
        };
        _cfrUseCaseMock.Setup(x => x.GetConfirmedFellingAndRestockingDetailsAsync(
            _applicationId, It.IsAny<InternalUser>(), _cancellationToken, It.IsAny<string>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Success(model));

        var result = await _controller.AddConfirmedRestockingDetails(
            _applicationId, _compartmentId, _fellingDetailsId, _cfrUseCaseMock.Object, _cancellationToken);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ConfirmedFellingAndRestocking", redirect.ActionName);
        Assert.Equal(_applicationId, redirect.RouteValues["applicationId"]);
    }

    [Fact]
    public async Task AmendConfirmedRestockingDetails_Post_Redirects_WhenSaveFails()
    {
        var viewModel = new AmendConfirmedRestockingDetailsViewModel
        {
            ApplicationId = _applicationId,
            ConfirmedFellingRestockingDetails = new IndividualConfirmedRestockingDetailModel
            {
                CompartmentId = _compartmentId,
                ConfirmedRestockingDetails = new ConfirmedRestockingDetailViewModel
                {
                    ConfirmedFellingDetailsId = _fellingDetailsId,
                    RestockingProposal = TypeOfProposal.CreateDesignedOpenGround
                }
            },
            SubmittedFlaPropertyCompartments = new List<SubmittedFlaPropertyCompartment>(),
        };
        _cfrUseCaseMock.Setup(x => x.SaveConfirmedRestockingDetailsAsync(
            viewModel, It.IsAny<InternalUser>(), _cancellationToken))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure("fail"));
        _amendRestockingValidatorMock.Setup(s => s.Validate(It.IsAny<AmendConfirmedRestockingDetailsViewModel>()))
            .Returns(new ValidationResult());

        var result = await _controller.AmendConfirmedRestockingDetails(
            viewModel, _cfrUseCaseMock.Object, _amendRestockingValidatorMock.Object, _cancellationToken);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ConfirmedFellingAndRestocking", redirect.ActionName);
        Assert.Equal(_applicationId, redirect.RouteValues["id"]);
    }

    [Fact]
    public async Task AmendConfirmedRestockingDetails_Post_Redirects_WhenSaveSucceeds()
    {
        var viewModel = new AmendConfirmedRestockingDetailsViewModel
        {
            ApplicationId = _applicationId,
            ConfirmedFellingRestockingDetails = new IndividualConfirmedRestockingDetailModel
            {
                CompartmentId = _compartmentId,
                ConfirmedRestockingDetails = new ConfirmedRestockingDetailViewModel
                {
                    ConfirmedFellingDetailsId = _fellingDetailsId,
                    RestockingProposal = TypeOfProposal.CreateDesignedOpenGround
                }
            },
            SubmittedFlaPropertyCompartments = new List<SubmittedFlaPropertyCompartment>(),
        };
        _cfrUseCaseMock.Setup(x => x.SaveConfirmedRestockingDetailsAsync(
            viewModel, It.IsAny<InternalUser>(), _cancellationToken))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Success(true));
        _amendRestockingValidatorMock.Setup(s => s.Validate(It.IsAny<AmendConfirmedRestockingDetailsViewModel>()))
            .Returns(new ValidationResult());

        var result = await _controller.AmendConfirmedRestockingDetails(
            viewModel, _cfrUseCaseMock.Object, _amendRestockingValidatorMock.Object, _cancellationToken);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ConfirmedFellingAndRestocking", redirect.ActionName);
        Assert.Equal(_applicationId, redirect.RouteValues["id"]);
    }

    [Fact]
    public async Task AmendConfirmedRestockingDetails_Post_ReturnsView_WhenModelStateInvalid()
    {
        var viewModel = new AmendConfirmedRestockingDetailsViewModel
        {
            ApplicationId = _applicationId,
            ConfirmedFellingRestockingDetails = new IndividualConfirmedRestockingDetailModel
            {
                CompartmentId = _compartmentId,
                ConfirmedRestockingDetails = new ConfirmedRestockingDetailViewModel
                {
                    ConfirmedFellingDetailsId = _fellingDetailsId,
                    RestockingProposal = TypeOfProposal.CreateDesignedOpenGround
                }
            },
            SubmittedFlaPropertyCompartments = new List<SubmittedFlaPropertyCompartment>(),
        };
        _controller.ModelState.AddModelError("Test", "Error");
        var compartment = new CompartmentConfirmedFellingRestockingDetailsModel
        {
            CompartmentId = _compartmentId,
            ConfirmedFellingDetails = new[]
            {
                new ConfirmedFellingDetailViewModel
                {
                    ConfirmedFellingDetailsId = _fellingDetailsId,
                    OperationType = FellingOperationType.None
                }
            }
        };
        var model = new ConfirmedFellingRestockingDetailsModel
        {
            ApplicationId = _applicationId,
            Compartments = new[]
            {
                compartment
            },
            SubmittedFlaPropertyCompartments = new List<SubmittedFlaPropertyCompartment>(),
            FellingLicenceApplicationSummary = new(),
            Breadcrumbs = new(),
            Amendment = new AmendmentReview
            {
                CanCurrentUserAmend = false,
                AmendmentState = AmendmentStateEnum.NoAmendment
            },
        };
        _cfrUseCaseMock.Setup(x => x.GetConfirmedFellingAndRestockingDetailsAsync(
            _applicationId, It.IsAny<InternalUser>(), _cancellationToken, It.IsAny<string>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Success(model));
        _amendRestockingValidatorMock.Setup(s => s.Validate(It.IsAny<AmendConfirmedRestockingDetailsViewModel>()))
            .Returns(new ValidationResult(_fixture.Create<List<ValidationFailure>>()));

        var result = await _controller.AmendConfirmedRestockingDetails(
            viewModel, _cfrUseCaseMock.Object, _amendRestockingValidatorMock.Object, _cancellationToken);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<AmendConfirmedRestockingDetailsViewModel>(viewResult.Model);
    }

    [Fact]
    public async Task AddConfirmedRestockingDetails_Post_Redirects_WhenSaveFails()
    {
        var viewModel = new AmendConfirmedRestockingDetailsViewModel
        {
            ApplicationId = _applicationId,
            ConfirmedFellingRestockingDetails = new IndividualConfirmedRestockingDetailModel
            {
                CompartmentId = _compartmentId,
                ConfirmedRestockingDetails = new ConfirmedRestockingDetailViewModel
                {
                    ConfirmedFellingDetailsId = _fellingDetailsId,
                    RestockingProposal = TypeOfProposal.CreateDesignedOpenGround
                }
            },
            SubmittedFlaPropertyCompartments = new List<SubmittedFlaPropertyCompartment>(),
        };
        _amendRestockingValidatorMock.Setup(s => s.Validate(It.IsAny<AmendConfirmedRestockingDetailsViewModel>()))
            .Returns(new ValidationResult());
        _cfrUseCaseMock.Setup(x => x.SaveConfirmedRestockingDetailsAsync(
            viewModel, It.IsAny<InternalUser>(), _cancellationToken))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Failure("fail"));

        var result = await _controller.AddConfirmedRestockingDetails(
            viewModel, _cfrUseCaseMock.Object, _amendRestockingValidatorMock.Object, _cancellationToken);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ConfirmedFellingAndRestocking", redirect.ActionName);
        Assert.Equal(_applicationId, redirect.RouteValues["id"]);
    }

    [Fact]
    public async Task AddConfirmedRestockingDetails_Post_Redirects_WhenSaveSucceeds()
    {
        var viewModel = new AmendConfirmedRestockingDetailsViewModel
        {
            ApplicationId = _applicationId,
            ConfirmedFellingRestockingDetails = new IndividualConfirmedRestockingDetailModel
            {
                CompartmentId = _compartmentId,
                ConfirmedRestockingDetails = new ConfirmedRestockingDetailViewModel
                {
                    ConfirmedFellingDetailsId = _fellingDetailsId,
                    RestockingProposal = TypeOfProposal.CreateDesignedOpenGround
                }
            },
            SubmittedFlaPropertyCompartments = new List<SubmittedFlaPropertyCompartment>(),
        };
        _cfrUseCaseMock.Setup(x => x.SaveConfirmedRestockingDetailsAsync(
            viewModel, It.IsAny<InternalUser>(), _cancellationToken))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Success(true));
        _amendRestockingValidatorMock.Setup(s => s.Validate(It.IsAny<AmendConfirmedRestockingDetailsViewModel>()))
            .Returns(new ValidationResult());

        var result = await _controller.AddConfirmedRestockingDetails(
            viewModel, _cfrUseCaseMock.Object, _amendRestockingValidatorMock.Object, _cancellationToken);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ConfirmedFellingAndRestocking", redirect.ActionName);
        Assert.Equal(_applicationId, redirect.RouteValues["id"]);
    }

    [Fact]
    public async Task AddConfirmedRestockingDetails_Post_ReturnsView_WhenModelStateInvalid()
    {
        var viewModel = new AmendConfirmedRestockingDetailsViewModel
        {
            ApplicationId = _applicationId,
            ConfirmedFellingRestockingDetails = new IndividualConfirmedRestockingDetailModel
            {
                CompartmentId = _compartmentId,
                ConfirmedRestockingDetails = new ConfirmedRestockingDetailViewModel
                {
                    ConfirmedFellingDetailsId = _fellingDetailsId,
                    RestockingProposal = TypeOfProposal.CreateDesignedOpenGround
                }
            },
            SubmittedFlaPropertyCompartments = new List<SubmittedFlaPropertyCompartment>(),
        };
        _controller.ModelState.AddModelError("Test", "Error");
        var compartment = new CompartmentConfirmedFellingRestockingDetailsModel
        {
            CompartmentId = _compartmentId,
            ConfirmedFellingDetails = new[]
            {
                new ConfirmedFellingDetailViewModel
                {
                    ConfirmedFellingDetailsId = _fellingDetailsId,
                    OperationType = FellingOperationType.None
                }
            }
        };
        var model = new ConfirmedFellingRestockingDetailsModel
        {
            ApplicationId = _applicationId,
            Compartments = new[]
            {
                compartment
            },
            SubmittedFlaPropertyCompartments = new List<SubmittedFlaPropertyCompartment>(),
            FellingLicenceApplicationSummary = new(),
            Breadcrumbs = new(),
            Amendment = new AmendmentReview
            {
                CanCurrentUserAmend = false,
                AmendmentState = AmendmentStateEnum.NoAmendment
            },
        };

        _amendRestockingValidatorMock.Setup(s => s.Validate(It.IsAny<AmendConfirmedRestockingDetailsViewModel>()))
            .Returns(new ValidationResult(_fixture.Create<List<ValidationFailure>>()));
        _cfrUseCaseMock.Setup(x => x.GetConfirmedFellingAndRestockingDetailsAsync(
            _applicationId, It.IsAny<InternalUser>(), _cancellationToken, It.IsAny<string>()))
            .ReturnsAsync(CSharpFunctionalExtensions.Result.Success(model));

        var result = await _controller.AddConfirmedRestockingDetails(
            viewModel, _cfrUseCaseMock.Object, _amendRestockingValidatorMock.Object, _cancellationToken);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<AmendConfirmedRestockingDetailsViewModel>(viewResult.Model);
    }


    [Fact]
    public async Task SendAmendmentsToApplicant_ReturnsError_WhenReasonIsNullOrWhitespace()
    {
        var cfrUseCaseMock = new Mock<IConfirmedFellingAndRestockingDetailsUseCase>();
        var woReviewUseCaseMock = new Mock<IWoodlandOfficerReviewUseCase>();

        var result1 = await _controller.SendAmendmentsToApplicant(_applicationId, null, cfrUseCaseMock.Object, woReviewUseCaseMock.Object, _cancellationToken);
        var redirect1 = Assert.IsType<RedirectToActionResult>(result1);
        Assert.Equal("ConfirmedFellingAndRestocking", redirect1.ActionName);
        Assert.Equal(_applicationId, redirect1.RouteValues["id"]);

        var result2 = await _controller.SendAmendmentsToApplicant(_applicationId, "   ", cfrUseCaseMock.Object, woReviewUseCaseMock.Object, _cancellationToken);
        var redirect2 = Assert.IsType<RedirectToActionResult>(result2);
        Assert.Equal("ConfirmedFellingAndRestocking", redirect2.ActionName);
        Assert.Equal(_applicationId, redirect2.RouteValues["id"]);
    }

    [Fact]
    public async Task SendAmendmentsToApplicant_ReturnsError_WhenUseCaseFails()
    {
        var cfrUseCaseMock = new Mock<IConfirmedFellingAndRestockingDetailsUseCase>();
        var woReviewUseCaseMock = new Mock<IWoodlandOfficerReviewUseCase>();
        cfrUseCaseMock.Setup(x => x.SendAmendmentsToApplicant(_applicationId, It.IsAny<InternalUser>(), "reason", _cancellationToken))
            .ReturnsAsync(Result.Failure("fail"));

        var result = await _controller.SendAmendmentsToApplicant(_applicationId, "reason", cfrUseCaseMock.Object, woReviewUseCaseMock.Object, _cancellationToken);
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ConfirmedFellingAndRestocking", redirect.ActionName);
        Assert.Equal(_applicationId, redirect.RouteValues["id"]);
    }

    [Fact]
    public async Task SendAmendmentsToApplicant_ReturnsConfirmation_WhenUseCaseSucceeds()
    {
        var cfrUseCaseMock = new Mock<IConfirmedFellingAndRestockingDetailsUseCase>();
        var woReviewUseCaseMock = new Mock<IWoodlandOfficerReviewUseCase>();
        cfrUseCaseMock.Setup(x => x.SendAmendmentsToApplicant(_applicationId, It.IsAny<InternalUser>(), "reason", _cancellationToken))
            .ReturnsAsync(Result.Success());

        var result = await _controller.SendAmendmentsToApplicant(_applicationId, "reason", cfrUseCaseMock.Object, woReviewUseCaseMock.Object, _cancellationToken);
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ConfirmedFellingAndRestocking", redirect.ActionName);
        Assert.Equal("WoodlandOfficerReview", redirect.ControllerName);
        Assert.Equal(_applicationId, redirect.RouteValues["id"]);
    }

    [Fact]
    public async Task MakeFurtherAmendments_ReturnsError_WhenUseCaseFails()
    {
        var cfrUseCaseMock = new Mock<IConfirmedFellingAndRestockingDetailsUseCase>();
        var woReviewUseCaseMock = new Mock<IWoodlandOfficerReviewUseCase>();
        cfrUseCaseMock.Setup(x => x.MakeFurtherAmendments(It.IsAny<InternalUser>(), _amendmentReviewId, _cancellationToken))
            .ReturnsAsync(Result.Failure("fail"));

        var result = await _controller.MakeFurtherAmendments(_applicationId, _amendmentReviewId, cfrUseCaseMock.Object, woReviewUseCaseMock.Object, _cancellationToken);
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ConfirmedFellingAndRestocking", redirect.ActionName);
        Assert.Equal(_applicationId, redirect.RouteValues["id"]);
    }

    [Fact]
    public async Task MakeFurtherAmendments_ReturnsConfirmation_WhenUseCaseSucceeds()
    {
        var cfrUseCaseMock = new Mock<IConfirmedFellingAndRestockingDetailsUseCase>();
        var woReviewUseCaseMock = new Mock<IWoodlandOfficerReviewUseCase>();
        cfrUseCaseMock.Setup(x => x.MakeFurtherAmendments(It.IsAny<InternalUser>(), _amendmentReviewId, _cancellationToken))
            .ReturnsAsync(Result.Success());


        var result = await _controller.MakeFurtherAmendments(_applicationId, _amendmentReviewId, cfrUseCaseMock.Object, woReviewUseCaseMock.Object, _cancellationToken);
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ConfirmedFellingAndRestocking", redirect.ActionName);
        Assert.Equal("WoodlandOfficerReview", redirect.ControllerName);
        Assert.Equal(_applicationId, redirect.RouteValues["id"]);
    }
}