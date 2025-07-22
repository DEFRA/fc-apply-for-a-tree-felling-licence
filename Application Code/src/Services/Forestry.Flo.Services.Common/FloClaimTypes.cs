namespace Forestry.Flo.Services.Common;

public static class FloClaimTypes
{
    internal const string ClaimTypeNamespace = "https://www.gov.uk/government/organisations/forestry-commission";

    public const string ClaimsIdentityAuthenticationType = "flo";
    public const string InternalUserLocalSourceClaimLabel = "InternalUserLocalSource";
    public const string LocalAccountId = ClaimTypeNamespace + "/localaccountid";
    public const string AccountType = ClaimTypeNamespace + "/accounttype";
    public const string AccountTypeOther = ClaimTypeNamespace + "/accounttypeother";
    public const string UserName = ClaimTypeNamespace + "/username";
    public const string WoodlandOwnerId = ClaimTypeNamespace + "/woodlandownerid";
    public const string WoodlandOwnerName = ClaimTypeNamespace + "/woodlandownername";
    public const string AgencyId = ClaimTypeNamespace + "/agencyid";
    public const string AcceptedTermsAndConditions = ClaimTypeNamespace + "/acceptedtcs";
    public const string AcceptedPrivacyPolicy = ClaimTypeNamespace + "/acceptedprivacypolicy";
    public const string Email = "emails";
    public const string Invited = ClaimTypeNamespace + "/invited";
    public const string LastChanged = $"{ClaimTypeNamespace}/lastchanged";
    public const string LastChecked = $"{ClaimTypeNamespace}/lastchecked";
    public const string FcUser = $"{ClaimTypeNamespace}/fcUser";
    public const string AccountStatus = $"{ClaimTypeNamespace}/accountStatus";
    public const string UserCanApproveApplications = $"{ClaimTypeNamespace}/canapproveapplications";
}