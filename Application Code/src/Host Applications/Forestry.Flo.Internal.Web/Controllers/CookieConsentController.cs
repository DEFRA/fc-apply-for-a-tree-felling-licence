using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace Forestry.Flo.Internal.Web.Controllers;

[Route("cookie-consent")]
public class CookieConsentController : Controller
{
 [HttpPost("accept")]
 [ValidateAntiForgeryToken]
 public IActionResult Accept(string? returnUrl = null)
 {
 var consent = HttpContext.Features.Get<ITrackingConsentFeature>();
 if (consent != null)
 {
 consent.GrantConsent();
 var cookie = consent.CreateConsentCookie();
 if (!string.IsNullOrEmpty(cookie))
 {
 Response.Headers.Append(HeaderNames.SetCookie, cookie);
 }
 else
 {
 Response.Cookies.Append(".AspNet.Consent", "yes", BuildCookieOptions());
 }
 }
 else
 {
 Response.Cookies.Append(".AspNet.Consent", "yes", BuildCookieOptions());
 }

 TempData["CookieConsentStatus"] = "accepted";
 return RedirectToLocal(returnUrl);
 }

 [HttpPost("reject")]
 [ValidateAntiForgeryToken]
 public IActionResult Reject(string? returnUrl = null)
 {
 var consent = HttpContext.Features.Get<ITrackingConsentFeature>();
 if (consent != null)
 {
 consent.WithdrawConsent();
 }

 // Persist explicit rejection so banner is hidden.
 Response.Cookies.Append(".AspNet.Consent", "no", BuildCookieOptions());

 TempData["CookieConsentStatus"] = "rejected";
 return RedirectToLocal(returnUrl);
 }

 [HttpPost("set")]
 [ValidateAntiForgeryToken]
 public IActionResult Set([FromForm(Name = "functional-cookies")] string? value, string? returnUrl = null)
 {
 if (string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase))
 {
 var consent = HttpContext.Features.Get<ITrackingConsentFeature>();
 if (consent != null)
 {
 consent.GrantConsent();
 var cookie = consent.CreateConsentCookie();
 if (!string.IsNullOrEmpty(cookie))
 {
 Response.Headers.Append(HeaderNames.SetCookie, cookie);
 }
 else
 {
 Response.Cookies.Append(".AspNet.Consent", "yes", BuildCookieOptions());
 }
 }
 else
 {
 Response.Cookies.Append(".AspNet.Consent", "yes", BuildCookieOptions());
 }
 TempData["CookieConsentStatus"] = "accepted";
 }
 else if (string.Equals(value, "no", StringComparison.OrdinalIgnoreCase))
 {
 var consent = HttpContext.Features.Get<ITrackingConsentFeature>();
 if (consent != null)
 {
 consent.WithdrawConsent();
 }
 Response.Cookies.Append(".AspNet.Consent", "no", BuildCookieOptions());
 TempData["CookieConsentStatus"] = "rejected";
 }
 // if value is null/empty, don't change anything

 return RedirectToLocal(returnUrl ?? Url.Action("Cookies", "Home"));
 }

 private static CookieOptions BuildCookieOptions() => new CookieOptions
 {
 Expires = DateTimeOffset.UtcNow.AddYears(1),
 IsEssential = true,
 SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax,
 Secure = true,
 HttpOnly = false,
 Path = "/"
 };

 private IActionResult RedirectToLocal(string? returnUrl)
 {
 if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
 {
 return Redirect(returnUrl);
 }
 return RedirectToAction("Index", "Home");
 }
}
