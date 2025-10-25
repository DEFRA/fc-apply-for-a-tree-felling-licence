using System.Net.Http.Headers;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.Gis.Models.Esri.Configuration;
using Forestry.Flo.Services.Gis.Models.Esri.Responses;
using Forestry.Flo.Services.Gis.Models.Internal;
using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;
using Forestry.Flo.Services.InternalUsers.Entities.UserAccount;
using Forestry.Flo.Tests.Common.Testing;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;

namespace Forestry.Flo.Internal.Web.Tests.Controllers;

public class ConstraintsCheckControllerTests : IClassFixture<InternalWebApplicationFactory<Startup>>
{
    private readonly InternalWebApplicationFactory<Startup> _factory;
    private readonly IOptions<LandInformationSearchOptions> _lisOptions;
  
    public ConstraintsCheckControllerTests(InternalWebApplicationFactory<Startup> factory)
    {
        _factory = factory;
        //todo remove all the mocks regarding data retrieval, and replace with actual data setup in the in-memory databases in the test webapp factory.
        _factory.FellingLicenceApplicationInternalRepositoryMock.Reset();
        _factory.UnitOfWorkMock = new Mock<IUnitOfWork>();
        _factory.FellingLicenceApplicationRepositoryMock.SetupGet(r => r.UnitOfWork).Returns(_factory.UnitOfWorkMock.Object);
        _factory.UnitOfWorkMock.Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(UnitResult.Success<UserDbErrorReason>());
        _factory.LandInformationSearchMock.Setup(x => x.AddFellingLicenceGeometriesAsync(It.IsAny<Guid>(),
                It.IsAny<List<InternalCompartmentDetails<Polygon>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Result<CreateUpdateDeleteResponse<int>>());
        _lisOptions = _factory.Services.GetRequiredService<IOptions<LandInformationSearchOptions>>();
    }

    private async Task<HttpResponseMessage> InitialRequest(UserAccount userAccount, Guid applicationId)
    {
        var request = new HttpRequestMessage { Method = HttpMethod.Get, 
            RequestUri = new Uri($"/GetAdminOfficerReviewAsync/Index/{applicationId}", 
                UriKind.Relative) };

        using var client = CreateClient(userAccount);

        return await client.SendAsync(request);
    }

    private static async Task<HttpRequestMessage> CreateRequest(
        HttpResponseMessage initialResponse, 
        Dictionary<string,string> formValuesDictionary, 
        Uri requestUri, 
        HttpMethod httpMethod)
    {
        var antiForgeryToken = await HtmlHelper.ExtractAntiForgeryTokenAsync(initialResponse);

        var request = new HttpRequestMessage
        {
            RequestUri = requestUri,
            Method = httpMethod
        };

        var antiForgeryCookie = CookieHelper.GetCookieFromResponse(".AspNetCore.AntiForgery.", initialResponse);

        HtmlHelper.ApplyForm(request, formValuesDictionary, antiForgeryToken, antiForgeryCookie);

        return request;
    }

    private HttpClient CreateClient(UserAccount userAccount)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(userAccount.IdentityProviderId!);
        client.DefaultRequestHeaders.Add("TestLocalUserId",userAccount.Id.ToString());

        return client;
    }

      private void EnsureSubmittedCompartments(Flo.Services.FellingLicenceApplications.Entities.FellingLicenceApplication application)
        {
          //arrange
            foreach (var submittedFlaPropertyCompartment in application.SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!)
            {
                submittedFlaPropertyCompartment.GISData =
                    "{\"spatialReference\":{\"latestWkid\":27700,\"wkid\":27700},\"rings\":[[[195289.4412971726,63418.84690534063],[195308.55055424382,63380.62839119805],[195320.49383991337,63351.96450559112],[195334.82578271683,63337.63256278765],[195384.98758252896,63306.58002004681],[195444.70401087674,63292.24807724334],[195473.3678964837,63280.304791573784],[195518.752382028,63261.1955345025],[195540.2502962332,63258.80687736859],[195499.6431249567,63237.308963163385],[195473.3678964837,63232.531648895565],[195449.4813251446,63220.58836322601],[195427.9834109394,63206.25642042254],[195427.9834109394,63191.924477619075],[195427.9834109394,63172.81522054779],[195423.20609667158,63151.317306342586],[195418.42878240376,63141.76267780694],[195408.87415386812,63136.98536353912],[195399.31952533248,63132.2080492713],[195382.5989253951,63136.98536353912],[195353.93503978816,63146.539992074766],[195339.6030969847,63151.317306342586],[195332.43712558298,63156.094620610405],[195325.27115418125,63160.871934878225],[195322.88249704734,63163.260592012135],[195313.3278685117,63172.81522054778],[195303.77323997606,63187.14716335125],[195267.94338296738,63218.19970609209],[195119.84664066488,63294.63673437725],[195045.79826951365,63318.523305716364],[194964.58392696068,63340.02121992157],[194890.53555580945,63356.741819858944],[194835.5964417295,63351.964505591124],[194771.1026991139,63316.134648582454],[194699.44298509657,63273.138820172055],[194630.17192821315,63218.1997060921],[194553.734899928,63189.535820485165],[194496.40712871414,63184.758506217346],[194470.13190024113,63211.03373469037],[194448.63398603594,63239.6976202973],[194443.85667176812,63246.86359169904],[194443.85667176812,63249.25224883295],[194431.91338609857,63280.30479157379],[194422.35875756294,63323.30061998419],[194415.1927861612,63344.798534189395],[194396.08352908993,63371.07376266242],[194393.69487195602,63373.46241979633],[194365.03098634907,63409.292276805],[194341.14441500997,63435.56750527802],[194322.0351579387,63445.12213381367],[194295.75992946568,63459.454076617134],[194262.31872959092,63466.62004801887],[194216.9342440466,63471.39736228669],[194171.54975850228,63485.72930509016],[194140.49721576142,63516.781847831],[194121.38795869015,63540.66841917011],[194121.38795869015,63586.05290471442],[194142.88587289533,63626.66007599091],[194216.93424404657,63660.101275865665],[194307.7032151352,63664.878590133485],[194381.75158628644,63669.655904401305],[194477.2978716429,63652.93530446393],[194587.1760998028,63638.60336166046],[194728.10687070357,63617.10544745526],[194837.9850988635,63595.607533250055],[194952.64064129122,63581.27559044659],[195062.51886945113,63574.10961904485],[195155.67649767367,63569.33230477703],[195172.39709761104,63564.55499050921],[195215.39292602145,63552.611704839655],[195227.336211691,63552.611704839655],[195239.27949736055,63540.6684191701],[195256.00009729792,63528.72513350054],[195275.1093543692,63497.6725907597],[195279.88666863702,63459.45407661712],[195279.88666863702,63447.51079094756],[195289.4412971726,63418.84690534063]]]}";
            }

        }
}