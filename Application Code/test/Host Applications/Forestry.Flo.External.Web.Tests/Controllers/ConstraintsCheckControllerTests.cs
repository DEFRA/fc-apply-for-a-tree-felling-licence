using AutoFixture;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Entities.WoodlandOwner;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.Gis.Models.Esri.Configuration;
using Forestry.Flo.Services.Gis.Models.Esri.Responses;
using Forestry.Flo.Services.Gis.Models.Internal;
using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;
using Forestry.Flo.Services.PropertyProfiles.Entities;
using Forestry.Flo.Tests.Common;
using Forestry.Flo.Tests.Common.Testing;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Headers;
using ExternalUserAccount = Forestry.Flo.Services.Applicants.Entities.UserAccount.UserAccount;

namespace Forestry.Flo.External.Web.Tests.Controllers;

public class ConstraintsCheckControllerTests : IClassFixture<ExternalWebApplicationFactory<Startup>>
{
    private readonly ExternalWebApplicationFactory<Startup> _factory;
    private readonly IOptions<LandInformationSearchOptions> _lisOptions;
  
    public ConstraintsCheckControllerTests(ExternalWebApplicationFactory<Startup> factory)
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

        _factory.FixtureInstance.Customizations.Add(new CompartmentEntitySpecimenBuilder());
        _factory.FixtureInstance.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _factory.FixtureInstance.Behaviors.Remove(b));
        _factory.FixtureInstance.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Theory, AutoMoqData]
    public async Task WhenValidRequest_ReturnsLISDeepLinkAsRedirect(FellingLicenceApplication application, IList<ActivityFeedItemModel> activityFeedItems, PropertyProfile propertyProfile)
    {
        // Arrange
        EnsurePreSubmittedCompartmentsInApplication(application);

        //todo remove all the mocks regarding data retrieval, and replace with actual data setup in the in-memory databases in the test webapp factory.
    
        _factory.FellingLicenceApplicationRepositoryMock.Setup(r => r.GetAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);

        _factory.FellingLicenceApplicationInternalRepositoryMock.Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);

        var woodlandOwner = _factory.FixtureInstance.Create<WoodlandOwner>();
        _factory.WoodlandOwnerRepositoryMock.Setup(w => w.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<WoodlandOwner, UserDbErrorReason>(woodlandOwner));

        _factory.FellingLicenceApplicationInternalRepositoryMock.Setup(r => r.ListByIncludedStatus(It.IsAny<bool>(),
            It.IsAny<Guid>(), It.IsAny<List<FellingLicenceStatus>>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(new List<FellingLicenceApplication> { application });

        _factory.FellingLicenceApplicationRepositoryMock.Setup(r => r.GetAsync(application.Id,
            It.IsAny<CancellationToken>())).ReturnsAsync(application);

        _factory.ActivityFeedServiceMock.Setup(r => r.RetrieveActivityFeedItemsAsync(
            It.IsAny<ActivityFeedItemProviderModel>(),
            It.IsAny<ActorType>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(activityFeedItems));

        _factory.PropertyProfileRepositoryMock.Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success<PropertyProfile, UserDbErrorReason>(propertyProfile));


        var expectedQueryParams = QueryHelpers.ParseQuery($"isFlo=true&config={_lisOptions.Value.LisConfig}&caseId={application.Id}");

        var testUser = new UserAccount
        {
            AccountType = AccountTypeExternal.WoodlandOwner,
            IdentityProviderId = Guid.NewGuid().ToString(),
            Title = "Mr",
            FirstName = "First",
            LastName = "Last",
            Email = "test@email.com",
            DateAcceptedPrivacyPolicy = DateTime.Now.AddDays(-1),
            DateAcceptedTermsAndConditions = DateTime.Now.AddDays(-1),
            PreferredContactMethod = PreferredContactMethod.Email,
            Status = UserAccountStatus.Active,
            WoodlandOwner = woodlandOwner,
            WoodlandOwnerId = woodlandOwner.Id
        };

        await _factory.AddApplicantUserAccountToDbAsync(testUser, new CancellationToken());
        
        //send initial request to obtain auth cookie and also anti-forgery verification form field value later.
        var initialResponse = await InitialRequest(testUser); 
        var client = CreateClient(testUser);

        var request = await CreateRequest(initialResponse,  
            new Dictionary<string, string> {{"applicationId", application.Id.ToString()}},
            new Uri("/FellingLicenceApplication/RunConstraintsCheck", UriKind.Relative), 
            HttpMethod.Post);

        // Act
        var response = await client.SendAsync(request);

        //Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(_lisOptions.Value.DeepLinkUrlAndPath, response.Headers.Location?.ToString().Split("?")[0]);
        var actualQueryParams = QueryHelpers.ParseQuery(response.Headers.Location?.Query);
        Assert.True(actualQueryParams.All(e => expectedQueryParams.Contains(e)));
    }

    private async Task<HttpResponseMessage> InitialRequest(ExternalUserAccount userAccount)
    {
        //This is the profile page (arriving as an already authenticated user) has an html form so has __RequestVerificationToken
        var request = new HttpRequestMessage { Method = HttpMethod.Get, 
            RequestUri = new Uri("Account/RegisterPersonName", UriKind.Relative) }; 
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

    private void EnsurePreSubmittedCompartmentsInApplication(FellingLicenceApplication application)
    {
        var propertyProfile = _factory.FixtureInstance.Create<PropertyProfile>();
        propertyProfile.Compartments.Clear();

        //ensure enough property compartments exist to match the count of ProposedFellingDetails by AutoFixture FLA 
        var propertyComps = _factory.FixtureInstance.CreateMany<Compartment>(application.LinkedPropertyProfile!.ProposedFellingDetails!.Count).ToList();
        propertyProfile.Compartments.AddRange(propertyComps);

        //force the pfd compartment Ids to match the actual compartmentIds
        const int i = 0;
        foreach (var pfd in application.LinkedPropertyProfile.ProposedFellingDetails)
        {
            pfd.PropertyProfileCompartmentId = propertyComps.ElementAt(i).Id;
        }

        _factory.PropertyProfileRepositoryMock.Setup(x => x.GetByIdAsync(application.LinkedPropertyProfile.PropertyProfileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<PropertyProfile, UserDbErrorReason>(propertyProfile));
    }
}