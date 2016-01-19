using System;

namespace Iris.Core.Types
{
    public interface IIrisPins : IIrisData
    {
        void Add(IIrisPin item);

        bool HasPin(IrisId id);
        bool HasPin(NodePath nodepath);

        IIrisPin Find(IrisId id);
        IIrisPin Find(NodePath nodepath);

        void Remove(NodePath nodepathh);
        void Remove(IIrisPin item);
        void RemoveAt(int idx);

        void Each(Action<IIrisPin> fun);

        int Size();
    }
}
