using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace amitkuzi.Identity.RavenEmbaded
{

    public static class PocoUtils
    {

        public static TIdProvider SetId<TIdProvider, TPayload>(this TIdProvider idp, string id)
            where TIdProvider : IdProviderWithRef<TPayload>, new()
             where TPayload : class
        {

            return new TIdProvider
            {
                id = id,
                Payload = idp.Payload,
                Refs = idp.Refs,
            };
        }

        public static TIdProvider AddRefs<TIdProvider, TPayload>(this TIdProvider idp , params  string[] refs )
           where TIdProvider : IdProviderWithRef<TPayload>, new()
            where TPayload :  class
        {
            var oldrefs = idp.Refs.ToList();
            oldrefs.AddRange(refs);

            return new TIdProvider
            {
                id = idp.id,
                Payload = idp.Payload,
                Refs = oldrefs,
            };
        }

        public static TIdProvider RemoveRefs<TIdProvider, TPayload>(this TIdProvider idp, params string[] refs)
           where TPayload : class
           where TIdProvider : IdProviderWithRef<TPayload>, new()
        {
            var oldrefs = idp.Refs.ToList();
            refs.ToList().ForEach(r => oldrefs.Remove(r));
            return new TIdProvider
            {
                id = idp.id,
                Payload = idp.Payload,
                Refs = oldrefs,
            };
        }
    }

    public class IdProvider
    {
        public string id { get; init; }
    }
    public class IdProvider<TPayload> : IdProvider where TPayload : class
    {
       
        public TPayload Payload { get; init; }
    }
    public class IdProviderWithRef<TPayload> where TPayload : class
    {
        public string id { get; init; }
        public TPayload Payload { get; init; }
        public IEnumerable<string> Refs { get; init; } = Enumerable.Empty<string>();
    }


    

}
