// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: EquipmentSlotID.cs
// Immutable ID for an equipment slot in a UnitDesign.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable ID for an equipment slot in a UnitDesign.
    /// </summary>
    public struct EquipmentSlotID : IEquatable<EquipmentSlotID> {

        private const string DebugNameFormat = "{0}[{1} {2}]";

        #region Comparison Operators Override

        // see C# 4.0 In a Nutshell, page 254

        public static bool operator ==(EquipmentSlotID left, EquipmentSlotID right) {
            return left.Equals(right);
        }

        public static bool operator !=(EquipmentSlotID left, EquipmentSlotID right) {
            return !left.Equals(right);
        }

        #endregion

        public string DebugName { get; private set; }

        public int SlotNumber { get; private set; }

        public EquipmentCategory Category { get; private set; }

        public EquipmentSlotID(int slot, EquipmentCategory category) {
            SlotNumber = slot;
            Category = category;
            DebugName = DebugNameFormat.Inject(typeof(EquipmentSlotID).Name, Category.GetEnumAttributeText(), SlotNumber);
        }

        #region Object.Equals and GetHashCode Override

        public override bool Equals(object obj) {
            if (!(obj is EquipmentSlotID)) { return false; }
            return Equals((EquipmentSlotID)obj);
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
                hash = hash * 31 + Category.GetHashCode();
                return hash;
            }
        }

        #endregion

        public override string ToString() {
            return DebugName;
        }

        #region IEquatable<EquipmentInventorySlotID> Members

        public bool Equals(EquipmentSlotID other) {
            return SlotNumber == other.SlotNumber && Category == other.Category;
        }

        #endregion

    }
}

