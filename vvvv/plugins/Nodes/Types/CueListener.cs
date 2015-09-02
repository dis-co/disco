using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Iris.Core.Couch;
using Iris.Core.Types;
using Iris.Core.Logging;

using VVVV.Core.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Iris.Nodes.Types
{
    using CuesDict = ConcurrentDictionary<string,Cue>;

    public class CueListener : ChangesListener
    {
        private       ILogger  Logger;
        private const string   TAG = "CueListener";
        private       CuesDict Cues;
        private       Project  Project;

        public CueListener(ILogger logger, Project project, CuesDict cues)
            : base(project.Database, "cues")
        {
            if (project == null || project.Database == null)
                throw new Exception ("Must have a valid project/database");

            if (cues == null)
                throw new Exception ("Must have a cues data structure");
            
            Cues     = cues;
            Logger   = logger;
            Project  = project;
        }

        public override void OnStart()
        {
            try
            {
                if(Cues.Count > 0) Cues.Clear();

                var result = Couch
                    .GetView(String.Format("{0}/_design/cues/_view/all",
                                           Project.Database));

                result.ToList().ForEach(ProcessCue);
            }
            catch(Exception ex)
            {
                Logger.Log(LogType.Debug, ex.Message);
                Logger.Log(LogType.Debug, ex.StackTrace);
            }
        }

        public override void OnStop()
        {
        }

        private void ProcessCue(JToken item)
        {
            if(item["deleted"] == null)
            {
                var cue = item["doc"].ToObject<Cue>();
                if(Cues.ContainsKey(cue._id))
                {
                    Cues[cue._id] = cue;
                }
                else
                {
                    Cues.TryAdd(cue._id, cue);
                }
            }
            else
            {
                Cue cue; Cues.TryRemove(item["id"].ToObject<string>(), out cue);
            }
        }

        public override void Process(JObject data)
        {
            try
            {
                data["results"].ToList().ForEach(ProcessCue);
            }
            catch(Exception ex)
            {
                LogEntry.Fatal(TAG, ex.Message);
            }
        }
    }
}
