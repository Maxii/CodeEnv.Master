// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: OptionalEquipSlotID.cs
// Immutable ID for a slot for optional equipment in a UnitDesign.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable ID for a slot for optional equipment in a UnitDesign.
    /// </summary>
    public struct OptionalEquipSlotID : IEquatable<OptionalEquipSlotID> {

        private const string DebugNameFormat = "{0}[{1} {2}]";

        #region Comparison Operators Override

        // see C# 4.0 In a Nutshell, page 254

        public static bool operator ==(OptionalEquipSlotID left, OptionalEquipSlotID right) {
            return left.Equals(right);
        }

        public static bool operator !=(OptionalEquipSlotID left, OptionalEquipSlotID right) {
            return !left.Equals(right);
        }

        #endregion

        public string DebugName { get; private set; }

        public int SlotNumber { get; private set; }

        /// <summary>
        /// The HullMountCategory that this EquipmentSlot supports when mounting equipment.
        /// </summary>
        public OptionalEquipMountCategory SupportedMount { get; private set; }

        public OptionalEquipSlotID(int slotNumber, OptionalEquipMountCategory supportedMount) {
            SlotNumber = slotNumber;
            SupportedMount = supportedMount;
            DebugName = DebugNameFormat.Inject(typeof(OptionalEquipSlotID).Name, supportedMount.GetEnumAttributeText(), slotNumber);
        }

        #region Object.Equals and GetHashCode Override

        public override bool Equals(object obj) {
            if (!(obj is OptionalEquipSlotID)) { return false; }
            return Equals((OptionalEquipSlotID)obj);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// See "Page 254, C# 4.0 in a Nutshell."
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode() {
            unchecked { // http://dobrzanski.net/2010/09/13/csharp-gethashcode-cause-overflowexception/
                int hash = 17;  // 17 = some prime number
                hash = hash * 31 + SlotNumber.GetHashCode(); // 31 = another prime number
                hash = hash * 31 + SupportedMount.GetHashCode();
                return hash;
            }
        }

        #endregion

        public override string ToString() {
            return DebugName;
        }

        #region IEquatable<OptionalEquipSlotID> Members

        public bool Equals(OptionalEquipSlotID other) {
            return SlotNumber == other.SlotNumber && SupportedMount == other.SupportedMount;
        }

        #endregion

    }
}

