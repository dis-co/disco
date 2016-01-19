using System.Collections.Generic;

using Iris.Core.Types;
using Iris.FrontEnd.Types;
using Iris.FrontEnd.Api.Interfaces;

using RabbitMQ.Client.Events;

namespace Iris.FrontEnd.Api.Impl
{
    public class HostStatService : IrisService, IHostStatService
    {
        public HostStatService() : base(DataUri.HostStatBase)
        {
        }

        protected override void BeforeStart()
        {
        }

        protected override void OnMessage(BasicDeliverEventArgs ea)
        {
            //  Fixme;
        }

        public List<HostStat> ListStats(string sid)
        {
            return new List<HostStat>{ new HostStat() };
        }

        public HostStat ReadStat(string sid)
        {
            return new HostStat();
        }
    }
}
