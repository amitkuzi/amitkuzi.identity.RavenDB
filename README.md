# amitkuzi.identity.RavenDB
this will help replace EF with RavenDb as Microsoft Identity UserStore And RoleStore


just add and replace code : 

            /* 
            //comment out mssql database initialization
                services.AddDbContext<ApplicationDbContext>(options =>
                 options.UseSqlServer(
                     Configuration.GetConnectionString("DefaultConnection")));
             services.AddDatabaseDeveloperPageExceptionFilter();*/


            var RavenOptions = services.EnsureOptions<RavenStoreOptions>(); // add raven stor option to appsetting
            var documentStoreFactory = services.InitiateRavenDb(RavenOptions); // use options to init raven and creat a document store factory (save for later user )



            // comment out Identity initialization 
            /* services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
                 .AddEntityFrameworkStores<ApplicationDbContext>();*/

            services.AddIdentity<IdentityUser, IdentityRole>(). // init MS identity (with your preferd user and role types )
                AddDefaultTokenProviders(). 
                AddDefaultUI().
                AddRavenStores(documentStoreFactory). // add raven store to identity , inject document stor factory for any of ravendb varient (5.0 > )
                    EnsureUserRols(new IdentityUser("basic user da mast have "), // add your bootstapuser and his roles
                        new IdentityRole("first role user must have"), 
                        new IdentityRole("2nd role user must have"));

            services.AddControllersWithViews();



and you will have identity running on RaveDb.Embadded in no time (dont forget to install and config ravendb and its licence )
it will run only on NET5.0 and RavenDB 5XX.

looking for help and contributers.
