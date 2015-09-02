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
    using CueListsDict = ConcurrentDictionary<string,CueList>;

    public class CueListListener : ChangesListener
    {
        private       ILogger      Logger;
        private const string       TAG = "CueListListener";
        private       CueListsDict CueLists;
        private       Project      Project;

        public CueListListener(ILogger logger, Project project, CueListsDict lists)
            : base(project.Database, "cuelists")
        {
            CueLists = lists;
            Logger   = logger;
            Project  = project;
        }

        public override void OnStart()
        {
            try
            {
                if(CueLists.Count > 0) CueLists.Clear();

                var result = Couch
                    .GetView(String.Format("{0}/_design/cuelists/_view/all",
                                           Project.Database));

                result.ToList().ForEach(ProcessCueList);
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

        private void ProcessCueList(JToken item)
        {
            if(item["deleted"] == null)
            {
                var cuelist = item["doc"].ToObject<CueList>();
                if(CueLists.ContainsKey(cuelist._id))
                {
                    CueLists[cuelist._id] = cuelist;
                }
                else
                {
                    CueLists.TryAdd(cuelist._id, cuelist);
                }
            }
            else
            {
                CueList list;
                CueLists.TryRemove(item["id"].ToObject<string>(), out list);
            }
        }

        public override void Process(JObject data)
        {
            try
            {
                data["results"].ToList().ForEach(ProcessCueList);
            }
            catch(Exception ex)
            {
                LogEntry.Fatal(TAG, ex.Message);
            }
        }
    }
}
