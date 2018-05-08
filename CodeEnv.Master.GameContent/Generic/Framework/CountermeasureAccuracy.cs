// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2018 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: CountermeasureAccuracy.cs
// Immutable data container holding intercept accuracy values for ActiveCountermeasures.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using System;
    using UnityEngine;

    /// <summary>
    /// Immutable data container holding intercept accuracy values for ActiveCountermeasures.
    /// </summary>
    public struct CountermeasureAccuracy : IEquatable<CountermeasureAccuracy> {

        #region Operators Override

        // see C# 4.0 In a Nutshell, page 254

        public static bool operator ==(CountermeasureAccuracy left, CountermeasureAccuracy right) {
            return left.Equals(right);
        }

        public static bool operator !=(CountermeasureAccuracy left, CountermeasureAccuracy right) {
            return !left.Equals(right);
        }

        /// <summary>
        /// Adds two CountermeasureAccuracy values together. 
        /// <remarks>Clamped to 100%.</remarks>
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns></returns>
        public static CountermeasureAccuracy operator +(CountermeasureAccuracy left, CountermeasureAccuracy right) {
            var mAccy = Mathf.Clamp01(left._antiMissileValue + right._antiMissileValue);
            var pAccy = Mathf.Clamp01(left._antiProjectileValue + right._antiProjectileValue);
            var aAccy = Mathf.Clamp01(left._antiAssaultValue + right._antiAssaultValue);
            return new CountermeasureAccuracy(mAccy, pAccy, aAccy);
        }

        /// <summary>
        /// Scales this intercept <c>accuracy</c> by <c>scaler</c>.
        /// <remarks>Clamped to 100%.</remarks>
        /// </summary>
        /// <param name="accuracy">The CountermeasureAccuracy.</param>
        /// <param name="scaler">The scaler.</param>
        /// <returns></returns>
        public static CountermeasureAccuracy operator *(CountermeasureAccuracy accuracy, float scaler) {
            Utility.ValidateNotNegative(scaler);
            var mAccy = Mathf.Clamp01(accuracy._antiMissileValue * scaler);
            var pAccy = Mathf.Clamp01(accuracy._antiProjectileValue * scaler);
            var aAccy = Mathf.Clamp01(accuracy._antiAssaultValue * scaler);
            return new CountermeasureAccuracy(mAccy, pAccy, aAccy);
        }

        /// <summary>
        /// Scales this <c>accuracy</c> by <c>scaler</c>.
        /// <remarks>Clamped to 100%.</remarks>
        /// </summary>
        /// <param name="scaler">The scaler.</param>
        /// <param name="accuracy">The CountermeasureAccuracy.</param>
        /// <returns></returns>
        public static CountermeasureAccuracy operator *(float scaler, CountermeasureAccuracy accuracy) {
            return accuracy * scaler;
        }

        #endregion

        public string DebugName { get { return GetType().Name; } }

        private float _antiMissileValue;
        private float _antiProjectileValue;
        private float _antiAssaultValue;

        public CountermeasureAccuracy(EquipmentCategory weapCat, float antiWeapAccy) : this() {
            Utility.ValidateForRange(antiWeapAccy, Constants.ZeroPercent, Constants.OneHundredPercent);
            switch (weapCat) {
                case EquipmentCategory.ProjectileWeapon:
                    _antiProjectileValue = antiWeapAccy;
                    break;
                case EquipmentCategory.MissileWeapon:
                    _antiMissileValue = antiWeapAccy;
                    break;
                case EquipmentCategory.AssaultWeapon:
                    _antiAssaultValue = antiWeapAccy;
                    break;
                case EquipmentCategory.BeamWeapon:
                case EquipmentCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(weapCat));
            }
        }

        private CountermeasureAccuracy(float antiMissileAccy, float antiProjectileAccy, float antiAssaultAccy) {
            _antiMissileValue = antiMissileAccy;
            _antiProjectileValue = antiProjectileAccy;
            _antiAssaultValue = antiAssaultAccy;
        }

        public float GetAccuracy(EquipmentCategory weapCat) {
            switch (weapCat) {
                case EquipmentCategory.ProjectileWeapon:
                    return _antiProjectileValue;
                case EquipmentCategory.MissileWeapon:
                    return _antiMissileValue;
                case EquipmentCategory.AssaultWeapon:
                    return _antiAssaultValue;
                case EquipmentCategory.BeamWeapon:
                case EquipmentCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(weapCat));
            }
        }

        #region Object.Equals and GetHashCode Override

        public override bool Equals(object obj) {
            if (!(obj is CountermeasureAccuracy)) { return false; }
            return Equals((CountermeasureAccuracy)obj);
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
                hash = hash * 31 + _antiMissileValue.GetHashCode(); // 31 = another prime number
                hash = hash * 31 + _antiProjectileValue.GetHashCode();
                hash = hash * 31 + _antiAssaultValue.GetHashCode();
                return hash;
            }
        }

        #endregion

        public override string ToString() {
            return DebugName;
        }

        #region IEquatable<CountermeasureAccuracy> Members

        public bool Equals(CountermeasureAccuracy other) {
            return _antiMissileValue == other._antiMissileValue && _antiProjectileValue == other._antiProjectileValue && _antiAssaultValue == other._antiAssaultValue;
        }

        #endregion


    }
}

