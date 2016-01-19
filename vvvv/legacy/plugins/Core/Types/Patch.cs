using System;
using System.Collections.Generic;
using System.Net;
using System.Reactive.Linq;

using Newtonsoft.Json;
using Iris.Core.Events;
using Iris.Core.Logging;

namespace Iris.Core.Types
{
    public class Patch : IIrisPatch
    {
        public IrisId    Id       { get; set; }
        public NodePath  NodePath { get; set; }
        public IrisIP    HostIP   { get; set; }
        public IrisId    HostId   { get; set; }
        public Name      HostName { get; set; }
        public Name      Name     { get; set; }
        public FilePath  FilePath { get; set; }
        public IIrisPins Pins     { get; set; }

        public event EventHandler<PatchEventArgs> Change;

        private List<IDisposable> Subscriptions =
            new List<IDisposable>();

        public Patch(IrisId id, NodePath nodepath, Name name, FilePath filepath, Pins pins)
        {
            Id       = id.Equals(null) ? new IrisId() : id;
            Pins     = pins;
            Name     = name;
            HostIP   = Host.IP;
            HostId   = Host.HostId;
            HostName = Host.HostName;
            NodePath = nodepath;
            FilePath = filepath;
        }

        public void Dispose()
        {
            Subscriptions.ForEach(sub => sub.Dispose());
            Pins.Dispose();
        }

        public Name GetName()
        {
            return Name;
        }

        public IrisId GetId()
        {
            return Id;
        }

        public NodePath GetNodePath()
        {
            return NodePath;
        }

        public IIrisPins GetPins()
        {
            return Pins;
        }

        public bool HasPin(IrisId id)
        {
            return Pins.HasPin(id);
        }

        public bool HasPin(NodePath nodepath)
        {
            return Pins.HasPin(nodepath);
        }

        public IIrisPin FindPin(IrisId id)
        {
            return Pins.Find(id);
        }

        public IIrisPin FindPin(NodePath nodepath)
        {
            return Pins.Find(nodepath);
        }

        public void RemovePin(IIrisPin pin)
        {
            Pins.Remove(pin);
        }

        public void Update(IIrisPatch patch)
        {
            Name = patch.GetName();
            if(Change != null)
                Change(null, new PatchEventArgs(null, this));
        }

        public void AddPin(IIrisPin pin)
        {
            Pins.Add(pin);
        }

        public string GetEncodedNodePath()
        {
            return WebUtility.UrlEncode(Id.ToString()).ToLower();
        }

        public IObservable<PatchEventArgs> GetObservable()
        {
            return Observable.FromEvent<PatchEventArgs>(
                handler => Change += (o,e) => handler(e),
                handler => Change -= (o,e) => handler(e));
        }

        public void Subscribe(Action<IIrisPatch> a)
        {
            var Subscription = GetObservable()
                .Subscribe(args => a.Invoke(this));
            Subscriptions.Add(Subscription);
        }

        public string ToJSON()
        {
            return JsonConvert.SerializeObject(this);
        }

        public string ToUriPath()
        {
            return String.Empty;
        }
    }
}