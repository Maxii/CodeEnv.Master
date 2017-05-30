// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiElementIDEqualityComparer.cs
// IEqualityComparer for GuiElementID. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using CodeEnv.Master.Common;

    /// <summary>
    /// IEqualityComparer for GuiElementID. 
    /// <remarks>For use when GuiElementID is used as a Dictionary key as it avoids boxing from use of object.Equals.</remarks>
    /// </summary>
    public class GuiElementIDEqualityComparer : IEqualityComparer<GuiElementID> {

        public static readonly GuiElementIDEqualityComparer Default = new GuiElementIDEqualityComparer();

        public string DebugName { get { return GetType().Name; } }

        public override string ToString() {
            return DebugName;
        }

        #region IEqualityComparer<GuiElementID> Members

        public bool Equals(GuiElementID value1, GuiElementID value2) {
            return value1 == value2;
        }

        public int GetHashCode(GuiElementID value) {
            return value.GetHashCode();
        }

        #endregion


    }
}

