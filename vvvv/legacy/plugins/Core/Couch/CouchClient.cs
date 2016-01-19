using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

using Iris.Core.Types;

namespace Iris.Core.Couch
{
    using Newtonsoft.Json.Linq;
    using System.Net;
    using System.Threading;
    using IrisHttpResponse = Tuple<HttpStatusCode, string>;

    public class CouchClient
    {
        public uint TimeOut = 60000;

        private HttpClient Http;
        private string     Host;

        public CouchClient(string host)
        {
            Http = new HttpClient();
            Init(host);
        }

        public CouchClient(string host, string user, string pass)
        {
            var handler = new HttpClientHandler();
            handler.Credentials = new NetworkCredential(user, pass);

            Http = new HttpClient(handler);
            Init(host);
        }

        private void Init(string host)
        {
            Host = host; // we have to construct an absolute uri later for our CouchUri hack to work
            Http.Timeout = TimeSpan.FromMilliseconds(TimeOut);
            Http.DefaultRequestHeaders.Accept.Clear();
            Http.DefaultRequestHeaders.Accept
                .Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        //  _______ _______ _______ ______
        // |   |   |_     _|_     _|   __ \
        // |       | |   |   |   | |    __/
        // |___|___| |___|   |___| |___|   machinery
        //

        protected async Task<IrisHttpResponse> GenericRequest(HttpMethod meth, string path, string json)
        {
            HttpResponseMessage response;
            string body;

            var token = new CancellationTokenSource();
            var req = new HttpRequestMessage(meth, CouchUri.Create(Host + "/" + path));

            if(json != String.Empty)
                req.Content = new StringContent(json, Encoding.UTF8, "application/json");

            response = await Http.SendAsync(req, token.Token);
            body = await response.Content.ReadAsStringAsync();

            return Tuple.Create(response.StatusCode, body);
        }

        public IrisHttpResponse Get(string _id)
        {
            return GenericRequest(HttpMethod.Get, _id, String.Empty).Result;
        }

        public IrisHttpResponse Delete(string _id)
        {
            return GenericRequest(HttpMethod.Delete, _id, String.Empty).Result;
        }

        public IrisHttpResponse Post(string _id, string json)
        {
            return GenericRequest(HttpMethod.Post, _id, json).Result;
        }

        public IrisHttpResponse Put(string _id)
        {
            return GenericRequest(HttpMethod.Put, _id, String.Empty).Result;
        }

        public IrisHttpResponse Put(string _id, string json)
        {
            return GenericRequest(HttpMethod.Put, _id, json).Result;
        }

        //       _______ __   __ __
        //      |   |   |  |_|__|  |
        //      |   |   |   _|  |  |
        // http.|_______|____|__|__|
        //

        public JArray AllDBs()
        {
            var resp = Get("_all_dbs");
            if(resp.Item1 == HttpStatusCode.OK)
                return JArray.Parse(resp.Item2);
            return new JArray();
        }

        public JArray GetView(string view)
        {
            var resp = Get(view + "?include_docs=true");
            if(resp.Item1 == HttpStatusCode.OK)
            {
                JObject result = JObject.Parse(resp.Item2);
                return (JArray)result["rows"];
            }
            return new JArray();
        }

        public Project GetActiveProject()
        {
            var req = GetView("projects/_design/projects/_view/active");

            return (req.Count > 0)
                ? req.ToList()[0]["doc"].ToObject<Project>()
                : null;
        }

    }
}
