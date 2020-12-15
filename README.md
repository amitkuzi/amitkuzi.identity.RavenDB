# amitkuzi.identity.RavenDB
this will help replace EF with RavenDb as Microsoft Identity UserStore And RoleStore


just add and replace code : 

 							/* services.AddDbContext<ApplicationDbContext>(options =>
                 options.UseSqlServer(
                     Configuration.GetConnectionString("DefaultConnection")));
             services.AddDatabaseDeveloperPageExceptionFilter();*/

            var RavenOptions = services.EnsureOptions<RavenStoreOptions>();
            var documentStoreFactory = services.InitiateRavenDb(RavenOptions);


            /* services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
                 .AddEntityFrameworkStores<ApplicationDbContext>();*/

            services.AddIdentity<IdentityUser, IdentityRole>().
                AddDefaultTokenProviders().
                AddDefaultUI().
                AddRavenStores(documentStoreFactory).
                    EnsureUserRols(new IdentityUser("basic user da mast have "),
                        new IdentityRole("first role user must have"), 
                        new IdentityRole("2nd role user must have"));



and you will have identity running on RaveDb.Embadded in no time (dont forget to install and config ravendb and its licence )
it will run only on NET5.0 .

looking for help and contributers.
