namespace Forestry.Flo.External.Web.Infrastructure;

public class AzureAdB2COptions
{
    public const string AzureAdB2C = "AzureAdB2C";

    public string Domain { get; set; } = null!;
    
    public string Instance { get; set; } = null!;

    public string ClientId { get; set; } = null!;

    public string ClientSecret { get; set; } = null!;
    
    public string SignedOutCallbackPath { get; set; } = null!;
    
    public string SignInPolicyId { get; set; } = null!;

    public string SignUpPolicyId { get; set; } = null!;

    public string SignInAuthority => $"{Instance}/tfp/{Domain}/{SignInPolicyId}/v2.0";

    public string SignUpAuthority => $"{Instance}/tfp/{Domain}/{SignUpPolicyId}/v2.0";
}