using System;
using System.Globalization;
using System.Collections.Generic;
using System.Reactive.Linq;
using Newtonsoft.Json;
using Iris.Core.Events;

namespace Iris.Core.Types
{
    using PinAddr = String;

    //think about generic type of pins
    public class PinData : IIrisPin
    {
        public event EventHandler<PinEventArgs> Change;

        #region Properties
        public IrisId Id     { get; set; }

        public IrisId HostId { get; set; }

        /// <summary>
        /// Gets the node path this class was initialized with. The NodePath
        /// property uniquely identifies a PinData, and should be used as ID
        /// wherever necessary.
        /// </summary>
        /// <value>The node path.</value>
        public NodePath NodePath { get; set; }

        /// <summary>
        /// Gets or sets the name of the PinData. This can be altered by clients
        /// at run-time.
        /// </summary>
        /// <value>The name to .</value>
        public string Name     { get; set; }

        /// <summary>
        /// Gets or sets the tag. Tags can be used to store additional information
        /// about the PinData, and should be considered a metadata field.
        /// </summary>
        /// <value>The tag.</value>
        public string Tag      { get; set; }

        /// <summary>
        /// Gets or the OSC address of the PinData for direct addressing.
        /// </summary>
        /// <value>The address.</value>
        public OSCAddress Address  { get; set; }

        /// <summary>
        /// Gets or the type of current PinData. Can be either one of
        /// Value, String, Color or Enum. The behavior of the PinData significantlyh
        /// depends on this value.
        /// </summary>
        /// <value>The type.</value>
        public PinType Type    { get; set; }

        /// <summary>
        /// A list of PinSlice objects, representing the current state of the PinData.
        /// </summary>
        /// <value>The values.</value>
        public PinSlices Values { get; set; }

        /// <summary>
        /// If the Behavior of the PinData is Value, MinValue determines the minimum
        /// value the PinData may take. Values lower than minimum will get truncated.
        /// </summary>
        /// <value>The minimum value.</value>
        public double MinValue     { get; set; }

        /// <summary>
        /// If the Behavior of the PinData is Value, MaxValue determines the maximum
        /// value the PinData may take. Values higher than maximum will get truncated.
        /// </summary>
        /// <value>The max value.</value>
        public double MaxValue     { get; set; }

        /// <summary>
        /// A string field to display next to the PinData input as orientation for
        /// the user.
        /// </summary>
        /// <value>The units.</value>
        public string Units        { get; set; }

        /// <summary>
        /// Behavior determines the way the PinData will ultimately be displayed
        /// and the way it reacts to user input.
        /// </summary>
        /// <value>The behavior.</value>
        public Behavior Behavior   { get; set; }

        /// <summary>
        /// If the value of the pin is an element of the set of real numbers,
        /// this factor determines the number of after-comma digits to display.
        /// </summary>
        /// <value>The precision.</value>
        public int Precision    { get; set; }

        /// <summary>
        /// Used to determine how to display lists of values.
        /// </summary>
        /// <value>The size of the vector.</value>
        public int VectorSize      { get; set; }

        /// <summary>
        /// Show the value of the PinData as part of UI.
        /// </summary>
        /// <value><c>true</c> if show value; otherwise, <c>false</c>.</value>
        public bool ShowValue      { get; set; }

        /// <summary>
        /// Show the UI element of this PinData.
        /// </summary>
        /// <value><c>true</c> if show slider; otherwise, <c>false</c>.</value>
        public bool ShowSlider     { get; set; }

        /// <summary>
        /// One of Real, Int or Bool. Applies to PinType.Value only
        /// </summary>
        /// <value>The type of the value.</value>
        public ValType ValueType   { get; set; }

        /// <summary>
        /// Gets the maximum number of characters that a PinData of type String can
        /// accept.
        /// </summary>
        /// <value>The max char.</value>
        public int MaxChar { get; set; }

        public string FileMask { get; set; }

        public List<String> Properties { get; set; }
        #endregion

        #region Type Constructors
        public PinData () {}
        
        /// <summary>
        /// Initializes a new instance of the <see cref="Iris.Core.Types.PinData"/> class.
        /// This is a private a constructor, as we want to use the static factory methods
        /// provided to contruct the types correctly.
        /// </summary>
        /// <param name="type">Type.</param>
        /// <param name="behavior">Behavior.</param>
        /// <param name="valtype">Valtype.</param>
        /// <param name="nodepath">Nodepath.</param>
        /// <param name="address">Address.</param>
        /// <param name="name">Name.</param>
        /// <param name="tag">Tag.</param>
        /// <param name="minvalue">Minvalue.</param>
        /// <param name="maxvalue">Maxvalue.</param>
        /// <param name="units">Units.</param>
        /// <param name="precision">Precision.</param>
        /// <param name="vectorsize">Vectorsize.</param>
        /// <param name="showvalue">If set to <c>true</c> showvalue.</param>
        /// <param name="showslider">If set to <c>true</c> showslider.</param>
        /// <param name="maxchar">Maxchar.</param>
        /// <param name="filemask">Filemask.</param>
        /// <param name="values">Values.</param>
        /// <param name="properties">Properties.</param>
        public PinData (
            PinType type,
            Behavior behavior,
            ValType valtype,
            NodePath nodepath,
            OSCAddress address,
            string name,
            string tag,
            double minvalue,
            double maxvalue,
            string units,
            int precision,
            int vectorsize,
            bool showvalue,
            bool showslider,
            int maxchar,
            string filemask,
            PinSlices values,
            List<String> properties
        )
        {
            HostId = Host.HostId;
            Type = type;
            Behavior = behavior;
            ValueType = valtype;
            NodePath = nodepath;
            Address = address;
            Name = name;
            Tag = tag;
            MinValue = minvalue;
            MaxValue = maxvalue;
            Units = units;
            Precision = precision;
            VectorSize = vectorsize;
            ShowValue = showvalue;
            ShowSlider = showslider;
            MaxChar = maxchar;
            FileMask = filemask;
            Values = values;
            Properties = properties;
        }

        /// <summary>
        /// Construct a PinData of type Value
        /// </summary>
        /// <returns>The pin.</returns>
        /// <param name="behavior">Behavior.</param>
        /// <param name="valtype">Valtype.</param>
        /// <param name="nodepath">Nodepath.</param>
        /// <param name="address">Address.</param>
        /// <param name="name">Name.</param>
        /// <param name="tag">Tag.</param>
        /// <param name="minvalue">Minvalue.</param>
        /// <param name="maxvalue">Maxvalue.</param>
        /// <param name="units">Units.</param>
        /// <param name="precision">Precision.</param>
        /// <param name="vectorsize">Vectorsize.</param>
        /// <param name="showvalue">If set to <c>true</c> showvalue.</param>
        /// <param name="showslider">If set to <c>true</c> showslider.</param>
        /// <param name="values">Values.</param>
        public static PinData ValuePin (
            Behavior behavior,
            ValType valtype,
            NodePath nodepath,
            OSCAddress address,
            string name,
            string tag,
            double minvalue,
            double maxvalue,
            string units,
            int precision,
            int vectorsize,
            bool showvalue,
            bool showslider,
            PinSlices values
        )
        {
            return new PinData (
                PinType.Value, behavior, valtype,
                nodepath, address, name, tag,
                minvalue, maxvalue,
                units, precision, vectorsize,
                showvalue, showslider,
                0, null,
                values, null
            );
        }

        /// <summary>
        /// Construct a PinData type String
        /// </summary>
        /// <returns>The pin.</returns>
        /// <param name="behavior">Behavior.</param>
        /// <param name="nodepath">Nodepath.</param>
        /// <param name="address">Address.</param>
        /// <param name="name">Name.</param>
        /// <param name="tag">Tag.</param>
        /// <param name="maxchar">Maxchar.</param>
        /// <param name="filemask">Filemask.</param>
        /// <param name="values">Values.</param>
        public static PinData StringPin (
            Behavior behavior,
            NodePath nodepath,
            OSCAddress address,
            string name,
            string tag,
            int maxchar,
            string filemask,
            PinSlices values
        )
        {
            return new PinData (
                PinType.String, behavior, ValType.None,
                nodepath, address, name, tag,
                0.0, 0.0,
                null, 0, 0,
                false, false,
                maxchar, filemask,
                values, null
            );
        }

        /// <summary>
        /// Construct a PinData of type Color
        /// </summary>
        /// <returns>The pin.</returns>
        /// <param name="behavior">Behavior.</param>
        /// <param name="nodepath">Nodepath.</param>
        /// <param name="address">Address.</param>
        /// <param name="name">Name.</param>
        /// <param name="tag">Tag.</param>
        /// <param name="properties">Properties.</param>
        /// <param name="values">Values.</param>
        public static PinData ColorPin (
            Behavior behavior,
            NodePath nodepath,
            OSCAddress address,
            string name,
            string tag,
            bool showvalue,
            PinSlices values
        )
        {
            return new PinData (
                PinType.Color, behavior, ValType.None,
                nodepath, address, name, tag,
                0.0, 0.0,
                null, 0, 0,
                showvalue, false,
                0, null,
                values, null
            );
        }

        /// <summary>
        /// Construct a PinData of type Enum
        /// </summary>
        /// <returns>The pin.</returns>
        /// <param name="nodepath">Nodepath.</param>
        /// <param name="address">Address.</param>
        /// <param name="name">Name.</param>
        /// <param name="tag">Tag.</param>
        /// <param name="properties">Properties.</param>
        /// <param name="values">Values.</param>
        public static PinData EnumPin (
            NodePath nodepath,
            OSCAddress address,
            string name,
            string tag,
            List<String> properties,
            PinSlices values
        )
        {
            return new PinData (
                PinType.Enum, Behavior.None, ValType.None,
                nodepath, address, name, tag,
                0.0, 0.0,
                null, 0, 0,
                false, false,
                0, null,
                values, properties
            );
        }
        #endregion

        #region Destructors
        public void Dispose()
        {
        }
        #endregion

        public string ToJSON()
        {
            return JsonConvert.SerializeObject(this);
        }

        public static PinData FromJSON(string json)
        {
            return JsonConvert.DeserializeObject<PinData>(json); 
        }

        public string ToUriPath()
        {
            return String.Empty;
        }

        #region Interface
        public Behavior GetBehavior()
        {
            return Behavior;
        }

        public void SetBehavior(Behavior b)
        {
            Behavior = b;
        }

        public string GetName()
        {
            return Name;
        }

        public IrisId GetId()
        {
            return Id;
        }

        public void SetId(IrisId id)
        {
            Id = id; 
        }

        public IrisId GetHostId()
        {
            return HostId;
        }

        public OSCAddress GetAddress()
        {
            return Address;
        }

        public NodePath GetNodePath()
        {
            return NodePath;
        }

        public PinSlices GetValues()
        {
            return Values;
        }

        public void SetValues(PinSlices values)
        {
            Values = values;
        }

        public void SetVectorSize(int size)
        {
            VectorSize = size;
        }

        public void SetMaxChar(int max)
        {
            MaxChar = max;
        }

        public void SetMaxValue(double max)
        {
            MaxValue = max;
        }

        public void SetMinValue(double min)
        {
            MinValue = min;
        }

        public void SetName(string name)
        {
            Name = name;
        }

        public void SetShowSlider(bool show)
        {
            ShowSlider = show;
        }

        public void SetShowValue(bool show)
        {
            ShowSlider = show;
        }

        public void SetUnits(string units)
        {
            Units = units;
        }

        public void SetValueType(ValType t)
        {
            ValueType = t;
        }

        public void Reset()
        {
        }

        public IIrisPin Update(PinEventArgs args)
        {
            UpdateWith(args.Pin);
            return this;
        }

        public IIrisPin Update(UpdateDirection dir, IIrisPin pin)
        {
            UpdateWith(pin);
            return this;
        }

        public IIrisPin Update(UpdateDirection dir, PinSlices values)
        {
            Values = values;
            return this;
        }

        public IIrisPin Update(UpdateDirection dir, string prop, string val)
        {
            switch(prop)
            {
                case "Descriptive Name":
                    Name = val;
                    break;
                case "Minimum":
                    MinValue = ParseReal(val);
                    break;
                case "Maximum":
                    MaxValue = ParseReal(val);
                    break;
                case "Value Type":
                    ValueType = ParseValType(val);
                    break;
                case "Vector Size":
                    VectorSize = ParseInt(val);
                    break;
                case "Units":
                    Units = val;
                    break;
                case "Show Value":
                    ShowValue = ParseBool(val);
                    break;
                case "Show Slider":
                    ShowSlider = ParseBool(val);
                    break;
                case "Behavior":
                    Behavior = ParseBehavior(val);
                    break;
                case "String Type":
                    Behavior = ParseBehavior(val);
                    break;
                case "Maximum Characters":
                    MaxChar = ParseInt(val);
                    break;
            }
            return this;            
        }


        public IIrisPin Update(IIrisPin other)
        {
            UpdateWith(other);
            return this;
        }

        public IIrisPin Update(PinSlices other)
        {
            Values = other;
            return this;
        }
        #endregion

        public bool IsBang()
        {
            return Behavior == Behavior.Bang;
        }

        private void UpdateWith(IIrisPin pin)
        {
            Name = pin.GetName();
            Values = pin.GetValues();
        }

        private Behavior ParseBehavior(string val)
        {
            switch(val)
            {
                case "Toggle":
                    return Behavior.Toggle;
                case "Bang":
                    return Behavior.Bang;
                case "Press":
                    return Behavior.Bang;
                case "String":
                    return Behavior.String;
                case "MultiLine":
                    return Behavior.MultiLine;
                case "Filename":
                    return Behavior.FileName;
                case "Directory":
                    return Behavior.Directory;
                case "URL":
                    return Behavior.Url;
                case "IP":
                    return Behavior.IP;
                default:
                    return Behavior.None;
            }
        }

        private bool ParseBool(string val)
        {
            bool bl;
            bool.TryParse(val, out bl);
            return bl;
        }

        private ValType ParseValType(string val)
        {
            switch(val)
            {
                case "Integer":
                    return ValType.Int;
                case "Real":
                    return ValType.Real;
                case "Boolean":
                    return ValType.Bool;
                default:
                    return ValType.None;
            }
        }

        private int ParseInt(string val)
        {
            int intg = 0;
            int.TryParse(val, out intg);
            return intg;
        }

        private double ParseReal(string val)
        {
            double dbl = 0;
            double.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out dbl);
            return dbl;
        }

        public IObservable<PinEventArgs> GetObservable()
        {
            return Observable.FromEvent<PinEventArgs> (
                handler => Change += (o, e) => handler (e),
                handler => Change -= (o, e) => handler (e));
        }
    }
}
