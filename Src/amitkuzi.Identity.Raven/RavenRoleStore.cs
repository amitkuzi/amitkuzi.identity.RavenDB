using amitkuzi.Identity.Raven;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace amitkuzi.Identity.RavenEmbaded
{
    internal class RavenRoleStore<TRole> : RoleStoreBase<TRole, string, IdentityUserRole<string>, IdentityRoleClaim<string>> /*IRoleStore<TRole>*/
        where TRole : IdentityRole
    {

        private readonly IDocumentStore identityDocumentStore;
        private readonly IdentityErrorDescriber identityErrorDescriber;
        private readonly ILogger<RavenRoleStore<TRole>> logger;
        private readonly IMapper userMapper;


        public RavenRoleStore(IDocumentStore identityDocumentStore,
            IdentityErrorDescriber identityErrorDescriber,
            ILogger<RavenRoleStore<TRole>> logger) : base(identityErrorDescriber)
        {
            this.identityDocumentStore = identityDocumentStore;
            this.identityErrorDescriber = identityErrorDescriber;
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            userMapper = new MapperConfiguration(cfg => cfg.CreateMap<TRole, TRole>()).CreateMapper();
        }

        public override IQueryable<TRole> Roles => identityDocumentStore.OpenSession().Query<TRole>();

        internal async Task<IEnumerable<UserClaimsContainer>> GetUserClaimsContainer(TRole role, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            try
            {
                return (await identityDocumentStore.IdentitySession<IEnumerable<UserClaimsContainer>>(async session => await session.Query<UserClaimsContainer>().Where(ucc => ucc.Refs.Contains(role.Id)).ToListAsync())) ?? Enumerable.Empty<UserClaimsContainer>();
            }
            catch (Exception e)
            {
                logger.LogError(e, $"error GetUserClaimsContainer role:{role} ");
                throw;
            }
        }
        internal async Task StoreUserClaimsContainer(UserClaimsContainer userClaimsContainer, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            try
            {
                var normalizedId = userClaimsContainer.Payload.GetId();
                await identityDocumentStore.IdentitySession(async session => await session.StoreAsync(userClaimsContainer.SetId<UserClaimsContainer, Claim>(normalizedId), normalizedId, cancellationToken));
            }
            catch (Exception e)
            {
                logger.LogError(e, $"error StoreUserClaimsContainer user:{userClaimsContainer} ");
                throw;
            }
        }

        internal async Task<UserClaimsContainer> EnsureUserClaimsContainer(UserClaimsContainer userClaimsContainer, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            try
            {
                var enureIdExist = userClaimsContainer.Payload.GetId();
                var ucc = await identityDocumentStore.IdentitySession<UserClaimsContainer>(async session => await session.LoadAsync<UserClaimsContainer>(enureIdExist));
                if (ucc == null)
                {
                    await StoreUserClaimsContainer(userClaimsContainer.SetId<UserClaimsContainer, Claim>(enureIdExist), cancellationToken);
                    return await EnsureUserClaimsContainer(userClaimsContainer, cancellationToken);
                }
                return ucc;
            }
            catch (Exception e)
            {
                logger.LogError(e, $"error EnsureUserClaimsContainer user:{userClaimsContainer} ");
                throw;
            }
        }


        public override async Task AddClaimAsync(TRole role, Claim claim, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            try
            {
                var claimsContainer = new UserClaimsContainer { id = claim.GetId(), Payload = claim }.AddRefs<UserClaimsContainer, Claim>(role.Id);

                await EnsureUserClaimsContainer(claimsContainer, cancellationToken);
            }
            catch (Exception e)
            {
                logger.LogError(e, $"error AddClaimsAsync role:{role} ");
                throw;
            }
        }

        public override async Task<IdentityResult> CreateAsync(TRole role, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            try
            {
                await identityDocumentStore.IdentitySession(async session => await session.StoreAsync(role, cancellationToken));
                return IdentityResult.Success;
            }
            catch (Exception e)
            {
                logger.LogError(e, $"error CreateAsync role:{role} ");
                return IdentityResult.Failed(new IdentityError[] { new IdentityError() { Code = e.HResult.ToString(), Description = e.Message, } });
            }
        }

        public override Task<IdentityResult> DeleteAsync(TRole role, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            try
            {
                identityDocumentStore.IdentitySession(session => session.Delete(role));
                return Task.FromResult(IdentityResult.Success);
            }
            catch (Exception e)
            {
                logger.LogError(e, $"error DeleteAsync role:{role} ");
                return Task.FromResult(IdentityResult.Failed(new IdentityError[] { new IdentityError() { Code = e.HResult.ToString(), Description = e.Message, } }));
            }
        }

        public new void Dispose()
        {

            identityDocumentStore.Dispose();
            logger.LogInformation("disposing RavenIdentityPersistenceManager");
            base.Dispose();
        }

        public override async Task<TRole> FindByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            try
            {
                return await identityDocumentStore.IdentitySession(async session => await session.LoadAsync<TRole>(id, cancellationToken));
            }
            catch (Exception e)
            {
                logger.LogError(e, $"error FindByIdAsync role:{id} ");
                throw;
            }
        }

        public override async Task<TRole> FindByNameAsync(string normalizedName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            try
            {
                return await identityDocumentStore.IdentitySession<TRole>(async session =>
                await session.Query<TRole>().FirstOrDefaultAsync(role => role.NormalizedName == normalizedName));
            }
            catch (InvalidOperationException er)
            {
                logger.LogWarning(er, $"warning FindByNameAsync role:{normalizedName} ");
                return (TRole)null;
            }
            catch (Exception e)
            {
                logger.LogError(e, $"error FindByNameAsync role:{normalizedName} ");
                throw;
            }
        }

        public override async Task<IList<Claim>> GetClaimsAsync(TRole role, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            try
            {
                return (await GetUserClaimsContainer(role, cancellationToken)).Select(ucc => ucc.Payload).ToList();
            }
            catch (Exception e)
            {
                logger.LogError(e, $"error GetClaimsAsync role:{role} ");
                throw;
            }
        }

        public override Task RemoveClaimAsync(TRole role, Claim claim, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            try
            {

                var idsToRemove = claim.GetId();
                identityDocumentStore.IdentitySession(s => s.Delete(idsToRemove));
                return Task.CompletedTask;
            }
            catch (Exception e)
            {
                logger.LogError(e, $"error AddClaimsAsync user:{role} ");
                throw;
            }
        }

        public override async Task<IdentityResult> UpdateAsync(TRole role, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            try
            {
                await identityDocumentStore.IdentitySession(async session => await session.StoreAsync(role, cancellationToken));
                return IdentityResult.Success;
            }
            catch (Exception e)
            {
                logger.LogError(e, $"error CreateAsync role:{role} ");
                return IdentityResult.Failed(new IdentityError[] { new IdentityError() { Code = e.HResult.ToString(), Description = e.Message, } });
            }
        }
    }
}
