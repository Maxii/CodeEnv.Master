// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FormationStationSlotIDEqualityComparer.cs
// IEqualityComparer for FormationStationSlotID. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using CodeEnv.Master.Common;

    /// <summary>
    /// IEqualityComparer for FormationStationSlotID. 
    /// <remarks>For use when FormationStationSlotID is used as a Dictionary key as it avoids boxing from use of object.Equals.</remarks>
    /// </summary>
    public class FormationStationSlotIDEqualityComparer : IEqualityComparer<FormationStationSlotID> {

        public static readonly FormationStationSlotIDEqualityComparer Default = new FormationStationSlotIDEqualityComparer();

        public string DebugName { get { return GetType().Name; } }

        public override string ToString() {
            return DebugName;
        }

        #region IEqualityComparer<FormationStationSlotID> Members

        public bool Equals(FormationStationSlotID value1, FormationStationSlotID value2) {
            return value1 == value2;
        }

        public int GetHashCode(FormationStationSlotID value) {
            return value.GetHashCode();
        }

        #endregion

    }
}

