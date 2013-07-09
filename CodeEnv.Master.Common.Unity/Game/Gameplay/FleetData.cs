// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetData.cs
// All the data associated with a particular fleet.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR

namespace CodeEnv.Master.Common.Unity {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// All the data associated with a particular fleet.
    /// </summary>
    public class FleetData : AData {

        public float MaxSpeed { get; set; }

        public float Speed { get; set; }

        // TODO Composition
        private string _composition = "TBD";
        public string Composition {
            get { return _composition; }
            set { _composition = value; }
        }

        public FleetData(Transform t)
            : base(t) {
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

