using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using VVVV.PluginInterfaces.V2;
using VVVV.PluginInterfaces.V2.Graph;

using Iris.Core.Types;
using Iris.Core.Logging;

namespace Iris.Nodes.Graph
{
    public static class NodeParser
    {
        static private string tag = "[NodeParser] ";

        /// <summary>
        /// Turn an INode2 into a Iris.Core.Types.PinData object.
        /// </summary>
        /// <param name="node">Node to convert.</param>
        /// <returns>Instance of Iris.Core.Types.PinData corresponding to node.</returns>
        public static IIrisPin Parse (INode2 node)
        {
            switch (GetPinType (node)) {
                case PinType.Color:
                    return ParseColorNode (node);
                case PinType.Enum:
                    return ParseEnumNode (node);
                case PinType.Node:
                    return ParseNodeNode (node);
                case PinType.String:
                    return ParseStringNode (node);
                default:
                    return ParseValueNode (node);
            }
        }

        /// <summary>
        /// Parse an INode2 IOBox as a ValuePin.
        /// </summary>
        /// <param name="node">INode2 to parse</param>
        /// <returns>PinData with PinType.Value</returns>
        private static IIrisPin ParseValueNode (INode2 node)
        {
            return PinData.ValuePin (
                GetBehavior (node),
                GetValType (node),
                GetNodePath (node),
                GetAddress (node),
                GetDescriptiveName (node),
                GetTag (node),
                GetMinValue (node),
                GetMaxValue (node),
                GetUnits (node),
                GetPrecision (node),
                GetVectorSize (node),
                GetShowValue (node),
                GetShowSlider (node),
                GetValues (node));
        }

        /// <summary>
        /// Parse an INode2 as a Color IOBox.
        /// </summary>
        /// <param name="node">INode2 to parse.</param>
        /// <returns>PinData of PinType.Color.</returns>
        private static IIrisPin ParseColorNode (INode2 node)
        {
            return PinData.ColorPin (
                GetBehavior (node),
                GetNodePath (node),
                GetAddress (node),
                GetDescriptiveName (node),
                GetTag (node),
                GetShowValue (node),
                GetValues (node));
        }

        /// <summary>
        /// Parse an INode2 IOBox as a PinData of PinType String.
        /// </summary>
        /// <param name="node">INode2 to parse.</param>
        /// <returns>PinData of PinType.String</returns>
        private static IIrisPin ParseStringNode (INode2 node)
        {
            return PinData.StringPin (
                GetBehavior (node),
                GetNodePath (node),
                GetAddress (node),
                GetDescriptiveName (node),
                GetTag (node),
                GetMaxChar (node),
                GetFileMask (node),
                GetValues (node));
        }

        /// <summary>
        /// Parse an INode2 IOBox of type Node (??).
        /// </summary>
        /// <param name="node">INode2 to parse.</param>
        /// <returns>PinData of PinType.Enum</returns>
        private static IIrisPin ParseNodeNode (INode2 node)
        {
            return PinData.EnumPin (
                GetNodePath (node),
                GetAddress (node),
                GetDescriptiveName (node),
                GetTag (node),
                GetProperties (node),
                GetValues (node));
        }

        /// <summary>
        /// Parse an INode2 IOBox of type Enum.
        /// </summary>
        /// <param name="node">INode2 to parse</param>
        /// <returns>PinData of PinType.Enum</returns>
        private static IIrisPin ParseEnumNode (INode2 node)
        {
            return PinData.EnumPin (
                GetNodePath (node),
                GetAddress (node),
                GetDescriptiveName (node),
                GetTag (node),
                GetProperties (node),
                GetValues (node));
        }

        /// <summary>
        /// Parse the Value Type pin on a node.
        /// </summary>
        /// <param name="node">INode2 to parse</param>
        /// <returns>ValType (Real, Int or Bool) of node</returns>
        public static ValType GetValType (INode2 node)
        {
            switch (SingleStringProperty ("Value Type", node)) {
                case "Real":
                    return ValType.Real;
                case "Integer":
                    return ValType.Int;
                case "Boolean":
                    return ValType.Bool;
                default:
                    return ValType.None;
            }
        }

        /// <summary>
        /// Parse the Minimum value of a value IOBox.
        /// </summary>
        /// <param name="node">INode2 to parse</param>
        /// <returns>Minimum value of IOBox as double</returns>
        public static double GetMinValue (INode2 node)
        {
            return SingleDecimalProperty ("Minimum", node);
        }

        /// <summary>
        /// Parse the Maximum value of the value IOBox passed in.
        /// </summary>
        /// <param name="node">INode2 to parse</param>
        /// <returns>Maximum value of IOBox as double</returns>
        public static double GetMaxValue (INode2 node)
        {
            return SingleDecimalProperty ("Maximum", node);
        }

        /// <summary>
        /// Parse the Units pin of a INode2.
        /// </summary>
        /// <param name="node">INode2 to parse</param>
        /// <returns>Units as string</returns>
        public static string GetUnits (INode2 node)
        {
            return SingleStringProperty ("Units", node);
        }

        /// <summary>
        /// Parse the precision value of an IOBox of type value.
        /// </summary>
        /// <param name="node">INode2 to parse</param>
        /// <returns>integer precision value of IOBox</returns>
        public static int GetPrecision (INode2 node)
        {
            return SingleIntProperty ("Precision", node);
        }

        /// <summary>
        /// Parse the vector size of an IOBox.
        /// </summary>
        /// <param name="node">INode2 to parse</param>
        /// <returns>integer vector size value of IOBox</returns>
        public static int GetVectorSize (INode2 node)
        {
            return SingleIntProperty ("Vector Size", node);
        }

        /// <summary>
        /// Parse the Show Value pin of an IOBox.
        /// </summary>
        /// <param name="node">INode2 to parse.</param>
        /// <returns>boolean value of Show Value pin</returns>
        public static bool GetShowValue (INode2 node)
        {
            return SingleBoolProperty ("Show Value", node);
        }

        /// <summary>
        /// Parse the Show Slider pin of a value IOBox
        /// </summary>
        /// <param name="node">INode2 to parse</param>
        /// <returns>boolean value of the Show Slider pin</returns>
        public static bool GetShowSlider (INode2 node)
        {
            return SingleBoolProperty ("Show Slider", node);
        }

        /// <summary>
        /// Parse the Maximum Characters pin of a string IOBox
        /// </summary>
        /// <param name="node">INode2 to parse</param>
        /// <returns>maximum number of characters allowed as integer</returns>
        public static int GetMaxChar (INode2 node)
        {
            return SingleIntProperty ("Maximum Characters", node);
        }

        /// <summary>
        /// Parse the File Mask pin of a string IOBox.
        /// </summary>
        /// <param name="node">INode2 to parse</param>
        /// <returns>File mask value</returns>
        public static string GetFileMask (INode2 node)
        {
            return SingleStringProperty ("File Mask", node);
        }

        /// <summary>
        /// Parse the IOBox values.
        /// </summary>
        /// <param name="node">INode2 to parse.</param>
        /// <returns>List of PinSlice objects</returns>
        public static PinSlices GetValues (INode2 node)
        {
            switch (GetPinType (node)) {
                case PinType.Color:
                    return GetSimpleValue ("Color Input", node);
                case PinType.Enum:
                    return GetSimpleValue ("Input Enum", node);
                case PinType.String:
                    return GetSimpleValue ("Input String", node);
                default:
                    if (IsXYSlider (node))
                        return GetComplexValue ("X Input Value", "Y Input Value", node);
                    else
                        return GetSimpleValue ("Y Input Value", node);
            }
        }

        public static PinSlices GetSimpleValue (string Name, INode2 node)
        {
            var pin = node.FindPin (Name);
            var behavior = NodeParser.GetBehavior (node);
            var list = new PinSlices ();
            for (int i = 0; i < pin.SliceCount; i++) {
                Object val = null;

                // normalize value for toggle behavior to boolean
                if (behavior == Behavior.Toggle || behavior == Behavior.Bang)
                    val = (pin[i] == "1");
                else 
                    val = pin[i];

                // val can be null specifically for string ioboxes!
                // hence normalize to default values if the graph hands us null
                if(val == null)
                {
                    switch(behavior)
                    {
                        case Behavior.XSlider:
                            val = 0;
                            break;
                        default:
                            val = String.Empty;
                            break;
                    }
                }
                list.Add (new PinSlice (behavior, val));
            }
            return list;
        }

        public static PinSlices GetComplexValue (string X, string Y, INode2 node)
        {
            var pin1 = node.FindPin (X);
            var pin2 = node.FindPin (Y);
            var list = new PinSlices ();
            var behavior = NodeParser.GetBehavior (node);

            for (int i = 0; i < pin1.SliceCount; i++) {
                var val = pin1 [i] + "|" + pin2 [i];
                list.Add (new PinSlice (behavior, val));
            }
            return list;
        }

        /// <summary>
        /// Parse the properties of an enum IOBox.
        /// </summary>
        /// <param name="node">INode2 to parse</param>
        /// <returns>List of properties (string)</returns>
        public static List<string> GetProperties (INode2 node)
        {
            var properties = new List<string> ();
            var prop = node.FindPin ("Input Enum");
            if (prop == null)
                return properties;
            string name = prop.SubType;

            //cut out the part between the two commas (its the enum name)
            name = name.Substring (
                name.IndexOf (',') + 2,
                name.LastIndexOf (',') - name.IndexOf (',') - 2);

            int count = EnumManager.GetEnumEntryCount (name);
            for (int i = 0; i < count; i++) {
                properties.Add (EnumManager.GetEnumEntry (name, i));
            }
            return properties;
        }

        /// <summary>
        /// Parse the OSC Address for a particular pin.
        /// </summary>
        /// <param name="node">INode2 to parse</param>
        /// <returns>String OSC Address</returns>
        public static OSCAddress GetAddress (INode2 node)
        {
            return new OSCAddress(GetNodePath(node).ToString() + "/" + GetPinName (node));
        }

        /// <summary>
        /// Parse the NodePath of a node.
        /// </summary>
        /// <param name="node">INode2 to parse.</param>
        /// <returns>string NodePath</returns>
        public static NodePath GetNodePath (INode2 node)
        {
            return new NodePath(node.GetNodePath (false));
        }

        /// <summary>
        /// Parse the Descriptive Name pin of an IOBox.
        /// </summary>
        /// <param name="node">INode2 to parse</param>
        /// <returns>Name of IOBox</returns>
        public static string GetDescriptiveName (INode2 node)
        {
            return SingleStringProperty ("Descriptive Name", node);
        }

        /// <summary>
        /// Parse the Tag pin of an IOBox.
        /// </summary>
        /// <param name="node">INode2 to parse</param>
        /// <returns>Tag string</returns>
        public static string GetTag (INode2 node)
        {
            return SingleStringProperty ("Tag", node);
        }

        /// <summary>
        /// Parse the Tag pin of an IOBox.
        /// </summary>
        /// <param name="node">INode2 to parse</param>
        /// <returns>Tag string</returns>
        public static string GetAttributes (IPin2 pin)
        {
            return HttpUtility.HtmlDecode(pin[0]);
        }

        public static string FormatNodeTagSnippet(INode2 node, string raw)
        {
            string tmpl =
                @"<NODE id=""{0}"">
                      <PIN pinname=""Tag"" slicecount=""1"" values=""{1}""/>
                  </NODE>";
            return String.Format(tmpl, node.ID, HttpUtility.HtmlEncode(raw));
        }

        public static string FormatPatchTagSnippet(int id, string tags)
        {
            string tmpl = @"<PATCH id=""{0}"">{1}</PATCH>";
            return String.Format(tmpl, id, tags);
        }

        /// <summary>
        /// Parse (or, in this case better, normalize) the Behavior of an IOBox.
        /// </summary>
        /// <param name="node">INode2 to parse</param>
        /// <returns>Behavior of IOBox as per Behavior enumeration</returns>
        public static Behavior GetBehavior (INode2 node)
        {
            Behavior result = Behavior.None;
            switch (GetPinType (node)) {
                case PinType.String:
                    switch (SingleStringProperty ("String Type", node)) {
                        case "Multiline":
                            result = Behavior.MultiLine;
                            break;
                        case "Filename":
                            result = Behavior.FileName;
                            break;
                        case "Directory":
                            result = Behavior.Directory;
                            break;
                        case "URL":
                            result = Behavior.Url;
                            break;
                        case "IP":
                            result = Behavior.IP;
                            break;
                        default:
                            result = Behavior.String;
                            break;
                    }
                    break;
                case PinType.Value:
                    var valType = GetValType (node);
                    if (valType == ValType.Real || valType == ValType.Int) {
                        if (IsXYSlider (node)) {
                            result = Behavior.XYSlider;
                        } else {
                            result = Behavior.XSlider;
                        }
                    } else {
                        if (SingleStringProperty ("Behavior", node) == "Bang")
                            result = Behavior.Bang;
                        else
                            result = Behavior.Toggle;
                    }
                    break;
            }
            return result;
        }

        public static bool IsXYSlider (INode2 node)
        {
            var x = node.FindPin ("X Output Value");
            var y = node.FindPin ("Y Output Value");
            if ((x != null && x.Status == StatusCode.IsConnected) &&
                (y != null && y.Status == StatusCode.IsConnected)) {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Check whether input has connected output pins.
        /// </summary>
        /// <param name="node">Node to check</param>
        /// <returns></returns>
        private static bool IsConnectedOutput (INode2 node)
        {
            return node.Pins
                .Where (pin => {
                        return pin.Direction == PinDirection.Output && pin.ConnectedPins.Count > 0;
                    })
                .Count () > 0;
        }

        /// <summary>
        /// Parse the type of an IOBox.
        /// </summary>
        /// <param name="node">INode2 to parse</param>
        /// <returns>PinType of IOBox</returns>
        public static PinType GetPinType (INode2 node)
        {
            PinType type = PinType.Value;
            switch (node.NodeInfo.Systemname) {
                case "IOBox (String)":
                    type = PinType.String;
                    break;
                case "IOBox (Color)":
                    type = PinType.Color;
                    break;
                case "IOBox (Enumerations)":
                    type = PinType.Enum;
                    break;
                case "IOBox (Node)":
                    type = PinType.Node;
                    break;
            }
            return type;
        }

        public static string GetPinName (INode2 node)
        {
            if (node.NodeInfo.Systemname == "IOBox (Value Advanced)")
                return "Y Input Value";
            else if (node.NodeInfo.Systemname == "IOBox (String)")
                return "Input String";
            else if (node.NodeInfo.Systemname == "IOBox (Color)")
                return "Color Input";
            else if (node.NodeInfo.Systemname == "IOBox (Enumerations)")
                return "Input Enum";
            else if (node.NodeInfo.Systemname == "IOBox (Node)")
                return "Input Node";
            else
                return "";
        }

        /// <summary>
        /// Normalize the ParentNode property on IPin2, as it can be
        /// literally anything as per spec.
        /// </summary>
        /// <param name="pin">PinData to get parent INode2 of</param>
        /// <returns>INode2 parent of pin</returns>
        public static INode2 GetParent (IPin2 pin)
        {
            var parent = pin.ParentNodeByPatch (pin.ParentNode);
            if (parent == null)
                return pin.ParentNode;
            return parent;
        }

        /// <summary>
        /// Helper to parse pins with only one value slice (string).
        /// </summary>
        /// <param name="Name">Name of pin (property) to parse</param>
        /// <param name="node">INode2 whose Pins will be examined</param>
        /// <returns>string value</returns>
        private static string SingleStringProperty (string Name, INode2 node)
        {
            var prop = node.FindPin (Name);
            if (prop == null)
                return "";
            return prop [0];
        }

        /// <summary>
        /// Helper to parse pins with only one value slice (boolean).
        /// </summary>
        /// <param name="Name">Name of pin (property) to parse</param>
        /// <param name="node">INode2 to parse</param>
        /// <returns>boolean value</returns>
        private static bool SingleBoolProperty (string Name, INode2 node)
        {
            var prop = node.FindPin (Name);
            if (prop == null)
                return false;
            return prop [0] == "1";
        }

        /// <summary>
        /// Helper to parse pins with only one value slice (decimal).
        /// </summary>
        /// <param name="Name">Name of pin to get value of</param>
        /// <param name="node">INode2 to inspect for pin</param>
        /// <returns>pin value as double</returns>
        private static double SingleDecimalProperty (string Name, INode2 node)
        {
            var prop = node.FindPin (Name);
            if (prop == null) return 0.0;

            double result = 0.0;

            Double.TryParse(prop[0], NumberStyles.Float,
                CultureInfo.InvariantCulture, out result);

            return result;
        }

        /// <summary>
        /// Helper to parse pins with only one value slice (integer).
        /// </summary>
        /// <param name="Name">Name of property to get</param>
        /// <param name="node">INode2 to parse</param>
        /// <returns>integer value</returns>
        private static int SingleIntProperty (string Name, INode2 node)
        {
            var prop = node.FindPin (Name);
            if (prop == null)
                return 0;
            int result;
            int.TryParse (prop [0], out result);
            return result;
        }

        private static void Log (string thing)
        {
            Iris.Core.Logging.Log.Debug (tag + thing);
        }
    }
}
