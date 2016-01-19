using System;
using System.Collections.Generic;
using System.Reactive.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using Iris.Core.Events;
using Iris.Core.Logging;

namespace Iris.Core.Types
{
    public class Cue : IIrisData
    {
        public const string TC_FILE_NAME = "IrisTC";
        public const string HOSTS_FILE_NAME = "IrisHosts";

        public event EventHandler<CueEventArgs> Change;

        private List<IDisposable> Subscriptions =
            new List<IDisposable>();


        /// <summary>
        /// Get/set the ID of this Cue. Read-only.
        /// </summary>
        /// <value>The identifier.</value>
        public string _id     { get; set; }
        public string _rev    { get; set; }

        public string Type    = "Cue";
        public string Project { get; set; }

        /// <summary>
        /// The name of this Cue.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        public Int64 ExecFrame { get; set; }

        public List<string> Tags  { get; set; }
        public List<string> Hosts { get; set; }

        /// <summary>
        /// Trigger this cue.
        /// </summary>
        public bool Trigger { get; set; }

        /// <summary>
        /// List of CueValues to execute when this Cue is triggered.
        /// </summary>
        /// <value>The values.</value>
        public List<CueValue> Values { get; set; }

        public Cue()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Iris.Core.Types.Cue"/>
        /// class.  For automatic serialization/deserialization to work, the
        /// constructor has to
        ///
        /// a) be public
        /// b) be unique (there can only be one constructor per class)
        /// c) must initialize all fields, even if they are null.
        /// </summary>
        /// <param name="name">Name.</param>
        /// <param name="values">Values.</param>
        public Cue(string name, List<CueValue> values)
        {
            Name   = name;
            Values = values;
        }

        /// <summary>
        /// Create a CueList with given name and list of Cue IDs. This factory
        /// method will auto-generate an ID for the CueList.
        /// </summary>
        /// <param name="name">Name.</param>
        /// <param name="values">list of CueValues.</param>
        public static Cue Create(string name, List<CueValue> values)
        {
            return new Cue(name, values);
        }

        public Cue Update(string _sid, Cue newCue)
        {
            Name = newCue.Name;
            Values = newCue.Values;

            if(Change != null)
                Change(null, new CueEventArgs(_sid, this));

            return this;
        }

        public Cue Update(CueEventArgs args)
        {
            Name = args.Cue.Name;
            Values = args.Cue.Values;

            if(Change != null)
                Change(null, args);

            return this;
        }

        public IObservable<CueEventArgs> GetObservable()
        {
            return Observable.FromEvent<CueEventArgs>(
                handler => Change += (o,e) => handler(e),
                handler => Change -= (o,e) => handler(e));
        }

        public void Subscribe(Action<Cue> a)
        {
            var Subscription = GetObservable()
                .Subscribe(args => a.Invoke(this));
            Subscriptions.Add(Subscription);
        }

        public static Cue FromJson(string json)
        {
            return JsonConvert.DeserializeObject<Cue>(json);
        }

        public string ToJSON()
        {
            return JsonConvert.SerializeObject(this);
        }

        public string ToUriPath()
        {
            return Project + "/" + _id;
        }

        public void Dispose()
        {
            Subscriptions.ForEach(sub => sub.Dispose());
            Subscriptions.Clear();
        }

        public bool ShouldSerialize_id()
        {
            return !(_id == null);
        }

        public bool ShouldSerialize_rev()
        {
            return !(_rev == null);
        }
    }
}
