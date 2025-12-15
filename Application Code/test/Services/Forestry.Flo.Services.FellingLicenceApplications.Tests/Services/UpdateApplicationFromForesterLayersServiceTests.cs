using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.FellingLicenceApplications.Configuration;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.Gis.Interfaces;
using Forestry.Flo.Services.Gis.Models.Esri.Responses.Layers;
using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services;

public class UpdateApplicationFromForesterLayersServiceTests
{
    private readonly Mock<IFellingLicenceApplicationExternalRepository> _repository = new();
    private readonly Mock<IForesterServices> _foresterServices = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly DesignationsOptions _designationsOptions = new();

    [Theory, AutoMoqData]
    public async Task WhenRepositoryThrows(
        Guid applicationId,
        Dictionary<Guid, string> compartments)
    {
        var sut = CreateSut();

        _repository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Repository failure"));

        var result = await sut.UpdateForPawsLayersAsync(applicationId, compartments, CancellationToken.None);

        Assert.False(result.IsSuccess);

        _repository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _repository.VerifyNoOtherCalls();

        _unitOfWork.VerifyNoOtherCalls();

        _foresterServices.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenApplicationNotFoundInRepository(
        Guid applicationId,
        Dictionary<Guid, string> compartments)
    {
        var sut = CreateSut();

        _repository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.None);

        var result = await sut.UpdateForPawsLayersAsync(applicationId, compartments, CancellationToken.None);

        Assert.False(result.IsSuccess);

        _repository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _repository.VerifyNoOtherCalls();

        _unitOfWork.VerifyNoOtherCalls();

        _foresterServices.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenGisDataCannotBeDeserialised(
        Guid applicationId,
        Dictionary<Guid, string> compartments,
        FellingLicenceApplication application)
    {
        var sut = CreateSut();

        application.LinkedPropertyProfile.ProposedCompartmentDesignations = [];
        application.FellingLicenceApplicationStepStatus.CompartmentDesignationsStatuses.Clear();

        _repository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));

        var result = await sut.UpdateForPawsLayersAsync(applicationId, compartments, CancellationToken.None);

        Assert.False(result.IsSuccess);

        _repository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _repository.VerifyNoOtherCalls();

        _unitOfWork.VerifyNoOtherCalls();

        _foresterServices.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenGetAncientWoodlandLayerFails(
        Guid applicationId,
        Dictionary<Guid, string> compartments,
        Polygon polygon,
        FellingLicenceApplication application)
    {
        var sut = CreateSut();

        application.LinkedPropertyProfile.ProposedCompartmentDesignations = [];
        application.FellingLicenceApplicationStepStatus.CompartmentDesignationsStatuses.Clear();

        foreach (var compartment in compartments)
        {
            compartments[compartment.Key] = JsonConvert.SerializeObject(polygon);
        }

        _repository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));

        _foresterServices
            .Setup(x => x.GetAncientWoodlandAsync(It.IsAny<BaseShape>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<List<AncientWoodland>>("error"));

        var result = await sut.UpdateForPawsLayersAsync(applicationId, compartments, CancellationToken.None);

        Assert.False(result.IsSuccess);

        _repository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _repository.VerifyNoOtherCalls();

        _unitOfWork.VerifyNoOtherCalls();

        _foresterServices.Verify(x => x.GetAncientWoodlandAsync(It.Is<Polygon>(p => JsonConvert.SerializeObject(p) == compartments.First().Value), It.IsAny<CancellationToken>()), Times.Once);
        _foresterServices.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenGetAncientWoodlandRevisedLayerFails(
        Guid applicationId,
        Dictionary<Guid, string> compartments,
        Polygon polygon,
        FellingLicenceApplication application,
        List<AncientWoodland> ancientWoodlandResults)
    {
        var sut = CreateSut();

        application.LinkedPropertyProfile.ProposedCompartmentDesignations = [];
        application.FellingLicenceApplicationStepStatus.CompartmentDesignationsStatuses.Clear();

        foreach (var compartment in compartments)
        {
            compartments[compartment.Key] = JsonConvert.SerializeObject(polygon);
        }

        _repository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));

        _foresterServices
            .Setup(x => x.GetAncientWoodlandAsync(It.IsAny<BaseShape>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(ancientWoodlandResults));

        _foresterServices
            .Setup(x => x.GetAncientWoodlandsRevisedAsync(It.IsAny<BaseShape>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<List<AncientWoodland>>("error"));

        var result = await sut.UpdateForPawsLayersAsync(applicationId, compartments, CancellationToken.None);

        Assert.False(result.IsSuccess);

        _repository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _repository.VerifyNoOtherCalls();

        _unitOfWork.VerifyNoOtherCalls();

        _foresterServices.Verify(x => x.GetAncientWoodlandAsync(It.Is<Polygon>(p => JsonConvert.SerializeObject(p) == compartments.First().Value), It.IsAny<CancellationToken>()), Times.Once);
        _foresterServices.Verify(x => x.GetAncientWoodlandsRevisedAsync(It.Is<Polygon>(p => JsonConvert.SerializeObject(p) == compartments.First().Value), It.IsAny<CancellationToken>()), Times.Once);
        _foresterServices.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenSaveToDatabaseFails(
        Guid applicationId,
        Dictionary<Guid, string> compartments,
        Polygon polygon,
        FellingLicenceApplication application,
        List<AncientWoodland> ancientWoodlandResults)
    {
        var sut = CreateSut();

        application.LinkedPropertyProfile.ProposedCompartmentDesignations = [];
        application.FellingLicenceApplicationStepStatus.CompartmentDesignationsStatuses.Clear();

        foreach (var compartment in compartments)
        {
            compartments[compartment.Key] = JsonConvert.SerializeObject(polygon);
        }

        _repository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));

        _unitOfWork.Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Failure(UserDbErrorReason.General));

        _foresterServices
            .Setup(x => x.GetAncientWoodlandAsync(It.IsAny<BaseShape>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(ancientWoodlandResults));

        _foresterServices
            .Setup(x => x.GetAncientWoodlandsRevisedAsync(It.IsAny<BaseShape>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(ancientWoodlandResults));

        var result = await sut.UpdateForPawsLayersAsync(applicationId, compartments, CancellationToken.None);

        Assert.False(result.IsSuccess);

        _repository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _repository.VerifyNoOtherCalls();
        _unitOfWork.VerifyNoOtherCalls();

        _foresterServices.Verify(x => x.GetAncientWoodlandAsync(It.Is<Polygon>(p => JsonConvert.SerializeObject(p) == compartments.First().Value), It.IsAny<CancellationToken>()), Times.Exactly(compartments.Count));
        _foresterServices.Verify(x => x.GetAncientWoodlandsRevisedAsync(It.Is<Polygon>(p => JsonConvert.SerializeObject(p) == compartments.First().Value), It.IsAny<CancellationToken>()), Times.Exactly(compartments.Count));
        _foresterServices.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenNoMatchesFoundOnAncientWoodlandLayers_NoDesignationEntityYet(
        Guid applicationId,
        Dictionary<Guid, string> compartments,
        Polygon polygon,
        FellingLicenceApplication application,
        List<AncientWoodland> ancientWoodlandResults)
    {
        var sut = CreateSut();

        application.LinkedPropertyProfile.ProposedCompartmentDesignations = [];
        application.FellingLicenceApplicationStepStatus.CompartmentDesignationsStatuses.Clear();

        foreach (var compartment in compartments)
        {
            compartments[compartment.Key] = JsonConvert.SerializeObject(polygon);
        }

        _repository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));

        _unitOfWork.Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        _foresterServices
            .Setup(x => x.GetAncientWoodlandAsync(It.IsAny<BaseShape>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(ancientWoodlandResults));

        _foresterServices
            .Setup(x => x.GetAncientWoodlandsRevisedAsync(It.IsAny<BaseShape>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(ancientWoodlandResults));

        var result = await sut.UpdateForPawsLayersAsync(applicationId, compartments, CancellationToken.None);

        Assert.True(result.IsSuccess);

        foreach (var compartment in compartments)
        {
            var designations = application.LinkedPropertyProfile.ProposedCompartmentDesignations
                .Single(d => d.PropertyProfileCompartmentId == compartment.Key);

            Assert.Equal(0, designations.CrossesPawsZones.Count);
            Assert.Null(designations.ProportionBeforeFelling);
            Assert.Null(designations.ProportionAfterFelling);
        }

        Assert.Empty(application.FellingLicenceApplicationStepStatus.CompartmentDesignationsStatuses);

        _repository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(x=> x.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _repository.VerifyNoOtherCalls();
        _unitOfWork.VerifyNoOtherCalls();

        _foresterServices.Verify(x => x.GetAncientWoodlandAsync(It.Is<Polygon>(p => JsonConvert.SerializeObject(p) == compartments.First().Value), It.IsAny<CancellationToken>()), Times.Exactly(compartments.Count));
        _foresterServices.Verify(x => x.GetAncientWoodlandsRevisedAsync(It.Is<Polygon>(p => JsonConvert.SerializeObject(p) == compartments.First().Value), It.IsAny<CancellationToken>()), Times.Exactly(compartments.Count));
        _foresterServices.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenNoMatchesFoundOnAncientWoodlandLayers_ExistingDesignationEntityIsCleared(
        Guid applicationId,
        Dictionary<Guid, string> compartments,
        Polygon polygon,
        FellingLicenceApplication application,
        List<AncientWoodland> ancientWoodlandResults)
    {
        var sut = CreateSut();

        application.LinkedPropertyProfile.ProposedCompartmentDesignations = 
        [
            new ProposedCompartmentDesignations
            {
                LinkedPropertyProfile = application.LinkedPropertyProfile,
                CrossesPawsZones = ["ARW", "IAWPP"],
                PropertyProfileCompartmentId = compartments.Keys.First(),
                ProportionBeforeFelling = NativeTreeSpeciesProportion.GreaterThan80Percent,
                ProportionAfterFelling = NativeTreeSpeciesProportion.Between50And80Percent
            }
        ];

        application.FellingLicenceApplicationStepStatus.CompartmentDesignationsStatuses.Clear();
        application.FellingLicenceApplicationStepStatus.CompartmentDesignationsStatuses.Add(
            new CompartmentDesignationStatus
            {
                CompartmentId = compartments.Keys.First(),
                Status = true
            });

        foreach (var compartment in compartments)
        {
            compartments[compartment.Key] = JsonConvert.SerializeObject(polygon);
        }

        _repository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));

        _unitOfWork.Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        _foresterServices
            .Setup(x => x.GetAncientWoodlandAsync(It.IsAny<BaseShape>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(ancientWoodlandResults));

        _foresterServices
            .Setup(x => x.GetAncientWoodlandsRevisedAsync(It.IsAny<BaseShape>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(ancientWoodlandResults));

        var result = await sut.UpdateForPawsLayersAsync(applicationId, compartments, CancellationToken.None);

        Assert.True(result.IsSuccess);

        foreach (var compartment in compartments)
        {
            var designations = application.LinkedPropertyProfile.ProposedCompartmentDesignations
                .Single(d => d.PropertyProfileCompartmentId == compartment.Key);

            Assert.Empty(designations.CrossesPawsZones);
            Assert.Null(designations.ProportionBeforeFelling);
            Assert.Null(designations.ProportionAfterFelling);
        }

        Assert.Empty(application.FellingLicenceApplicationStepStatus.CompartmentDesignationsStatuses);

        _repository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _repository.VerifyNoOtherCalls();
        _unitOfWork.VerifyNoOtherCalls();

        _foresterServices.Verify(x => x.GetAncientWoodlandAsync(It.Is<Polygon>(p => JsonConvert.SerializeObject(p) == compartments.First().Value), It.IsAny<CancellationToken>()), Times.Exactly(compartments.Count));
        _foresterServices.Verify(x => x.GetAncientWoodlandsRevisedAsync(It.Is<Polygon>(p => JsonConvert.SerializeObject(p) == compartments.First().Value), It.IsAny<CancellationToken>()), Times.Exactly(compartments.Count));
        _foresterServices.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenSingleMatchFoundOnAncientWoodlandLayers_NoDesignationEntityYet(
        Guid applicationId,
        Dictionary<Guid, string> compartments,
        Polygon polygon,
        FellingLicenceApplication application,
        List<AncientWoodland> ancientWoodlandResults)
    {
        var sut = CreateSut();

        application.LinkedPropertyProfile.ProposedCompartmentDesignations = [];
        application.FellingLicenceApplicationStepStatus.CompartmentDesignationsStatuses.Clear();

        foreach (var compartment in compartments)
        {
            compartments[compartment.Key] = JsonConvert.SerializeObject(polygon);
        }

        ancientWoodlandResults.First().Status = _designationsOptions.PawsZoneNames.First();

        _repository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));

        _unitOfWork.Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        _foresterServices
            .Setup(x => x.GetAncientWoodlandAsync(It.IsAny<BaseShape>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(ancientWoodlandResults));

        _foresterServices
            .Setup(x => x.GetAncientWoodlandsRevisedAsync(It.IsAny<BaseShape>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(ancientWoodlandResults));

        var result = await sut.UpdateForPawsLayersAsync(applicationId, compartments, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(compartments.Count, application.FellingLicenceApplicationStepStatus.CompartmentDesignationsStatuses.Count);
        Assert.Equal(compartments.Count, application.LinkedPropertyProfile.ProposedCompartmentDesignations.Count);
        
        foreach (var compartment in compartments)
        {
            var designations = application.LinkedPropertyProfile.ProposedCompartmentDesignations
                .Single(d => d.PropertyProfileCompartmentId == compartment.Key);

            Assert.Single(designations.CrossesPawsZones);
            Assert.Equal(designations.CrossesPawsZones.Single(), _designationsOptions.PawsZoneNames.First());

            Assert.Null(designations.ProportionBeforeFelling);
            Assert.Null(designations.ProportionAfterFelling);

            var stepStatus = application.FellingLicenceApplicationStepStatus.CompartmentDesignationsStatuses
                .Single(s => s.CompartmentId == compartment.Key);
            Assert.Null(stepStatus.Status);
        }
        
        _repository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _repository.VerifyNoOtherCalls();
        _unitOfWork.VerifyNoOtherCalls();

        _foresterServices.Verify(x => x.GetAncientWoodlandAsync(It.Is<Polygon>(p => JsonConvert.SerializeObject(p) == compartments.First().Value), It.IsAny<CancellationToken>()), Times.Exactly(compartments.Count));
        _foresterServices.Verify(x => x.GetAncientWoodlandsRevisedAsync(It.Is<Polygon>(p => JsonConvert.SerializeObject(p) == compartments.First().Value), It.IsAny<CancellationToken>()), Times.Exactly(compartments.Count));
        _foresterServices.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenMultipleMatchFoundOnAncientWoodlandLayers_NoDesignationEntityYet(
        Guid applicationId,
        Dictionary<Guid, string> compartments,
        Polygon polygon,
        FellingLicenceApplication application,
        List<AncientWoodland> ancientWoodlandResults)
    {
        var sut = CreateSut();

        application.LinkedPropertyProfile.ProposedCompartmentDesignations = [];
        application.FellingLicenceApplicationStepStatus.CompartmentDesignationsStatuses.Clear();

        foreach (var compartment in compartments)
        {
            compartments[compartment.Key] = JsonConvert.SerializeObject(polygon);
        }

        ancientWoodlandResults.First().Status = _designationsOptions.PawsZoneNames.First();
        ancientWoodlandResults.Last().Status = _designationsOptions.PawsZoneNames.Last();

        _repository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));

        _unitOfWork.Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        _foresterServices
            .Setup(x => x.GetAncientWoodlandAsync(It.IsAny<BaseShape>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(ancientWoodlandResults));

        _foresterServices
            .Setup(x => x.GetAncientWoodlandsRevisedAsync(It.IsAny<BaseShape>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(ancientWoodlandResults));

        var result = await sut.UpdateForPawsLayersAsync(applicationId, compartments, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(compartments.Count, application.FellingLicenceApplicationStepStatus.CompartmentDesignationsStatuses.Count);
        Assert.Equal(compartments.Count, application.LinkedPropertyProfile.ProposedCompartmentDesignations.Count);

        foreach (var compartment in compartments)
        {
            var designations = application.LinkedPropertyProfile.ProposedCompartmentDesignations
                .Single(d => d.PropertyProfileCompartmentId == compartment.Key);

            Assert.Equal(2, designations.CrossesPawsZones.Count);
            Assert.Contains(_designationsOptions.PawsZoneNames.First(), designations.CrossesPawsZones);
            Assert.Contains(_designationsOptions.PawsZoneNames.Last(), designations.CrossesPawsZones);

            Assert.Null(designations.ProportionBeforeFelling);
            Assert.Null(designations.ProportionAfterFelling);

            var stepStatus = application.FellingLicenceApplicationStepStatus.CompartmentDesignationsStatuses
                .Single(s => s.CompartmentId == compartment.Key);
            Assert.Null(stepStatus.Status);
        }

        _repository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _repository.VerifyNoOtherCalls();
        _unitOfWork.VerifyNoOtherCalls();

        _foresterServices.Verify(x => x.GetAncientWoodlandAsync(It.Is<Polygon>(p => JsonConvert.SerializeObject(p) == compartments.First().Value), It.IsAny<CancellationToken>()), Times.Exactly(compartments.Count));
        _foresterServices.Verify(x => x.GetAncientWoodlandsRevisedAsync(It.Is<Polygon>(p => JsonConvert.SerializeObject(p) == compartments.First().Value), It.IsAny<CancellationToken>()), Times.Exactly(compartments.Count));
        _foresterServices.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task WhenSingleMatchFoundOnAncientWoodlandLayers_DesignationEntityExistsForOneCompartment(
        Guid applicationId,
        Dictionary<Guid, string> compartments,
        Polygon polygon,
        FellingLicenceApplication application,
        List<AncientWoodland> ancientWoodlandResults)
    {
        var sut = CreateSut();

        application.LinkedPropertyProfile.ProposedCompartmentDesignations =
        [
            new ProposedCompartmentDesignations
            {
                LinkedPropertyProfile = application.LinkedPropertyProfile,
                CrossesPawsZones = ["ARW"],
                PropertyProfileCompartmentId = compartments.Keys.First(),
                ProportionBeforeFelling = NativeTreeSpeciesProportion.GreaterThan80Percent,
                ProportionAfterFelling = NativeTreeSpeciesProportion.Between50And80Percent
            }
        ];

        application.FellingLicenceApplicationStepStatus.CompartmentDesignationsStatuses.Clear();
        application.FellingLicenceApplicationStepStatus.CompartmentDesignationsStatuses.Add(
            new CompartmentDesignationStatus
            {
                CompartmentId = compartments.Keys.First(),
                Status = true
            });

        foreach (var compartment in compartments)
        {
            compartments[compartment.Key] = JsonConvert.SerializeObject(polygon);
        }

        ancientWoodlandResults.First().Status = _designationsOptions.PawsZoneNames.First();

        _repository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));

        _unitOfWork.Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        _foresterServices
            .Setup(x => x.GetAncientWoodlandAsync(It.IsAny<BaseShape>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(ancientWoodlandResults));

        _foresterServices
            .Setup(x => x.GetAncientWoodlandsRevisedAsync(It.IsAny<BaseShape>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(ancientWoodlandResults));

        var result = await sut.UpdateForPawsLayersAsync(applicationId, compartments, CancellationToken.None);

        Assert.True(result.IsSuccess);

        Assert.Equal(compartments.Count, application.FellingLicenceApplicationStepStatus.CompartmentDesignationsStatuses.Count);
        Assert.Equal(compartments.Count, application.LinkedPropertyProfile.ProposedCompartmentDesignations.Count);

        foreach (var compartment in compartments)
        {
            var designations = application.LinkedPropertyProfile.ProposedCompartmentDesignations
                .Single(d => d.PropertyProfileCompartmentId == compartment.Key);

            Assert.Single(designations.CrossesPawsZones);
            Assert.Equal(_designationsOptions.PawsZoneNames.First(), designations.CrossesPawsZones.Single());

            var stepStatus = application.FellingLicenceApplicationStepStatus.CompartmentDesignationsStatuses
                .Single(s => s.CompartmentId == compartment.Key);

            if (compartments.First().Key == compartment.Key)
            {
                Assert.Equal(NativeTreeSpeciesProportion.GreaterThan80Percent, designations.ProportionBeforeFelling);
                Assert.Equal(NativeTreeSpeciesProportion.Between50And80Percent, designations.ProportionAfterFelling);
                Assert.True(stepStatus.Status);
            }
            else
            {
                Assert.Null(designations.ProportionBeforeFelling);
                Assert.Null(designations.ProportionAfterFelling);
                Assert.Null(stepStatus.Status);
            }
        }

        _repository.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _repository.VerifyNoOtherCalls();
        _unitOfWork.VerifyNoOtherCalls();

        _foresterServices.Verify(x => x.GetAncientWoodlandAsync(It.Is<Polygon>(p => JsonConvert.SerializeObject(p) == compartments.First().Value), It.IsAny<CancellationToken>()), Times.Exactly(compartments.Count));
        _foresterServices.Verify(x => x.GetAncientWoodlandsRevisedAsync(It.Is<Polygon>(p => JsonConvert.SerializeObject(p) == compartments.First().Value), It.IsAny<CancellationToken>()), Times.Exactly(compartments.Count));
        _foresterServices.VerifyNoOtherCalls();
    }

    private UpdateApplicationFromForesterLayersService CreateSut()
    {
        _repository.Reset();
        _foresterServices.Reset();
        _unitOfWork.Reset();
        var options = new OptionsWrapper<DesignationsOptions>(_designationsOptions);

        _repository.SetupGet(x => x.UnitOfWork).Returns(_unitOfWork.Object);

        return new UpdateApplicationFromForesterLayersService(
            _repository.Object,
            _foresterServices.Object,
            options,
            new NullLogger<UpdateApplicationFromForesterLayersService>());
    }
}