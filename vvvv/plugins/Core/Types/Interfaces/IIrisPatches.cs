using System;
using System.Collections.Generic;

using Iris.Core.Events;

namespace Iris.Core.Types
{
    public interface IIrisPatches : IIrisData
    {
        event EventHandler<PatchEventArgs> PatchAdded;
        event EventHandler<PatchEventArgs> PatchRemoved;

        bool HasPin(IrisId id);

        void Add(IIrisPatch item);
        void Remove(IIrisPatch item);
        void RemoveAt(int idx);

        IIrisPatch Create(NodePath np, Name name);
        IIrisPatch Find(IrisId id);
        IIrisPatch Find(NodePath NodePath);

        IIrisPin FindPin(IrisId id);
        IIrisPin FindPin(NodePath nodepath);
        IIrisPin FindPin(NodePath patchpath, NodePath nodepath);

        List<IIrisPin> FindPins(IrisId id);
        int Size();
    }
}
