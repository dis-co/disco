using System;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;

using Iris.Core.Types;
using Iris.Core.Logging;
using Iris.FrontEnd.Types;
using Iris.FrontEnd.Api.Impl;
using Iris.FrontEnd.Api.Interfaces;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RabbitMQ.Client.Events;

namespace Iris.FrontEnd.Api.Impl
{
    public class CueService : IrisService, ICueService
    {
        private const string TAG = "CueService";

        private MemoryMappedFile TcFile;
        private MemoryMappedViewAccessor TcAccessor;

        private Int64 ExecFrame;

        public CueService() : base(DataUri.CueBase)
        {
        }

        protected override void BeforeStart()
        {
        }

        protected override void OnMessage(BasicDeliverEventArgs ea)
        {
            Publish(DataUri.CueUpdate, Encoding.UTF8.GetString(ea.Body));
        }

        public IIrisData ListCues(string _sid) // add project id parameter
        {
            var project = Couch.GetActiveProject(); 
            if (project == null) throw IrisException.ObjectNotFound();

            var cues = new Cues(project.Database);
            var list = Couch.GetView(cues.ToUriPath());
            list.ToList().ForEach(obj => cues.Add(obj["doc"].ToObject<Cue>()));
            return cues;
        }

        public IIrisData CreateCue(string _sid, Cue cue)
        {
            var resp = Couch.Post(cue.Project, cue.ToJSON());
            if(resp.Item1 == HttpStatusCode.Created || 
               resp.Item1 == HttpStatusCode.Accepted)
            {
                var json = JObject.Parse(resp.Item2);
                cue._id = json["id"].ToObject<string>();
                cue._rev = json["rev"].ToObject<string>();
                return cue;
            }
            throw IrisException.FromHttpStatusCode(resp.Item1);
        }

        public IIrisData ReadCue(string _sid, Cue cue)
        {
            var resp = Couch.Get(cue.ToUriPath());
            if (resp.Item1 == HttpStatusCode.OK ||
                resp.Item1 == HttpStatusCode.NotModified)
            {
                return Cue.FromJson(resp.Item2);
            }
            throw IrisException.FromHttpStatusCode(resp.Item1);
        }

        public IIrisData UpdateCue(string _sid, Cue cue)
        {
            if (cue.Trigger)
            {
                if(TcFile == null)
                {
                    try {
                        TcFile = MemoryMappedFile.OpenExisting(Cue.TC_FILE_NAME);
                        TcAccessor = TcFile.CreateViewAccessor(0, sizeof(ulong), MemoryMappedFileAccess.Read);
                    }
                    catch(Exception ex)
                    {
                        LogEntry.Info("CueService",
                                      "Could not open shared memory segment for fetching ExecFrame: " +
                                      ex.Message);
                    }
                }

                if(TcAccessor != null)
                {
                    TcAccessor.Read<Int64>(0, out ExecFrame);
                    cue.ExecFrame = ExecFrame;
                }
                else
                {
                    cue.ExecFrame = -1;
                }

                var oldhosts = cue.Hosts;

                try
                {
                    using (var mmf = MemoryMappedFile.OpenExisting(Cue.HOSTS_FILE_NAME))
                    {
                        using (var stream = mmf.CreateViewStream())
                        {
                            using (var binReader = new BinaryReader(stream))
                            {
                                var bytearr =  binReader
                                    .ReadChars((int)stream.Length);
                                var hosts = JsonConvert
                                    .DeserializeObject<List<string>>(new String(bytearr));

                                cue.Hosts = hosts;
                                Console.WriteLine("cue after " + cue.ToJSON());
                            }
                        }
                    }
                }
                catch {}
                
                Call(DataUri.UpdateAction, cue.ToJSON());
                cue.Trigger = false;
                cue.ExecFrame = -1;
                cue.Hosts = oldhosts;
                return cue;
            }

            var resp = Couch.Put(cue.ToUriPath(), cue.ToJSON());
            if(resp.Item1 == HttpStatusCode.Created ||
               resp.Item1 == HttpStatusCode.Accepted)
            {
                var obj = JObject.Parse(resp.Item2);
                cue._rev = obj["rev"].ToObject<string>();
                return cue;
            }

            throw IrisException.FromHttpStatusCode(resp.Item1);
        }

        public IIrisData DeleteCue(string _sid, Cue cue)
        {
            var resp = Couch.Delete(cue.ToUriPath() + "?rev=" + cue._rev);
            if(resp.Item1 == HttpStatusCode.OK ||
               resp.Item1 == HttpStatusCode.Accepted) return cue;
            throw IrisException.FromHttpStatusCode(resp.Item1);
        }
    }
}
