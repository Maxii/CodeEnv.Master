// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AElementData.cs
// Abstract base class that holds data for Items that are elements of a command.
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
    /// Abstract base class that holds data for Items that are elements of a command.
    /// </summary>
    public abstract class AElementData : AMortalItemData {


        public IList<Weapon> Weapons { get; private set; }

        /// <summary>
        /// Dictionary for finding the list of weapons associated with a particular rangeTracker.
        /// </summary>
        //protected IDictionary<Guid, IList<Weapon>> _weaponRangeTrackerLookup;

        /// <summary>
        /// Initializes a new instance of the <see cref="AElementData" /> class.
        /// </summary>
        /// <param name="name">The name of the Element.</param>
        /// <param name="mass">The mass of the Element.</param>
        /// <param name="maxHitPoints">The maximum hit points.</param>
        public AElementData(string name, float mass, float maxHitPoints)
            : base(name, mass, maxHitPoints) {
            //_weaponRangeTrackerLookup = new Dictionary<Guid, IList<Weapon>>();
            Weapons = new List<Weapon>();
        }

        /// <summary>
        /// Adds the weapon to this element data pairing it with the assigned tracker ID.
        /// </summary>
        /// <param name="weapon">The weapon.</param>
        /// <param name="trackerID">The range tracker identifier.</param>
        //public void AddWeapon(Weapon weapon, Guid trackerID) {
        //    weapon.MonitorID = trackerID;
        //    if (!_weaponRangeTrackerLookup.ContainsKey(trackerID)) {
        //        _weaponRangeTrackerLookup.Add(trackerID, new List<Weapon>());
        //    }
        //    _weaponRangeTrackerLookup[trackerID].Add(weapon);   // duplicates allowed
        //    RecalcMaxWeaponsRange();
        //    RecalcCombatStrength();
        //}

        public void AddWeapon(Weapon weapon) {
            D.Assert(weapon.MonitorID != default(Guid));
            D.Assert(!Weapons.Contains(weapon));
            Weapons.Add(weapon);

            RecalcMaxWeaponsRange();
            RecalcCombatStrength();
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
        //public bool RemoveWeapon(Weapon weapon) {
        //    var trackerID = weapon.MonitorID;
        //    var trackerWeapons = _weaponRangeTrackerLookup[trackerID];  // throws KeyNotFoundException 
        //    if (!trackerWeapons.Remove(weapon)) {
        //        throw new KeyNotFoundException("{0} has no weapon to remove named {1}.".Inject(FullName, weapon.Name));
        //    }
        //    var isRangeTrackerStillInUse = true;
        //    if (trackerWeapons.Count == Constants.Zero) {
        //        _weaponRangeTrackerLookup.Remove(trackerID);
        //        isRangeTrackerStillInUse = false;
        //        D.Warn("{0} has removed weapon {1}, leaving an unused WeaponRangeMonitor.", Name, weapon.Name);
        //    }
        //    RecalcMaxWeaponsRange();
        //    RecalcCombatStrength();
        //    return isRangeTrackerStillInUse;
        //}

        public void RemoveWeapon(Weapon weapon) {
            D.Assert(Weapons.Contains(weapon));
            Weapons.Remove(weapon);
            RecalcMaxWeaponsRange();
            RecalcCombatStrength();
        }

        //public IList<Weapon> GetWeapons(Guid trackerID) {
        //    IList<Weapon> weapons;
        //    if (!_weaponRangeTrackerLookup.TryGetValue(trackerID, out weapons)) {
        //        D.Warn("{0} has no weapons utilizing provided tracker ID.", FullName);
        //        weapons = new List<Weapon>(0);
        //    }
        //    return weapons;
        //}

        //public IEnumerable<Weapon> GetWeapons() {
        //    return _weaponRangeTrackerLookup.Values.SelectMany(weap => weap);
        //}

        //private void RecalcMaxWeaponsRange() {
        //    MaxWeaponsRange = GetWeapons().Max(weap => weap.Range);
        //}

        //private void RecalcCombatStrength() {
        //    var defaultValueIfEmpty = default(CombatStrength);
        //    Strength = GetWeapons().Select(weap => weap.Strength).Aggregate(defaultValueIfEmpty, (accum, wStrength) => accum + wStrength);
        //}

        private void RecalcMaxWeaponsRange() {
            MaxWeaponsRange = Weapons.Max(weap => weap.Range);
        }

        private void RecalcCombatStrength() {
            var defaultValueIfEmpty = default(CombatStrength);
            Strength = Weapons.Select(weap => weap.Strength).Aggregate(defaultValueIfEmpty, (accum, wStrength) => accum + wStrength);
        }


    }
}

