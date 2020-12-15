using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace amitkuzi.Persistence.RavenEmbaded
{
    public static class GlobalUtils
    {
        public static IHostEnvironment Env(this IServiceCollection services) => services.BuildServiceProvider().Env();
        public static IConfiguration Config(this IServiceCollection services) => services.BuildServiceProvider().Config();

        public static IHostEnvironment Env(this IServiceProvider serviceProvider) => serviceProvider.GetService<IHostEnvironment>();
        public static IConfiguration Config(this IServiceProvider serviceProvider) => serviceProvider.GetService<IConfiguration>();


    }
}
