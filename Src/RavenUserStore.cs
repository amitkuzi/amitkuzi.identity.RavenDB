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
    internal class RavenUserStore<TUser> : UserStoreBase<TUser, string, IdentityUserClaim<string>, IdentityUserLogin<string>, IdentityUserToken<string>>, IUserStore<TUser>, IUserRoleStore<TUser>
        where TUser : IdentityUser
    {
        private readonly IDocumentStore identityDocumentStore;
        private readonly ILogger<RavenUserStore<TUser>> logger;
        private readonly IMapper userMapper;
        private bool _disposed = false;

        public override IQueryable<TUser> Users => identityDocumentStore.OpenSession().Query<TUser>();

        public RavenUserStore(IDocumentStore identityDocumentStore,
            IdentityErrorDescriber identityErrorDescriber,
            ILogger<RavenUserStore<TUser>> logger) : base(identityErrorDescriber)
        {
            this.identityDocumentStore = identityDocumentStore;
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            userMapper = new MapperConfiguration(cfg => cfg.CreateMap<TUser, TUser>()).CreateMapper();

        }

        public new void Dispose()
        {
            identityDocumentStore.Dispose();
            logger.LogInformation("disposing RavenUserStore");
            base.Dispose();
        }



        public override async Task<IdentityResult> CreateAsync(TUser user, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            try
            {
                await identityDocumentStore.IdentitySession(async session => await session.StoreAsync(user, cancellationToken));
                return IdentityResult.Success;
            }
            catch (Exception e)
            {
                logger.LogError(e, $"error CreateAsync user:{user} ");
                return IdentityResult.Failed(new IdentityError[] { new IdentityError() { Code = e.HResult.ToString(), Description = e.Message, } });
            }
        }

        public override async Task<IdentityResult> UpdateAsync(TUser user, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            try
            {
                await identityDocumentStore.IdentitySession(async session => await session.StoreAsync(user, cancellationToken));
                return IdentityResult.Success;
            }
            catch (Exception e)
            {
                logger.LogError(e, $"error CreateAsync user:{user} ");
                return IdentityResult.Failed(new IdentityError[] { new IdentityError() { Code = e.HResult.ToString(), Description = e.Message, } });
            }
        }

        public override Task<IdentityResult> DeleteAsync(TUser user, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            try
            {
                identityDocumentStore.IdentitySession(session => session.Delete(user));
                return Task.FromResult(IdentityResult.Success);
            }
            catch (Exception e)
            {
                logger.LogError(e, $"error DeleteAsync user:{user} ");
                return Task.FromResult(IdentityResult.Failed(new IdentityError[] { new IdentityError() { Code = e.HResult.ToString(), Description = e.Message, } }));
            }
        }

        public override async Task<TUser> FindByIdAsync(string userId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            try
            {
                return await identityDocumentStore.IdentitySession(async session => await session.LoadAsync<TUser>(userId, cancellationToken));
            }
            catch (Exception e)
            {
                logger.LogError(e, $"error FindByIdAsync user:{userId} ");
                throw;
            }
        }

        public override async Task<TUser> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            try
            {
                return await identityDocumentStore.IdentitySession<TUser>(async session =>
                await session.Query<TUser>().FirstOrDefaultAsync(User => User.NormalizedUserName == normalizedUserName));
            }
            catch (InvalidOperationException er)
            {
                logger.LogWarning(er, $"warning FindByNameAsync user:{normalizedUserName} ");
                return (TUser)null;
            }
            catch (Exception e)
            {
                logger.LogError(e, $"error FindByNameAsync user:{normalizedUserName} ");
                throw;
            }
        }

        protected override async Task<TUser> FindUserAsync(string userId, CancellationToken cancellationToken)
        {
            return await FindByIdAsync(userId, cancellationToken);
        }


        protected async Task<UserLoginContainer> EnsureUserLoginAsync(UserLoginContainer userLoginContainer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            var normalizedId = userLoginContainer.Payload.GetId();
            try
            {

                var ulc = await identityDocumentStore.IdentitySession(async session => await session.LoadAsync<UserLoginContainer>(normalizedId, cancellationToken));
                if (ulc == null)
                {
                    await identityDocumentStore.IdentitySession(async session => await session.StoreAsync(userLoginContainer, normalizedId, cancellationToken));
                    return await EnsureUserLoginAsync(userLoginContainer, cancellationToken);
                }
                return ulc;
            }
            catch (Exception e)
            {
                logger.LogError(e, $"error EnsureUserLoginAsync user:{normalizedId} ");
                throw;
            }
        }
        protected override async Task<IdentityUserLogin<string>> FindUserLoginAsync(string userId, string loginProvider, string providerKey, CancellationToken cancellationToken)
        {

            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            try
            {
                var ulc = UserLoginContainer.Create(userId, loginProvider, providerKey);
                var lookingForId = ulc.Payload.GetId();
                return (await EnsureUserLoginAsync(ulc, cancellationToken)).Payload;
            }
            catch (Exception e)
            {
                logger.LogError(e, $"error FindUserLoginAsync user:{userId} ");
                throw;
            }
        }

        protected override async Task<IdentityUserLogin<string>> FindUserLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            var ulc = UserLoginContainer.Create(loginProvider, providerKey);
            var lookingForId = ulc.Payload.GetId();
            try
            {
                return (await EnsureUserLoginAsync(ulc, cancellationToken)).Payload;
            }
            catch (Exception e)
            {
                logger.LogError(e, $"error FindUserLoginAsync id:{lookingForId} ");
                throw;
            }
        }

        internal async Task<IEnumerable<UserClaimsContainer>> GetUserClaimsContainer(TUser user, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            try
            {
                return (await identityDocumentStore.IdentitySession<IEnumerable<UserClaimsContainer>>(async session => await session.Query<UserClaimsContainer>().Where(ucc => ucc.Refs.Contains(user.Id)).ToListAsync())) ?? Enumerable.Empty<UserClaimsContainer>();
            }
            catch (Exception e)
            {
                logger.LogError(e, $"error GetUserClaimsContainer user:{user} ");
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

        internal async Task<UserClaimsContainer[]> EnsureUserClaimsContainer(IEnumerable<UserClaimsContainer> userClaimsContainers, CancellationToken cancellationToken = default)
        {
            var tasks = userClaimsContainers.Select(ucc => EnsureUserClaimsContainer(ucc, cancellationToken));
            return await Task.WhenAll(tasks);

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

        public override async Task<IList<Claim>> GetClaimsAsync(TUser user, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            try
            {
                return (await GetUserClaimsContainer(user, cancellationToken)).Select(ucc => ucc.Payload).ToList();
            }
            catch (Exception e)
            {
                logger.LogError(e, $"error GetClaimsAsync user:{user} ");
                throw;
            }
        }

        public override async Task AddClaimsAsync(TUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            try
            {
                var claimsContainers = claims.Select(cl => new UserClaimsContainer { id = cl.GetId(), Payload = cl }.AddRefs<UserClaimsContainer, Claim>(user.Id));

                await EnsureUserClaimsContainer(claimsContainers, cancellationToken);
            }
            catch (Exception e)
            {
                logger.LogError(e, $"error AddClaimsAsync user:{user} ");
                throw;
            }
        }

        public override async Task ReplaceClaimAsync(TUser user, Claim claim, Claim newClaim, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            try
            {
                await RemoveClaimsAsync(user, new[] { claim }, cancellationToken);
                await EnsureUserClaimsContainer(UserClaimsContainer.Create(claim, user.Id), cancellationToken);
            }
            catch (Exception e)
            {
                logger.LogError(e, $"error AddClaimsAsync user:{user} ");
                throw;
            }
        }

        public override Task RemoveClaimsAsync(TUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            try
            {

                var idsToRemove = claims.Select(cl => cl.GetId());
                idsToRemove.ToList().ForEach(cid => identityDocumentStore.IdentitySession(s => s.Delete(cid)));
                return Task.CompletedTask;
            }
            catch (Exception e)
            {
                logger.LogError(e, $"error AddClaimsAsync user:{user} ");
                throw;
            }
        }

        internal async Task<UserLoginInfoContainer> EnsureUserLoginInfoContainer(UserLoginInfoContainer uliContainer, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            try
            {
                var enureIdExist = uliContainer.Payload.GetId();
                var ucc = await identityDocumentStore.IdentitySession<UserLoginInfoContainer>(async session => await session.LoadAsync<UserLoginInfoContainer>(enureIdExist));
                if (ucc == null)
                {
                    await identityDocumentStore.IdentitySession(async session => await session.StoreAsync(uliContainer, enureIdExist, cancellationToken));
                    return await EnsureUserLoginInfoContainer(uliContainer, cancellationToken);
                }
                return ucc;
            }
            catch (Exception e)
            {
                logger.LogError(e, $"error EnsureUserLoginInfoContainer   ");
                throw;
            }
        }


        public override async Task AddLoginAsync(TUser user, UserLoginInfo login, CancellationToken cancellationToken = default)
        {//UserLoginInfoContainer
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            try
            {
                var uliContainers = new UserLoginInfoContainer
                {
                    id = login.GetId(),
                    Payload = login,
                    Refs = new[] { user.Id },
                };

                await EnsureUserLoginInfoContainer(uliContainers, cancellationToken);
            }
            catch (Exception e)
            {
                logger.LogError(e, $"error AddClaimsAsync user:{user} ");
                throw;
            }
        }

        public override Task RemoveLoginAsync(TUser user, string loginProvider, string providerKey, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            try
            {
                var uliContainer = UserLoginInfoContainer.Create(loginProvider, providerKey);
                identityDocumentStore.IdentitySession(s => s.Delete(uliContainer.id));
                return Task.CompletedTask;
            }
            catch (Exception e)
            {
                logger.LogError(e, $"error AddClaimsAsync user:{user} ");
                throw;
            }
        }

        public override async Task<IList<UserLoginInfo>> GetLoginsAsync(TUser user, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            try
            {
                return (await identityDocumentStore.IdentitySession<IEnumerable<UserLoginInfoContainer>>(async session => await session.Query<UserLoginInfoContainer>().Where(ucc => ucc.Refs.Contains(user.Id)).ToListAsync())).Select(ulic => ulic.Payload).ToList();
            }
            catch (Exception e)
            {
                logger.LogError(e, $"error GetUserClaimsContainer user:{user} ");
                throw;
            }
        }

        public override async Task<TUser> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            try
            {
                return await identityDocumentStore.IdentitySession<TUser>(async session =>
                await session.Query<TUser>().FirstOrDefaultAsync(User => User.NormalizedEmail == normalizedEmail));
            }
            catch (InvalidOperationException er)
            {
                logger.LogWarning(er, $"warning FindByEmailAsync user:{normalizedEmail}  ");
                return (TUser)null;
            }
            catch (Exception e)
            {
                logger.LogError(e, $"error FindByEmailAsync user:{normalizedEmail} ");
                throw;
            }
        }

        public override async Task<IList<TUser>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            try
            {
                var ucc = await identityDocumentStore.IdentitySession(async session => await session.LoadAsync<UserClaimsContainer>(claim.GetId()));
                return (await identityDocumentStore.IdentitySession(async session => await session.LoadAsync<TUser>(ucc.Refs))).Values.ToList();
            }
            catch (Exception e)
            {
                logger.LogError(e, $"error GetUsersForClaimAsync user:{claim.GetId()} ");
                throw;
            }
        }

        protected async Task<IdentityUserToken<string>> EnsureTokenAsync(IdentityUserToken<string> token, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            try
            {

                var iut = await identityDocumentStore.IdentitySession<IdentityUserToken<string>>(async session => await session.LoadAsync<IdentityUserToken<string>>(token.UserId, cancellationToken));
                if (iut == null)
                {
                    await identityDocumentStore.IdentitySession(async session => await session.StoreAsync(token, token.UserId, cancellationToken));
                    return await EnsureTokenAsync(token, cancellationToken);
                }
                return iut;
            }
            catch (Exception e)
            {
                logger.LogError(e, $"error EnsureTokenAsync token:{token} ");
                throw;
            }
        }
        protected override async Task<IdentityUserToken<string>> FindTokenAsync(TUser user, string loginProvider, string name, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            try
            {
                return await identityDocumentStore.IdentitySession(async session => await session.LoadAsync<IdentityUserToken<string>>(user.Id));
            }
            catch (Exception e)
            {
                logger.LogError(e, $"error FindTokenAsync user:{user} ");
                throw;
            }
        }

        protected override async Task AddUserTokenAsync(IdentityUserToken<string> token)
        {
            await EnsureTokenAsync(token);
        }

        protected override Task RemoveUserTokenAsync(IdentityUserToken<string> token)
        {
            ThrowIfDisposed();
            try
            {
                identityDocumentStore.IdentitySession(session => session.Delete<IdentityUserToken<string>>(token));
                return Task.CompletedTask;
            }
            catch (Exception e)
            {
                logger.LogError(e, $"error FindTokenAsync token:{token} ");
                throw;
            }
        }

        public async Task<UserRoleContainer> EnsureUserRoleContainer(TUser user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            var urc = UserRoleContainer.Create(user.Id);
            var normalizedId = urc.id;
            urc = null;
            try
            {

                urc = await identityDocumentStore.IdentitySession(async session => await session.LoadAsync<UserRoleContainer>(normalizedId, cancellationToken));
                if (urc == null)
                {
                    await identityDocumentStore.IdentitySession(async session => await session.StoreAsync(UserRoleContainer.Create(user.Id), normalizedId, cancellationToken));
                    return await EnsureUserRoleContainer(user, cancellationToken);
                }
                return urc;
            }
            catch (Exception e)
            {
                logger.LogError(e, $"error EnsureUserRoleContainer user:{normalizedId} ");
                throw;
            }
        }

        public async Task AddToRoleAsync(TUser user, string roleName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            try
            {
                UserRoleContainer urc = await EnsureUserRoleContainer(user, cancellationToken);
                urc = urc.AddRefs<UserRoleContainer, string>(roleName);
                await identityDocumentStore.IdentitySession(async s => await s.StoreAsync(urc, urc.id, cancellationToken));
            }
            catch (Exception e)
            {
                logger.LogError(e, $"error AddToRoleAsync user:{user} ");
                throw;
            }
        }

        public async Task RemoveFromRoleAsync(TUser user, string roleName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            try
            {
                UserRoleContainer urc = await EnsureUserRoleContainer(user, cancellationToken);
                urc = urc.RemoveRefs<UserRoleContainer, string>(roleName);
                await identityDocumentStore.IdentitySession(async s => await s.StoreAsync(urc, urc.id, cancellationToken));
            }
            catch (Exception e)
            {
                logger.LogError(e, $"error AddToRoleAsync user:{user} ");
                throw;
            }
        }

        public async Task<IList<string>> GetRolesAsync(TUser user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            try
            {
                UserRoleContainer urc = await EnsureUserRoleContainer(user, cancellationToken);
                return urc.Refs.ToList();
            }
            catch (Exception e)
            {
                logger.LogError(e, $"error GetRolesAsync user:{user} ");
                throw;
            }
        }

        public async Task<bool> IsInRoleAsync(TUser user, string roleName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            try
            {
                var rols = (await GetRolesAsync(user, cancellationToken)).ToList();
                return rols.Contains(roleName);
            }
            catch (Exception e)
            {
                logger.LogError(e, $"error IsInRoleAsync user:{user} ");
                throw;
            }
        }

        public async Task<IList<TUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            try
            {
                var urcs = await identityDocumentStore.IdentitySession<IEnumerable<UserRoleContainer>>(async s => await s.Query<UserRoleContainer>().Where(urc => urc.Refs.Contains(roleName)).ToListAsync());
                return await identityDocumentStore.IdentitySession<IList<TUser>>(async s => (await s.LoadAsync<TUser>(urcs.Select(urc => urc.Payload))).Values.ToList());

            }
            catch (Exception e)
            {
                logger.LogError(e, $"error GetUsersInRoleAsync roleName:{roleName} ");
                throw;
            }
        }
    }
}