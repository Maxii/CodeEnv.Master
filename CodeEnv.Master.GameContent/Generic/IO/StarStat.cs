// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarStat.cs
// Immutable struct containing externally acquirable values for Stars.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable struct containing externally acquirable values for Stars.
    /// </summary>
    public struct StarStat {

        public string Name { get; private set; }
        public StarCategory Category { get; private set; }
        public int Capacity { get; private set; }
        public OpeYield Resources { get; private set; }
        public XYield SpecialResources { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="StarStat" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="category">The category of Star.</param>
        /// <param name="capacity">The capacity.</param>
        /// <param name="resources">The resources.</param>
        public StarStat(string name, StarCategory category, int capacity, OpeYield resources)
            : this(name, category, capacity, resources, new XYield()) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="StarStat" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="category">The category of Star.</param>
        /// <param name="capacity">The capacity.</param>
        /// <param name="resources">The resources.</param>
        /// <param name="specialResources">The special resources.</param>
        public StarStat(string name, StarCategory category, int capacity, OpeYield resources, XYield specialResources)
            : this() {
            Name = name;
            Category = category;
            Capacity = capacity;
            Resources = resources;
            SpecialResources = specialResources;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

