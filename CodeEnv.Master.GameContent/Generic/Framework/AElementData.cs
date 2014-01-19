// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ADefendedData.cs
// Abstract generic base class for data associated with Elements (Items under a Command) that can engage in combat.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Abstract generic base class for data associated with Elements (Items under a Command).
    /// </summary>
    /// <typeparam name="ElementCategoryType">The Type that defines the possible sub-categories of an Element, eg. a ShipItem can be sub-categorized as a Frigate which is defined within the ShipCategory Type.</typeparam>
    public abstract class AElementData<ElementCategoryType> : AMortalData where ElementCategoryType : struct {

        public ElementCategoryType ElementCategory { get; private set; }

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
        /// Initializes a new instance of the <see cref="AElementData{ElementCategoryType}"/> class.
        /// </summary>
        /// <param name="category">The Category of this Element instance.</param>
        /// <param name="name">The name of the Element.</param>
        /// <param name="maxHitPoints">The maximum hit points.</param>
        /// <param name="mass">The mass.</param>
        /// <param name="optionalParentName">Name of the optional parent.</param>
        public AElementData(ElementCategoryType category, string name, float maxHitPoints, float mass, string optionalParentName = "")
            : base(name, maxHitPoints, optionalParentName) {
            ElementCategory = category;
            Mass = mass;
        }

    }
}

