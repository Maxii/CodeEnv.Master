// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2018 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: TreeNodeID.cs
// ID for the node of a tree containing column and row values.
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
    /// ID for the node of a tree containing column and row values.
    /// <remarks>To guard against errors, Row and Column values must not be zero.</remarks>
    /// </summary>
    [Serializable]
    public struct TreeNodeID : IEquatable<TreeNodeID> {

        private const string DebugNameFormat = "{0}[{1},{2}]";

        #region Comparison Operators Override

        // see C# 4.0 In a Nutshell, page 254

        public static bool operator ==(TreeNodeID left, TreeNodeID right) {
            return left.Equals(right);
        }

        public static bool operator !=(TreeNodeID left, TreeNodeID right) {
            return !left.Equals(right);
        }

        #endregion

        public string DebugName { get { return DebugNameFormat.Inject(typeof(TreeNodeID).Name, Column, Row); } }

        [SerializeField]
        private int _column;
        public int Column { get { return _column; } }

        [SerializeField]
        private int _row;
        public int Row { get { return _row; } }

        public TreeNodeID(int col, int row) {
            _column = col;
            _row = row;
            __Validate();
        }

        #region Object.Equals and GetHashCode Override

        public override bool Equals(object obj) {
            if (!(obj is TreeNodeID)) { return false; }
            return Equals((TreeNodeID)obj);
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
                hash = hash * 31 + Column.GetHashCode(); // 31 = another prime number
                hash = hash * 31 + Row.GetHashCode();
                return hash;
            }
        }

        #endregion

        #region Debug

        [System.Diagnostics.Conditional("DEBUG")]
        private void __Validate() {
            D.AssertNotEqual(Constants.Zero, Column);
            D.AssertNotEqual(Constants.Zero, Row);
        }

        #endregion

        public override string ToString() {
            return DebugName;
        }

        #region IEquatable<TreeNodeID> Members

        public bool Equals(TreeNodeID other) {
            return Column == other.Column && Row == other.Row;
        }

        #endregion



    }
}

