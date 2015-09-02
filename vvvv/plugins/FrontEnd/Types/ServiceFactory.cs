using System.Collections.Generic;
using WampSharp.V2.Realm;


namespace Iris.FrontEnd.Types
{
    public class ServiceFactory
    {
        public List<IrisService> services;

        public void Register(IWampHostedRealm realm)
        {
            services.ForEach(p => p.Register(realm));
        }
    }
}