// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DamageStrength.cs
// Immutable data container holding the damage infliction and mitigation capabilities of ordnance and equipment.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;

    /// <summary>
    /// Immutable data container holding the damage infliction and mitigation capabilities of ordnance and equipment.
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
            var t = left._thermalValue + right._thermalValue;
            var s = left._structuralValue + right._structuralValue;
            var i = left._incursionValue + right._incursionValue;
            return new DamageStrength(t, s, i);
        }

        /// <summary>
        /// Returns the damage to be inflicted (a >= 0 DamageStrength) on the defender (right operand) 
        /// by the attacker (left operand) after the defender mitigates what damage it can. 
        /// </summary>
        /// <param name="attacker">The attacker.</param>
        /// <param name="defender">The defender.</param>
        /// <returns></returns>
        public static DamageStrength operator -(DamageStrength attacker, DamageStrength defender) {
            var t = attacker._thermalValue - defender._thermalValue;
            if (t < Constants.ZeroF) { t = Constants.ZeroF; }
            var s = attacker._structuralValue - defender._structuralValue;
            if (s < Constants.ZeroF) { s = Constants.ZeroF; }
            var i = attacker._incursionValue - defender._incursionValue;
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
            var t = strength._thermalValue * scaler;
            var s = strength._structuralValue * scaler;
            var i = strength._incursionValue * scaler;
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
                return DebugNameFormat.Inject(GetType().Name, _thermalValue.FormatValue(), _structuralValue.FormatValue(), _incursionValue.FormatValue());
            }
        }

        public float __Total { get { return _thermalValue + _structuralValue + _incursionValue; } }

        private float _thermalValue;
        private float _structuralValue;
        private float _incursionValue;

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
                    _thermalValue = value;
                    break;
                case DamageCategory.Structural:
                    _structuralValue = value;
                    break;
                case DamageCategory.Incursion:
                    _incursionValue = value;
                    break;
                case DamageCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(damageCat));
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DamageStrength" /> struct.
        /// <remarks>3.9.18 In general, this constructor is limited in use to defensive countermeasure damage mitigation rather than
        /// offensive weapon damage potential. My reasoning: I don't want the player focusing their decisions on damage potential mix
        /// of a weapon as I don't consider it to be an interesting decision, aka its too far down in the weeds. A more
        /// interesting decision is what weapons and defenses to deploy against particular enemies who have specialized in their
        /// own weapons and defenses. However, I can imagine that more advanced weapons of a particular type (e.g. missiles) 
        /// could deal small amounts of supplemental damage in categories not usually associated with a (missile) weapon type...</remarks>
        /// <remarks>4.20.18 Made it private to require use of the + operator in constructing multi-DamageCategory instances.</remarks>
        /// </summary>
        /// <param name="thermal">The thermal.</param>
        /// <param name="structural">The structural.</param>
        /// <param name="incursion">The incursion.</param>
        private DamageStrength(float thermal, float structural, float incursion)
            : this() {
            Utility.ValidateNotNegative(thermal, structural, incursion);
            _thermalValue = thermal;
            _structuralValue = structural;
            _incursionValue = incursion;
        }

        public float GetValue(DamageCategory damageCat) {
            switch (damageCat) {
                case DamageCategory.Thermal:
                    return _thermalValue;
                case DamageCategory.Structural:
                    return _structuralValue;
                case DamageCategory.Incursion:
                    return _incursionValue;
                case DamageCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(damageCat));
            }
        }

        private string ConstructLabelOutput(bool includeTotalValue) {
            string thermalValueText = _thermalValue.FormatValue();
            string structuralValueText = _structuralValue.FormatValue();
            string incursionValueText = _incursionValue.FormatValue();

            if (includeTotalValue) {
                string total = __Total.FormatValue();
                return LabelFormatWithTotal.Inject(total, DamageCategory.Thermal.GetEnumAttributeText(), thermalValueText,
                    DamageCategory.Structural.GetEnumAttributeText(), structuralValueText, DamageCategory.Incursion.GetEnumAttributeText(),
                    incursionValueText);
            }
            return LabelFormatNoTotal.Inject(DamageCategory.Thermal.GetEnumAttributeText(), thermalValueText,
                DamageCategory.Structural.GetEnumAttributeText(), structuralValueText, DamageCategory.Incursion.GetEnumAttributeText(),
                incursionValueText);
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
                hash = hash * 31 + _thermalValue.GetHashCode(); // 31 = another prime number
                hash = hash * 31 + _structuralValue.GetHashCode();
                hash = hash * 31 + _incursionValue.GetHashCode();
                return hash;
            }
        }

        #endregion

        public string ToLabel(bool includeTotal = false) { return ConstructLabelOutput(includeTotal); }

        public string ToTextHud(bool includeTotal = false) {
            if (__Total == Constants.ZeroF) {
                return string.Empty;
            }
            string totalText = includeTotal ? __Total.FormatValue() : string.Empty;
            return DebugNameFormat.Inject(totalText, _thermalValue.FormatValue(), _structuralValue.FormatValue(), _incursionValue.FormatValue());
        }

        public override string ToString() {
            return DebugName;
        }

        #region IEquatable<DamageStrength> Members

        public bool Equals(DamageStrength other) {
            return _thermalValue == other._thermalValue && _structuralValue == other._structuralValue && _incursionValue == other._incursionValue;
        }

        #endregion

        #region IComparable<DamageStrength> Members

        public int CompareTo(DamageStrength other) {
            //D.Log("{0}.CompareTo({1}) called.", ToString(), other.ToString());
            return __Total.CompareTo(other.__Total);
        }

        #endregion

    }
}

