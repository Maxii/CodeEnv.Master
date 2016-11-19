// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IconSectionEqualityComparer.cs
// IEqualityComparer for IconSection. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using CodeEnv.Master.Common;

    /// <summary>
    /// IEqualityComparer for IconSection. 
    /// <remarks>For use when IconSection is used as a Dictionary key as it avoids boxing from use of object.Equals.</remarks>
    /// </summary>
    public class IconSectionEqualityComparer : IEqualityComparer<IconSection> {

        public static readonly IconSectionEqualityComparer Default = new IconSectionEqualityComparer();

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region IEqualityComparer<IconSection> Members

        public bool Equals(IconSection value1, IconSection value2) {
            return value1 == value2;
        }

        public int GetHashCode(IconSection value) {
            return value.GetHashCode();
        }

        #endregion


    }
}

