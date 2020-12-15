using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using System;
using System.Threading.Tasks;

namespace Starlinks.Connect.Persistence.RavenDB
{
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
}
