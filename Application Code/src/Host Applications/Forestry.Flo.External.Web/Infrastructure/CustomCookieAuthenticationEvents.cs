using System.Security.Claims;
using Ardalis.GuardClauses;
using Forestry.Flo.External.Web.Services;
using Forestry.Flo.Services.Applicants.Configuration;
using Forestry.Flo.Services.Applicants.Repositories;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
using NodaTime;

namespace Forestry.Flo.External.Web.Infrastructure;

public class CustomCookieAuthenticationEvents: CookieAuthenticationEvents
{
    private readonly IUserAccountRepository _userRepository;
    private readonly IClock _clock;
    private readonly SecurityOptions _userSettings;
    private const string UserLastChecked = "user-last-checked";
    private readonly FcAgencyOptions _fcAgencyOptions;

    public CustomCookieAuthenticationEvents(
        IUserAccountRepository userRepository,
        IClock clock,
        IOptions<SecurityOptions> options,
        IOptions<FcAgencyOptions> fcAgencyOptions)
    {
        _userRepository = Guard.Against.Null(userRepository);
        _clock = Guard.Against.Null(clock);
        _userSettings = Guard.Against.Null(options).Value;
        _fcAgencyOptions = Guard.Against.Null(fcAgencyOptions.Value);
    }

    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        //We verify only Get requests and skip Post to avoid Anti forgery token validation issues
        if (context.Request.Method != HttpMethods.Get) return;

        var userPrincipal = context.Principal;
        if (userPrincipal is null) return;

        var user = new ExternalApplicant(userPrincipal);
        var lastChecked = GetLastCheckedTime(context);
        var checkTime = _clock.GetCurrentInstant().ToDateTimeUtc();
        if (lastChecked is not null && user.UserAccountId.HasValue
                                    && lastChecked.Value.AddMinutes(_userSettings.UserDbCheckIntervalMinutes) <
                                    checkTime)
        {
            // Look for the LastChanged claim.
            var lastChanged = user.LastChanged;
            var account = await _userRepository.GetAsync(user.UserAccountId.Value, CancellationToken.None);

            if (account.IsSuccess && account.Value.LastChanged > lastChanged.GetValueOrDefault())
            {
                List<ClaimsIdentity> identities = new()
                {
                    ClaimsIdentityHelper.CreateClaimsIdentityFromApplicantUserAccount(account.Value, _fcAgencyOptions.PermittedEmailDomainsForFcAgent)
                };
                identities.AddRange(user.Principal.Identities.Where(x =>
                    x.AuthenticationType != FloClaimTypes.ClaimsIdentityAuthenticationType));

                user = new ExternalApplicant(new ClaimsPrincipal(identities));
                context.ReplacePrincipal(user.Principal);
                context.ShouldRenew = true;
            }
            else if (account.IsFailure)
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme);
                context.HttpContext.Response.Headers.Add("Clear-Site-Data", "\"cookies\", \"storage\", \"cache\"");
                return;
            }
        }
        UpdateLastCheckTime(context, checkTime);
    }

    private static DateTime? GetLastCheckedTime(CookieValidatePrincipalContext context)
    {
        var lastCheckedString = context.HttpContext.Session.GetString(UserLastChecked);
        if (string.IsNullOrEmpty(lastCheckedString)) return null;
        return DateTime.TryParse(lastCheckedString, out var lastChecked) ? lastChecked.ToUniversalTime() : null;
    }

    private static void UpdateLastCheckTime(CookieValidatePrincipalContext context, DateTime checkTime) =>
        context.HttpContext.Session.SetString(UserLastChecked,checkTime.ToString("o"));
}
