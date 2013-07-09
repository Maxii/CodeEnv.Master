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

        /// <summary>
        /// Readonly. Gets the position of the gameObject containing this data.
        /// </summary>
        public Vector3 Position {
            get {
                return _transform.position;
            }
        }

        public GameDate DateHumanPlayerExplored { get; set; }

        public Players Owner { get; set; }

        public float Health { get; set; }

        public float MaxHitPoints { get; set; }

        public CombatStrength CombatStrength { get; set; }

        private Transform _transform;

        public AData(Transform t) {
            _transform = t;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

