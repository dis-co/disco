using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Iris.Core.Types
{
    using Members = List<ClusterMember>;

    [JsonConverter(typeof(StringEnumConverter))]
    public enum ClusterStatus
    {
        Deactivated, // default is not active
        Deactivate,
        Activate,
        Activated
    }

    public class Cluster : IIrisData, IDisposable
    {
        public string Type = "Cluster";

        public IrisId           _id { get; set; }
        public IrisId          _rev { get; set; }
        public ClusterStatus Status { get; set; }
        public Name            Name { get; set; }
        public Members      Members { get; set; }

        public void Dispose()
        {
        }

        public void PullUp()
        {
            Console.WriteLine("Clustering: beginning configuration process.");

            var projects = GetAllProjects();

            Console.WriteLine("Clustering: creating all dbs on all hosts if they don't exist'");

            Parallel.ForEach(Members, member => {
                    member.CreateProjectDBs(projects);
                });

            Console.WriteLine("Clustering: realizing configurations on all hosts");

            var result = Parallel.ForEach(Members, member => {
                    Console.WriteLine("Clustering: configuring target: " + member.IP);
                    member.Realize(this);
                    Console.WriteLine("Clustering: done configuring " + member.IP);
                    // var status = member.Realize(this);
                    // if(status == ClusterStatus.OK && status != Status)
                    //     Status = status;
                });

            while(!result.IsCompleted)
            {
                Thread.Sleep(1000);
            }

            Status = ClusterStatus.Deactivated;
            Console.WriteLine("Clustering: configuration complete.");
        }

        public void PullDown()
        {
            Members.ForEach(member => member.Reset());
            Status = ClusterStatus.Deactivated;
        }

        public List<string> GetAllProjects()
        {
            var projects = new List<string> { "projects" }; // with default 

            Members.ForEach(member => {
                    var local = member.GetProjects();
                    local.ForEach(localp => {
                            if(!projects.Contains(localp))
                                projects.Add(localp);
                        });
                });

            return projects;
        }

        public ClusterStatus GetStatus()
        {
            // update overall status of cluster
            return Status;
        }

        public bool HasMember(ClusterMember member)
        {
            return Members.Aggregate(false, (result, mem) => {
                    result |= mem.IP == member.IP; // nice
                    return result;
                });
        }

        public bool ShouldSerialize_id()
        {
            return !object.ReferenceEquals(null, _id);
        }

        public bool ShouldSerialize_rev()
        {
            return !object.ReferenceEquals(null, _rev);
        }

        public string ToUriPath()
        {
            return "projects/" + _id;
        }

        public static Cluster FromJson(string json)
        {
            return JsonConvert.DeserializeObject<Cluster>(json);
        }

        public string ToJSON()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
