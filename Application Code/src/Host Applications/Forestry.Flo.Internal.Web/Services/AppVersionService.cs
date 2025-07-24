using System.Reflection;

namespace Forestry.Flo.Internal.Web.Services
{
    public class AppVersionService
    {
        public string Version => Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
    }
}
