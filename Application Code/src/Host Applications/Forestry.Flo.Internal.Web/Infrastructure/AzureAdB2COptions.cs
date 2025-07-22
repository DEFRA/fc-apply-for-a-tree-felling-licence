namespace Forestry.Flo.Internal.Web.Infrastructure;

public class AzureAdB2COptions
{
    public const string AzureAdB2C = "AzureAdB2C";

    public string Domain { get; set; } = null!;
    
    public string Instance { get; set; } = null!;

    public string ClientId { get; set; } = null!;

    public string ClientSecret { get; set; } = null!;
    
    public string SignedOutCallbackPath { get; set; } = null!;
    
    public string SignUpSignInPolicyId { get; set; } = null!;

    public string Authority => $"{Instance}/tfp/{Domain}/{SignUpSignInPolicyId}/v2.0";
}