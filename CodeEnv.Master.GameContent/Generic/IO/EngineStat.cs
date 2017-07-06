﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: EngineStat.cs
// Immutable stat for an element's engine.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {
    using System;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable stat for an element's engine.
    /// <remarks>Implements value-based Equality and HashCode.</remarks>
    /// </summary>
    public class EngineStat : AEquipmentStat {

        #region Comparison Operators Override

        // see C# 4.0 In a Nutshell, page 254

        public static bool operator ==(EngineStat left, EngineStat right) {
            // https://msdn.microsoft.com/en-us/library/ms173147(v=vs.90).aspx
            if (ReferenceEquals(left, right)) { return true; }
            if (((object)left == null) || ((object)right == null)) { return false; }
            return left.Equals(right);
        }

        public static bool operator !=(EngineStat left, EngineStat right) {
            return !(left == right);
        }

        #endregion

        public override EquipmentCategory Category { get { return EquipmentCategory.Propulsion; } }

        public bool IsFtlEngine { get; private set; }

        /// <summary>
        /// The maximum power the engine can project as thrust when operating. 
        /// <remarks>FullSpeed = FullPropulsionPower / (Mass * rigidbody.drag).
        /// This value uses a Game Hour denominator. It is adjusted in
        /// realtime to a Unity seconds value in EngineRoom.ApplyThrust() using GeneralSettings.HoursPerSecond.</remarks>
        /// </summary>
        public float FullPropulsionPower { get; private set; }

        public float MaxTurnRate { get; private set; }  // IMPROVE replace with LateralThrust and calc maxTurnRate using mass

        /// <summary>
        /// Initializes a new instance of the <see cref="EngineStat" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="imageAtlasID">The image atlas identifier.</param>
        /// <param name="imageFilename">The image filename.</param>
        /// <param name="description">The description.</param>
        /// <param name="fullPropulsionPower">The maximum propulsion power the engine can produce.</param>
        /// <param name="maxTurnRate">The maximum turn rate the engine is capable of.</param>
        /// <param name="size">The total physical space consumed by the engine.</param>
        /// <param name="mass">The total mass of the engine.</param>
        /// <param name="expense">The total expense consumed by the engine.</param>
        /// <param name="isDamageable">if set to <c>true</c> [is damageable].</param>
        /// <param name="isFtlEngine">if set to <c>true</c> [is FTL engine].</param>
        public EngineStat(string name, AtlasID imageAtlasID, string imageFilename, string description, float fullPropulsionPower,
            float maxTurnRate, float size, float mass, float expense, bool isDamageable, bool isFtlEngine)
            : base(name, imageAtlasID, imageFilename, description, size, mass, Constants.ZeroF, expense, isDamageable) {
            FullPropulsionPower = fullPropulsionPower;
            if (maxTurnRate < TempGameValues.MinimumTurnRate) {
                D.Warn("{0}'s MaxTurnRate {1:0.#} is too low. Game MinTurnRate = {2:0.#}.", DebugName, maxTurnRate, TempGameValues.MinimumTurnRate);
            }
            MaxTurnRate = maxTurnRate;
            IsFtlEngine = isFtlEngine;
        }

        #region Object.Equals and GetHashCode Override

        public override int GetHashCode() {
            unchecked {
                int hash = base.GetHashCode();
                hash = hash * 31 + IsFtlEngine.GetHashCode();
                hash = hash * 31 + FullPropulsionPower.GetHashCode();
                hash = hash * 31 + MaxTurnRate.GetHashCode();
                return hash;
            }
        }

        public override bool Equals(object obj) {
            if (base.Equals(obj)) {
                EngineStat oStat = (EngineStat)obj;
                return oStat.IsFtlEngine == IsFtlEngine && oStat.FullPropulsionPower == FullPropulsionPower
                    && oStat.MaxTurnRate == MaxTurnRate;
            }
            return false;
        }

        #endregion

    }
}

