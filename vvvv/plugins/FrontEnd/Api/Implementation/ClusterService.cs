using System;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Collections.Generic;

using Iris.Core.Types;
using Iris.Core.Logging;
using Iris.FrontEnd.Types;
using Iris.FrontEnd.Api.Impl;
using Iris.FrontEnd.Api.Interfaces;

using Newtonsoft.Json.Linq;
using RabbitMQ.Client.Events;

namespace Iris.FrontEnd.Api.Impl
{
    public class ClusterService : IrisService, IClusterService
    {
        private const string TAG = "ClusterService";

        public ClusterService() : base(DataUri.ClusterBase)
        {
        }

        protected override void BeforeStart()
        {
        }

        protected override void OnMessage(BasicDeliverEventArgs ea)
        {
        }

        public IIrisData ListCluster(string _sid)
        {
            var resp = new Clusters();
            var list = Couch.GetView("projects/_design/cluster/_view/all");
            list.ToList().ForEach(obj => resp.Add(obj["doc"].ToObject<Cluster>()));
            return resp;
        }

        public IIrisData CreateCluster(string _sid, Cluster cluster)
        {
            var resp = Couch.Post("projects", cluster.ToJSON());
            if(resp.Item1 == HttpStatusCode.Created || 
               resp.Item1 == HttpStatusCode.Accepted)
            {
                var json = JObject.Parse(resp.Item2);
                cluster._id = json["id"].ToObject<IrisId>();
                cluster._rev = json["rev"].ToObject<IrisId>();
                return cluster;
            }
            throw IrisException.FromHttpStatusCode(resp.Item1);
        }

        public IIrisData ReadCluster(string _sid, IrisId id)
        {
            var resp = Couch.Get("projects/" + id);
            if (resp.Item1 == HttpStatusCode.OK ||
                resp.Item1 == HttpStatusCode.NotModified)
            {
                return Cluster.FromJson(resp.Item2);
            }
            throw IrisException.FromHttpStatusCode(resp.Item1);
        }

        public IIrisData UpdateCluster(string _sid, Cluster cluster)
        {
            /// <summary>
            ///   Activate
            /// </summary>
            if (cluster.Status == ClusterStatus.Activate)
            {
                Console.WriteLine("Activating");
                cluster.PullDown();
                cluster.PullUp();
                cluster.Status = ClusterStatus.Activated;
            }

            /// <summary>
            ///   Deactivate
            /// </summary>
            if (cluster.Status == ClusterStatus.Deactivate)
            {
                Console.WriteLine("Deactivating");
                cluster.PullDown();
                cluster.Status = ClusterStatus.Deactivated;
            }

            /// <summary>
            ///   Save current state.
            /// </summary>
            var resp = Couch.Put(cluster.ToUriPath(), cluster.ToJSON());
            if(resp.Item1 == HttpStatusCode.Created ||
               resp.Item1 == HttpStatusCode.Accepted)
            {
                var obj = JObject.Parse(resp.Item2);
                cluster._rev = obj["rev"].ToObject<IrisId>();
                return cluster;
            }

            throw IrisException.FromHttpStatusCode(resp.Item1);
        }

        public IIrisData DeleteCluster(string _sid, Cluster cluster)
        {
            var resp = Couch.Delete(cluster.ToUriPath() + "?rev=" + cluster._rev);
            if(resp.Item1 == HttpStatusCode.OK ||
               resp.Item1 == HttpStatusCode.Accepted) return cluster;
            throw IrisException.FromHttpStatusCode(resp.Item1);
        }
    }
}
