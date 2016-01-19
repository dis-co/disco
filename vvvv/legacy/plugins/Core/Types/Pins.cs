using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Iris.Core.Types
{
    public class Pins : List<IIrisPin>,  IIrisPins, IIrisData
    {
        public event EventHandler OnAdd;
        public event EventHandler OnRemove;
        public event EventHandler OnReset;

        public new void Add(IIrisPin item)
        {
            base.Add(item);
            if (OnAdd != null) OnAdd(item, EventArgs.Empty);
        }

        public IIrisPin Find(IrisId id)
        {
            return base.Find(pin => pin.GetId() == id);
        }

        public IIrisPin Find(NodePath nodepath)
        {
            return base.Find(pin => pin.GetNodePath() == nodepath);
        }

        public bool HasPin(IrisId id)
        {
            return this.Any<IIrisPin>(p => p.GetId() == id);
        }

        public bool HasPin(NodePath np)
        {
            return this.Any(p => p.GetNodePath() == np);
        }

        public void Each(Action<IIrisPin> fun)
        {
            ForEach(fun);
        }

        public void Remove(NodePath nodepath)
        {
            var pin = Find(nodepath);
            if (pin == null) return;
            pin.Dispose();
            base.Remove(pin);
            if (OnRemove != null) OnRemove(pin, EventArgs.Empty);
        }

        public new void Remove(IIrisPin pin)
        {
            pin.Dispose();
            base.Remove(pin);
            if (OnRemove != null) OnRemove(pin, EventArgs.Empty);
        }

        public new void RemoveAt(int idx)
        {
            var pin = this.ElementAt(idx);
            pin.Dispose();
            base.RemoveAt(idx);
            if (OnRemove != null) OnRemove(pin, EventArgs.Empty);
        }

        public new void Clear()
        {
            base.Clear();
            if (OnReset != null) OnReset(this, EventArgs.Empty);
        }

        public int Size()
        {
            return Count;
        }

        public string ToJSON()
        {
            return JsonConvert.SerializeObject(this);
        }

        public string ToUriPath()
        {
            return String.Empty;
        }

        public void Dispose()
        {
            base.ForEach(p => p.Dispose());
            Clear();
        }
    }
}
