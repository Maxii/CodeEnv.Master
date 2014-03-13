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

    using System;
    using System.Linq;
    using System.Collections.Generic;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Abstract base class for data associated with Elements (Items under a Command).
    /// </summary>
    public abstract class AElementData : AMortalItemData {

        /// <summary>
        /// The desired position offset of this Element from the HQElement.
        /// </summary>
        public Vector3 FormationPosition { get; set; }

        private float _maxWeaponsRange;
        public float MaxWeaponsRange {
            get { return _maxWeaponsRange; }
            set { SetProperty<float>(ref _maxWeaponsRange, value, "MaxWeaponsRange"); }
        }

        /// <summary>
        /// Dictionary for finding the list of weapons associated with a particular rangeTracker.
        /// </summary>
        protected IDictionary<Guid, IList<Weapon>> _weaponRangeTrackerLookup;

        /// <summary>
        /// Initializes a new instance of the <see cref="AElementData" /> class.
        /// </summary>
        /// <param name="name">The name of the Element.</param>
        /// <param name="maxHitPoints">The maximum hit points.</param>
        /// <param name="mass">The mass of the Element.</param>
        /// <param name="optionalParentName">Name of the optional parent.</param>
        public AElementData(string name, float maxHitPoints, float mass, string optionalParentName = "")
            : base(name, maxHitPoints, mass, optionalParentName) {
            _weaponRangeTrackerLookup = new Dictionary<Guid, IList<Weapon>>();
        }

        public void AddWeapon(Weapon weapon, Guid trackerID) {
            weapon.TrackerID = trackerID;
            if (!_weaponRangeTrackerLookup.ContainsKey(trackerID)) {
                _weaponRangeTrackerLookup.Add(trackerID, new List<Weapon>());
            }
            _weaponRangeTrackerLookup[trackerID].Add(weapon);   // duplicates allowed
            RecalcMaxWeaponsRange();
        }

        public bool RemoveWeapon(Weapon weapon) {
            var trackerID = weapon.TrackerID;
            var trackerWeapons = _weaponRangeTrackerLookup[trackerID];
            var result = trackerWeapons.Remove(weapon);
            if (trackerWeapons.Count == Constants.Zero) {
                _weaponRangeTrackerLookup.Remove(trackerID);
                D.Warn("{0} has removed a weapon, leaving an unused {1}.", Name, typeof(IRangeTracker).Name);
            }
            RecalcMaxWeaponsRange();
            return result;
        }

        public IList<Weapon> GetWeapons(Guid trackerID) {
            IList<Weapon> weapons;
            if (!_weaponRangeTrackerLookup.TryGetValue(trackerID, out weapons)) {
                weapons = new List<Weapon>(0);
            }
            return weapons;
        }

        public IEnumerable<Weapon> GetWeapons() {
            IEnumerable<Weapon> result = Enumerable.Empty<Weapon>();
            foreach (var key in _weaponRangeTrackerLookup.Keys) {
                result = result.Concat(_weaponRangeTrackerLookup[key]);
            }
            return result;
        }

        private void RecalcMaxWeaponsRange() {
            float result = Constants.ZeroF;
            foreach (var key in _weaponRangeTrackerLookup.Keys) {
                var weapons = _weaponRangeTrackerLookup[key];
                var maxRange = weapons.Max<Weapon>(w => w.Range);
                if (maxRange > result) {
                    result = maxRange;
                }
            }
            MaxWeaponsRange = result;
        }

    }
}

