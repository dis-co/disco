using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;

using Iris.Core.Events;

namespace Iris.Core.Types
{
    public class Cues : List<Cue>, IIrisData
    {
        public event EventHandler<CueEventArgs> CueAdded;
        public event EventHandler<CueEventArgs> CueRemoved;
        public event EventHandler<CueEventArgs> CueChanged;

        public string Database { get; set; }

        private Dictionary<string, IDisposable> observers =
            new Dictionary<string, IDisposable> ();

        public Cues()
        {
        }

        public Cues(string db)
        {
            Database = db;
        }

        public bool Initialized()
        {
            return Database != null;
        }

        public void Dispose()
        {
            observers.ToList().ForEach(reg => reg.Value.Dispose());
            observers.Clear();
            ForEach(cue => cue.Dispose());
            Clear();
        }

        public void RegisterAll()
        {
            ForEach(cue => Register(cue));
        }

        private void Register(Cue cue)
        {
            var subscription = cue.GetObservable()
                .Subscribe(args => {
                        if(CueChanged != null) {
                            CueChanged(null, args);
                        }
                    });
            observers.Add(cue._id, subscription);
        }

        private void Unregister(Cue cue)
        {
            if(observers.ContainsKey(cue._id))
            {
                observers[cue._id].Dispose();
                observers.Remove(cue._id);
            }
        }

        public Cue Create(string _sid, string name, List<CueValue> values)
        {
            var cue = Cue.Create(name, values);
            Add(_sid, cue);
            return cue;
        }

        public void Add(string _sid, Cue item)
        {
            Register(item);
            base.Add(item);
            if (CueAdded != null)
                CueAdded(null, new CueEventArgs(_sid, item));
        }

        public Cue Find(string id)
        {
            return Find(cue => cue._id == id);
        }

        public Cue FindByName(string name)
        {
            return Find(cue => cue.Name == name);
        }

        public void Remove(string _sid, Cue item)
        {
            Unregister(item);
            Remove(item);

            if (CueRemoved != null)
                CueRemoved(null, new CueEventArgs(_sid, item));
        }

        public void RemoveCue(string _sid, int idx)
        {
            var cue = this.ElementAt(idx);
            if(cue == null) return;

            Unregister(cue);
            Remove(cue);

            if (CueRemoved != null)
                CueRemoved(null, new CueEventArgs(_sid, cue));
        }

        public string ToJSON()
        {
            return JsonConvert.SerializeObject(this);
        }

        public string ToUriPath()
        {
            return Database + "/_design/cues/_view/all";
        }
    }
}
