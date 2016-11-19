// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FormationStationSlotInfo.cs
// Immutable custom struct containing info about individual FormationStation Slots in a Formation.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Immutable custom struct containing info about individual FormationStation Slots in a Formation.
    /// </summary>
    public struct FormationStationSlotInfo : IEquatable<FormationStationSlotInfo> {

        #region Comparison Operators Override

        // see C# 4.0 In a Nutshell, page 254

        public static bool operator ==(FormationStationSlotInfo left, FormationStationSlotInfo right) {
            return left.Equals(right);
        }

        public static bool operator !=(FormationStationSlotInfo left, FormationStationSlotInfo right) {
            return !left.Equals(right);
        }

        #endregion

        private const string ToStringFormat = "{0}: SlotID: {1}, IsReserve: {2}, LocalOffset: {3}, IsHQSlot: {4}";

        /// <summary>
        /// The ID of the FormationStationSlot this info describes.
        /// OPTIMIZE Unused?
        /// </summary>
        public FormationStationSlotID SlotID { get; private set; }

        /// <summary>
        /// Indicates whether this Station Slot is designated as a reserve, aka protected backup slot.
        /// </summary>
        public bool IsReserve { get; private set; }

        /// <summary>
        /// The offset in local space from HQ/Cmd.
        /// </summary>
        public Vector3 LocalOffset { get; private set; }

        public bool IsHQSlot { get { return LocalOffset == Vector3.zero; } }

        public FormationStationSlotInfo(FormationStationSlotID slotID, bool isReserve, Vector3 localOffset) : this() {
            SlotID = slotID;
            IsReserve = isReserve;
            LocalOffset = localOffset;
        }

        #region Object.Equals and GetHashCode Override

        public override bool Equals(object obj) {
            if (!(obj is FormationStationSlotInfo)) { return false; }
            return Equals((FormationStationSlotInfo)obj);
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
                hash = hash * 31 + SlotID.GetHashCode(); // 31 = another prime number
                hash = hash * 31 + IsReserve.GetHashCode();
                hash = hash * 31 + LocalOffset.GetHashCode();
                return hash;
            }
        }

        #endregion


        public override string ToString() {
            return ToStringFormat.Inject(GetType().Name, SlotID, IsReserve, LocalOffset, IsHQSlot);
        }

        #region IEquatable<FormationStationSlotInfo> Members

        public bool Equals(FormationStationSlotInfo other) {
            return SlotID == other.SlotID && IsReserve == other.IsReserve && LocalOffset == other.LocalOffset;
        }

        #endregion


    }
}

