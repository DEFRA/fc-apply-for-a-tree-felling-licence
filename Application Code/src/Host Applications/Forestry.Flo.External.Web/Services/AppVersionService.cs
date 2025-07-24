using System.Reflection;

namespace Forestry.Flo.External.Web.Services
{
    public class AppVersionService
    {
        public string Version => Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
    }
}
