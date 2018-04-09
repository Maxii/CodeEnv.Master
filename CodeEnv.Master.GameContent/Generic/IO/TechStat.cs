// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2018 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: TechStat.cs
// Immutable stat for a technology.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Linq;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable stat for a technology.
    /// <remarks>IMPROVE Need something to ID the stats enabled by this tech. 
    /// StatNames won't work as they are names for display purposes and not necessarily unique.</remarks>
    /// </summary>
    public struct TechStat : IEquatable<TechStat> {

        private const string DebugNameFormat = "{0}.{1}";

        #region Comparison Operators Override

        // see C# 4.0 In a Nutshell, page 254

        public static bool operator ==(TechStat left, TechStat right) {
            return left.Equals(right);
        }

        public static bool operator !=(TechStat left, TechStat right) {
            return !left.Equals(right);
        }

        #endregion

        public string DebugName { get; private set; }

        /// <summary>
        /// The name of the technology represented by this stat.
        /// </summary>
        public string TechName { get; private set; }

        public AtlasID ImageAtlasID { get; private set; }

        public string ImageFilename { get; private set; }

        public string Description { get; private set; }

        public TreeNodeID NodeID { get; private set; }

        public string[] PrerequisiteTechNames { get; private set; }

        /// <summary>
        /// The names of all AEquipment and Capability Stats made available for use by this technology.
        /// </summary>
        /// <param name="techName">The name of the technology represented by this stat.</param>
        /// <param name="imageAtlasID">The image atlas identifier.</param>
        /// <param name="imageFilename">The image filename.</param>
        /// <param name="description">The description.</param>
        /// <param name="nodeID">The node identifier.</param>
        /// <param name="prereqTechNames">The names of any Techs that are prerequisites of this Tech.</param>
        public TechStat(string techName, AtlasID imageAtlasID, string imageFilename, string description, TreeNodeID nodeID, string[] prereqTechNames) {
            TechName = techName;
            ImageAtlasID = imageAtlasID;
            ImageFilename = imageFilename;
            Description = description;
            NodeID = nodeID;
            PrerequisiteTechNames = prereqTechNames;
            DebugName = DebugNameFormat.Inject(typeof(TechStat).Name, techName);
        }

        #region Object.Equals and GetHashCode Override

        public override bool Equals(object obj) {
            if (!(obj is TechStat)) { return false; }
            return Equals((TechStat)obj);
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
                hash = hash * 31 + TechName.GetHashCode();
                hash = hash * 31 + ImageAtlasID.GetHashCode();
                hash = hash * 31 + ImageFilename.GetHashCode();
                hash = hash * 31 + Description.GetHashCode();
                hash = hash * 31 + NodeID.GetHashCode();
                foreach (var techName in PrerequisiteTechNames) {
                    hash = hash * 31 + techName.GetHashCode();
                }
                //foreach (var statName in EnabledStatNames) { // stat name order matters
                //    hash = hash * 31 + statName.GetHashCode();
                //}
                return hash;
            }
        }

        #endregion

        public override string ToString() {
            return DebugName;
        }

        #region IEquatable<TechStat> Members

        public bool Equals(TechStat other) {
            return TechName == other.TechName && ImageAtlasID == other.ImageAtlasID && ImageFilename == other.ImageFilename
                && Description == other.Description && NodeID == other.NodeID && PrerequisiteTechNames.SequenceEqual(other.PrerequisiteTechNames)
                /* && EnabledStatNames.SequenceEqual(other.EnabledStatNames)*/;  // stat name order matters
        }

        #endregion



    }
}

