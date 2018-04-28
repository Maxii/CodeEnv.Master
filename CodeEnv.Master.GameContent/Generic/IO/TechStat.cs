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
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable stat for a technology.
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
        public string Name { get; private set; }

        public AtlasID ImageAtlasID { get; private set; }

        public string ImageFilename { get; private set; }

        public string Description { get; private set; }

        public float BaseResearchCost { get; private set; }

        public TreeNodeID NodeID { get; private set; }

        public IEnumerable<string> PrerequisiteTechNames { get; private set; }

        public IEnumerable<EquipmentStatID> EnabledEquipmentIDs { get; private set; }

        public IEnumerable<CapabilityStatID> EnabledCapabilityIDs { get; private set; }

        /// <summary>
        /// Immutable stat for a technology.
        /// </summary>
        /// <param name="name">The name of the technology represented by this stat.</param>
        /// <param name="imageAtlasID">The image atlas identifier.</param>
        /// <param name="imageFilename">The image filename.</param>
        /// <param name="description">The description.</param>
        /// <param name="baseRschCost">The baseline cost to research.</param>
        /// <param name="nodeID">The tech tree node identifier.</param>
        /// <param name="prereqTechNames">The names of any Techs that are prerequisites of this Tech.</param>
        /// <param name="enabledEquipIDs">The EquipmentStatIDs of equipment enabled by researching this tech.</param>
        /// <param name="enabledCapIDs">The CapabilityStatIDs of Capabilities enabled by researching this tech.</param>
        public TechStat(string name, AtlasID imageAtlasID, string imageFilename, string description, float baseRschCost, TreeNodeID nodeID,
            IEnumerable<string> prereqTechNames, IEnumerable<EquipmentStatID> enabledEquipIDs, IEnumerable<CapabilityStatID> enabledCapIDs) {
            Name = name;
            ImageAtlasID = imageAtlasID;
            ImageFilename = imageFilename;
            Description = description;
            BaseResearchCost = baseRschCost;
            NodeID = nodeID;
            PrerequisiteTechNames = prereqTechNames;
            EnabledEquipmentIDs = enabledEquipIDs;
            EnabledCapabilityIDs = enabledCapIDs;
            DebugName = DebugNameFormat.Inject(typeof(TechStat).Name, name);
        }

        public TechStat(string name, AtlasID imageAtlasID, string imageFilename, string description, float baseRschCost, TreeNodeID nodeID,
            IEnumerable<string> prereqTechNames, IEnumerable<EquipmentStatID> enabledEquipIDs)
            : this(name, imageAtlasID, imageFilename, description, baseRschCost, nodeID, prereqTechNames, enabledEquipIDs,
                  Enumerable.Empty<CapabilityStatID>()) { }


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
                hash = hash * 31 + Name.GetHashCode();
                hash = hash * 31 + ImageAtlasID.GetHashCode();
                hash = hash * 31 + ImageFilename.GetHashCode();
                hash = hash * 31 + Description.GetHashCode();
                hash = hash * 31 + BaseResearchCost.GetHashCode();
                hash = hash * 31 + NodeID.GetHashCode();
                // handled this way, for hashCode and Equality to be compatible, the order in Equality matters
                foreach (var techName in PrerequisiteTechNames) {
                    hash = hash * 31 + techName.GetHashCode();
                }
                foreach (var eId in EnabledEquipmentIDs) {
                    hash = hash * 31 + eId.GetHashCode();
                }
                foreach (var cId in EnabledCapabilityIDs) {
                    hash = hash * 31 + cId.GetHashCode();
                }
                return hash;
            }
        }

        #endregion

        public override string ToString() {
            return DebugName;
        }

        #region IEquatable<TechStat> Members

        public bool Equals(TechStat other) {
            return Name == other.Name && ImageAtlasID == other.ImageAtlasID && ImageFilename == other.ImageFilename
                && Description == other.Description && BaseResearchCost == other.BaseResearchCost && NodeID == other.NodeID
                && PrerequisiteTechNames.SequenceEqual(other.PrerequisiteTechNames) && EnabledEquipmentIDs.SequenceEqual(other.EnabledEquipmentIDs)
                && EnabledCapabilityIDs.SequenceEqual(other.EnabledCapabilityIDs);
            // order matters
        }

        #endregion



    }
}

