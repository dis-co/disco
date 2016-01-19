using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Iris.Core.Types;
using Iris.Core.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Iris.Core.Couch
{
    public abstract class ChangesListener
    {
        private       bool   RunLoop = true;
        private       Thread ListenerLoop;
        private       uint   LastSeq = 0;
        private       string Database;
        private       string ItemName;

        private       int    Retries = 0;
        private const int    MAX_RETRIES = 5;
        private const string TAG = "ChangesListener";
        private const string url = "http://localhost:5984/";
        private const string UrlFormat =
            "{0}/_changes?include_docs=true&since={1}&feed=longpoll&timeout={2}&filter={3}/{3}&descending=true";

        protected CouchClient Couch;

        public abstract void OnStart();
        public abstract void OnStop();
        public abstract void Process(JObject data);

        protected ChangesListener(string database, string item)
        {
            Database = database;
            ItemName = item;
            Couch = new CouchClient(url);
        }

        public void Start()
        {
            ListenerLoop = new Thread(InnerProcess);
            ListenerLoop.Start();
        }

        private void InnerProcess()
        {
            OnStart();

            while(RunLoop)
            {
                if(Retries == MAX_RETRIES) break;

                Thread.Sleep(Retries * 1000);

                var resource = String.Format(UrlFormat, Database, LastSeq, Couch.TimeOut - 10000, ItemName);

                LogEntry.Debug(TAG, "ChangesListener running for " + Database + " resource");

                try
                {
                    var resp = Couch.Get(resource);
                    if (resp.Item1 == HttpStatusCode.OK)
                    {
                        var json = JObject.Parse(resp.Item2);
                        LastSeq = json["last_seq"].ToObject<uint>();

                        /// <summary>
                        /// Process json only if RunLoop is still intended. This
                        /// solves a situation where a loop was already
                        /// cancelled, but took some time to complete the
                        /// request cycle.
                        /// </summary>
                        if (LastSeq != 0 && RunLoop) Process(json);

                        Retries = 0;

                        continue; // all good. keep rollin'
                    }

                    if(resp.Item1 == HttpStatusCode.NotFound)
                    {
                        LogEntry.Debug(TAG, "Could not find database in changes feed request. Aborting.");
                    }
                    else
                    {
                        LogEntry.Debug(TAG, "Changes feed request failed. " + resp.Item1 + " Aborting.");
                    }
                }
                catch(AggregateException ex)
                {
                    LogEntry.Debug(TAG, "Request to " + Database + " changes feed for item " + ItemName + " failed: " + ex.InnerException.Message);
                }
                catch(Exception ex)
                {
                    LogEntry.Debug(TAG, "Request to " + Database + " changes feed for item " + ItemName + " failed: " + ex.Message);
                }

                Retries += 1;
                LogEntry.Debug(TAG, "Retries left: " + Retries);
            }

            if(Retries == MAX_RETRIES)
                LogEntry.Debug(TAG, "Changes feed listener on " + Database + " for " + ItemName + " timeed out. Aborted.");

            OnStop();
        }

        public void Stop()
        {
            RunLoop = false;
        }
    }
}
