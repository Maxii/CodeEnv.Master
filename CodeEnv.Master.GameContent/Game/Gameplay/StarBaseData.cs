// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarBaseData.cs
// All the data associated with a particular StarBase.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// All the data associated with a particular StarBase.
    /// </summary>
    public class StarBaseData : AMortalData {

        private float _maxTurnRate;
        /// <summary>
        /// Gets or sets the maximum turn rate of the ship in degrees per day.
        /// </summary>
        public float MaxTurnRate {
            get { return _maxTurnRate; }
            set {
                SetProperty<float>(ref _maxTurnRate, value, "MaxTurnRate");
            }
        }

        private IPlayer _owner;
        public IPlayer Owner {
            get { return _owner; }
            set {
                SetProperty<IPlayer>(ref _owner, value, "Owner");
            }
        }

        private CombatStrength _combatStrength;
        public CombatStrength Strength {
            get { return _combatStrength; }
            set {
                SetProperty<CombatStrength>(ref _combatStrength, value, "Strength");
            }
        }

        private ShipHull _hull;
        public ShipHull Hull {
            get { return _hull; }
            set {
                SetProperty<ShipHull>(ref _hull, value, "Hull");
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShipData" /> class.
        /// </summary>
        /// <param name="shipName">Name of the ship.</param>
        /// <param name="maxHitPoints">The maximum hit points.</param>
        public StarBaseData(string shipName, float maxHitPoints)
            : base(shipName, maxHitPoints) {
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

