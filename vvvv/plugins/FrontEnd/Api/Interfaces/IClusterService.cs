using Iris.Core.Types;
using WampSharp.V2.Rpc;

namespace Iris.FrontEnd.Api.Interfaces
{
    public interface IClusterService
    {
        [WampProcedure(DataUri.ClusterList)]
        IIrisData ListCluster(string sid);

        [WampProcedure(DataUri.ClusterCreate)]
        IIrisData CreateCluster(string sid, Cluster cluster);

        [WampProcedure(DataUri.ClusterRead)]
        IIrisData ReadCluster(string sid, IrisId id);

        [WampProcedure(DataUri.ClusterUpdate)]
        IIrisData UpdateCluster(string sid, Cluster cluter);

        [WampProcedure(DataUri.ClusterDelete)]
        IIrisData DeleteCluster(string sid, Cluster cluster);
    }
}

