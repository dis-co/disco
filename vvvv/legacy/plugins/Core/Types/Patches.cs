using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;

using Iris.Core.Events;

namespace Iris.Core.Types
{
    public class Patches : List<IIrisPatch>, IIrisPatches
    {
        public event EventHandler<PatchEventArgs> PatchAdded;
        public event EventHandler<PatchEventArgs> PatchRemoved;

        public void Dispose()
        {
            ForEach(patch => patch.Dispose());
            Clear();
        }

        public bool HasPin(IrisId id)
        {
            return this.Any(patch => patch.HasPin(id));
        }

        public IIrisPatch Create(NodePath np, Name name)
        {
            var id = new IrisId(Guid.NewGuid().ToString());
            var Patch = new Patch (id, np, name, null, new Pins ());
            Add(Patch);
            return Patch;
        }

        public new void Add (IIrisPatch item)
        {
            base.Add (item);

            if (PatchAdded != null)
                PatchAdded (null, new PatchEventArgs(null, item));
        }

        public IIrisPatch Find (IrisId id)
        {
            return base.Find (ptc => ptc.GetId() == id);
        }

        public IIrisPatch Find (NodePath nodepath)
        {
            var p = Find(ptc => ptc.GetNodePath() == nodepath);
            return p;
        }

        public new void Remove (IIrisPatch item)
        {
            if (PatchRemoved != null)
                PatchRemoved (null, new PatchEventArgs(null, item));
            base.Remove (item);
        }

        public new void RemoveAt (int idx)
        {
            var patch = this.ElementAt (idx);

            if (PatchRemoved != null)
                PatchRemoved (null, new PatchEventArgs(null, patch));

            base.RemoveAt (idx);
        }

        public List<IIrisPin> FindPins(IrisId id)
        {
            var result = new List<IIrisPin>();
            ForEach(patch => patch.GetPins().Each(pin => {
                        if(pin.GetId() == id) result.Add(pin);
                    }));
            return result;
        }

        public IIrisPin FindPin (IrisId id)
        {
            IIrisPin pin = null;
            ForEach(patch => {
                var local = patch.FindPin(id);
                if (local != null) pin = local;
            });
            return pin;
        }

        public IIrisPin FindPin (NodePath nodepath)
        {
            return FindPin(nodepath);
        }

        public IIrisPin FindPin (NodePath PatchPath, NodePath nodepath)
        {
            var patch = Find (PatchPath);
            if (patch == null)
                return null;
            return patch.FindPin(nodepath);
        }

        public int Size()
        {
            return Count;
        }

        private void Log (string thing)
        {
            Iris.Core.Logging.Log.Debug ("[Patches] " + thing);
        }

        public string ToUriPath()
        {
            return String.Empty;
        }

        public string ToJSON()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
