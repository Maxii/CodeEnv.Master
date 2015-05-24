// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IconInfo.cs
// Immutable struct containing info needed to construct icons.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable struct containing info needed to construct icons.
    /// </summary>
    public struct IconInfo : IEquatable<IconInfo> {

        #region Comparison Operators Override

        // see C# 4.0 In a Nutshell, page 254

        public static bool operator ==(IconInfo left, IconInfo right) {
            return left.Equals(right);
        }

        public static bool operator !=(IconInfo left, IconInfo right) {
            return !left.Equals(right);
        }

        #endregion

        private static string _toStringFormat = "{0}.Filename: {1}, AtlasID: {2}, Color: {3}";

        public string Filename { get; private set; }

        public AtlasID AtlasID { get; private set; }

        public GameColor Color { get; private set; }

        public IconInfo(string filename, AtlasID atlasID, GameColor color)
            : this() {
            Filename = filename;
            AtlasID = atlasID;
            Color = color;
        }

        #region Object.Equals and GetHashCode Override

        public override bool Equals(object obj) {
            if (!(obj is IconInfo)) { return false; }
            return Equals((IconInfo)obj);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// See "Page 254, C# 4.0 in a Nutshell."
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode() {
            int hash = 17;  // 17 = some prime number
            hash = hash * 31 + Filename.GetHashCode(); // 31 = another prime number
            hash = hash * 31 + AtlasID.GetHashCode();
            hash = hash * 31 + Color.GetHashCode();
            return hash;
        }

        #endregion

        public override string ToString() {
            return _toStringFormat.Inject(GetType().Name, Filename, AtlasID.GetName(), Color.GetName());
        }

        #region IEquatable<IconInfo> Members

        public bool Equals(IconInfo other) {
            return Filename == other.Filename && AtlasID == other.AtlasID && Color == other.Color;
        }

        #endregion
    }
}

