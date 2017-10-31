// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: OutputIDEqualityComparer.cs
// IEqualityComparer for OutputID. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;

    /// <summary>
    /// IEqualityComparer for OutputID. 
    /// <remarks>For use when OutputID is used as a Dictionary key as it avoids boxing from use of object.Equals.</remarks>
    /// </summary>
    public class OutputIDEqualityComparer : IEqualityComparer<OutputID> {

        public static readonly OutputIDEqualityComparer Default = new OutputIDEqualityComparer();

        public string DebugName { get { return GetType().Name; } }

        public override string ToString() {
            return DebugName;
        }

        #region IEqualityComparer<OutputID> Members

        public bool Equals(OutputID value1, OutputID value2) {
            return value1 == value2;
        }

        public int GetHashCode(OutputID value) {
            return value.GetHashCode();
        }

        #endregion


    }
}

