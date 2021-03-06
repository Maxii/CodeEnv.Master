﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2018 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: CapabilityStat.cs
// An AImprovableStat holding a Capability along with data that allows user display of image and textual information.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// An AImprovableStat holding a Capability along with data that allows user display of image and textual information.
    /// </summary>
    public class CapabilityStat : AImageStat {

        public Capability Capability { get { return ID.Capability; } }

        public Level Level { get { return ID.Level; } }

        public CapabilityStatID ID { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CapabilityStat" /> class.
        /// </summary>
        /// <param name="name">The display name of the Capability.</param>
        /// <param name="imageAtlasID">The image atlas identifier.</param>
        /// <param name="imageFilename">The image filename.</param>
        /// <param name="description">The description.</param>
        /// <param name="id">The stat identifier.</param>
        public CapabilityStat(string name, AtlasID imageAtlasID, string imageFilename, string description, CapabilityStatID id)
            : base(name, imageAtlasID, imageFilename, description) {
            ID = id;
        }


        #region Value-based Equality Archive
        // 2.23.18 ATechStat instances are always the same as they are acquired via factory caching

        ////public static bool operator ==(CapabilityStat left, CapabilityStat right) {
        ////    // https://msdn.microsoft.com/en-us/library/ms173147(v=vs.90).aspx
        ////    if (ReferenceEquals(left, right)) { return true; }
        ////    if (((object)left == null) || ((object)right == null)) { return false; }
        ////    return left.Equals(right);
        ////}

        ////public static bool operator !=(CapabilityStat left, CapabilityStat right) {
        ////    return !(left == right);
        ////}

        ////public override int GetHashCode() {
        ////    unchecked {
        ////        int hash = base.GetHashCode();
        ////        hash = hash * 31 + Capability.GetHashCode(); // 31 = another prime number
        ////        return hash;
        ////    }
        ////}

        ////public override bool Equals(object obj) {
        ////    if (base.Equals(obj)) {
        ////        CapabilityStat oStat = (CapabilityStat)obj;
        ////        return oStat.Capability == Capability;
        ////    }
        ////    return false;
        ////}

        #endregion


    }
}

