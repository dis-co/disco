using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Iris.Core.Types;
using Iris.Core.Couch;
using Iris.Core.Logging;
using Newtonsoft.Json.Linq;

using WampSharp.V2;
using WampSharp.V2.Realm;
using WampSharp.V2.Core.Contracts;

namespace Iris.FrontEnd.Api.Listeners
{
    public class ProjectListener : ChangesListener
    {
        private const string TAG = "ProjectListener";

        private Project      CurrentProject;
        private CueListener  Cues;
        private LogListener  CueLists;
        private IWampSubject Subject;

        public ProjectListener(IWampHostedRealm realm)
            : base("projects", "projects")
        {
            Subject = realm.Services.GetSubject("iris.updates");
        }

        public new void Start()
        {
            base.Start(); // starting this listener

            var project = Couch.GetActiveProject();
            if (project == null) return;
            CurrentProject = project;
            StartListeners();
        }

        private void StopListeners()
        {
            if (Cues != null) Cues.Stop();
            if (CueLists != null) CueLists.Stop();
            Cues = null;
            CueLists = null;
        }

        private void RestartListeners()
        {
            StopListeners();
            StartListeners();
        }

        private void StartListeners()
        {
            Cues = new CueListener(Subject, CurrentProject.Database);
            CueLists = new LogListener(Subject, CurrentProject.Database);

            Cues.Start();
            CueLists.Start();
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
                var projects = new Dictionary<string, Project>();

                /// <summary>
                ///   we *RELY* on descending=true to be passed to
                ///   changes listener and discard all older changes to docs
                ///   (we're only ever interested in the newest revision)
                /// </summary>

                data["results"].ToList().ForEach(item =>
                        {
                            var project = item["doc"].ToObject<Project>();
                            if(!projects.ContainsKey(project._id))
                                projects.Add(project._id, project);
                        });

                projects.ToList()
                    .ForEach(kv => {
                            var project = kv.Value;

                            if (CurrentProject != null)
                            {
                                if (project._id == CurrentProject._id)
                                {
                                    if (!project.Loaded)
                                    {
                                        CurrentProject = null;
                                        StopListeners();
                                    }
                                }
                                else
                                {
                                    if (project.Loaded)
                                    {
                                        StopListeners();
                                        CurrentProject = project;
                                        StartListeners();
                                    }
                                }
                            }
                            else
                            {
                                if (project.Loaded)
                                {
                                    CurrentProject = project;
                                    StartListeners();
                                }
                            }

                            Publish(null, DataUri.ProjectUpdate, project);
                        });
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
