using System;

namespace Iris.Core.Types
{
    public interface IIrisPatch : IIrisData
    {
        void AddPin(IIrisPin pin);

        bool HasPin(IrisId id);
        bool HasPin(NodePath nodepath);

        IIrisPins GetPins();
        IIrisPin FindPin(IrisId id);
        IIrisPin FindPin(NodePath nodepath);

        IrisId GetId();
        NodePath GetNodePath();
        Name GetName();

        void Update(IIrisPatch patch);
        void RemovePin(IIrisPin pin);

        void Subscribe(Action<IIrisPatch> action);
    }
}
