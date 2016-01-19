using System;


using Iris.Core.Events;

namespace Iris.Core.Types
{
    public interface IIrisPin : IIrisData
    {
        event EventHandler<PinEventArgs> Change;

        IrisId GetId();
        string GetName();

        IrisId GetHostId();

        NodePath GetNodePath();
        Behavior GetBehavior();
        OSCAddress GetAddress();
        PinSlices GetValues();

        void SetId(IrisId id);
        void SetName(string val);
        void SetMinValue(double val);
        void SetMaxValue(double val);
        void SetValueType(ValType val);
        void SetVectorSize(int val);
        void SetUnits(string val);
        void SetShowValue(bool val);
        void SetShowSlider(bool val);
        void SetBehavior(Behavior val);
        void SetMaxChar(int val);
        void SetValues(PinSlices values);

        IObservable<PinEventArgs> GetObservable();

        bool IsBang();

        IIrisPin Update(IIrisPin other);
        IIrisPin Update(PinSlices values);

        IIrisPin Update(PinEventArgs args);
        IIrisPin Update(UpdateDirection dir, IIrisPin pin);
        IIrisPin Update(UpdateDirection dir, PinSlices values);
        IIrisPin Update(UpdateDirection dir, string prop, string value);
    }
}
