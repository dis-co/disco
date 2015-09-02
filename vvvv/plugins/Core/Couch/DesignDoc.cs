using System;
using Newtonsoft.Json;

namespace Iris.Core.Couch
{
    public abstract class DesignDoc
    {
        public string _id      { get; set; }
        public string _rev     { get; set; }

        public object views;
        public object filters;
        public object show;
        public object list;
        public object update;
        public object validate;

        public abstract string Uri();

        public string ToJSON()
        {
            return JsonConvert.SerializeObject(this);
        }

        public bool ShouldSerialize_id()
        {
            return _id != null;
        }

        public bool ShouldSerialize_rev()
        {
            return _rev != null;
        }

        public bool ShouldSerializeviews()
        {
            return views != null;
        }

        public bool ShouldSerializefilter()
        {
            return filters != null;
        }

        public bool ShouldSerializeshow()
        {
            return show != null;
        }

        public bool ShouldSerializelist()
        {
            return list != null;
        }

        public bool ShouldSerializeupdate()
        {
            return update != null;
        }

        public bool ShouldSerializevalidate()
        {
            return validate != null;
        }
    }
}