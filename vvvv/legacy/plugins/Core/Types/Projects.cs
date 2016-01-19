using System.Collections.Generic;
using Newtonsoft.Json;

namespace Iris.Core.Types
{
    public class Projects : List<Project>, IIrisData
    {
        public void Dispose()
        {
            ForEach(p => p.Dispose());
            Clear();
        }

        public string ToJSON()
        {
            return JsonConvert.SerializeObject(this);
        }

        public string ToUriPath()
        {
            return "projects/_design/projects/_view/all";
        }
    }
}