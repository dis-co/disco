using System;
using System.Linq;
using System.Reactive.Linq;
using System.Collections.Generic;

using Newtonsoft.Json;
using Iris.Core.Events;
using Iris.Core.Logging;

namespace Iris.Core.Types
{
    public class CueList : IIrisData, IDisposable
    {
        private List<IDisposable> Subscriptions =
            new List<IDisposable>();

        public event EventHandler<CueListEventArgs> Change;

        public string _id     { get; set; }
        public string _rev    { get; set; }

        public string Type    = "CueList";
        public string Project { get; set; }

        public string Name    { get; set; }

        public List<string> Cues { get; set; }

        public CueList()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Iris.Core.Types.CueList"/> class.
        /// Constructor is marked private, because we want to have control over how these
        /// values are being constructed.
        /// Specifically, we either construct a new value, in which case we generate an ID
        /// for it, or we de-serialize JSON (e.g. from disk) where we set the ID to that
        /// contained in the JSON.
        /// </summary>
        /// <param name="id">Identifier.</param>
        /// <param name="name">Name.</param>
        /// <param name="cues">Cues.</param>
        public CueList(string name, List<string> cues)
        {
            Name = name;
            Cues = cues;
        }

        public void Update(string _sid, CueList CueList)
        {
            Name = CueList.Name;
            Cues = CueList.Cues;
            if(Change != null)
                Change(null, new CueListEventArgs(_sid, this));
        }

        public void Update(string _sid, Cues cues)
        {
            Cues = (List<string>)cues.Select(cue => cue._id);
            if(Change != null)
                Change(null, new CueListEventArgs(_sid, this));
        }

        public void Update(CueListEventArgs args)
        {
            Name = args.CueList.Name;
            Cues = args.CueList.Cues;
            if(Change != null)
                Change(null, args);
        }

        public IObservable<CueListEventArgs> GetObservable()
        {
            return Observable.FromEvent<CueListEventArgs>(
                handler => Change += (o,e) => handler(e),
                handler => Change -= (o,e) => handler(e));
        }

        public void Subscribe(Action<CueList> a)
        {
            var Subscription = GetObservable()
                .Subscribe(args => a.Invoke(this));
            Subscriptions.Add(Subscription);
        }

        public void Dispose()
        {
            Subscriptions.ForEach(sub => sub.Dispose());
            Subscriptions.Clear();
        }

        public static CueList FromJson(string json)
        {
            return JsonConvert.DeserializeObject<CueList>(json);
        }

        public string ToJSON()
        {
            return JsonConvert.SerializeObject(this);
        }

        public string ToUriPath()
        {
            return Project + "/" + _id;
        }

        public bool ShouldSerialize_id()
        {
            return !(_id == null);
        }

        public bool ShouldSerialize_rev()
        {
            return !(_rev == null);
        }

        /// <summary>
        /// Create a CueList with given name and list of Cue IDs. This factory method
        /// will auto-generate an ID for the CueList.
        /// </summary>
        /// <param name="name">Name.</param>
        /// <param name="cues">Cues.</param>
        public static CueList Create(string name, List<string> cues)
        {
            return new CueList(name, cues);
        }
    }
}
