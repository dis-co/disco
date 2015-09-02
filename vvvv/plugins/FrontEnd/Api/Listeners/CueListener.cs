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
    public class CueListener : ChangesListener
    {
        private const string TAG = "CueListener";

        private IWampSubject Subject;

        public CueListener(IWampSubject subject, string database)
            : base(database, "cues")
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
                var cues = new Dictionary<string, Cue>();

                /// <summary>
                ///   we *RELY* on descending=true to be passed to
                ///   changes listener and discard all older changes to docs
                ///   (we're only ever interested in the newest revision)
                /// </summary>

                data["results"].ToList().ForEach(item =>
                        {
                            var cue = item["doc"].ToObject<Cue>();
                            if(!cues.ContainsKey(cue._id))
                                cues.Add(cue._id, cue);
                        });

                cues.ToList()
                    .ForEach(kv => Publish(null, DataUri.CueUpdate, kv.Value));
            }
            catch(Exception ex)
            {
                LogEntry.Fatal(TAG, ex.Message);
            }
        }

        public void Publish (string sid, string uri, Object a)
        {
            var @event = new WampEvent {
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
