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

    /// <summary>
    /// All the data associated with a particular fleet.
    /// </summary>
    public class FleetData : AData {

        public FleetData() { }

        private float _speed = 23.5F;
        public float Speed {
            get { return _speed; }
            set { _speed = value; }
        }

        // TODO Composition
        private string _composition = "Composition Test";
        public string Composition {
            get { return _composition; }
            set { _composition = value; }
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

