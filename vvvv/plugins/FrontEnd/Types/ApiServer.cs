using System;
using System.Threading;

using WampSharp.V2;
using WampSharp.V2.Realm;

using Iris.Core.Couch;
using Iris.Core.Types;
using Iris.Core.Logging;
using Iris.FrontEnd.Api;
using Iris.FrontEnd.Api.Listeners;

namespace Iris.FrontEnd.Types
{
    public class ApiServer : IDisposable
    {
        private const string TAG = "ApiServer";

        private DefaultWampHost Host;
        private Thread WorkerThread;

        // private HostStatUpdateService Stats;
        private readonly string Location = @"ws://0.0.0.0:9500";
        private ServiceFactory Services;

        private readonly object Lock = new object ();
        private ProjectListener CouchListener;

        #region Constructor
        public ApiServer (string location)
        {
            Location = location;
            Init ();
        }

        public ApiServer ()
        {
            Init ();
        }

        private void Init ()
        {
            // Stats = new HostStatUpdateService (this);
            WorkerThread = new Thread (Runnable);
        }
        #endregion

        #region Destructor
        public void Dispose ()
        {
            Stop ();
        }
        #endregion

        /// <summary>
        /// Start the server thread.
        /// </summary>
        public void Start ()
        {
            WorkerThread.Start ();
        }

        /// <summary>
        /// Stop and dispose the server.
        /// </summary>
        public void Stop ()
        {
            // Stats.Stop ();
            lock (Lock) {
                Monitor.Pulse (Lock);
            }
        }

        /// <summary>
        /// Executed by Thread.
        /// </summary>
        private void Runnable ()
        {
            try
            {
                LogEntry.Debug(TAG, "starting WAMP server code");

                // using (Host = .... for automagic Dispose() on Host!
                using (Host = new DefaultWampHost (Location)) {
                    // register all our services with this 'realm'
                    IWampHostedRealm realm = Host.RealmContainer.GetRealmByName ("realm1");
                    realm.TopicContainer.CreateTopicByUri ("iris.updates", true);

                    if (Services == null)
                    {
                        Services = new IrisServices();
                        Services.Register (realm);

                        CouchListener = new ProjectListener(realm);
                        CouchListener.Start();
                    }

                    // start the WAMP server
                    Host.Open ();

                    // start the push service host statistics
                    // if (Stats != null) Stats.Start ();

                    // Enter the lock.
                    lock (Lock) {
                        // Blocks, until the `Lock` is released during Stop()
                        Monitor.Wait (Lock);
                        // No calls should go here, or it will deadlock.
                    }
                }
            }
            catch (Exception ex)
            {
                LogEntry.Fatal(TAG, "Could not start ApiServer: " + ex.Message);
                LogEntry.Fatal(TAG, ex.StackTrace);
            }
        }

        //  Publish ( "iris.pin/update/" + e.PinData.GetEncodedId(), e.PinData);
        //  Publish ( "iris.pins/create", e.PinData);
        //  Publish ( "iris.pin/delete/" + e.PinData.GetEncodedId ());
        //  Publish ( "iris.patches/create", e.Patch);
        //  Publish ( "iris.patch/delete/" + e.Patch.GetEncodedNodePath ());
        //  Publish ( "iris.patch/update/" + e.Patch.GetEncodedNodePath(), e.Patch);
        //  Publish ( "iris.cues/create", e.Cue);
        //  Publish ( "iris.cue/delete/" + e.Cue._id);
        //  Publish ( "iris.cue/update/" + e.Cue._id, e.Cue);
        //  Publish ( "iris.cuelists/create", e.CueList);
        //  Publish ( "iris.cuelist/delete/" + e.CueList._id);
        //  Publish ( "iris.cuelist/update/" + e.CueList._id, e.CueList);
    }
}