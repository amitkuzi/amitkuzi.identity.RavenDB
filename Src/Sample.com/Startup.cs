using amitkuzi.Identity.Raven;
using amitkuzi.Persistence.RavenEmbaded;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Sample.com
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            /* 
            //remove mssql database initialization
                services.AddDbContext<ApplicationDbContext>(options =>
                 options.UseSqlServer(
                     Configuration.GetConnectionString("DefaultConnection")));
             services.AddDatabaseDeveloperPageExceptionFilter();*/


            var RavenOptions = services.EnsureOptions<RavenStoreOptions>(); // add raven stor option to appsetting
            var documentStoreFactory = services.InitiateRavenDb(RavenOptions); // use options to init raven and creat a document store factory (save for later user )



            // comment out Identiy init 
            /* services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
                 .AddEntityFrameworkStores<ApplicationDbContext>();*/

            services.AddIdentity<IdentityUser, IdentityRole>(). // init ms identity (with your preferd user and role types 
                AddDefaultTokenProviders(). 
                AddDefaultUI().
                AddRavenStores(documentStoreFactory). // add raven store to identity , inject document stor factory for any of ravendb varient (5.0 > )
                    EnsureUserRols(new IdentityUser("basic user da mast have "), // add your bootstapuser and his roles
                        new IdentityRole("first role user must have"), 
                        new IdentityRole("2nd role user must have"));

            services.AddControllersWithViews();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapRazorPages();
            });
        }
    }
}
