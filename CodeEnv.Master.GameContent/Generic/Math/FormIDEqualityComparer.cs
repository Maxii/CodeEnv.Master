// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FormIDEqualityComparer.cs
// IEqualityComparer for FormID. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using CodeEnv.Master.Common;

    /// <summary>
    /// IEqualityComparer for FormID. 
    /// <remarks>For use when FormID is used as a Dictionary key as it avoids boxing from use of object.Equals.</remarks>
    /// </summary>
    public class FormIDEqualityComparer : IEqualityComparer<FormID> {

        public static readonly FormIDEqualityComparer Default = new FormIDEqualityComparer();

        public string DebugName { get { return GetType().Name; } }

        public override string ToString() {
            return DebugName;
        }

        #region IEqualityComparer<FormID> Members

        public bool Equals(FormID value1, FormID value2) {
            return value1 == value2;
        }

        public int GetHashCode(FormID value) {
            return value.GetHashCode();
        }

        #endregion


    }
}

