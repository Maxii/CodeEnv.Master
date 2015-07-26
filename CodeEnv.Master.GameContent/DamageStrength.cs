// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DamageStrength.cs
// COMMENT - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using CodeEnv.Master.GameContent;
    using UnityEngine;

    /// <summary>
    /// Immutable data container holding the offense and defensive damage 
    /// infliction and prevention capabilities of weapons and countermeasures respectively.
    /// </summary>
    public struct DamageStrength : IEquatable<DamageStrength>, IComparable<DamageStrength> {

        private static string _toStringFormat = "T({0:0.#}), A({1:0.#}), K({2:0.#})";

        private static string _labelFormatWithTotal = "{0}" + Constants.NewLine
                                             + GameConstants.IconMarker_Beam + " {1}" + Constants.NewLine
                                             + GameConstants.IconMarker_Missile + " {2}" + Constants.NewLine
                                             + GameConstants.IconMarker_Projectile + " {3}";

        private static string _labelFormatNoTotal = GameConstants.IconMarker_Beam + " {0}" + Constants.NewLine
                                             + GameConstants.IconMarker_Missile + " {1}" + Constants.NewLine
                                             + GameConstants.IconMarker_Projectile + " {2}";

        #region Operators Override

        // see C# 4.0 In a Nutshell, page 254

        public static bool operator ==(DamageStrength left, DamageStrength right) {
            return left.Equals(right);
        }

        public static bool operator !=(DamageStrength left, DamageStrength right) {
            return !left.Equals(right);
        }

        public static DamageStrength operator +(DamageStrength left, DamageStrength right) {
            var b = left.Thermal + right.Thermal;
            var m = left.Atomic + right.Atomic;
            var p = left.Kinetic + right.Kinetic;
            return new DamageStrength(b, m, p);
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
            D.Log("Termal result: " + t);
            if (t < Constants.ZeroF) { t = Constants.ZeroF; }
            var a = attacker.Atomic - defender.Atomic;
            D.Log("Atomic result: " + a);
            if (a < Constants.ZeroF) { a = Constants.ZeroF; }
            var k = attacker.Kinetic - defender.Kinetic;
            D.Log("Kinetic result: " + k);
            if (k < Constants.ZeroF) { k = Constants.ZeroF; }
            return new DamageStrength(t, a, k);
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
            Arguments.ValidateNotNegative(scaler);
            var t = strength.Thermal * scaler;
            var a = strength.Atomic * scaler;
            var k = strength.Kinetic * scaler;
            return new DamageStrength(t, a, k);
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

        public float Total { get { return Thermal + Atomic + Kinetic; } }

        public float Thermal { get; private set; }

        public float Atomic { get; private set; }

        public float Kinetic { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DamageStrength" /> struct.
        /// DamageCategory.None is illegal.
        /// </summary>
        /// <param name="damageCat">The DamageCategory.</param>
        /// <param name="value">The value.</param>
        public DamageStrength(DamageCategory damageCat, float value)
            : this() {
            Arguments.ValidateNotNegative(value);
            switch (damageCat) {
                case DamageCategory.Thermal:
                    Thermal = value;
                    break;
                case DamageCategory.Atomic:
                    Atomic = value;
                    break;
                case DamageCategory.Kinetic:
                    Kinetic = value;
                    break;
                case DamageCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(damageCat));
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DamageStrength" /> struct.
        /// </summary>
        /// <param name="thermal"><c>DamageCategory.Thermal</c> value.</param>
        /// <param name="atomic"><c>DamageCategory.Atomic</c> value.</param>
        /// <param name="kinetic"><c>DamageCategory.Kinetic</c> value.</param>
        public DamageStrength(float thermal, float atomic, float kinetic)
            : this() {
            Arguments.ValidateNotNegative(thermal, atomic, kinetic);
            Thermal = thermal;
            Atomic = atomic;
            Kinetic = kinetic;
        }

        public float GetValue(DamageCategory damageCat) {
            switch (damageCat) {
                case DamageCategory.Thermal:
                    return Thermal;
                case DamageCategory.Atomic:
                    return Atomic;
                case DamageCategory.Kinetic:
                    return Kinetic;
                case DamageCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(damageCat));
            }
        }

        private string ConstructLabelOutput(bool includeCombined) {
            string beamText = Thermal.FormatValue();
            string missileText = Atomic.FormatValue();
            string projectileText = Kinetic.FormatValue();

            if (includeCombined) {
                string combinedText = Constants.FormatFloat_0Dp.Inject(Total);
                return _labelFormatWithTotal.Inject(combinedText, beamText, missileText, projectileText);
            }
            return _labelFormatNoTotal.Inject(beamText, missileText, projectileText);
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
            int hash = 17;  // 17 = some prime number
            hash = hash * 31 + Thermal.GetHashCode(); // 31 = another prime number
            hash = hash * 31 + Atomic.GetHashCode();
            hash = hash * 31 + Kinetic.GetHashCode();
            return hash;
        }

        #endregion

        public string ToLabel(bool includeCombined = false) { return ConstructLabelOutput(includeCombined); }

        public override string ToString() { return _toStringFormat.Inject(Thermal, Atomic, Kinetic); }

        #region IEquatable<DamageStrength> Members

        public bool Equals(DamageStrength other) {
            return Thermal == other.Thermal && Atomic == other.Atomic && Kinetic == other.Kinetic;
        }

        #endregion

        #region IComparable<DamageStrength> Members

        public int CompareTo(DamageStrength other) {
            //D.Log("{0}.CompareTo({1}) called.", ToString(), other.ToString());
            return Total.CompareTo(other.Total);
        }

        #endregion

        #region Nested Classes

        public enum DamageCategory {

            None,
            Thermal,
            Atomic,
            Kinetic

        }

        #endregion


    }
}

