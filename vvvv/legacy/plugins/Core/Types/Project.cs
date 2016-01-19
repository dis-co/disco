using System;
using System.Collections.Generic;
using Newtonsoft.Json;

using Iris.Core.Logging;

namespace Iris.Core.Types
{
    public class Project : IIrisData, IDisposable
    {
        public event EventHandler Changed;

        public string   Type   = "Project";
        public bool     Loaded = false;

        public string      _id       { get; set; }
        public string      _rev      { get; set; }
        public string      Database  { get; set; }
        public string      Name      { get; set; }
        public DateTime    Created   { get; set; }
        public DateTime    Updated   { get; set; }
        public IrisId      ClusterId { get; set; }
        public CustomViews Views     { get; set; }
 
        public void Dispose()
        {
        }

        public void Touch()
        {
            Updated = DateTime.Now;
        }

        protected virtual void OnChanged(EventArgs e)
        {
            if (Changed != null) Changed(this, e);
        }

        // json helpers

        public bool ShouldSerialize_id()
        {
            return (_id != null);
        }

        public bool ShouldSerialize_rev()
        {
            return (_rev != null);
        }

        public static Project FromJSON(string value)
        {
            return JsonConvert.DeserializeObject<Project>(value);
        }

        public string ToJSON()
        {
            return JsonConvert.SerializeObject(this);
        }

        public string ToUriPath()
        {
            return "projects/" + _id;
        }
    }
}
