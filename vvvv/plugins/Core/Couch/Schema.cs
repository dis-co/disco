using System;
using System.IO;
using System.Net;
using System.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Iris.Core.Types;
using Iris.Core.Logging;

namespace Iris.Core.Couch
{
    public class Schema
    {
        public  static readonly int         CURRENT_VERSION = 7;
        private static readonly string      TAG = "SchemaMigrator";
        private                 CouchClient Couch;

        public Schema()
        {
            Couch = new CouchClient("http://localhost:5984");
        }

        public static void Migrate()
        {
            new Schema().RunMigration();
        }

        public void RunMigration()
        {
            if(HasStructure())
            {
                LogEntry.Info(TAG, "Iris application structure present.");

                if(UpgradeNeeded())
                {
                    LogEntry.Info(TAG, "Schema out of data. Upgrading.");
                    // upgrade all design documents here
                    UpgradeProjects();
                    UpgradeCluster();
                    UpgradeDatabases();
                    UpgradeSchema();
                }
                else
                {
                    LogEntry.Info(TAG, "Schema up-to-date.");
                }
            }
            else
            {
                LogEntry.Info(TAG, "Does not have Iris application structure. Creating.");
                CreateStructure();
            }
        }

        private void UpgradeProjects()
        {
            var resp = Couch.Get("projects/_design/projects");

            if(resp.Item1 == HttpStatusCode.OK ||
               resp.Item1 == HttpStatusCode.NotModified)
            {
                var old = JsonConvert.DeserializeObject<ProjectsDesign>(resp.Item2);
                var design = new ProjectsDesign();

                resp = Couch.Put(String.Format("projects/_design/projects?rev={0}", old._rev),
                                 JsonConvert.SerializeObject(design));

                if(resp.Item1 == HttpStatusCode.Created ||
                   resp.Item1 == HttpStatusCode.Accepted)
                {
                    LogEntry.Info(TAG, "[OK] updated projects design documents");
                }
                else
                {
                    LogEntry.Info(TAG, "[FAIL] could not upgrade project design docs: " + resp.Item2);
                }
            }
            else if(resp.Item1 == HttpStatusCode.NotFound)
            {
                CreateDesignDoc(new ProjectsDesign());
            }
            else
            {
                LogEntry.Info(
                    TAG, "[FAIL] could not upgrade project design docs: " +
                    resp.Item1 + " " + resp.Item2);
            }
        }

        private void UpgradeCluster()
        {
            var resp = Couch.Get("projects/_design/cluster");

            if(resp.Item1 == HttpStatusCode.OK ||
               resp.Item1 == HttpStatusCode.NotModified)
            {
                var old = JsonConvert.DeserializeObject<ClusterDesign>(resp.Item2);
                var design = new ClusterDesign();

                resp = Couch.Put(String.Format("{0}?rev={1}", design.Uri(), old._rev),
                                 JsonConvert.SerializeObject(design));

                if(resp.Item1 == HttpStatusCode.Created ||
                   resp.Item1 == HttpStatusCode.Accepted)
                {
                    LogEntry.Info(TAG, "[OK] updated cluster design documents");
                }
                else
                {
                    LogEntry.Info(TAG, "[FAIL] could not upgrade cluster design docs: " + resp.Item2);
                }
            }
            else if(resp.Item1 == HttpStatusCode.NotFound)
            {
                CreateDesignDoc(new ClusterDesign());
            }
            else
            {
                LogEntry.Info(
                    TAG, "[FAIL] could not upgrade cluster design docs: " +
                    resp.Item1 + " " + resp.Item2);
            }
        }

        private void UpgradeDatabases()
        {
            var resp = Couch.GetView("projects/_design/projects/_view/all");
            resp.ToList().ForEach(item => {
                    var project = item["doc"].ToObject<Project>();
                    UpgradeCues(project);
                    UpgradeCueLists(project);
                });
        }

        private void UpgradeCues(Project project)
        {
            var url = String.Format("{0}/_design/cues", project.Database);
            var resp = Couch.Get(url);

            var old = JsonConvert.DeserializeObject<CuesDesign>(resp.Item2);
            var design = new CuesDesign();

            resp = Couch.Put(String.Format(url+ "?rev={0}", old._rev),
                      JsonConvert.SerializeObject(design));

            if(resp.Item1 == HttpStatusCode.Created ||
               resp.Item1 == HttpStatusCode.Accepted)
            {
                LogEntry.Info(TAG, "[OK] updated cues design documents for " + project.Database);
            }
            else
            {
                LogEntry.Info(TAG, "[FAIL] could not upgrade cues design docs on " + project.Database + ": " + resp.Item2);
            }
        }

        private void UpgradeCueLists(Project project)
        {
            var url = String.Format("{0}/_design/cuelists", project.Database);

            var resp = Couch.Get(url);

            var old = JsonConvert.DeserializeObject<CueListsDesign>(resp.Item2);
            var design = new CueListsDesign();

            resp = Couch.Put(String.Format(url+ "?rev={0}", old._rev),
                      JsonConvert.SerializeObject(design));

            if(resp.Item1 == HttpStatusCode.Created ||
               resp.Item1 == HttpStatusCode.Accepted)
            {
                LogEntry.Info(TAG, "[OK] updated cuelists design documents for " + project.Database);
            }
            else
            {
                LogEntry.Info(TAG, "[FAIL] could not upgrade cuelists design docs on " + project.Database + ": " + resp.Item2);
            }
        }

        private void UpgradeSchema()
        {
            var resp = Couch.Get("projects/schema");

            var old = JsonConvert.DeserializeObject<SchemaDoc>(resp.Item2);
            var schema = new SchemaDoc {
                Version = CURRENT_VERSION
            };

            resp = Couch.Put(String.Format("projects/schema?rev={0}", old._rev),
                      JsonConvert.SerializeObject(schema));

            if(resp.Item1 == HttpStatusCode.Created ||
               resp.Item1 == HttpStatusCode.Accepted)
            {
                LogEntry.Info(TAG, "[OK] updated schema document");
            }
            else
            {
                LogEntry.Info(TAG, "[FAIL] could not upgrade schema document");
            }
        }

        public bool UpgradeNeeded()
        {
            var resp = Couch.Get("projects/schema");
            if(resp.Item1 == HttpStatusCode.OK ||
               resp.Item1 == HttpStatusCode.NotModified)
            {
                var current = JsonConvert.DeserializeObject<SchemaDoc>(resp.Item2);
                return current < CURRENT_VERSION;
            }

            return true;
        }

        public bool HasStructure()
        {
            var resp = Couch.Get("projects");
            return resp.Item1 == HttpStatusCode.OK;
        }

        public void CreateStructure()
        {
            var req = Couch.Put("projects");
            var json = JObject.Parse(req.Item2);
            if (json["ok"].ToObject<bool>())
            {
                CreateDesignDoc(new ProjectsDesign());
                CreateDesignDoc(new ClusterDesign());

                LogEntry.Debug(TAG, "Project database successufully created on " + Host.IP);
            }
            else LogEntry.Debug(TAG, String.Format("Project database could not be created on {0}. Reason: {1}", Host.IP, req.Item1));
        }

        private void CreateDesignDoc(DesignDoc design)
        {
            var schema = new SchemaDoc {
                Version = CURRENT_VERSION
            };

            Couch.Post("projects", JsonConvert.SerializeObject(design));
            Couch.Post("projects", JsonConvert.SerializeObject(schema));
        }

        private int RetrieveLinkerTimestamp()
        {
            var filePath = System.Reflection.Assembly.GetCallingAssembly().Location;

            const int c_PeHeaderOffset = 60;
            const int c_LinkerTimestampOffset = 8;

            var b = new byte[2048];

            Stream s = null;

            try
            {
                s = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                s.Read(b, 0, 2048);
            }
            finally
            {
                if (s != null) s.Close();
            }

            var i = BitConverter.ToInt32(b, c_PeHeaderOffset);
            return BitConverter.ToInt32(b, i + c_LinkerTimestampOffset);
        }

        public static void CreateViews (string database)
        {
            var Couch = new CouchClient("http://localhost:5984");
            Couch.Post(database, JsonConvert.SerializeObject(new CuesDesign()));
            Couch.Post(database, JsonConvert.SerializeObject(new CueListsDesign()));
        }
    }
}
