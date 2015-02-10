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

#define DEBUG_LOG
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
    public struct CombatStrength : IEquatable<CombatStrength> {

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
            var p = left.Particle + right.Particle;
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
            var p = attacker.Particle - defender.Particle;
            //D.Log("Particle result: " + p);
            if (p < Constants.ZeroF) { p = Constants.ZeroF; }
            return new CombatStrength(b, m, p);
        }

        #endregion

        private static string _toStringFormat = "B({0:0.#}), M({1:0.#}), P({2:0.#})";

        public float Combined { get { return Beam + Missile + Particle; } }

        public float Beam { get; private set; }

        public float Missile { get; private set; }

        public float Particle { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CombatStrength" /> struct.
        /// ArmamentCategory.None is illegal.
        /// </summary>
        /// <param name="armCategory">The armament category.</param>
        /// <param name="value">The value.</param>
        public CombatStrength(ArmamentCategory armCategory, float value)
            : this() {
            Arguments.ValidateNotNegative(value);
            switch (armCategory) {
                case ArmamentCategory.Beam:
                    Beam = value;
                    break;
                case ArmamentCategory.Missile:
                    Missile = value;
                    break;
                case ArmamentCategory.Particle:
                    Particle = value;
                    break;
                case ArmamentCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(armCategory));
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CombatStrength"/> struct.
        /// </summary>
        /// <param name="beam"><c>ArmamentCategory.Beam</c> value.</param>
        /// <param name="missile"><c>ArmamentCategory.Missile</c> value.</param>
        /// <param name="particle"><c>ArmamentCategory.Particle</c> value.</param>
        private CombatStrength(float beam, float missile, float particle)
            : this() {
            Arguments.ValidateNotNegative(beam, missile, particle);
            Beam = beam;
            Missile = missile;
            Particle = particle;
        }

        public float GetValue(ArmamentCategory armament) {
            switch (armament) {
                case ArmamentCategory.Beam:
                    return Beam;
                case ArmamentCategory.Missile:
                    return Missile;
                case ArmamentCategory.Particle:
                    return Particle;
                case ArmamentCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(armament));
            }
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
            hash = hash * 31 + Particle.GetHashCode();
            return hash;
        }

        #endregion

        public override string ToString() {
            return _toStringFormat.Inject(Beam, Missile, Particle);
        }

        #region IEquatable<CombatStrength> Members

        public bool Equals(CombatStrength other) {
            return Beam == other.Beam && Missile == other.Missile && Particle == other.Particle;
        }

        #endregion

    }
}

