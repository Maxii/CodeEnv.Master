// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2018 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: CapabilityStat.cs
// A stat holding a Capability along with data that allows user display of image and textual information.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// A stat holding a Capability along with data that allows user display of image and textual information.
    /// </summary>
    public class CapabilityStat : ATechStat {

        public Capability Capability { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CapabilityStat" /> class.
        /// </summary>
        /// <param name="name">The display name of the Capability.</param>
        /// <param name="imageAtlasID">The image atlas identifier.</param>
        /// <param name="imageFilename">The image filename.</param>
        /// <param name="description">The description.</param>
        /// <param name="level">The level of technological advancement of this stat.</param>
        /// <param name="capability">The capability.</param>
        public CapabilityStat(string name, AtlasID imageAtlasID, string imageFilename, string description, Level level, Capability capability)
            : base(name, imageAtlasID, imageFilename, description, level) {
            Capability = capability;
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

