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

        /// <summary>
        /// Adds the weapon to this element data pairing it with the assigned tracker ID.
        /// </summary>
        /// <param name="weapon">The weapon.</param>
        /// <param name="trackerID">The range tracker identifier.</param>
        public void AddWeapon(Weapon weapon, Guid trackerID) {
            weapon.TrackerID = trackerID;
            if (!_weaponRangeTrackerLookup.ContainsKey(trackerID)) {
                _weaponRangeTrackerLookup.Add(trackerID, new List<Weapon>());
            }
            _weaponRangeTrackerLookup[trackerID].Add(weapon);   // duplicates allowed
            RecalcMaxWeaponsRange();
        }

        /// <summary>
        /// Removes the weapon from the Element's collection of weapons, returning a flag indicating
        /// whether this weapon's range tracker is still being utilized by other weapons.
        /// </summary>
        /// <param name="weapon">The weapon.</param>
        /// <returns>
        /// <c>true</c> if there are one or more weapons still utilizing this weapon's range tracker, 
        /// <c>false</c> if this was the last weapon utilizing the weapon's range tracker.
        /// </returns>
        /// <exception cref="KeyNotFoundException">{0} has no weapon to remove named {1}..Inject(FullName, weapon.Name)</exception>
        public bool RemoveWeapon(Weapon weapon) {
            var trackerID = weapon.TrackerID;
            var trackerWeapons = _weaponRangeTrackerLookup[trackerID];  // throws KeyNotFoundException 
            if (!trackerWeapons.Remove(weapon)) {
                throw new KeyNotFoundException("{0} has no weapon to remove named {1}.".Inject(FullName, weapon.Name));
            }
            var isRangeTrackerStillInUse = true;
            if (trackerWeapons.Count == Constants.Zero) {
                _weaponRangeTrackerLookup.Remove(trackerID);
                isRangeTrackerStillInUse = false;
                D.Warn("{0} has removed weapon {1}, leaving an unused {2}.", Name, weapon.Name, typeof(IWeaponRangeTracker).Name);
            }
            RecalcMaxWeaponsRange();
            return isRangeTrackerStillInUse;
        }

        public IList<Weapon> GetWeapons(Guid trackerID) {
            IList<Weapon> weapons;
            if (!_weaponRangeTrackerLookup.TryGetValue(trackerID, out weapons)) {
                D.Warn("{0} has no weapons utilizing provided tracker ID.", FullName);
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

