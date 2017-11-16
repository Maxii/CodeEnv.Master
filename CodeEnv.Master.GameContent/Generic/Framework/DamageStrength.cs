// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DamageStrength.cs
// Immutable data container holding the offensive and defensive damage 
// infliction and mitigation capabilities of weapons and countermeasures respectively.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;

    /// <summary>
    /// Immutable data container holding the offensive and defensive damage 
    /// infliction and mitigation capabilities of weapons and countermeasures respectively.
    /// </summary>
    public struct DamageStrength : IEquatable<DamageStrength>, IComparable<DamageStrength> {

        private const string DebugNameFormat = "{0}({1},{2},{3})";

        private static readonly string LabelFormatWithTotal = "{0}" + Constants.NewLine
                                             + "{1}: {2}" + Constants.NewLine
                                             + "{3}: {4}" + Constants.NewLine
                                             + "{5}: {6}";

        private static readonly string LabelFormatNoTotal = "{0}: {1}" + Constants.NewLine
                                             + "{2}: {3}" + Constants.NewLine
                                             + "{4}: {5}";

        #region Operators Override

        // see C# 4.0 In a Nutshell, page 254

        public static bool operator ==(DamageStrength left, DamageStrength right) {
            return left.Equals(right);
        }

        public static bool operator !=(DamageStrength left, DamageStrength right) {
            return !left.Equals(right);
        }

        public static DamageStrength operator +(DamageStrength left, DamageStrength right) {
            var t = left.Thermal + right.Thermal;
            var s = left.Structural + right.Structural;
            var i = left.Incursion + right.Incursion;
            return new DamageStrength(t, s, i);
        }

        /// <summary>
        /// Returns the damage inflicted (a positive DamageStrength) on the defender (right operand) by the attacker (left operand).
        /// </summary>
        /// <param name="attacker">The attacker.</param>
        /// <param name="defender">The defender.</param>
        /// <returns>
        /// The damage (a positive DamageStrength) inflicted on the defender by the attacker.
        /// </returns>
        public static DamageStrength operator -(DamageStrength attacker, DamageStrength defender) {
            var t = attacker.Thermal - defender.Thermal;
            if (t < Constants.ZeroF) { t = Constants.ZeroF; }
            var s = attacker.Structural - defender.Structural;
            if (s < Constants.ZeroF) { s = Constants.ZeroF; }
            var i = attacker.Incursion - defender.Incursion;
            if (i < Constants.ZeroF) { i = Constants.ZeroF; }
            return new DamageStrength(t, s, i);
        }

        /// <summary>
        /// Scales this <c>strength</c> by <c>scaler</c>.
        /// </summary>
        /// <param name="strength">The strength.</param>
        /// <param name="scaler">The scaler.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static DamageStrength operator *(DamageStrength strength, float scaler) {
            Utility.ValidateNotNegative(scaler);
            var t = strength.Thermal * scaler;
            var s = strength.Structural * scaler;
            var i = strength.Incursion * scaler;
            return new DamageStrength(t, s, i);
        }

        /// <summary>
        /// Scales this <c>strength</c> by <c>scaler</c>.
        /// </summary>
        /// <param name="scaler">The scaler.</param>
        /// <param name="strength">The strength.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static DamageStrength operator *(float scaler, DamageStrength strength) {
            return strength * scaler;
        }

        #endregion


        public string DebugName {
            get {
                return DebugNameFormat.Inject(GetType().Name, Thermal.FormatValue(), Structural.FormatValue(), Incursion.FormatValue());
            }
        }

        public float Total { get { return Thermal + Structural + Incursion; } }

        public float Thermal { get; private set; }

        public float Structural { get; private set; }

        public float Incursion { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DamageStrength" /> struct.
        /// DamageCategory.None is illegal.
        /// </summary>
        /// <param name="damageCat">The DamageCategory.</param>
        /// <param name="value">The value.</param>
        public DamageStrength(DamageCategory damageCat, float value)
            : this() {
            Utility.ValidateNotNegative(value);
            switch (damageCat) {
                case DamageCategory.Thermal:
                    Thermal = value;
                    break;
                case DamageCategory.Structural:
                    Structural = value;
                    break;
                case DamageCategory.Incursion:
                    Incursion = value;
                    break;
                case DamageCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(damageCat));
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DamageStrength" /> struct.
        /// </summary>
        /// <param name="thermal">The thermal.</param>
        /// <param name="structural">The structural.</param>
        /// <param name="incursion">The incursion.</param>
        public DamageStrength(float thermal, float structural, float incursion)
            : this() {
            Utility.ValidateNotNegative(thermal, structural, incursion);
            Thermal = thermal;
            Structural = structural;
            Incursion = incursion;
        }

        public float GetValue(DamageCategory damageCat) {
            switch (damageCat) {
                case DamageCategory.Thermal:
                    return Thermal;
                case DamageCategory.Structural:
                    return Structural;
                case DamageCategory.Incursion:
                    return Incursion;
                case DamageCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(damageCat));
            }
        }

        private string ConstructLabelOutput(bool includeTotalValue) {
            string thermal = Thermal.FormatValue();
            string structural = Structural.FormatValue();
            string incursion = Incursion.FormatValue();

            if (includeTotalValue) {
                string total = Total.FormatValue();
                return LabelFormatWithTotal.Inject(total, DamageCategory.Thermal.GetEnumAttributeText(), thermal,
                    DamageCategory.Structural.GetEnumAttributeText(), structural, DamageCategory.Incursion.GetEnumAttributeText(), incursion);
            }
            return LabelFormatNoTotal.Inject(DamageCategory.Thermal.GetEnumAttributeText(), thermal, DamageCategory.Structural.GetEnumAttributeText(),
                structural, DamageCategory.Incursion.GetEnumAttributeText(), incursion);
        }

        #region Object.Equals and GetHashCode Override

        public override bool Equals(object obj) {
            if (!(obj is DamageStrength)) { return false; }
            return Equals((DamageStrength)obj);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// See Page 254, C# 4.0 in a Nutshell.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode() {
            unchecked { // http://dobrzanski.net/2010/09/13/csharp-gethashcode-cause-overflowexception/
                int hash = 17;  // 17 = some prime number
                hash = hash * 31 + Thermal.GetHashCode(); // 31 = another prime number
                hash = hash * 31 + Structural.GetHashCode();
                hash = hash * 31 + Incursion.GetHashCode();
                return hash;
            }
        }

        #endregion

        public string ToLabel(bool includeTotal = false) { return ConstructLabelOutput(includeTotal); }

        public string ToTextHud(bool includeTotal = false) {
            if (Total == Constants.ZeroF) {
                return string.Empty;
            }
            string totalText = includeTotal ? Total.FormatValue() : string.Empty;
            return DebugNameFormat.Inject(totalText, Thermal.FormatValue(), Structural.FormatValue(), Incursion.FormatValue());
        }

        public override string ToString() {
            return DebugName;
        }

        #region IEquatable<DamageStrength> Members

        public bool Equals(DamageStrength other) {
            return Thermal == other.Thermal && Structural == other.Structural && Incursion == other.Incursion;
        }

        #endregion

        #region IComparable<DamageStrength> Members

        public int CompareTo(DamageStrength other) {
            //D.Log("{0}.CompareTo({1}) called.", ToString(), other.ToString());
            return Total.CompareTo(other.Total);
        }

        #endregion

    }
}

