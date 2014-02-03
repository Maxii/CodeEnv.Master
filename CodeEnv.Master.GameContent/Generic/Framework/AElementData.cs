// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ADefendedData.cs
// Abstract base class for data associated with Elements (Items under a Command).
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Abstract base class for data associated with Elements (Items under a Command).
    /// </summary>
    public abstract class AElementData : AMortalItemData {

        /// <summary>
        /// The local position of this Element relative to HQ.
        /// </summary>
        public Vector3 FormationPosition { get; set; }

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

        /// <summary>
        /// The mass of the Element.
        /// </summary>
        public float Mass { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AElementData" /> class.
        /// </summary>
        /// <param name="name">The name of the Element.</param>
        /// <param name="maxHitPoints">The maximum hit points.</param>
        /// <param name="mass">The mass.</param>
        /// <param name="optionalParentName">Name of the optional parent.</param>
        public AElementData(string name, float maxHitPoints, float mass, string optionalParentName = "")
            : base(name, maxHitPoints, optionalParentName) {
            Mass = mass;
        }


    }
}

