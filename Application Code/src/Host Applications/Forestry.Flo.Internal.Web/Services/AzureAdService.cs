using System.Net.Http.Headers;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;

namespace Forestry.Flo.Internal.Web.Services;

public class AzureAdService : IAzureAdService
{
    private readonly AzureAdServiceConfiguration _azureAdServiceConfiguration;

    public AzureAdService(IOptions<AzureAdServiceConfiguration> azureAdServiceConfigurationOptions)
    {
        _azureAdServiceConfiguration = azureAdServiceConfigurationOptions.Value;
    }

    /// <summary>
    /// Compare user email address against Mail property of Azure AD users to determine if user
    /// is member of AD directory
    /// </summary>
    /// <param name="emailAddress">The email address.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns></returns>
    public async Task<bool> UserIsInDirectoryAsync(
        string emailAddress, 
        CancellationToken cancellationToken = default)
    {
        // Mechanism for querying Azure AD graph was adapted from samples. Ref:

        // https://stackoverflow.com/questions/56152979/using-authprovider-with-ms-sdk-for-graph-calls-in-c-sharp
        // https://github.com/Azure-Samples/active-directory-dotnetcore-daemon-v2

        var confidentialClientApplication = ConfidentialClientApplicationBuilder
            .Create(_azureAdServiceConfiguration.ClientId)
            .WithClientSecret(_azureAdServiceConfiguration.ClientSecret)
            .WithAuthority(new Uri(_azureAdServiceConfiguration.Authority))
            .Build();

        confidentialClientApplication.AddInMemoryTokenCache();

        // With client credentials flows the scopes is ALWAYS of the shape "resource/.default", as the 
        // application permissions need to be set statically (in the portal or by PowerShell), and then granted by
        // a tenant administrator. 

        // Generates a scope -> "https://graph.microsoft.com/.default"

        string[] scopes = {

            $"{_azureAdServiceConfiguration.ApiUrl}.default"
        }; 

        // Call MS graph using the Graph SDK

        var users = await ListUsersMsGraphAsync(confidentialClientApplication, scopes, cancellationToken);

        bool userIsInDirectory = users.Any(x => x.Mail == emailAddress);

        return userIsInDirectory;
    }

    /// <summary>
    /// Query all users using the MS Graph SDK
    /// </summary>
    private static async Task<IList<User>> ListUsersMsGraphAsync(
        IConfidentialClientApplication app, 
        string[] scopes, 
        CancellationToken cancellationToken)
    {
        var graphServiceClient = GetAuthenticatedGraphClient(app, scopes);

        var allUsers = new List<User>();

        IGraphServiceUsersCollectionPage users = await graphServiceClient.Users.Request().GetAsync(cancellationToken);

        allUsers.AddRange(users);

        return allUsers;
    }

    /// <summary>
    /// Authenticate the Microsoft Graph SDK using the MSAL library
    /// </summary>
    private static GraphServiceClient GetAuthenticatedGraphClient(IConfidentialClientApplication app, string[] scopes)
    {
        var graphServiceClient = new GraphServiceClient(
            "https://graph.microsoft.com/V1.0/",
            new DelegateAuthenticationProvider(async requestMessage =>
            {
                // Retrieve an access token for Microsoft Graph (gets a fresh token if needed).
                AuthenticationResult result = await app.AcquireTokenForClient(scopes).ExecuteAsync();

                // Add the access token in the Authorization header of the API request.
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
            }));

        return graphServiceClient;
    }
}