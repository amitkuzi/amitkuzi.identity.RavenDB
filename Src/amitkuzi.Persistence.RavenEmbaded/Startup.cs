using Microsoft.Extensions.DependencyInjection;
using Raven.Embedded;
using Microsoft.Extensions.Hosting;
using System.IO;
using System;
using Raven.Client.Documents;

namespace amitkuzi.Persistence.RavenEmbaded
{
    //IIdentityPercistenceManager
    public static partial class Startup
    {

        public static Func<IDocumentStore> InitiateRavenDb(this IServiceCollection services, ServerOptions options)
        {

            IHostEnvironment hostEnvironment = services.Env();
            var dataPath = Path.Combine(hostEnvironment.ContentRootPath, options.DataDirectory);

            if (!Directory.Exists(dataPath))
            {
                Directory.CreateDirectory(dataPath);
            }
            EmbeddedServer.Instance.StartServer(options);
            string IdentityDatabaseName = "identity";
            if(options is RavenStoreOptions   ravenStoreOptions)
            {
                IdentityDatabaseName = ravenStoreOptions.IdentityDatabaseName;

                if (hostEnvironment.IsDevelopment() && ravenStoreOptions.OpenDbStudio)
                {
                    EmbeddedServer.Instance.OpenStudioInBrowser();
                }
            }
            return (() => EmbeddedServer.Instance.GetDocumentStore(IdentityDatabaseName));

        }


    }
}
