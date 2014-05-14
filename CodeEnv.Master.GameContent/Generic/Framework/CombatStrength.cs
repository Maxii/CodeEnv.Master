// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: CombatStrength.cs
// Immutable data container holding the offense and defensive damage 
// infliction and absorbtion capabilities of armaments, elements or Units.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Linq;
    using System.Text;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;

    /// <summary>
    /// Immutable data container holding the offense and defensive damage 
    /// infliction and absorbtion capabilities of armaments, elements or Units.
    /// </summary>
    public struct CombatStrength : IEquatable<CombatStrength> {

        public struct CombatStrengthValuePair : IEquatable<CombatStrengthValuePair> {

            #region Operators Override

            // see C# 4.0 In a Nutshell, page 254

            public static bool operator ==(CombatStrengthValuePair left, CombatStrengthValuePair right) {
                return left.Equals(right);
            }

            public static bool operator !=(CombatStrengthValuePair left, CombatStrengthValuePair right) {
                return !left.Equals(right);
            }

            public static CombatStrengthValuePair operator +(CombatStrengthValuePair left, CombatStrengthValuePair right) {
                D.Assert(left.ArmCategory == right.ArmCategory);
                var newValue = left.Value + right.Value;
                return new CombatStrengthValuePair(left.ArmCategory, newValue);
            }

            #endregion

            public ArmamentCategory ArmCategory { get; private set; }
            public float Value { get; private set; }

            public CombatStrengthValuePair(ArmamentCategory armCat, float value)
                : this() {
                ArmCategory = armCat;
                Value = value;
            }

            #region Object.Equals and GetHashCode Override

            public override bool Equals(object obj) {
                if (!(obj is CombatStrengthValuePair)) { return false; }
                return Equals((CombatStrengthValuePair)obj);
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
                hash = hash * 31 + ArmCategory.GetHashCode(); // 31 = another prime number
                hash = hash * 31 + Value.GetHashCode();
                return hash;
            }

            #endregion

            public override string ToString() {
                return "{0}[{1:0.#}]".Inject(ArmCategory.GetName(), Value);
            }

            #region IEquatable<CombatStrengthValuePair> Members

            public bool Equals(CombatStrengthValuePair other) {
                return ArmCategory == other.ArmCategory && Value == other.Value;
            }

            #endregion

        }

        #region Operators Override

        // see C# 4.0 In a Nutshell, page 254

        public static bool operator ==(CombatStrength left, CombatStrength right) {
            return left.Equals(right);
        }

        public static bool operator !=(CombatStrength left, CombatStrength right) {
            return !left.Equals(right);
        }

        public static CombatStrength operator +(CombatStrength left, CombatStrength right) {
            var bo = left.BeamOffense + right.BeamOffense;
            var bd = left.BeamDefense + right.BeamDefense;
            var mo = left.MissileOffense + right.MissileOffense;
            var md = left.MissileDefense + right.MissileDefense;
            var po = left.ParticleOffense + right.ParticleOffense;
            var pd = left.ParticleDefense + right.ParticleDefense;
            return new CombatStrength(bo, bd, mo, md, po, pd);
        }

        /// <summary>
        /// Returns the damage inflicted (a positive value) on the defender (left operand) by the attacker (right operand).
        /// </summary>
        /// <param name="defender">The defender.</param>
        /// <param name="attacker">The attacker.</param>
        /// <returns>
        /// The damage (a positive value) inflicted on the defender by the attacker.
        /// </returns>
        public static float operator -(CombatStrength defender, CombatStrength attacker) {
            float damage = Constants.ZeroF;
            float d;
            if ((d = defender.BeamDefense - attacker.BeamOffense) < Constants.ZeroF) {
                damage += Math.Abs(d);
            }
            if ((d = defender.MissileDefense - attacker.MissileOffense) < Constants.ZeroF) {
                damage += Math.Abs(d);
            }
            if ((d = defender.ParticleDefense - attacker.ParticleOffense) < Constants.ZeroF) {
                damage += Math.Abs(d);
            }
            return damage;
        }

        #endregion

        private static string _toStringFormat = "(B[{0:0.#}/{1:0.#}], M[{2:0.#}/{3:0.#}], P[{4:0.#}/{5:0.#}])";

        public float BeamOffense { get; private set; }

        public float BeamDefense { get; private set; }

        public float MissileOffense { get; private set; }

        public float MissileDefense { get; private set; }

        public float ParticleOffense { get; private set; }

        public float ParticleDefense { get; private set; }

        public float Combined {
            get {
                return MissileDefense + MissileOffense + ParticleDefense + ParticleOffense + BeamDefense + BeamOffense;
            }
        }

        /// <summary>
        /// Initializes a new instance of the 
        /// <see cref="CombatStrength" /> struct.
        /// ArmamentCategory.None is illegal.
        /// </summary>
        /// <param name="armCategory">The armament category.</param>
        /// <param name="value">The value.</param>
        public CombatStrength(ArmamentCategory armCategory, float value)
            : this(new CombatStrengthValuePair(armCategory, value)) { }

        public CombatStrength(params CombatStrengthValuePair[] armamentValuePairs)
            : this() {
            Arguments.ValidateNotNullOrEmpty(armamentValuePairs);
            // multiple pairs with the same ArmamentCategory are not allowed
            var duplicates = armamentValuePairs.GroupBy(wvp => wvp.ArmCategory).Where(group => group.Count() > 1);
            if (duplicates.Any()) {
                string duplicateArmsCategories = duplicates.Select(group => group.Key).Concatenate();
                throw new ArgumentException("Duplicate {0} values found: {1}.".Inject(typeof(ArmamentCategory).Name, duplicateArmsCategories));
            }

            foreach (var armValuePair in armamentValuePairs) {
                var armCat = armValuePair.ArmCategory;
                switch (armCat) {
                    case ArmamentCategory.BeamOffense:
                        BeamOffense = armValuePair.Value;
                        break;
                    case ArmamentCategory.BeamDefense:
                        BeamDefense = armValuePair.Value;
                        break;
                    case ArmamentCategory.MissileOffense:
                        MissileOffense = armValuePair.Value;
                        break;
                    case ArmamentCategory.MissileDefense:
                        MissileDefense = armValuePair.Value;
                        break;
                    case ArmamentCategory.ParticleOffense:
                        ParticleOffense = armValuePair.Value;
                        break;
                    case ArmamentCategory.ParticleDefense:
                        ParticleDefense = armValuePair.Value;
                        break;
                    case ArmamentCategory.None:
                    default:
                        throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(armCat));
                }
            }
        }

        public CombatStrength(float beamOff, float beamDef, float misOff, float misDef, float partOff, float partDef)
            : this() {
            BeamOffense = beamOff;
            BeamDefense = beamDef;
            MissileOffense = misOff;
            MissileDefense = misDef;
            ParticleOffense = partOff;
            ParticleDefense = partDef;
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
            hash = hash * 31 + BeamOffense.GetHashCode(); // 31 = another prime number
            hash = hash * 31 + BeamDefense.GetHashCode();
            hash = hash * 31 + MissileOffense.GetHashCode();
            hash = hash * 31 + MissileDefense.GetHashCode();
            hash = hash * 31 + ParticleOffense.GetHashCode();
            hash = hash * 31 + ParticleDefense.GetHashCode();
            return hash;
        }

        #endregion

        public override string ToString() {
            return _toStringFormat.Inject(BeamOffense, BeamDefense, MissileOffense, MissileDefense, ParticleOffense, ParticleDefense);
        }

        #region IEquatable<CombatStrength> Members

        public bool Equals(CombatStrength other) {
            return BeamOffense == other.BeamOffense && BeamDefense == other.BeamDefense &&
                MissileOffense == other.MissileOffense && MissileDefense == other.MissileDefense
                && ParticleOffense == other.ParticleOffense && ParticleDefense == other.ParticleDefense;
        }

        #endregion

    }
}

