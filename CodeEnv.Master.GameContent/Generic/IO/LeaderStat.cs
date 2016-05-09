// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: LeaderStat.cs
//  Immutable struct containing externally acquirable values for Leaders.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable struct containing externally acquirable values for Leaders.
    /// </summary>
    public struct LeaderStat : IEquatable<LeaderStat> {

        #region Comparison Operators Override

        // see C# 4.0 In a Nutshell, page 254

        public static bool operator ==(LeaderStat left, LeaderStat right) {
            return left.Equals(right);
        }

        public static bool operator !=(LeaderStat left, LeaderStat right) {
            return !left.Equals(right);
        }

        #endregion

        public string Name { get; private set; }

        public AtlasID ImageAtlasID { get; private set; }

        public string ImageFilename { get; private set; }

        public LeaderStat(string name, AtlasID imageAtlasID, string imageFilename)
            : this() {
            Name = name;
            ImageAtlasID = imageAtlasID;
            ImageFilename = imageFilename;
        }

        #region Object.Equals and GetHashCode Override

        public override bool Equals(object obj) {
            if (!(obj is LeaderStat)) { return false; }
            return Equals((LeaderStat)obj);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// See Page 254, C# 4.0 in a Nutshell.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode() {
            unchecked { // http://dobrzanski.net/2010/09/13/csharp-gethashcode-cause-overflowexception/
                int hash = 17;
                hash = hash * 31 + Name.GetHashCode();
                hash = hash * 31 + ImageAtlasID.GetHashCode();
                hash = hash * 31 + ImageFilename.GetHashCode();
                return hash;
            }
        }

        #endregion

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region IEquatable<LeaderStat> Members

        public bool Equals(LeaderStat other) {
            return Name == other.Name && ImageAtlasID == other.ImageAtlasID && ImageFilename == other.ImageFilename;
        }

        #endregion

    }
}

