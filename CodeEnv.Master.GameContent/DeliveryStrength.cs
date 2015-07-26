// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DeliveryStrength.cs
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
    public struct DeliveryStrength : IEquatable<DeliveryStrength> {

        public static float MaxValue = 10F;

        #region Operators Override

        // see C# 4.0 In a Nutshell, page 254

        public static bool operator ==(DeliveryStrength left, DeliveryStrength right) {
            return left.Equals(right);
        }

        public static bool operator !=(DeliveryStrength left, DeliveryStrength right) {
            return !left.Equals(right);
        }

        /// <summary>
        /// Returns the DeliveryStrength remaining (a positive DeliveryStrength) of the attacker (left operand) after interception 
        /// by the defender (right operand). If the defender's DeliveryVehicle does not match the attacker's DeliveryVehicle then 
        /// the interception was a failure and the attackers DeliveryStrength is returned. If the DeliveryVehicles match, then the
        /// interception is successful and the value returned represents the remaining DeliveryStrength of the attacker, if any.
        /// The value returned will never be negative but can be Zero if the defender's value was equal to or exceeded the attacker's
        /// value.
        /// </summary>
        /// <param name="attacker">The attacker.</param>
        /// <param name="defender">The defender.</param>
        /// <returns>
        /// The remaining DeliveryStrength of the attacker after interception.
        /// </returns>
        public static DeliveryStrength operator -(DeliveryStrength attacker, DeliveryStrength defender) {
            D.Assert(attacker.Vehicle != ArmamentCategory.None && defender.Vehicle != ArmamentCategory.None);
            if (defender.Vehicle != attacker.Vehicle) {
                return attacker;
            }
            var v = attacker.Value - defender.Value;
            if (v <= Constants.ZeroF) {
                return new DeliveryStrength(attacker.Vehicle, Constants.ZeroF);
            }
            return new DeliveryStrength(attacker.Vehicle, v);
        }
        //public static DeliveryStrength operator -(DeliveryStrength attacker, DeliveryStrength defender) {
        //    D.Assert(attacker.Vehicle != DeliveryVehicle.None && defender.Vehicle != DeliveryVehicle.None);
        //    if (defender.Vehicle != attacker.Vehicle) {
        //        return attacker;
        //    }
        //    var v = attacker.Value - defender.Value;
        //    if (v <= Constants.ZeroF) {
        //        return new DeliveryStrength(attacker.Vehicle, Constants.ZeroF);
        //    }
        //    return new DeliveryStrength(attacker.Vehicle, v);
        //}

        /// <summary>
        /// Scales this <c>strength</c> by <c>scaler</c>.
        /// </summary>
        /// <param name="strength">The strength.</param>
        /// <param name="scaler">The scaler.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static DeliveryStrength operator *(DeliveryStrength strength, float scaler) {
            Arguments.ValidateNotNegative(scaler);
            return new DeliveryStrength(strength.Vehicle, strength.Value * scaler);
        }

        /// <summary>
        /// Scales this <c>strength</c> by <c>scaler</c>.
        /// </summary>
        /// <param name="scaler">The scaler.</param>
        /// <param name="strength">The strength.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static DeliveryStrength operator *(float scaler, DeliveryStrength strength) {
            return strength * scaler;
        }

        #endregion


        public float Value { get; private set; }

        //public DeliveryVehicle Vehicle { get; private set; }
        public ArmamentCategory Vehicle { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryStrength" /> struct.
        /// DeliveryVehicle.None is illegal.
        /// </summary>
        /// <param name="deliveryVehicle">The delivery vehicle.</param>
        /// <param name="value">The value.</param>
        public DeliveryStrength(ArmamentCategory deliveryVehicle, float value)
            : this() {
            D.Assert(deliveryVehicle != ArmamentCategory.None);
            Arguments.ValidateNotNegative(value);
            Vehicle = deliveryVehicle;
            Value = value <= MaxValue ? value : MaxValue;
        }
        //public DeliveryStrength(DeliveryVehicle deliveryVehicle, float value)
        //    : this() {
        //        D.Assert(deliveryVehicle != DeliveryVehicle.None);
        //    Arguments.ValidateNotNegative(value);
        //    Vehicle = deliveryVehicle;
        //    Value = value <= MaxValue ? value : MaxValue;
        //}

        //private float GetValue(out DeliveryVehicle vehicle) {
        //    vehicle = Vehicle;
        //    return Value;
        //}

        #region Object.Equals and GetHashCode Override

        public override bool Equals(object obj) {
            if (!(obj is DeliveryStrength)) { return false; }
            return Equals((DeliveryStrength)obj);
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
            hash = hash * 31 + Vehicle.GetHashCode(); // 31 = another prime number
            hash = hash * 31 + Value.GetHashCode();
            return hash;
        }

        #endregion

        public override string ToString() { return "{0}: {1:0.#}".Inject(Vehicle.GetEnumAttributeText(), Value); }

        #region IEquatable<DeliveryStrength> Members

        public bool Equals(DeliveryStrength other) {
            return Vehicle == other.Vehicle && Value == other.Value;
        }

        #endregion

        //#region Nested Classes

        //public enum DeliveryVehicle {

        //    None,
        //    Beam,
        //    Projectile,
        //    Missile

        //}
        //#endregion

    }
}

