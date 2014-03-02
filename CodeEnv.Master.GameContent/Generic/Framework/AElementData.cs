﻿// --------------------------------------------------------------------------------------------------------------------
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

    using System.Collections.Generic;
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

        private float _weaponRange;
        public float WeaponRange {
            get { return _weaponRange; }
            set { SetProperty<float>(ref _weaponRange, value, "WeaponRange"); }
        }

        private float _weaponReloadPeriod;
        /// <summary>
        /// The time in hours required to reload and fire this weapon. The inverse is the
        /// Weapon's Rate of Fire.
        /// </summary>
        public float WeaponReloadPeriod {
            get { return _weaponReloadPeriod; }
            set { SetProperty<float>(ref _weaponReloadPeriod, value, "WeaponReloadPeriod"); }
        }

        //protected IDictionary<RangeTrackerID, IList<Weapon>> _weaponLookup;


        /// <summary>
        /// Initializes a new instance of the <see cref="AElementData" /> class.
        /// </summary>
        /// <param name="name">The name of the Element.</param>
        /// <param name="maxHitPoints">The maximum hit points.</param>
        /// <param name="mass">The mass of the Element.</param>
        /// <param name="optionalParentName">Name of the optional parent.</param>
        public AElementData(string name, float maxHitPoints, float mass, string optionalParentName = "")
            : base(name, maxHitPoints, mass, optionalParentName) {
            //_weaponLookup = new Dictionary<RangeTrackerID, IList<Weapon>>();

        }

        //public void AddWeapon(Weapon weapon) {

        //}

    }
}

