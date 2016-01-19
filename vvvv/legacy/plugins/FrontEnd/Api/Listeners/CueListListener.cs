using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Iris.Core.Couch;
using Iris.Core.Types;
using Iris.Core.Logging;
using Newtonsoft.Json.Linq;

using WampSharp.V2;
using WampSharp.V2.Realm;
using WampSharp.V2.Core.Contracts;

namespace Iris.FrontEnd.Api.Listeners
{
    public class LogListener : ChangesListener
    {
        private const string TAG = "LogListener";

        private IWampSubject Subject;

        public LogListener(IWampSubject subject, string database) 
            : base(database, "cuelists")
        {
            Subject = subject;
        }

        public override void OnStart()
        {
        }

        public override void OnStop()
        {
        }

        public override void Process(JObject data)
        {
            try
            {
                var cuelists = new Dictionary<string, CueList>();

                /// <summary>
                ///   we *RELY* on descending=true to be passed to
                ///   changes listener and discard all older changes to docs
                ///   (we're only ever interested in the newest revision)
                /// </summary>

                data["results"].ToList().ForEach(item =>
                    {
                        var cuelist = item["doc"].ToObject<CueList>();
                        if(!cuelists.ContainsKey(cuelist._id))
                            cuelists.Add(cuelist._id, cuelist);
                    });

                cuelists.ToList()
                    .ForEach(kv => Publish(null, DataUri.CueListUpdate, kv.Value));
            }
            catch(Exception ex)
            {
                LogEntry.Fatal(TAG, ex.Message);
            }
        }

        public void Publish (string sid, string uri, Object a)
        {
            WampEvent @event = new WampEvent () {
                Options = new PublishOptions (),
                Arguments = new object[] { sid, uri, a }
            };
            Send (@event);
        }

        private void Send (WampEvent @event)
        {
            try 
            {
                Subject.OnNext(@event);
            } 
            catch (WampException ex) 
            {
                LogEntry.Fatal(TAG, "WampException: " + ex.ErrorUri);
            } 
            catch (Exception ex) 
            {
                LogEntry.Fatal(TAG, "Exception: " + ex.Message);
            }
        }
    }
}
