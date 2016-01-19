using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;

using Iris.Core.Events;

namespace Iris.Core.Types
{
    public class CueLists : List<CueList>, IIrisData
    {
        public event EventHandler<CueListEventArgs> CueListAdded;
        public event EventHandler<CueListEventArgs> CueListRemoved;
        public event EventHandler<CueListEventArgs> CueListChanged;

        public string Database { get; set; }

        private Dictionary<string, IDisposable> observables =
            new Dictionary<string, IDisposable> ();

        public CueLists(string db)
        {
            Database = db;
        }

        public void Dispose()
        {
            observables.ToList().ForEach(reg => reg.Value.Dispose());
            observables.Clear();

            ForEach(cuelist => cuelist.Dispose());
            Clear();
        }

        public void RegisterAll()
        {
            ForEach(Register);
        }

        private void Register(CueList list)
        {
            var subscription = list.GetObservable()
                .Subscribe(args => {
                        if(CueListChanged != null)
                            CueListChanged(null, args);
                    });
            observables.Add(list._id, subscription);
        }

        private void Unregister(CueList list)
        {
            if(observables.ContainsKey(list._id))
            {
                observables[list._id].Dispose();
                observables.Remove(list._id);
            }
        }

        public CueList Create(CueList source)
        {
            var cuelist = CueList.Create(source.Name, source.Cues);
            Add(null, cuelist);
            return cuelist;
        }

        public void Add(string _sid, CueList item)
        {
            Register(item);
            base.Add(item);
            if (CueListAdded != null)
                CueListAdded(null, new CueListEventArgs(_sid, item));
        }

        public CueList Find(string id)
        {
            return base.Find(cl => cl._id == id);
        }

        public void Remove(string _sid, CueList item)
        {
            if(item == null) return;

            Unregister(item);
            Remove(item);

            if (CueListRemoved != null)
                CueListRemoved(null, new CueListEventArgs(_sid, item));
        }

        public void RemoveAt(string _sid, int idx)
        {
            var cuelist = base[idx];
            if(cuelist == null) return;

            Unregister(cuelist);
            Remove(cuelist);

            if (CueListRemoved != null)
                CueListRemoved(null, new CueListEventArgs(_sid, cuelist));
        }

        public string ToJSON()
        {
            return JsonConvert.SerializeObject(this);
        }

        public string ToUriPath()
        {
            return Database + "/_design/cuelists/_view/all";
        }
    }
}
