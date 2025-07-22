using Microsoft.AspNetCore.Authentication;

namespace Forestry.Flo.Tests.Common.Testing.AuthenticationHandlers
{
    public class HeaderAuthenticationOptions : AuthenticationSchemeOptions
    {
        public string Header { get; } = "Authorization";
        public string HeaderLocalUserId { get; } = "TestLocalUserId";
    }
}
