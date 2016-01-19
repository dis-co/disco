using System;
using System.Net;
using System.Linq;
using System.Collections.Generic;

using Iris.Core.Types;
using Iris.Core.Logging;
using Iris.Core.Couch;
using Iris.FrontEnd.Types;
using Iris.FrontEnd.Api.Impl;
using Iris.FrontEnd.Api.Interfaces;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using RabbitMQ.Client.Events;

namespace Iris.FrontEnd.Api.Impl
{
    public class ProjectService : IrisService, IProjectService
    {
        private const string TAG = "ProjectService";

        public ProjectService() : base(DataUri.ProjectBase)
        {
        }

        protected override void BeforeStart()
        {
        }

        protected override void OnMessage(BasicDeliverEventArgs ea)
        {
        }

        public IIrisData ListProjects(string _sid)
        {
            Projects projects = new Projects();
            var raw = Couch.GetView(projects.ToUriPath());
            raw.ToList().ForEach(obj => projects.Add(obj["doc"].ToObject<Project>()));
            return projects;
        }

        public IIrisData CreateProject(string _sid, Project project)
        {
            var resp = Couch.Post("projects", project.ToJSON());

            if(resp.Item1 == HttpStatusCode.Created ||
               resp.Item1 == HttpStatusCode.Accepted)
            {
                var parsed = JObject.Parse(resp.Item2);

                project._id  = parsed["id"].ToObject<string>();
                project._rev = parsed["rev"].ToObject<string>();

                resp = Couch.Put(project.Database);
                parsed = JObject.Parse(resp.Item2);

                if (parsed["ok"].ToObject<bool>())
                {
                    Schema.CreateViews(project.Database);
                    return project;
                }
            }

            throw IrisException.FromHttpStatusCode(resp.Item1);
        }

        public IIrisData ReadProject(string _sid, Project body)
        {
            var resp = Couch.Get("projects/" + body._id);

            if(resp.Item1 == HttpStatusCode.OK ||
               resp.Item1 == HttpStatusCode.NotModified)
                return JsonConvert.DeserializeObject<Project>(resp.Item2);

            throw IrisException.FromHttpStatusCode(resp.Item1);
        }

        public IIrisData UpdateProject(string _sid, Project project)
        {
            var resp = Couch.Put("projects/" + project._id + "?rev=" + project._rev,
                JsonConvert.SerializeObject(project));

            if (resp.Item1 == HttpStatusCode.Created ||
                resp.Item1 == HttpStatusCode.Accepted)
            {
                var parsed = JObject.Parse(resp.Item2);
                project._rev = parsed["rev"].ToObject<string>();
                return project;
            }

            throw IrisException.FromHttpStatusCode(resp.Item1);
        }

        public IIrisData DeleteProject(string _sid, Project project)
        {
            var resp = Couch.Delete(project.ToUriPath() + "?rev=" + project._rev);

            if (resp.Item1 == HttpStatusCode.OK ||
                resp.Item1 == HttpStatusCode.Accepted)
            {
                var parsed = JObject.Parse(resp.Item2);
                resp = Couch.Delete(project.Database);
                parsed = JObject.Parse(resp.Item2);

                if (parsed["ok"].ToObject<bool>())
                    return project;
            }

            throw IrisException.FromHttpStatusCode(resp.Item1);
        }
    }
}
