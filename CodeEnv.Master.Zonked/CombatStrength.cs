// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: CombatStrength.cs
// Immutable data container holding the offense and defensive damage 
// infliction and deflection capabilities of weapons, countermeasures and mortal items.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;

    /// <summary>
    /// Immutable data container holding the offense and defensive damage 
    /// infliction and prevention capabilities of weapons and countermeasures respectively.
    /// </summary>
    [Obsolete]
    public struct CombatStrength : IEquatable<CombatStrength>, IComparable<CombatStrength> {

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

        public static bool operator ==(CombatStrength left, CombatStrength right) {
            return left.Equals(right);
        }

        public static bool operator !=(CombatStrength left, CombatStrength right) {
            return !left.Equals(right);
        }

        public static CombatStrength operator +(CombatStrength left, CombatStrength right) {
            var b = left.Beam + right.Beam;
            var m = left.Missile + right.Missile;
            var p = left.Projectile + right.Projectile;
            return new CombatStrength(b, m, p);
        }

        /// <summary>
        /// Returns the damage inflicted (a positive CombatStrength) on the defender (right operand) by the attacker (left operand).
        /// </summary>
        /// <param name="attacker">The attacker.</param>
        /// <param name="defender">The defender.</param>
        /// <returns>
        /// The damage (a positive CombatStrength) inflicted on the defender by the attacker.
        /// </returns>
        public static CombatStrength operator -(CombatStrength attacker, CombatStrength defender) {
            var b = attacker.Beam - defender.Beam;
            //D.Log("Beam result: " + b);
            if (b < Constants.ZeroF) { b = Constants.ZeroF; }
            var m = attacker.Missile - defender.Missile;
            //D.Log("Missile result: " + m);
            if (m < Constants.ZeroF) { m = Constants.ZeroF; }
            var p = attacker.Projectile - defender.Projectile;
            //D.Log("Projectile result: " + p);
            if (p < Constants.ZeroF) { p = Constants.ZeroF; }
            return new CombatStrength(b, m, p);
        }

        /// <summary>
        /// Scales this <c>strength</c> by <c>scaler</c>.
        /// </summary>
        /// <param name="strength">The strength.</param>
        /// <param name="scaler">The scaler.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static CombatStrength operator *(CombatStrength strength, float scaler) {
            Arguments.ValidateNotNegative(scaler);
            var beam = strength.Beam * scaler;
            var missile = strength.Missile * scaler;
            var projectile = strength.Projectile * scaler;
            return new CombatStrength(beam, missile, projectile);
        }

        /// <summary>
        /// Scales this <c>strength</c> by <c>scaler</c>.
        /// </summary>
        /// <param name="scaler">The scaler.</param>
        /// <param name="strength">The strength.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static CombatStrength operator *(float scaler, CombatStrength strength) {
            return strength * scaler;
        }


        #endregion

        public float Combined { get { return Beam + Missile + Projectile; } }

        public float Beam { get; private set; }

        public float Missile { get; private set; }

        public float Projectile { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CombatStrength" /> struct.
        /// ArmamentCategory.None is illegal.
        /// </summary>
        /// <param name="armCategory">The armament category.</param>
        /// <param name="value">The value.</param>
        public CombatStrength(WDVCategory armCategory, float value)
            : this() {
            Arguments.ValidateNotNegative(value);
            switch (armCategory) {
                case WDVCategory.Beam:
                    Beam = value;
                    break;
                case WDVCategory.Missile:
                    Missile = value;
                    break;
                case WDVCategory.Projectile:
                    Projectile = value;
                    break;
                case WDVCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(armCategory));
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CombatStrength"/> struct.
        /// </summary>
        /// <param name="beam"><c>ArmamentCategory.Beam</c> value.</param>
        /// <param name="missile"><c>ArmamentCategory.Missile</c> value.</param>
        /// <param name="projectile"><c>ArmamentCategory.Projectile</c> value.</param>
        public CombatStrength(float beam, float missile, float projectile)
            : this() {
            Arguments.ValidateNotNegative(beam, missile, projectile);
            Beam = beam;
            Missile = missile;
            Projectile = projectile;
        }

        public float GetValue(WDVCategory armament) {
            switch (armament) {
                case WDVCategory.Beam:
                    return Beam;
                case WDVCategory.Missile:
                    return Missile;
                case WDVCategory.Projectile:
                    return Projectile;
                case WDVCategory.None:
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
            if (!(obj is CombatStrength)) { return false; }
            return Equals((CombatStrength)obj);
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

        #region IEquatable<CombatStrength> Members

        public bool Equals(CombatStrength other) {
            return Beam == other.Beam && Missile == other.Missile && Projectile == other.Projectile;
        }

        #endregion

        #region IComparable<CombatStrength> Members

        public int CompareTo(CombatStrength other) {
            //D.Log("{0}.CompareTo({1}) called.", ToString(), other.ToString());
            return Combined.CompareTo(other.Combined);
        }

        #endregion

    }
}

