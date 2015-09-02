
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Iris.Core.Types
{
    /// <summary>
    /// Cue value type. Can be either 0 (IOPin), or 1 (Cue). Determines
    /// whether Value is written to a PinData (found with Target) or another Cue
    /// is being triggered (also indexed by the Target property).
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum CueValueType
    {
        IOPin,
        Cue
    }

    /// <summary>
    /// CueValue is an element in the array/list of values to be executed once
    /// the Cue is being triggered.
    /// </summary>
    public class CueValue
    {
        /// <summary>
        /// The type of this CueValue object.
        /// </summary>
        /// <value>The type.</value>
        public CueValueType Type { get; set; }

        /// <summary>
        /// If the type is IOPin, Target denominates the NodePath/Address of the
        /// pin to write to, else Target is the ID of the Cue to trigger.
        /// </summary>
        /// <value>The target of this value.</value>
        public IrisId Target     { get; set; }

        /// <summary>
        /// The actual values to write to a pin. Ignored when type is Cue.
        /// </summary>
        /// <value>The value.</value>
        public PinSlices Values  { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Iris.Core.Types.Cue+CueValue"/> class.
        /// </summary>
        /// <param name="type">Type.</param>
        /// <param name="target">Target.</param>
        /// <param name="value">Value.</param>
        public CueValue(CueValueType type, IrisId target, PinSlices values)
        {
            Type   = type;
            Target = target;
            Values = values;
        }

        /// <summary>
        /// Create a CueValue with a PinData as the target.
        /// </summary>
        /// <returns>The target.</returns>
        /// <param name="target">Target.</param>
        /// <param name="value">Value.</param>
        public static CueValue PinTarget(IrisId target, PinSlices values)
        {
            return new CueValue(CueValueType.IOPin, target, values);
        }

        /// <summary>
        /// Create a CueValue with another Cue as the target.
        /// </summary>
        /// <returns>The target.</returns>
        /// <param name="target">Target.</param>
        public static CueValue CueTarget(IrisId target)
        {
            return new CueValue(CueValueType.Cue, target, null);
        }
    }
}
