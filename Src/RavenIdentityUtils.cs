using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace amitkuzi.Identity.RavenEmbaded
{
    //  IdentityBuilder,IdentityOptions
    public static class RavenIdentityUtils
    {
        static RavenIdentityUtils()
        {
        }

        public static IHostingEnvironment Env(this IServiceCollection services) => services.BuildServiceProvider().GetService<IHostingEnvironment>();

        public static RavenIdentityBuilder AddRavenEmbadedStores(this IdentityBuilder identityBuilder, Func<IDocumentStore> documentStoreFactory)
        {
            Debug.WriteLine("******** AddRavenEmbadedStores *****************");
            identityBuilder.Services.AddScoped(typeof(IRoleStore<>).MakeGenericType(identityBuilder.RoleType), typeof(RavenRoleStore<>).MakeGenericType(identityBuilder.RoleType));//identityBuilder.AddRoleStore<RavenRoleStore>()
            identityBuilder.Services.AddScoped(typeof(IUserStore<>).MakeGenericType(identityBuilder.UserType), typeof(RavenUserStore<>).MakeGenericType(identityBuilder.UserType));//.AddUserStore
            identityBuilder.Services.AddTransient(sp => documentStoreFactory());
            
            return new RavenIdentityBuilder(identityBuilder);
        }

        public static RavenIdentityBuilder EnsureUserRols<TUser, TRole>(this RavenIdentityBuilder identityBuilder, TUser user, params TRole[] rols)
            where TRole : IdentityRole
             where TUser : IdentityUser
        {
            identityBuilder.EnsureIdentityRols(rols);
            identityBuilder.EnsureIdentityUser(ref user);
            using (var userManager = identityBuilder.Services.BuildServiceProvider().GetService<UserManager<TUser>>())
            {
                Task.WaitAll(userManager.AddToRolesAsync(user, rols.Select(r => r.Name)));
            }
            return identityBuilder;
        }

        public static RavenIdentityBuilder EnsureIdentityRols<TRole>(this RavenIdentityBuilder identityBuilder, params TRole[] rols)
        where TRole : IdentityRole
        {
            using (var identityDocumentStore = identityBuilder.Services.BuildServiceProvider().GetService<IDocumentStore>())
            {
                foreach (var role in rols)
                {
                    TRole dbrole = null;
                    try
                    {
                        dbrole = identityDocumentStore.IdentitySession<TRole>(session => session.Query<TRole>().FirstOrDefault(r => r.NormalizedName == role.NormalizedName));
                    }
                    catch (InvalidCastException ie)
                    {
                        if (!ie.Message.Contains("seq")) throw;
                    }
                    if (dbrole == null)
                    {
                        identityDocumentStore.IdentitySession(session => session.Store(role));
                    }
                }
            }
            return identityBuilder;
        }

        public static RavenIdentityBuilder EnsureIdentityUser<TUser>(this RavenIdentityBuilder identityBuilder, ref TUser user)
            where TUser : IdentityUser
        {
            var NormalizedUserName = user.NormalizedUserName;
            using (var identityDocumentStore = identityBuilder.Services.BuildServiceProvider().GetService<IDocumentStore>())
            {
                {
                    TUser dbuser = null;
                    try
                    {
                        dbuser = identityDocumentStore.IdentitySession<TUser>(session => session.Query<TUser>().FirstOrDefault(u => u.NormalizedUserName == NormalizedUserName));
                    }
                    catch (InvalidCastException ie)
                    {
                        if (!ie.Message.Contains("seq")) throw;
                    }
                    if (dbuser == null)
                    {
                        using (var session = identityDocumentStore.OpenSession())
                        {
                            session.Store(user);
                            session.SaveChanges();
                        }
                    }
                    else
                    {
                        user = dbuser;
                    }
                }
            }
            return identityBuilder;
        }

        internal static string GetId(this Claim claim)
        {
            if (claim == null) return null;
             
            return $"{claim.Type}/{claim.Value}/{claim.ValueType}/{claim.Issuer}/{claim.OriginalIssuer}";
        }

        internal static string GetId(this IdentityUserLogin<string> iulin)
        {
            if (iulin == null) return null;
            return $"{iulin.LoginProvider}/{iulin.ProviderKey}/{iulin.UserId}";
        }

        internal static string GetId(this UserLoginInfo iulin)
        {
            if (iulin == null) return null;
            return $"{iulin.LoginProvider}/{iulin.ProviderKey}";
        }
    }

    public class UserLoginInfoContainer : IdProviderWithRef<UserLoginInfo>
    {
        public static UserLoginInfoContainer Create(string loginProvider, string providerKey, string userid = null)
        {
            var payload = new UserLoginInfo(loginProvider, providerKey, $"{loginProvider}:{providerKey}");
            return new UserLoginInfoContainer
            {
                Payload = payload,
                id = payload.GetId(),
                Refs = userid == null ? Enumerable.Empty<string>() : new[] { userid },
            };
        }
    }

    public class UserLoginContainer : IdProviderWithRef<IdentityUserLogin<string>>
    {
        public static UserLoginContainer Create(string loginProvider, string providerKey)
        {
            var payload = new IdentityUserLogin<string>
            {
                ProviderKey = providerKey,
                LoginProvider = loginProvider,
                ProviderDisplayName = $"{loginProvider}:{providerKey}"
            };
            return new UserLoginContainer
            {
                id = payload.GetId(),
                Payload = payload,
                Refs = Enumerable.Empty<string>(),
            };
        }

        public static UserLoginContainer Create(string userId, string loginProvider, string providerKey)
        {
            var payload = new IdentityUserLogin<string>
            {
                UserId = userId,
                ProviderKey = providerKey,
                LoginProvider = loginProvider,
                ProviderDisplayName = $"{loginProvider}:{providerKey}:{userId}"
            };
            return new UserLoginContainer
            {
                id = payload.GetId(),
                Payload = payload,
                Refs = new[] { userId },
            };
        }
    }

    public class UserClaimsContainer : IdProviderWithRef<Claim>
    {
        public static UserClaimsContainer Create(Claim claim, string userId = null)
        {
            return new UserClaimsContainer
            {
                id = claim.GetId(),
                Payload = claim,
                Refs = new[] { userId },
            };
        }
    }

    public class UserRoleContainer : IdProviderWithRef<string>
    {
        public static UserRoleContainer Create(string userId, params string[] rols)
        {
            return new UserRoleContainer
            {
                id = $"UserRoleContainer/{userId}",
                Payload = userId,
                Refs = rols,
            };
        }
    }

    public static class RavenExtention
    {
        public static async Task IdentitySession(this IDocumentStore store, Func<IAsyncDocumentSession, Task> action)
        {
            using (var asyncSession = store.OpenAsyncSession())
            {
                await action(asyncSession);
                await asyncSession.SaveChangesAsync();
            }
        }

        public static async Task<T> IdentitySession<T>(this IDocumentStore store, Func<IAsyncDocumentSession, Task<T>> action)
        {
            using (var asyncSession = store.OpenAsyncSession())
            {
                var result = await action(asyncSession);
                await asyncSession.SaveChangesAsync();
                return result;
            }
        }

        public static void IdentitySession(this IDocumentStore store, Action<IDocumentSession> action)
        {
            using (var session = store.OpenSession())
            {
                action(session);
                session.SaveChanges();
            }
        }

        public static T IdentitySession<T>(this IDocumentStore store, Func<IDocumentSession, T> action)
        {
            using (var session = store.OpenSession())
            {
                var result = action(session);
                session.SaveChanges();
                return result;
            }
        }
    }

    public class RavenIdentityBuilder : IdentityBuilder
    {
        public RavenIdentityBuilder(IdentityBuilder identityBuilder):base(identityBuilder.UserType, identityBuilder.RoleType, identityBuilder.Services)
        {

        }
       
    }
}