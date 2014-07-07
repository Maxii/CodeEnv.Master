// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: MoonData.cs
// All the data associated with a particular Moon orbiting a Planet.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// All the data associated with a particular Moon orbiting a Planet.
    /// </summary>
    public class MoonData : APlanetoidData {

        public MoonData(PlanetoidStat stat) : base(stat) { }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

