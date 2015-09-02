using System;
using System.Linq;
using System.Collections.Generic;
using System.Web;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Converters;

using Iris.Core.Couch;

namespace Iris.Core.Types
{
    using Replications = List<ClusterMember.Replication>;
    using Upstreams    = List<ClusterMember.FederationUpstream>;

    public class ClusterMember
    {
        public Name        HostName { get; set; }
        public IrisIP            IP { get; set; }
        public ClusterStatus Status { get; set; }

        private CouchClient Couch  { get; set; }
        private CouchClient Rabbit { get; set; }

        public class FederationUpstreamSerializer : JsonConverter
        {
            public override void WriteJson(JsonWriter writer, object val, JsonSerializer serializer)
            {
                var upstream = val as FederationUpstream;

                var obj   = new JObject();
                var value = new JObject();

                obj["value"] = value;
                obj["component"]  = new JValue("federation-upstream");

                if(!object.Equals(null, upstream.name))
                    obj["name"]  = new JValue(upstream.name);

                if(!object.Equals(null, upstream.vhost))
                    obj["vhost"]  = new JValue(upstream.vhost);

                if(!object.Equals(null, upstream.uri))
                    value["uri"] = new JValue(upstream.uri);

                if(!object.Equals(null, upstream.message_ttl))
                    value["message-ttl"] = new JValue(upstream.message_ttl);

                if(!object.Equals(null, upstream.ack_mode))
                    value["ack-mode"] = new JValue(upstream.ack_mode);

                if(!object.Equals(null, upstream.trust_user_id))
                    value["trust-user-id"] = new JValue(upstream.trust_user_id);

                serializer.Serialize(writer, obj);
            }

            public override object ReadJson(JsonReader reader, Type type, object existingValue, JsonSerializer serializer)
            {
                JObject obj = JObject.Load(reader);
                return new FederationUpstream {
                    name          = obj["name"].ToObject<string>(),
                    vhost         = obj["vhost"].ToObject<string>(),
                    uri           = obj["value"]["uri"].ToObject<string>(),
                    message_ttl   = obj["value"]["message-ttl"].ToObject<int>(),
                    ack_mode      = obj["value"]["ack-mode"].ToObject<string>(),
                    trust_user_id = obj["value"]["trust-user-id"].ToObject<bool>()
                };
            }

            public override bool CanConvert(Type type)
            {
                return typeof(FederationUpstream).IsAssignableFrom(type);
            }
        }

        [JsonConverter(typeof(FederationUpstreamSerializer))]
        public class FederationUpstream
        {
            public string name          { get; set; }
            public string vhost         { get; set; }
            public string uri           { get; set; }
            public string ack_mode      { get; set; }
            public bool   trust_user_id { get; set; }
            public int    message_ttl   { get; set; }

            public string ToJSON()
            {
                return JsonConvert.SerializeObject(this);
            }
        }

        public class Replication
        {
            public IrisId                       _id { get; set; }
            public IrisId                      _rev { get; set; }
            public string                    source { get; set; }
            public string                    target { get; set; }
            public bool               create_target { get; set; }
            public bool                  continuous { get; set; }
            public string        _replication_state { get; set; }
            public string   _replication_state_time { get; set; }
            public string _replication_state_reason { get; set; }
            public string           _replication_id { get; set; }

            public bool ShouldSerialize_id()
            {
                return !object.ReferenceEquals(null, _id);
            }

            public bool ShouldSerialize_rev()
            {
                return !object.ReferenceEquals(null, _rev);
            }

            public bool ShouldSerialize_replication_state()
            {
                return !object.ReferenceEquals(null, _replication_state);
            }

            public bool ShouldSerialize_replication_state_time()
            {
                return !object.ReferenceEquals(null, _replication_state_time);
            }

            public bool ShouldSerialize_replication_state_reason()
            {
                return !object.ReferenceEquals(null, _replication_state_reason);
            }

            public bool ShouldSerialize_replication_id()
            {
                return !object.ReferenceEquals(null, _replication_id);
            }
        }

        public ClusterStatus Realize(Cluster cluster)
        {
            Init();

            cluster.Members.ForEach(member => {
                    if(member.IP != IP)
                    {
                        // This host should...
                        ReplicateWith(member); // and ...
                        FederateWith(member);  // kthxbye.
                    }
                });

            return Status;
        }

        public void Reset()
        {
            // remove all entries on that Member
            ListReplications().ForEach(RemoveReplication);
            ListFederations().ForEach(RemoveFederation);
        }

        public ClusterStatus GetStatus()
        {
            return Status;
        }

        private void Init()
        {
            if(Couch == null)
                Couch = new CouchClient("http://" + IP + ":5984");

            if(Rabbit == null)
                Rabbit = new CouchClient("http://" + IP + ":15672", "iris", "iris");
        }

        /// <summary>
        ///  FIXME:  Get RabbitMQ federations status for this Member
        /// </summary>

        /// <summary>
        ///  FIXME:  Get CouchDB replications for this Member
        /// </summary>

        private void ReplicateWith(ClusterMember member)
        {
            var projects = GetProjects();
            projects.ForEach(db => ReplicateDBWith(member, db));
        }

        public List<string> GetProjects()
        {
            List<string> result = Couch
                .GetView("projects/_design/projects/_view/all")
                .Select(obj => obj["doc"]["Database"].ToObject<string>())
                .ToList();

            result.Add("projects");
            return result;
        }

        public void CreateProjectDBs(List<string> projects)
        {
            Init();
            projects.ForEach(project => {
                    Console.WriteLine("Creating db: " + project + " on " + IP);
                    Couch.Put(project);
                });
        }

        private void ReplicateDBWith(ClusterMember member, string db)
        {
            var replication = new Replication {
                source        = db,
                target        = "http://" + member.IP + ":5984/" + db,
                continuous    = true,
                create_target = true
            };
            Couch.Post("_replicator", JsonConvert.SerializeObject(replication));
        }

        private void FederateWith(ClusterMember member)
        {
            Rabbit.Put( // url resources to 'PUT' to
                String.Format("api/parameters/federation-upstream/%2F/{0}",
                              HttpUtility.UrlEncode(member.HostName.ToString())),
                // upstream definition
                new FederationUpstream {
                    name          = member.HostName.ToString(),
                    vhost         = "/",
                    uri           = "amqp://iris:iris@" + member.IP,
                    ack_mode      = "no-ack",
                    message_ttl   = 5,
                    trust_user_id = false
                }.ToJSON());
        }

        private Replications ListReplications()
        {
            Init();

            var result = new Replications();
            try {
                var resp = Couch.Get("_replicator/_all_docs?include_docs=true");
                if(resp.Item1 == HttpStatusCode.OK)
                {
                    var parsed = JObject.Parse(resp.Item2);
                    parsed["rows"].ToList().ForEach(jdoc => {
                            var replication = jdoc["doc"].ToObject<Replication>();

                            if(replication._id != "_design/_replicator")
                                result.Add(replication);
                        });
                }
            }
            catch {}

            return result;
        }

        private void RemoveReplication(Replication replication)
        {
            try {
                Couch.Delete("_replicator/" + replication._id +
                             "?rev=" + replication._rev);
            }
            catch(Exception ex) 
            {
                Console.WriteLine("Exception in request to CouchDB:");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }

        private void RemoveFederation(FederationUpstream upstream)
        {
            var uri = String
                .Format("api/parameters/federation-upstream/{0}/{1}",
                        HttpUtility.UrlEncode(upstream.vhost),
                        upstream.name);
            try
            {
                Rabbit.Delete(uri);
            }
            catch(Exception ex) 
            {
                Console.WriteLine("Exception in request RabbitMQ:");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }

        private Upstreams ListFederations()
        {
            var result = new Upstreams();

            try {
                var resp = Rabbit.Get("api/parameters/federation-upstream");

                if(resp.Item1 == HttpStatusCode.OK)
                    result = JsonConvert.DeserializeObject<Upstreams>(resp.Item2);
            }
            catch {}

            return result;
        }
    }
}
