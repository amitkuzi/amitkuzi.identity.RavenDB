using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Raven.Embedded;
using System.IO;

namespace amitkuzi.Persistence.RavenEmbaded
{

    public static class RavenOptionsExtention
    {
        public static TOption EnsureOptions<TOption>(this IServiceCollection services)
            where TOption : RavenStoreOptions, new()
        {
            var option = new TOption();
            services.DevEnsureConfigItem("RavenServerOptions", option);
            return option;
        }
    }
    public class RavenStoreOptions : ServerOptions
    {
        public RavenStoreOptions()
        {
            DataDirectory = "RavenDB_Data";
            ServerUrl = "http://localhost:8080";
        }

        public static string Name => "RavenStoreOptions";
        public string IdentityDatabaseName { get; set; } = "Identity";
        public bool OpenDbStudio { get; set; } = true;
    }
}