// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: WDVStrength.cs
// Immutable data container for survivability and interdiction strength values of weapon delivery vehicles.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using UnityEngine;

    /// <summary>
    /// Immutable data container for survivability and interdiction strength values of weapon delivery vehicles.
    /// </summary>
    public struct WDVStrength : IEquatable<WDVStrength>, IComparable<WDVStrength> {

        private const string VehicleIconFormat = "{0} {1}";

        private const float MaxValue = 100F;

        #region Operators Override

        // see C# 4.0 In a Nutshell, page 254

        public static bool operator ==(WDVStrength left, WDVStrength right) {
            return left.Equals(right);
        }

        public static bool operator !=(WDVStrength left, WDVStrength right) {
            return !left.Equals(right);
        }

        public static WDVStrength operator +(WDVStrength left, WDVStrength right) {
            D.Assert(left.Category == right.Category || left.Category == WDVCategory.None || right.Category == WDVCategory.None);
            if (left.Category == WDVCategory.None) {
                return right;
            }
            if (right.Category == WDVCategory.None) {
                return left;
            }
            return new WDVStrength(left.Category, left.Value + right.Value);
        }

        /// <summary>
        /// Returns the WDVStrength remaining (a positive WDVStrength) of the interdictedStrength (right operand) after interdiction
        /// by the interdictingStrength (left operand). If the interdictingStrength's Category does not match the interdictedStrength's Category then
        /// the interception is a failure and the interdictedStrength is returned. If the Categories match, then the
        /// interdiction is successful and the value returned represents the remaining interdictedStrength, if any.
        /// The value returned will never be negative but can be Zero if the interdictingStrength was equal to or exceeded the interdictedStrength.
        /// </summary>
        /// <param name="interdictedStrength">The strength of the object being interdicted.</param>
        /// <param name="interdictingStrength">The strength of the interdicting object.</param>
        /// <returns>
        /// The remaining WDVStrength (if any).
        /// </returns>
        public static WDVStrength operator -(WDVStrength interdictedStrength, WDVStrength interdictingStrength) {
            D.AssertNotDefault((int)interdictingStrength.Category);
            D.AssertNotDefault((int)interdictedStrength.Category);
            if (interdictedStrength.Category != interdictingStrength.Category) {
                // intercepting countermeasure vehicle is not the right tech reqd to intercept this ordnance delivery vehicle so it has no effect
                return interdictedStrength;
            }

            if (interdictingStrength.Value >= interdictedStrength.Value) {
                // the interceptedOrdnance should be destroyed so return its values as Zero
                return new WDVStrength(interdictedStrength.Category, Constants.ZeroF);
            }

            var remainingInterceptedOrdnanceValue = interdictedStrength.Value - interdictingStrength.Value;
            D.Assert(remainingInterceptedOrdnanceValue > Constants.ZeroF);
            return new WDVStrength(interdictedStrength.Category, remainingInterceptedOrdnanceValue);
        }

        /// <summary>
        /// Scales this <c>strength</c> by <c>scaler</c>.
        /// </summary>
        /// <param name="strength">The strength.</param>
        /// <param name="scaler">The scaler.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static WDVStrength operator *(WDVStrength strength, float scaler) {
            Utility.ValidateNotNegative(scaler);
            return new WDVStrength(strength.Category, strength.Value * scaler);
        }

        /// <summary>
        /// Scales this <c>strength</c> by <c>scaler</c>.
        /// </summary>
        /// <param name="scaler">The scaler.</param>
        /// <param name="strength">The strength.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static WDVStrength operator *(float scaler, WDVStrength strength) {
            return strength * scaler;
        }

        #endregion

        /// <summary>
        /// This Strength's value. When representing the strength of the delivery vehicle of a weapon's ordnance, 
        /// this value refers to the survivability of the ordnance delivery vehicle if/when interdicted.
        /// When representing the strength of an active countermeasure or shield, this value refers 
        /// to the interdiction strength to be used against the ordnance's delivery vehicle strength.
        /// </summary>
        public float Value { get; private set; }

        /// <summary>
        /// The WeaponDeliveryVehicleCategory for this Strength.
        /// </summary>
        public WDVCategory Category { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WDVStrength" /> struct.
        /// WDVCategory.None is illegal.
        /// </summary>
        /// <param name="category">The category of WeaponDeliveryVehicle.</param>
        /// <param name="value">The value.</param>
        public WDVStrength(WDVCategory category, float value)
            : this() {
            D.AssertNotDefault((int)category);
            Utility.ValidateNotNegative(value);
            Category = category;
            Value = value <= MaxValue ? value : MaxValue;
        }

        private string ConstructToLabelText() {
            string vehicleIcon = string.Empty;
            switch (Category) {
                case WDVCategory.Beam:
                    vehicleIcon = GameConstants.IconMarker_Beam;
                    break;
                case WDVCategory.Projectile:
                    vehicleIcon = GameConstants.IconMarker_Projectile;
                    break;
                case WDVCategory.Missile:
                    vehicleIcon = GameConstants.IconMarker_Missile;
                    break;
                case WDVCategory.None:
                    break;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(Category));
            }
            return VehicleIconFormat.Inject(vehicleIcon, Value.FormatValue());
        }

        #region Object.Equals and GetHashCode Override

        public override bool Equals(object obj) {
            if (!(obj is WDVStrength)) { return false; }
            return Equals((WDVStrength)obj);
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
                hash = hash * 31 + Category.GetHashCode(); // 31 = another prime number
                hash = hash * 31 + Value.GetHashCode();
                return hash;
            }
        }

        #endregion

        public string ToLabel() { return ConstructToLabelText(); }

        public string ToTextHud() {
            if (Value == Constants.ZeroF) {
                return string.Empty;
            }
            return "{0}({1})".Inject(Category.GetEnumAttributeText(), Value.FormatValue());
        }

        public override string ToString() { return "{0}: {1}({2})".Inject(GetType().Name, Category.GetEnumAttributeText(), Value.FormatValue()); }

        #region IEquatable<DeliveryStrength> Members

        public bool Equals(WDVStrength other) {
            return Category == other.Category && Value == other.Value;
        }

        #endregion

        #region IComparable<DeliveryStrength> Members

        public int CompareTo(WDVStrength other) {
            return Value.CompareTo(other.Value);
        }

        #endregion

    }
}

