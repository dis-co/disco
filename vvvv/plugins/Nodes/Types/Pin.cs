using System;
using System.Threading;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Reactive.Linq;

using VVVV.Core.Logging;
using VVVV.PluginInterfaces.V2.Graph;

using Newtonsoft.Json;

using Iris.Core.Types;
using Iris.Core.Events;
using Iris.Core.Logging;
using Iris.Nodes.Graph;
using Iris.Nodes.Events;

namespace Iris.Nodes.Types
{
    using PinAddr = String;
    using WriteJob = Tuple<UpdateDirection, IPin2, string>;
    using Registration = Tuple<IPin2, CancellationTokenSource>;

    public class PinSerializer : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var name = value as Pin;
            serializer.Serialize(writer, name.Data);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(Pin).IsAssignableFrom(objectType);
        }
    }

    [JsonConverter(typeof(PinSerializer))]
    public class Pin : IIrisPin
    {
        private const string TAG = "Pin";

        public event EventHandler<PinEventArgs> Change;

        public IIrisPin Data;

        public Pin(IIrisPin data)
        {
            Data = data;
        }

        #region Create
        public static IIrisPin FromNode (INode2 node, VVVVGraph context)
        {
            Pin pin = new Pin(NodeParser.Parse (node));
            pin.RegisterWith (node, context);
            return pin;
        }
        #endregion

        #region Interface
        public IrisId GetHostId()
        {
            return Data.GetHostId();
        }

        public Behavior GetBehavior()
        {
            return Data.GetBehavior();
        }

        public IIrisData GetData()
        {
            return Data;
        }

        public void SetBehavior(Behavior b)
        {
            Data.SetBehavior(b);
        }

        public string GetName()
        {
            return Data.GetName();
        }

        public IrisId GetId()
        {
            return Data.GetId();
        }

        public void SetId(IrisId id)
        {
            Data.SetId(id); 
        }

        public OSCAddress GetAddress()
        {
            return Data.GetAddress();
        }

        public NodePath GetNodePath()
        {
            return Data.GetNodePath();
        }

        public PinSlices GetValues()
        {
            return Data.GetValues();
        }

        public void SetValues(PinSlices values)
        {
            Data.SetValues(values);
        }

        public void SetVectorSize(int size)
        {
            Data.SetVectorSize(size);
        }

        public void SetMaxChar(int max)
        {
            Data.SetMaxChar(max);
        }

        public void SetMaxValue(double max)
        {
            Data.SetMaxValue(max);
        }

        public void SetMinValue(double min)
        {
            Data.SetMinValue(min);
        }

        public void SetName(string name)
        {
            Data.SetName(name);
        }

        public void SetShowSlider(bool show)
        {
            Data.SetShowSlider(show);
        }

        public void SetShowValue(bool show)
        {
            Data.SetShowSlider(show);
        }

        public void SetUnits(string units)
        {
            Data.SetUnits(units);
        }

        public void SetValueType(ValType t)
        {
            Data.SetValueType(t);
        }
        #endregion

        #region Update Methods
        public IIrisPin Update (UpdateDirection dir, IIrisPin pin)
        {
            Data.Update(dir, pin); // Name property only for now
            if (Change != null)
                Change (this, new PinEventArgs (dir, Data));
            return this;
        }

        public IIrisPin Update (UpdateDirection dir, PinSlices values)
        {
            Data.Update(dir, values);
            if (Change != null)
                Change (null, new PinEventArgs (dir, Data));
            return this;
        }

        public IIrisPin Update (UpdateDirection dir, string prop, string val)
        {
            Data.Update(dir, prop, val);
            if (Change != null)
                Change (null, new PinEventArgs (dir, Data));
            return this;
        }

        public IIrisPin Update(PinEventArgs args)
        {
            Data.Update(args);
            args.Pin = Data;
            if (Change != null)
                Change (null, args);
            return this;
        }

        public IIrisPin Update(IIrisPin other)
        {
            return Data.Update(other);
        }

        public IIrisPin Update(PinSlices other)
        {
            return Data.Update(other);
        }

        public bool IsBang()
        {
            return Data.IsBang();
        }
        #endregion

        #region Destructor
        public void Dispose ()
        {
            UnRegister();
        }
        #endregion

        #region Event List
        private enum HandlerType
        {
            Property,
            Value
        }

        private List<string> ConfigPins = new List<string> ()
        {   "Descriptive Name"
        ,   "Tag"
        ,   "SliceCount"
        ,   "Value Type"
        ,   "Precision"
        ,   "Behavior"
        ,   "Vector Size"
        ,   "Minimum"
        ,   "Maximum"
        ,   "Units"
        ,   "Show Value"
        ,   "Show Slider"
        ,   "File Mask"
        ,   "Maximum Characters"
        ,   "Chooser Style"
        ,   "String Type"
        };

        private List<string> InputPins = new List<string> ()
        {   "X Input Value"
        ,   "Y Input Value"
        ,   "Input String"
        ,   "Color Input"
        ,   "Input Enum"
        ,   "Input Node"
        };

        private Dictionary<PinAddr, Registration> CachedPins =
            new Dictionary<PinAddr, Registration> ();
        #endregion

        #region Utilities
        public string GetEncodedNodePath ()
        {
            return WebUtility.UrlEncode(Data.GetNodePath().ToString()).ToLower ();
        }

        public string GetEncodedId ()
        {
            return WebUtility.UrlEncode(Data.GetId().ToString());
        }

        public string GetProperty (string PropertyName)
        {
            var outp = "";
            switch (PropertyName) {
            case "Descriptive Name":
                outp = Data.GetName();
                break;
            }
            return outp;
        }

        public PinSlices GetValues (IPin2 pin)
        {
            PinSlices values = new PinSlices ();
            for (int i = 0; i < pin.SliceCount; i++) {
                var value = String.Empty;

                if (Data.GetBehavior() == Behavior.XYSlider) {
                    var addr = GetXPinAddress ().ToString();
                    value += CachedPins [addr].Item1 [i] + "x";
                }

                values.Add (new PinSlice (Data.GetBehavior(), value + pin [i]));
            }
            return values;
        }

        public OSCAddress GetXPinAddress ()
        {
            return new OSCAddress(Data.GetNodePath().ToString() + "/" + "X Input Value");
        }
        #endregion

        #region Events
        public IObservable<PinEventArgs> GetObservable ()
        {
            return Observable.FromEvent<PinEventArgs> (
                handler => Change += (o, e) => handler (e),
                handler => Change -= (o, e) => handler (e));
        }

        public void RegisterWith (INode2 node, VVVVGraph context)
        {
            ProcessAttributes(node, context);

            // register all the config pins
            ConfigPins.ForEach (name => {
                var pin = node.FindPin (name);
                if (pin != null)
                    RegisterLocalChangeHandler (HandlerType.Property, pin);
                });

            // register all the input pins
            InputPins.ForEach (name => {
                var pin = node.FindPin (name);
                if (pin != null)
                    RegisterLocalChangeHandler (HandlerType.Value, pin);
                });
        }

        private void RegisterLocalChangeHandler (HandlerType type, IPin2 pin)
        {
            var cts = new CancellationTokenSource ();

            EventHandler graphChanged = null;

            var observable =
                Observable.FromEvent<IPinEventArgs> (
                    handler => {
                        graphChanged = (o, e) => {
                            var ipin = o as IPin2;
                            handler (new IPinEventArgs (ipin));
                        };
                        pin.Changed += graphChanged;
                    },
                    handler => pin.Changed -= graphChanged);

            observable.Subscribe (args => {
                    if (type == HandlerType.Value)
                        Update (UpdateDirection.Graph, GetValues (args.Pin));
                    else if (type == HandlerType.Property)
                        Update (UpdateDirection.Graph, args.Pin.Name, args.Pin [0]);
                }, cts.Token);

            var Addr = Data.GetNodePath().ToString() + "/" + pin.Name;
            if (!CachedPins.ContainsKey(Addr))
                CachedPins [Addr] = new Registration (pin, cts);
        }

        public void UnRegister ()
        {
            CachedPins.ToList().ForEach(reg => reg.Value.Item2.Cancel ());
            CachedPins.Clear();
        }
        #endregion

        #region Attributes
        public void ProcessAttributes(INode2 node, VVVVGraph context)
        {
            var attrs = ReadAttributes(node);
            // no metadata exist yet, so we create it
            if(attrs == null)
            {
                attrs = CreateAttributes(node, context);
                Data.SetId(new IrisId(attrs.Id));
            }
            else
            {
                Data.SetId(new IrisId(attrs.Id));
            }
        }

        public PinAttributes CreateAttributes(INode2 node, VVVVGraph context)
        {
            var attrs = new PinAttributes(true);

            try
            {
                QueueNodePatch(context,
                               node.Parent.ID,
                               node.Parent.NodeInfo.Filename,
                               NodeParser.FormatNodeTagSnippet(node, attrs.ToJson()));
            }
            catch (Exception ex)
            {
                LogEntry.Fatal(TAG, "Exception CreateAttributes: " + ex.Message);
            }

            return attrs;
        }

        public void UpdateAttributes(INode2 node, VVVVGraph context, PinAttributes attrs)
        {
            var current = ReadAttributes(node);
            var tagPin  = node.FindPin("Tag");
            if (tagPin == null) return;

            current.Id = attrs.Id;

            try
            {
                QueueNodePatch(context,
                               node.Parent.ID,
                               node.Parent.NodeInfo.Filename,
                               NodeParser.FormatNodeTagSnippet(node, current.ToJson()));
            }
            catch(Exception ex)
            {
                LogEntry.Fatal(TAG, "Exception: " + ex.Message);
            }
        }

        public PinAttributes ReadAttributes(INode2 node)
        {
            var tagPin = node.FindPin("Tag");
            if (tagPin == null) return null;

            // no metadata exist yet, so we create it
            if(tagPin[0] == null || tagPin[0].Length <= 0)
            {
                return null;
            }
            else
            {
                var something = NodeParser.GetAttributes(tagPin);
                return PinAttributes.FromJson(something);
            }
        }

        private void QueueNodePatch(VVVVGraph ctx, int id, string path, string payload)
        {
            ctx.GraphPatchJobQueue.Enqueue(
                Tuple.Create(ctx.FrameCount, id, path, payload));
        }
        #endregion

        public IPin2 GetIPin()
        {
            return CachedPins [GetAddress().ToString()].Item1;
        }

        public string GetResetSpread()
        {
            var Values = Data.GetValues();
            return Values.Aggregate(String.Empty, (acc, val) =>
                    {
                        if(Values.IndexOf(val) == 0) acc += "|0|";
                        else acc += ",|0|";
                        return acc;
                    });
        }

        public string ToJSON()
        {
            return JsonConvert.SerializeObject(Data);
        }

        public string ToUriPath()
        {
            return String.Empty;
        }
    }
}
