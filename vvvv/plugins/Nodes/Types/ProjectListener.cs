using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Iris.Core.Types;
using Iris.Core.Couch;
using Iris.Core.Logging;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using VVVV.Core.Logging;

namespace Iris.Nodes.Types
{
    using Projects = ConcurrentDictionary<string, Project>;

    public class ProjectListener : ChangesListener
    {
        private const string TAG = "ProjectListener";

        private Projects Projects;

        public ProjectListener(Projects projects)
            : base("projects", "projects")
        {
            Projects = projects;
        }

        public override void OnStart()
        {
            Projects.Clear();

            var result = Couch.GetView("projects/_design/projects/_view/all");
            result.ToList()
                .ForEach(obj => ProcessProject(obj["doc"].ToObject<Project>()));
        }

        public override void OnStop()
        {
            Projects.Clear();
        }

        private void ProcessProject(Project project)
        {
            if(Projects.ContainsKey(project._id))
                Projects[project._id] = project;
            else
                Projects.TryAdd(project._id, project);
        }

        public override void Process(JObject data)
        {
            try
            {
                data["results"].ToList()
                    .ForEach(item => {
                            if(item["deleted"] != null)
                            {
                                Project p;
                                Projects.TryRemove(item["id"].ToObject<string>(), out p);
                            }
                            else
                            {
                                ProcessProject(item["doc"].ToObject<Project>());
                            }
                        });
            }
            catch(Exception ex)
            {
                LogEntry.Fatal(TAG, ex.Message);
                LogEntry.Fatal(TAG, ex.StackTrace);
            }
        }
    }
}
