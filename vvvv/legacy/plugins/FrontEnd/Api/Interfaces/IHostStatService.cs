using System.Collections.Generic;
using WampSharp.V2.Rpc;


using Iris.Core.Types;

namespace Iris.FrontEnd.Api.Interfaces
{
    public interface IHostStatService
    {
        [WampProcedure("iris.host.stats/read")]
        List<HostStat> ListStats(string sid);

        [WampProcedure("iris.host.stat/read")]
        HostStat ReadStat(string sid);
    }
}
