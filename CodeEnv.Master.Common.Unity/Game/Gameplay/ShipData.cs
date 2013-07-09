// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipData.cs
// All the data associated with a particular ship.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR

namespace CodeEnv.Master.Common.Unity {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// All the data associated with a particular ship.
    /// <remarks>MaxSpeed = MaxThrust / (Mass * Drag)</remarks>
    /// </summary>
    public class ShipData : AData {

        /// <summary>
        /// Readonly. The mass of the ship.
        /// </summary>
        public float Mass {
            get { return _rigidbody.mass; }
        }

        /// <summary>
        /// Readonly. The drag on the ship.
        /// </summary>
        public float Drag {
            get { return _rigidbody.drag; }
        }

        /// <summary>
        /// Gets or sets the max thrust achievable by the engines.
        /// </summary>
        public float MaxThrust { get; set; }

        /// <summary>
        /// Readonly. Gets the max speed that can be achieved, derived directly
        /// from the MaxThrust, mass and drag of the ship.
        /// </summary>
        public float MaxSpeed {
            get { return MaxThrust / (Mass * Drag); }
        }

        /// <summary>
        /// Gets or sets the max turn rate of the ship.
        /// </summary>
        public float MaxTurnRate { get; set; }

        /// <summary>
        /// Readonly. Gets the speed readout for the ship used by the HUD. To set this speed readout value
        /// for use by the HUD, use UpdateSpeedReadout(). To request a change in
        /// the actual speed of the ship, use Navigotor.RequestedSpeed.
        /// </summary>
        public float SpeedReadout { get; private set; }

        private Rigidbody _rigidbody;

        public ShipData(Transform t)
            : base(t) {
            _rigidbody = t.rigidbody;
        }

        public void UpdateSpeedReadout(float currentSpeed) {
            SpeedReadout = currentSpeed;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

