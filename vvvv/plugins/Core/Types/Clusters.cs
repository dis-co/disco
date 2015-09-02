using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Iris.Core.Types
{
    public class Clusters : List<Cluster>, IIrisData, IDisposable
    {
        public void Dispose()
        {
        }

        public string ToUriPath()
        {
            return "projects/_design/cluster/_view/all";
        }

        public string ToJSON()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}