// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StationaryLocation.cs
// A dummy ITarget wrapping a stationary location.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// A dummy ITarget wrapping a stationary location.
    /// </summary>
    public class StationaryLocation : ITarget {

        public StationaryLocation(Vector3 position) {
            Position = position;
        }

        public override string ToString() {
            return Name;
        }

        #region ITarget Members

        public string Name {
            get {
                return string.Format("{0} {1}", this.GetType().Name, Position);
            }
        }

        // OPTIMIZE consider letting this be settable so navigator's don't have to create a new one every time
        public Vector3 Position { get; private set; }

        public bool IsMovable { get { return false; } }

        public float Radius { get { return Constants.ZeroF; } }

        #endregion
    }
}

