// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DeliveryStrength.cs
// Immutable data container for survivability and interceptability strength values of weapon delivery vehicles.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using UnityEngine;

    /// <summary>
    /// Immutable data container for survivability and interceptability strength values
    /// of weapon delivery vehicles. Weapons hold the survivability value and Countermeasures 
    /// the interceptability value.
    /// </summary>
    public struct DeliveryStrength : IEquatable<DeliveryStrength>, IComparable<DeliveryStrength> {

        public static float MaxValue = 100F;

        #region Operators Override

        // see C# 4.0 In a Nutshell, page 254

        public static bool operator ==(DeliveryStrength left, DeliveryStrength right) {
            return left.Equals(right);
        }

        public static bool operator !=(DeliveryStrength left, DeliveryStrength right) {
            return !left.Equals(right);
        }

        public static DeliveryStrength operator +(DeliveryStrength left, DeliveryStrength right) {
            D.Assert(left.Vehicle == right.Vehicle || left.Vehicle == ArmamentCategory.None || right.Vehicle == ArmamentCategory.None);
            if (left.Vehicle == ArmamentCategory.None) {
                return right;
            }
            if (right.Vehicle == ArmamentCategory.None) {
                return left;
            }
            return new DeliveryStrength(left.Vehicle, left.Value + right.Value);
        }

        /// <summary>
        /// Returns the DeliveryStrength remaining (a positive DeliveryStrength) of the interceptedOrdnance (right operand) after interception 
        /// by the countermeasure (left operand). If the interceptingCM's DeliveryVehicle does not match the interceptedOrdnance's DeliveryVehicle then 
        /// the interception is a failure and the interceptedOrdnance's DeliveryStrength is returned. If the DeliveryVehicles match, then the
        /// interception is successful and the value returned represents the remaining DeliveryStrength of the interceptedOrdnance, if any.
        /// The value returned will never be negative but can be Zero if the interceptingCM's strength was equal to or exceeded the interceptedOrdnance's
        /// strength.
        /// </summary>
        /// <param name="interceptingCM">The countermeasure that is intercepting the ordnance.</param>
        /// <param name="interceptedOrdnance">The ordnance that is being intercepted.</param>
        /// <returns>
        /// The remaining DeliveryStrength (if any) of the interceptedOrdnance after interception by the interceptingCountermeasure.
        /// </returns>
        public static DeliveryStrength operator -(DeliveryStrength interceptingCM, DeliveryStrength interceptedOrdnance) {
            D.Assert(interceptingCM.Vehicle != ArmamentCategory.None && interceptedOrdnance.Vehicle != ArmamentCategory.None);
            if (interceptedOrdnance.Vehicle != interceptingCM.Vehicle) {
                // intercepting countermeasure vehicle is not the right tech reqd to intercept this ordnance delivery vehicle so it has no effect
                return interceptedOrdnance;
            }

            if (interceptingCM.Value >= interceptedOrdnance.Value) {
                // the interceptedOrdnance should be destroyed so return its values as Zero
                return new DeliveryStrength(interceptedOrdnance.Vehicle, Constants.ZeroF);
            }

            var remainingInterceptedOrdnanceValue = interceptedOrdnance.Value - interceptingCM.Value;
            D.Assert(remainingInterceptedOrdnanceValue > Constants.ZeroF);
            return new DeliveryStrength(interceptedOrdnance.Vehicle, remainingInterceptedOrdnanceValue);
        }

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

        private string ConstructToLabelText() {
            string vehicleIcon = string.Empty;
            switch (Vehicle) {
                case ArmamentCategory.Beam:
                    vehicleIcon = GameConstants.IconMarker_Beam;
                    break;
                case ArmamentCategory.Projectile:
                    vehicleIcon = GameConstants.IconMarker_Projectile;
                    break;
                case ArmamentCategory.Missile:
                    vehicleIcon = GameConstants.IconMarker_Missile;
                    break;
                case ArmamentCategory.None:
                    break;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(Vehicle));
            }
            return vehicleIcon + " {0}".Inject(Value.FormatValue());

        }

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

        public string ToLabel() { return ConstructToLabelText(); }

        public string ToTextHud() {
            if (Value == Constants.ZeroF) {
                return string.Empty;
            }
            return "{0}({1})".Inject(Vehicle.GetEnumAttributeText(), Value.FormatValue());
        }

        public override string ToString() { return "{0}: {1}({2})".Inject(GetType().Name, Vehicle.GetValueName(), Value.FormatValue()); }

        #region IEquatable<DeliveryStrength> Members

        public bool Equals(DeliveryStrength other) {
            return Vehicle == other.Vehicle && Value == other.Value;
        }

        #endregion

        #region IComparable<DeliveryStrength> Members

        public int CompareTo(DeliveryStrength other) {
            return Value.CompareTo(other.Value);
        }

        #endregion

    }
}

