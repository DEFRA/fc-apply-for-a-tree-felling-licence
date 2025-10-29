using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.Gis.Interfaces;
using Forestry.Flo.Services.Gis.Models.Esri.Configuration;
using Forestry.Flo.Services.Gis.Models.Esri.Responses;
using Forestry.Flo.Services.Gis.Models.Internal;
using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;
using Forestry.Flo.Services.PropertyProfiles.Entities;
using Forestry.Flo.Services.PropertyProfiles.Repositories;
using Forestry.Flo.Tests.Common;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services
{
    public class ConstraintCheckerServiceTests
    {
        private static readonly Fixture FixtureInstance = new();
      

        private readonly Mock<IFellingLicenceApplicationInternalRepository> _fellingLicenceApplicationRepository;
        private readonly Mock<IPropertyProfileRepository> _propertyProfileRepository;
        private readonly Mock<IAuditService<ConstraintCheckerService>> _mockConstraintCheckerServiceAudit;
        private readonly LandInformationSearchOptions _settings = new() { DeepLinkUrlAndPath = "http://www.tempuri.org/abc", LisConfig = "lisConfigValue"};
        private readonly Mock<ILandInformationSearch> _landInformationSearchService;
        private readonly ClaimsPrincipal _testInternalUser;
        private readonly ClaimsPrincipal _testExternalUser;
        private readonly JsonSerializerOptions _options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        private readonly CreateUpdateDeleteResponse<int>? _expectedEsriSuccessObject;

        private readonly Result<CreateUpdateDeleteResponse<int>?> _expectedEsriSuccessResponseResult;

        public ConstraintCheckerServiceTests()
        {
            _testInternalUser = UserFactory.CreateInternalUserIdentityProviderClaimsPrincipal(localAccountId:Guid.NewGuid());
            _testExternalUser = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(localAccountId:Guid.NewGuid());
            FixtureInstance.Customizations.Add(new CompartmentEntitySpecimenBuilder());
            FixtureInstance.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
                .ForEach(b => FixtureInstance.Behaviors.Remove(b));
            FixtureInstance.Behaviors.Add(new OmitOnRecursionBehavior());

            _landInformationSearchService = new Mock<ILandInformationSearch>();

            _mockConstraintCheckerServiceAudit = new Mock<IAuditService<ConstraintCheckerService>>();
            _fellingLicenceApplicationRepository = new Mock<IFellingLicenceApplicationInternalRepository>();
            _propertyProfileRepository = new Mock<IPropertyProfileRepository>();
            var unitOfWOrkMock = new Mock<IUnitOfWork>();
            _fellingLicenceApplicationRepository.SetupGet(r => r.UnitOfWork).Returns(unitOfWOrkMock.Object);

            var goodResponseContent=  "{\"AddResults\": [ {\"GlobalId\": \"00000000-0000-0000-0000-000000000000\",\"ObjectId\": 21,\"ErrorDetails\": null,\"WasSuccessful\": true}], \"DeleteResults\": null, \"UpdateResults\": null}";
            _expectedEsriSuccessObject = JsonSerializer.Deserialize<CreateUpdateDeleteResponse<int>?>(goodResponseContent);
            _expectedEsriSuccessResponseResult = Result.Success(_expectedEsriSuccessObject);
        }
        
        [Theory, AutoMoqData]
        public async Task WhenCalledByInternalUser(FellingLicenceApplication application)
        {
            //arrange
            EnsureSubmittedCompartments(application);
            var expectedQueryParams = QueryHelpers.ParseQuery($"isFlo=true&lisconfig={_settings.LisConfig}&caseId={application.Id}");
            
            _fellingLicenceApplicationRepository.Setup(x => x.GetAsync(application.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync( Maybe.From(application));

            _landInformationSearchService.Setup(x => x.AddFellingLicenceGeometriesAsync(application.Id,
                    It.IsAny<List<InternalCompartmentDetails<Polygon>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(_expectedEsriSuccessResponseResult!);

            var sut = CreateSut(_testInternalUser);
            var request = ConstraintCheckRequest.Create(_testInternalUser, application.Id);

            //act
            var result = await sut.ExecuteAsync(request, new CancellationToken());

            //assert
            Assert.True(result.IsSuccess);

            var actualQueryParams = QueryHelpers.ParseQuery(result.Value.Query);

            Assert.True(actualQueryParams.All(e => expectedQueryParams.Contains(e)));
            Assert.Equal(_settings.DeepLinkUrlAndPath, result.Value.AbsoluteUri.Split('?')[0]);

            _mockConstraintCheckerServiceAudit.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(y =>
                y.EventName == AuditEvents.ConstraintCheckerExecutionSuccess
                && y.SourceEntityId == application.Id
                && y.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && y.ActorType == ActorType.InternalUser
                && JsonSerializer.Serialize(y.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    url = result.Value,
                    esriFeatureServiceResponse = _expectedEsriSuccessObject
                }, _options)
            ), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory, AutoMoqData]
        public async Task WhenCalledByInternalUserButFailsToClearExistingData(FellingLicenceApplication application, string error)
        {
            //arrange
            EnsureSubmittedCompartments(application);

            _fellingLicenceApplicationRepository.Setup(x => x.GetAsync(application.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Maybe.From(application));

            _landInformationSearchService
                .Setup(x => x.ClearLayerAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Failure(error));
                

            var sut = CreateSut(_testInternalUser);
            var request = ConstraintCheckRequest.Create(_testInternalUser, application.Id);

            //act
            var result = await sut.ExecuteAsync(request, new CancellationToken());

            //assert
            Assert.True(result.IsFailure);

            var expectedError = $"Unable to proceed with LIS Constraint Check for application having id of [{application.Id}] " +
                    $"as could not clear the layer for the FLA having id of [{application.Id}]. Error is [{error}].";

            _fellingLicenceApplicationRepository.Verify(x => x.GetAsync(application.Id, It.IsAny<CancellationToken>()), Times.Once);
            _fellingLicenceApplicationRepository.VerifyNoOtherCalls();

            _propertyProfileRepository.VerifyNoOtherCalls();

            _landInformationSearchService.Verify(x => x.ClearLayerAsync(application.Id.ToString(), It.IsAny<CancellationToken>()), Times.Once);
            _landInformationSearchService.VerifyNoOtherCalls();

            _mockConstraintCheckerServiceAudit.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(y =>
                y.EventName == AuditEvents.ConstraintCheckerExecutionFailure
                && y.SourceEntityId == application.Id
                && y.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && y.ActorType == ActorType.InternalUser
                && JsonSerializer.Serialize(y.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    error = expectedError
                }, _options)
            ), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory, AutoMoqData]
        public async Task WhenCalledByExternalUser(FellingLicenceApplication application)
        {
            //arrange
            EnsurePreSubmittedCompartmentsInApplication(application);
            var expectedQueryParams = QueryHelpers.ParseQuery($"isFlo=true&config={_settings.LisConfig}&caseId={application.Id}");
            
            _fellingLicenceApplicationRepository.Setup(x => x.GetAsync(application.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(application));

            _landInformationSearchService.Setup(x => x.AddFellingLicenceGeometriesAsync(application.Id,
                    It.IsAny<List<InternalCompartmentDetails<Polygon>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(_expectedEsriSuccessResponseResult!);
            
            var sut = CreateSut(_testExternalUser);
            var request = ConstraintCheckRequest.Create(_testExternalUser, application.Id);

            //act
            var result = await sut.ExecuteAsync(request, new CancellationToken());

            //assert
            Assert.True(result.IsSuccess);

            var actualQueryParams = QueryHelpers.ParseQuery(result.Value.Query);

            Assert.True(actualQueryParams.All(e => expectedQueryParams.Contains(e)));
            Assert.Equal(_settings.DeepLinkUrlAndPath, result.Value.AbsoluteUri.Split('?')[0]);
            
            _mockConstraintCheckerServiceAudit.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(y =>
                y.EventName == AuditEvents.ConstraintCheckerExecutionSuccess
                && y.SourceEntityId == application.Id
                && y.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && y.ActorType == ActorType.ExternalApplicant
                && JsonSerializer.Serialize(y.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    url = result.Value,
                    esriFeatureServiceResponse = _expectedEsriSuccessObject
                }, _options)
            ), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory, AutoMoqData]
        public async Task WhenCalledByExternalUserButCannotLoadPropertyProfile(FellingLicenceApplication application)
        {
            //arrange
            EnsurePreSubmittedCompartmentsInApplication(application, p => p.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Failure<PropertyProfile, UserDbErrorReason>(UserDbErrorReason.General)));
            var expectedQueryParams = QueryHelpers.ParseQuery($"isFlo=true&config={_settings.LisConfig}&caseId={application.Id}");

            _fellingLicenceApplicationRepository.Setup(x => x.GetAsync(application.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe.From(application));

            var sut = CreateSut(_testExternalUser);
            var request = ConstraintCheckRequest.Create(_testExternalUser, application.Id);

            //act
            var result = await sut.ExecuteAsync(request, new CancellationToken());

            //assert
            Assert.True(result.IsFailure);

            var expectedError =
                $"Unable to proceed with LIS Constraint Check for application having id of [{application.Id}] " +
                $"as could not get related property details having profile id of [{application.LinkedPropertyProfile!.PropertyProfileId}] in order to retrieve compartments for the applicant's (non submitted) application." +
                $"Error reason is [{UserDbErrorReason.General.ToString()}]";

            _mockConstraintCheckerServiceAudit.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(y =>
                y.EventName == AuditEvents.ConstraintCheckerExecutionFailure
                && y.SourceEntityId == application.Id
                && y.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && y.ActorType == ActorType.ExternalApplicant
                && JsonSerializer.Serialize(y.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    error = expectedError
                }, _options)
            ), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory, AutoMoqData]
        public async Task WhenFellingLicenceNotFound(FellingLicenceApplication application)
        {
            //arrange
            _fellingLicenceApplicationRepository.Setup(x => x.GetAsync(application.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync( Maybe.None);

            var sut = CreateSut(_testInternalUser);
            var request = ConstraintCheckRequest.Create(_testInternalUser, application.Id);

            //act
            var result = await sut.ExecuteAsync(request, new CancellationToken());

            //assert
            Assert.True(result.IsFailure);

            _mockConstraintCheckerServiceAudit.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(y =>
                y.EventName == AuditEvents.ConstraintCheckerExecutionFailure
                && y.SourceEntityId == application.Id
                && y.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && y.ActorType == ActorType.InternalUser
                && JsonSerializer.Serialize(y.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    error =  $"An FLA was not found which had an application Id of [{request.ApplicationId}]. Unable to proceed with LIS Constraint Check."
                }, _options)
            ), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory, AutoMoqData]
        public async Task WhenLisServiceReturnsFailure(FellingLicenceApplication application)
        {
            //arrange
            const string esriError = "esri error";
            EnsureSubmittedCompartments(application);

            _fellingLicenceApplicationRepository.Setup(x => x.GetAsync(application.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync( Maybe.From(application));

            _landInformationSearchService.Setup(x => x.AddFellingLicenceGeometriesAsync(application.Id,
                    It.IsAny<List<InternalCompartmentDetails<Polygon>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync( Result.Failure<CreateUpdateDeleteResponse<int>>(esriError));

            var sut = CreateSut(_testInternalUser);
            var request = ConstraintCheckRequest.Create(_testInternalUser, application.Id);

            //act
            var result = await sut.ExecuteAsync(request, new CancellationToken());

            //assert
            Assert.True(result.IsFailure);

            _mockConstraintCheckerServiceAudit.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(y =>
                y.EventName == AuditEvents.ConstraintCheckerExecutionFailure
                && y.SourceEntityId == application.Id
                && y.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && y.ActorType == ActorType.InternalUser
                && JsonSerializer.Serialize(y.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    error =  $"Unable to proceed with LIS Constraint Check for application having id of [{request.ApplicationId}]" +
                             $" as could not successfully send case geometries to LIS, received error back is [{esriError}]."
                }, _options)
            ), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory, AutoMoqData]
        public async Task WhenLisServiceThrowsException(FellingLicenceApplication application)
        {
            //arrange
            EnsureSubmittedCompartments(application);

            _fellingLicenceApplicationRepository.Setup(x => x.GetAsync(application.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync( Maybe.From(application));

            _landInformationSearchService.Setup(x => x.AddFellingLicenceGeometriesAsync(application.Id,
                    It.IsAny<List<InternalCompartmentDetails<Polygon>>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ApplicationException("TestError"));

            var sut = CreateSut(_testInternalUser);
            var request = ConstraintCheckRequest.Create(_testInternalUser, application.Id);

            //act
            var result = await sut.ExecuteAsync(request, new CancellationToken());

            //assert
            Assert.True(result.IsFailure);

            _mockConstraintCheckerServiceAudit.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(y =>
                y.EventName == AuditEvents.ConstraintCheckerExecutionFailure
                && y.SourceEntityId == application.Id
                && y.SourceEntityType == SourceEntityType.FellingLicenceApplication
                && y.ActorType == ActorType.InternalUser
                && JsonSerializer.Serialize(y.AuditData, _options) ==
                JsonSerializer.Serialize(new
                {
                    error =  "TestError"
                }, _options)
            ), It.IsAny<CancellationToken>()), Times.Once);
        }

        private static void EnsureSubmittedCompartments(FellingLicenceApplication application)
        {
            foreach (var submittedFlaPropertyCompartment in application.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!)
            {
                submittedFlaPropertyCompartment.GISData =
                    "{\"spatialReference\":{\"latestWkid\":27700,\"wkid\":27700},\"rings\":[[[195289.4412971726,63418.84690534063],[195308.55055424382,63380.62839119805],[195320.49383991337,63351.96450559112],[195334.82578271683,63337.63256278765],[195384.98758252896,63306.58002004681],[195444.70401087674,63292.24807724334],[195473.3678964837,63280.304791573784],[195518.752382028,63261.1955345025],[195540.2502962332,63258.80687736859],[195499.6431249567,63237.308963163385],[195473.3678964837,63232.531648895565],[195449.4813251446,63220.58836322601],[195427.9834109394,63206.25642042254],[195427.9834109394,63191.924477619075],[195427.9834109394,63172.81522054779],[195423.20609667158,63151.317306342586],[195418.42878240376,63141.76267780694],[195408.87415386812,63136.98536353912],[195399.31952533248,63132.2080492713],[195382.5989253951,63136.98536353912],[195353.93503978816,63146.539992074766],[195339.6030969847,63151.317306342586],[195332.43712558298,63156.094620610405],[195325.27115418125,63160.871934878225],[195322.88249704734,63163.260592012135],[195313.3278685117,63172.81522054778],[195303.77323997606,63187.14716335125],[195267.94338296738,63218.19970609209],[195119.84664066488,63294.63673437725],[195045.79826951365,63318.523305716364],[194964.58392696068,63340.02121992157],[194890.53555580945,63356.741819858944],[194835.5964417295,63351.964505591124],[194771.1026991139,63316.134648582454],[194699.44298509657,63273.138820172055],[194630.17192821315,63218.1997060921],[194553.734899928,63189.535820485165],[194496.40712871414,63184.758506217346],[194470.13190024113,63211.03373469037],[194448.63398603594,63239.6976202973],[194443.85667176812,63246.86359169904],[194443.85667176812,63249.25224883295],[194431.91338609857,63280.30479157379],[194422.35875756294,63323.30061998419],[194415.1927861612,63344.798534189395],[194396.08352908993,63371.07376266242],[194393.69487195602,63373.46241979633],[194365.03098634907,63409.292276805],[194341.14441500997,63435.56750527802],[194322.0351579387,63445.12213381367],[194295.75992946568,63459.454076617134],[194262.31872959092,63466.62004801887],[194216.9342440466,63471.39736228669],[194171.54975850228,63485.72930509016],[194140.49721576142,63516.781847831],[194121.38795869015,63540.66841917011],[194121.38795869015,63586.05290471442],[194142.88587289533,63626.66007599091],[194216.93424404657,63660.101275865665],[194307.7032151352,63664.878590133485],[194381.75158628644,63669.655904401305],[194477.2978716429,63652.93530446393],[194587.1760998028,63638.60336166046],[194728.10687070357,63617.10544745526],[194837.9850988635,63595.607533250055],[194952.64064129122,63581.27559044659],[195062.51886945113,63574.10961904485],[195155.67649767367,63569.33230477703],[195172.39709761104,63564.55499050921],[195215.39292602145,63552.611704839655],[195227.336211691,63552.611704839655],[195239.27949736055,63540.6684191701],[195256.00009729792,63528.72513350054],[195275.1093543692,63497.6725907597],[195279.88666863702,63459.45407661712],[195279.88666863702,63447.51079094756],[195289.4412971726,63418.84690534063]]]}";
            }
        }

        private void EnsurePreSubmittedCompartmentsInApplication(FellingLicenceApplication application, Action<Mock<IPropertyProfileRepository>>? setupPropertyProfileRepo = null)
        {
            var propertyProfile = FixtureInstance.Create<PropertyProfile>();
            propertyProfile.Compartments.Clear();

            //ensure enough property compartments exist to match the count of ProposedFellingDetails by AutoFixture FLA 
            var propertyComps = FixtureInstance.CreateMany<Compartment>(application.LinkedPropertyProfile.ProposedFellingDetails.Count).ToList();
            propertyProfile.Compartments.AddRange(propertyComps);

            //force the pfd compartment Ids to match the actual compartmentIds
            const int i = 0;
            foreach (var pfd in application.LinkedPropertyProfile.ProposedFellingDetails)
            {
                pfd.PropertyProfileCompartmentId = propertyComps.ElementAt(i).Id;
            }

            if (setupPropertyProfileRepo == null)
            {
                _propertyProfileRepository.Setup(x => x.GetByIdAsync(application.LinkedPropertyProfile.PropertyProfileId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result.Success<PropertyProfile, UserDbErrorReason>(propertyProfile));
            }
            else
            {
                setupPropertyProfileRepo(_propertyProfileRepository);
            }

        }

        private ConstraintCheckerService CreateSut(ClaimsPrincipal userPrincipal)
        {
            return new ConstraintCheckerService(
                    _landInformationSearchService.Object,
                    Options.Create(_settings),
                    _fellingLicenceApplicationRepository.Object, 
                    _propertyProfileRepository.Object,
                    _mockConstraintCheckerServiceAudit.Object,
                    new RequestContext("test", new RequestUserModel(userPrincipal)),
                    new NullLogger<ConstraintCheckerService>());
        }
    }
}