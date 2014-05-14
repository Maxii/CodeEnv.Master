// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlanetoidStat.cs
// Immutable struct containing externally acquirable values for Planetoids, aka Planets and Moons.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable struct containing externally acquirable values for Planetoids, aka Planets and Moons.
    /// </summary>
    public struct PlanetoidStat {

        public string Name { get; private set; }
        public float Mass { get; private set; }
        public float MaxHitPoints { get; private set; }
        public PlanetoidCategory Category { get; private set; }
        public int Capacity { get; private set; }
        public OpeYield Resources { get; private set; }
        public XYield SpecialResources { get; private set; }

        public PlanetoidStat(string name, float mass, float maxHitPts, PlanetoidCategory category, int capacity, OpeYield resources)
            : this(name, mass, maxHitPts, category, capacity, resources, new XYield()) { }

        public PlanetoidStat(string name, float mass, float maxHitPts, PlanetoidCategory category, int capacity, OpeYield resources, XYield xResources)
            : this() {
            Name = name;
            Mass = mass;
            MaxHitPoints = maxHitPts;
            Category = category;
            Capacity = capacity;
            Resources = resources;
            SpecialResources = xResources;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

