// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: CombatStrength2.cs
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
    /// 
    /// </summary>
    public struct CombatStrength2 : IEquatable<CombatStrength2>, IComparable<CombatStrength2> {

        private static string _toStringFormat = "B({0:0.#}), M({1:0.#}), P({2:0.#})";

        private static string _labelFormatWithCombined = "{0}" + Constants.NewLine
                                             + GameConstants.IconMarker_Beam + " {1}" + Constants.NewLine
                                             + GameConstants.IconMarker_Missile + " {2}" + Constants.NewLine
                                             + GameConstants.IconMarker_Projectile + " {3}";

        private static string _labelFormatNoCombined = GameConstants.IconMarker_Beam + " {0}" + Constants.NewLine
                                             + GameConstants.IconMarker_Missile + " {1}" + Constants.NewLine
                                             + GameConstants.IconMarker_Projectile + " {2}";

        #region Operators Override

        // see C# 4.0 In a Nutshell, page 254

        public static bool operator ==(CombatStrength2 left, CombatStrength2 right) {
            return left.Equals(right);
        }

        public static bool operator !=(CombatStrength2 left, CombatStrength2 right) {
            return !left.Equals(right);
        }

        public static CombatStrength2 operator +(CombatStrength2 left, CombatStrength2 right) {
            var b = left.Beam + right.Beam;
            var m = left.Missile + right.Missile;
            var p = left.Projectile + right.Projectile;
            return new CombatStrength2(b, m, p);
        }

        /// <summary>
        /// Returns the damage inflicted (a positive CombatStrength) on the defender (right operand) by the attacker (left operand).
        /// </summary>
        /// <param name="attacker">The attacker.</param>
        /// <param name="defender">The defender.</param>
        /// <returns>
        /// The damage (a positive CombatStrength) inflicted on the defender by the attacker.
        /// </returns>
        public static CombatStrength2 operator -(CombatStrength2 attacker, CombatStrength2 defender) {
            var b = attacker.Beam - defender.Beam;
            //D.Log("Beam result: " + b);
            if (b < Constants.ZeroF) { b = Constants.ZeroF; }
            var m = attacker.Missile - defender.Missile;
            //D.Log("Missile result: " + m);
            if (m < Constants.ZeroF) { m = Constants.ZeroF; }
            var p = attacker.Projectile - defender.Projectile;
            //D.Log("Projectile result: " + p);
            if (p < Constants.ZeroF) { p = Constants.ZeroF; }
            return new CombatStrength2(b, m, p);
        }

        /// <summary>
        /// Scales this <c>strength</c> by <c>scaler</c>.
        /// </summary>
        /// <param name="strength">The strength.</param>
        /// <param name="scaler">The scaler.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static CombatStrength2 operator *(CombatStrength2 strength, float scaler) {
            Arguments.ValidateNotNegative(scaler);
            var beam = strength.Beam * scaler;
            var missile = strength.Missile * scaler;
            var projectile = strength.Projectile * scaler;
            return new CombatStrength2(beam, missile, projectile);
        }

        /// <summary>
        /// Scales this <c>strength</c> by <c>scaler</c>.
        /// </summary>
        /// <param name="scaler">The scaler.</param>
        /// <param name="strength">The strength.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static CombatStrength2 operator *(float scaler, CombatStrength2 strength) {
            return strength * scaler;
        }


        #endregion

        public float Combined { get { return Beam + Missile + Projectile; } }

        public float Beam { get; private set; }

        public float Missile { get; private set; }

        public float Projectile { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CombatStrength2" /> struct.
        /// ArmamentCategory.None is illegal.
        /// </summary>
        /// <param name="armCategory">The armament category.</param>
        /// <param name="value">The value.</param>
        public CombatStrength2(ArmamentCategory armCategory, float value)
            : this() {
            Arguments.ValidateNotNegative(value);
            switch (armCategory) {
                case ArmamentCategory.Beam:
                    Beam = value;
                    break;
                case ArmamentCategory.Missile:
                    Missile = value;
                    break;
                case ArmamentCategory.Projectile:
                    Projectile = value;
                    break;
                case ArmamentCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(armCategory));
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CombatStrength2"/> struct.
        /// </summary>
        /// <param name="beam"><c>ArmamentCategory.Beam</c> value.</param>
        /// <param name="missile"><c>ArmamentCategory.Missile</c> value.</param>
        /// <param name="projectile"><c>ArmamentCategory.Projectile</c> value.</param>
        public CombatStrength2(float beam, float missile, float projectile)
            : this() {
            Arguments.ValidateNotNegative(beam, missile, projectile);
            Beam = beam;
            Missile = missile;
            Projectile = projectile;
        }

        public float GetValue(ArmamentCategory armament) {
            switch (armament) {
                case ArmamentCategory.Beam:
                    return Beam;
                case ArmamentCategory.Missile:
                    return Missile;
                case ArmamentCategory.Projectile:
                    return Projectile;
                case ArmamentCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(armament));
            }
        }

        private string ConstructLabelOutput(bool includeCombined) {
            string beamText = Beam.FormatValue();
            string missileText = Missile.FormatValue();
            string projectileText = Projectile.FormatValue();

            if (includeCombined) {
                string combinedText = Constants.FormatFloat_0Dp.Inject(Combined);
                return _labelFormatWithCombined.Inject(combinedText, beamText, missileText, projectileText);
            }
            return _labelFormatNoCombined.Inject(beamText, missileText, projectileText);
        }

        #region Object.Equals and GetHashCode Override

        public override bool Equals(object obj) {
            if (!(obj is CombatStrength2)) { return false; }
            return Equals((CombatStrength2)obj);
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
            hash = hash * 31 + Beam.GetHashCode(); // 31 = another prime number
            hash = hash * 31 + Missile.GetHashCode();
            hash = hash * 31 + Projectile.GetHashCode();
            return hash;
        }

        #endregion

        public string ToLabel(bool includeCombined = false) { return ConstructLabelOutput(includeCombined); }

        public override string ToString() { return _toStringFormat.Inject(Beam, Missile, Projectile); }

        #region IEquatable<CombatStrength2> Members

        public bool Equals(CombatStrength2 other) {
            return Beam == other.Beam && Missile == other.Missile && Projectile == other.Projectile;
        }

        #endregion

        #region IComparable<CombatStrength2> Members

        public int CompareTo(CombatStrength2 other) {
            //D.Log("{0}.CompareTo({1}) called.", ToString(), other.ToString());
            return Combined.CompareTo(other.Combined);
        }

        #endregion


    }
}

