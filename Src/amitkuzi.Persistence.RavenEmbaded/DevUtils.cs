using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;

namespace amitkuzi.Persistence.RavenEmbaded
{
    internal static class DevUtils
    {



        internal static IServiceCollection DevEnsureConfigItem(this IServiceCollection services, string section, object value)
        {
            if (services.Env().IsDevelopment())
            {
                return services.DevelopmentEnsureConfigItem(section, value);
            }
            return services.ProductionEnsureConfigItem(section, value);
        }

        internal static IServiceCollection DevelopmentEnsureConfigItem(this IServiceCollection services, string section, object value)
        {
            ;
            var appsettingsPath = services.Env().ContentRootFileProvider.GetFileInfo("appsettings.json").PhysicalPath;
            string strOption = JsonConvert.SerializeObject(value, new JsonSerializerSettings { Formatting = Formatting.Indented });
            string appsettingsContent;
            using (var f = File.OpenText(appsettingsPath))
            {
                var doc = JsonConvert.DeserializeObject<JObject>(f.ReadToEnd());
                if (doc[section] == null)
                {
                    doc.Add(section, JToken.Parse(strOption));
                }
                else
                {
                    doc[section] = JToken.Parse(strOption);
                }
                appsettingsContent = JsonConvert.SerializeObject(doc, new JsonSerializerSettings { Formatting = Formatting.Indented });
            }

            using (var f = new StreamWriter(appsettingsPath))
            {
                f.Write(appsettingsContent);
            }

            return services.ProductionEnsureConfigItem(section, value);
        }

        internal static IServiceCollection ProductionEnsureConfigItem(this IServiceCollection services, string section, object value)
        {
            services.Config().GetSection(section).Value = JsonConvert.SerializeObject(value, new JsonSerializerSettings { Formatting = Formatting.Indented });
            return services;
        }
    }
}
