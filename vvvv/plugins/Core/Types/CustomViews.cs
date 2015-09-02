using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Iris.Core.Types
{

    public class CustomViews : Dictionary<string,List<string>>, IIrisData
    {
        public void Dispose()
        {
            Clear();
        }

        public string ToJSON()
        {
            return JsonConvert.SerializeObject(this);
        }

        public string ToUriPath()
        {
            return "customviews/_design/projects/_view/all";
        }
    }
}
