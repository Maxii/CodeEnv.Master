// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2018 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: TreeLinkID.cs
// ID for the link of a tree connecting two tree nodes.
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
    /// ID for the link of a tree connecting two tree nodes.
    /// </summary>
    [Serializable]
    public struct TreeLinkID : IEquatable<TreeLinkID> {

        private const string DebugNameFormat = "{0}[{1},{2}]";

        #region Comparison Operators Override

        // see C# 4.0 In a Nutshell, page 254

        public static bool operator ==(TreeLinkID left, TreeLinkID right) {
            return left.Equals(right);
        }

        public static bool operator !=(TreeLinkID left, TreeLinkID right) {
            return !left.Equals(right);
        }

        #endregion

        public string DebugName { get { return DebugNameFormat.Inject(typeof(TreeLinkID).Name, FromNodeID, ToNodeID); } }

        [SerializeField]
        private TreeNodeID _fromNodeID;
        public TreeNodeID FromNodeID { get { return _fromNodeID; } }

        [SerializeField]
        private TreeNodeID _toNodeID;
        public TreeNodeID ToNodeID { get { return _toNodeID; } }

        public TreeLinkID(TreeNodeID fromNodeID, TreeNodeID toNodeID) {
            _fromNodeID = fromNodeID;
            _toNodeID = toNodeID;
        }

        #region Object.Equals and GetHashCode Override

        public override bool Equals(object obj) {
            if (!(obj is TreeLinkID)) { return false; }
            return Equals((TreeLinkID)obj);
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
                hash = hash * 31 + FromNodeID.GetHashCode(); // 31 = another prime number
                hash = hash * 31 + ToNodeID.GetHashCode();
                return hash;
            }
        }

        #endregion

        public override string ToString() {
            return DebugName;
        }

        #region IEquatable<TreeLinkID> Members

        public bool Equals(TreeLinkID other) {
            return FromNodeID == other.FromNodeID && ToNodeID == other.ToNodeID;
        }

        #endregion


    }
}

