using System;
using System.Text;
using System.Security.Cryptography;
using System.Collections.Concurrent;
using System.Linq;

using Iris.Core.Amqp;
using Iris.Core.Types;
using Iris.Core.Logging;

using RabbitMQ.Client.Events;
using Newtonsoft.Json;

namespace Iris.Nodes.Types
{
    public class CueAmqpListener : AmqpListener
    {
        private ConcurrentDictionary<string, Cue> Cues;

        public CueAmqpListener(ConcurrentDictionary<string, Cue> cues)
        {
            Cues = cues;
            Exchange = DataUri.CueBase;
        }

        protected override void ProcessMessage(object o, BasicDeliverEventArgs ea)
        {
            var json = Encoding.UTF8.GetString(ea.Body);
            var cue = JsonConvert.DeserializeObject<Cue>(json);

            // all Cues get written to Graph if Cue has no Hosts argument
            if(cue.Hosts.Count == 0) 
            {
                Cues.TryAdd(Hash(cue), cue);
            }
            // Cue gets executed if current HostName is not contained in negated form
            else if(cue.Hosts.Any(el => el.StartsWith("!")) &&
                   !cue.Hosts.Contains("!" + Host.HostName))
            {
                Cues.TryAdd(Hash(cue), cue);
            }
            // Cue gets executed if current HostName is contained in Hosts list
            else if(cue.Hosts.Contains(Host.HostName.ToString()))
            {
                Cues.TryAdd(Hash(cue), cue);
            }
        }

        // We ensure that a cue destined for a specific frame in discrete
        // time only gets added once to the execution queue.
        private string Hash(Cue cue)
        {
            var sha   = SHA1.Create();
            var bytes = Encoding.UTF8.GetBytes(cue.ExecFrame + cue._id);
            var hash  = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hash, 0, 15);
        }
    }
}
