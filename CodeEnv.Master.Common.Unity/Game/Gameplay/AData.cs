// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AData.cs
// Abstract class basis for System and Fleet data.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR

namespace CodeEnv.Master.Common.Unity {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Abstract class basis for System and Fleet data.
    /// </summary>
    public abstract class AData {

        public string Name { get; set; }

        public Vector3 Position { get; set; }

        public Players Owner { get; set; }

        public float Health { get; set; }

        public float MaxHitPoints { get; set; }

        public float CombatStrength { get; set; }

        public AData() { }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

