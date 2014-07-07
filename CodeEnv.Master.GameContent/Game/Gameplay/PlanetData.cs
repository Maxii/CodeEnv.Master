// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlanetData.cs
// All the data associated with a particular Planet in a System.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// All the data associated with a particular Planet in a System.
    /// </summary>
    public class PlanetData : APlanetoidData {

        public float OrbitalSpeed { get; set; }

        public PlanetData(PlanetoidStat stat) : base(stat) { }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

