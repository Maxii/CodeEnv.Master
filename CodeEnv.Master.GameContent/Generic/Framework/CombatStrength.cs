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
    using System.Collections.Generic;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable data container holding the offense and defensive damage 
    /// infliction and deflection capabilities of weapons, countermeasures and mortal items.
    /// Note: This is a custom implementation that defaults to a value of 1F for each ArmamentCategory.
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
            var b = left.GetValue(ArmamentCategory.Beam) + right.GetValue(ArmamentCategory.Beam);
            var m = left.GetValue(ArmamentCategory.Missile) + right.GetValue(ArmamentCategory.Missile);
            var p = left.GetValue(ArmamentCategory.Particle) + right.GetValue(ArmamentCategory.Particle);
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
            var bValue = attacker.GetValue(ArmamentCategory.Beam) - defender.GetValue(ArmamentCategory.Beam);
            //D.Log("Beam result: " + bValue);
            if (bValue < Constants.ZeroF) { bValue = Constants.ZeroF; }
            var mValue = attacker.GetValue(ArmamentCategory.Missile) - defender.GetValue(ArmamentCategory.Missile);
            //D.Log("Missile result: " + mValue);
            if (mValue < Constants.ZeroF) { mValue = Constants.ZeroF; }
            var pValue = attacker.GetValue(ArmamentCategory.Particle) - defender.GetValue(ArmamentCategory.Particle);
            //D.Log("Particle result: " + pValue);
            if (pValue < Constants.ZeroF) { pValue = Constants.ZeroF; }
            return new CombatStrength(bValue, mValue, pValue);
        }

        #endregion

        private static string _toStringFormat = "(B[{0:0.#}], M[{1:0.#}], P[{2:0.#}])";

        private float _combined;
        public float Combined {
            get {
                if (_armamentValueLookup == null) { return 3F; }    // default CombatStrength
                return _combined;
            }
        }

        private IDictionary<ArmamentCategory, float> _armamentValueLookup;

        /// <summary>
        /// Initializes a new instance of the <see cref="CombatStrength" /> struct.
        /// ArmamentCategory.None is illegal.
        /// </summary>
        /// <param name="armCategory">The armament category.</param>
        /// <param name="value">The value.</param>
        public CombatStrength(ArmamentCategory armCategory, float value)
            : this() {
            Arguments.ValidateNotNegative(value);
            _armamentValueLookup = new Dictionary<ArmamentCategory, float>() {
                {ArmamentCategory.Beam, Constants.OneF},
                {ArmamentCategory.Missile, Constants.OneF},
                {ArmamentCategory.Particle, Constants.OneF}
            };
            _armamentValueLookup[armCategory] = value;
            _combined = value + 2F;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CombatStrength"/> struct.
        /// For use primarily in setting the defensive strength of a Command.
        /// </summary>
        /// <param name="beam"><c>ArmamentCategory.Beam</c> value.</param>
        /// <param name="missile"><c>ArmamentCategory.Missile</c> value.</param>
        /// <param name="particle"><c>ArmamentCategory.Particle</c> value.</param>
        private CombatStrength(float beam, float missile, float particle)
            : this() {
            Arguments.ValidateNotNegative(beam, missile, particle);
            _armamentValueLookup = new Dictionary<ArmamentCategory, float>() {
                {ArmamentCategory.Beam, beam},
                {ArmamentCategory.Missile, missile},
                {ArmamentCategory.Particle, particle}
            };
            _combined = beam + missile + particle;
        }

        public float GetValue(ArmamentCategory armament) {
            if (_armamentValueLookup == null) { return Constants.OneF; }    // default CombatStrength
            return _armamentValueLookup[armament];
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
            hash = hash * 31 + GetValue(ArmamentCategory.Beam).GetHashCode(); // 31 = another prime number
            hash = hash * 31 + GetValue(ArmamentCategory.Missile).GetHashCode();
            hash = hash * 31 + GetValue(ArmamentCategory.Particle).GetHashCode();
            return hash;
        }

        #endregion

        public override string ToString() {
            return _toStringFormat.Inject(GetValue(ArmamentCategory.Beam),
                                            GetValue(ArmamentCategory.Missile),
                                            GetValue(ArmamentCategory.Particle));
        }

        #region IEquatable<CombatStrength> Members

        public bool Equals(CombatStrength other) {
            return GetValue(ArmamentCategory.Beam) == other.GetValue(ArmamentCategory.Beam)
                && GetValue(ArmamentCategory.Missile) == other.GetValue(ArmamentCategory.Missile)
                && GetValue(ArmamentCategory.Particle) == other.GetValue(ArmamentCategory.Particle);
        }

        #endregion

    }
}

