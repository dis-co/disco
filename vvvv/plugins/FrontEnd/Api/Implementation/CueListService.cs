using System.Linq;
using System.Net;

using Iris.Core.Types;
using Iris.FrontEnd.Types;
using Iris.FrontEnd.Api.Impl;
using Iris.FrontEnd.Api.Interfaces;

using RabbitMQ.Client.Events;
using Newtonsoft.Json.Linq;

namespace Iris.FrontEnd.Api.Impl
{
    public class CueListService : IrisService, ICueListService
    {
        public CueListService() : base(DataUri.CueListBase)
        {
        }

        protected override void BeforeStart()
        {
        }

        protected override void OnMessage(BasicDeliverEventArgs ea)
        {
        }

        public IIrisData ListCueLists(string _sid)
        {
            var project = Couch.GetActiveProject();
            if (project == null) throw IrisException.ObjectNotFound();

            var cuelists = new CueLists(project.Database);
            var list = Couch.GetView(cuelists.ToUriPath());
            list.ToList().ForEach(obj =>
            {
                cuelists.Add(obj["doc"].ToObject<CueList>());
            });
            return cuelists;
        }

        public IIrisData CreateCueList(string _sid, CueList list)
        {
            var resp = Couch.Post(list.Project, list.ToJSON());
            if (resp.Item1 == HttpStatusCode.Created ||
                resp.Item1 == HttpStatusCode.Accepted)
            {
                var json = JObject.Parse(resp.Item2);
                list._id = json["id"].ToObject<string>();
                list._rev = json["rev"].ToObject<string>();
                return list;
            }
            throw IrisException.FromHttpStatusCode(resp.Item1);
        }

        public IIrisData ReadCueList(string _sid, CueList list)
        {
            var resp = Couch.Get(list.ToUriPath());
            if(resp.Item1 == HttpStatusCode.OK ||
               resp.Item1 == HttpStatusCode.NotModified)
                return CueList.FromJson(resp.Item2);
            throw IrisException.FromHttpStatusCode(resp.Item1);
        }

        public IIrisData UpdateCueList(string _sid, CueList list)
        {
            var resp = Couch.Put(list.ToUriPath(), list.ToJSON());
            if(resp.Item1 == HttpStatusCode.Created ||
               resp.Item1 == HttpStatusCode.Accepted)
            {
                var obj = JObject.Parse(resp.Item2);
                list._rev = obj["rev"].ToObject<string>();
                return list;
            }
            throw IrisException.FromHttpStatusCode(resp.Item1);
        }

        public IIrisData DeleteCueList(string _sid, CueList list)
        {
            var resp = Couch.Delete(list.ToUriPath() + "?rev=" + list._rev);
            if(resp.Item1 == HttpStatusCode.OK ||
               resp.Item1 == HttpStatusCode.Accepted)
                return list;
            throw IrisException.FromHttpStatusCode(resp.Item1);
        }
    }
}

