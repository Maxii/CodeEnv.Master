// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FacilityData.cs
// All the data associated with a particular Facility.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// All the data associated with a particular Facility.
    /// </summary>
    //public class FacilityData : AMortalData {
    public class FacilityData : AElementData<FacilityCategory> {

        //public FacilityCategory Category { get; private set; }

        ///// <summary>
        ///// The local position of this Facility within the Starbase.
        ///// </summary>
        //public Vector3 FormationPosition { get; set; }

        //private IPlayer _owner;
        //public IPlayer Owner {
        //    get { return _owner; }
        //    set {
        //        SetProperty<IPlayer>(ref _owner, value, "Owner");
        //    }
        //}

        //private CombatStrength _combatStrength;
        //public CombatStrength Strength {
        //    get { return _combatStrength; }
        //    set {
        //        SetProperty<CombatStrength>(ref _combatStrength, value, "Strength");
        //    }
        //}

        //public float Mass { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FacilityData" /> class.
        /// </summary>
        /// <param name="category">The kind of Facility.</param>
        /// <param name="name">Name of the Facility.</param>
        /// <param name="maxHitPoints">The maximum hit points.</param>
        /// <param name="mass">The mass.</param>
        public FacilityData(FacilityCategory category, string name, float maxHitPoints, float mass)
            : base(category, name, maxHitPoints, mass) {
            //Category = category;
            //Mass = mass;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

