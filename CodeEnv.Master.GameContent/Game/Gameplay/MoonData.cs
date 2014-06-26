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

        private OrbitalSlot _planetOrbitSlot;
        /// <summary>
        /// The OrbitSlot that this moon occupies around its planet.
        /// </summary>
        public OrbitalSlot PlanetOrbitSlot {
            get { return _planetOrbitSlot; }
            set { SetProperty<OrbitalSlot>(ref _planetOrbitSlot, value, "PlanetOrbitSlot", OnPlanetOrbitSlotChanged); }
        }

        public MoonData(PlanetoidStat stat) : base(stat) { }

        private void OnPlanetOrbitSlotChanged() {
            Transform.localPosition = PlanetOrbitSlot.GenerateRandomLocalPositionWithinSlot();
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

