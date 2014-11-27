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
        /// Initializes a new instance of the <see cref="AElementData" /> class.
        /// </summary>
        /// <param name="name">The name of the Element.</param>
        /// <param name="mass">The mass of the Element.</param>
        /// <param name="maxHitPoints">The maximum hit points.</param>
        public AElementData(string name, float mass, float maxHitPoints)
            : base(name, mass, maxHitPoints) {
            Weapons = new List<Weapon>();
        }

        /// <summary>
        /// Adds the weapon to this element data.
        /// </summary>
        /// <param name="weapon">The weapon.</param>
        public void AddWeapon(Weapon weapon) {
            D.Assert(weapon.RangeMonitor != null);
            D.Assert(!Weapons.Contains(weapon));
            Weapons.Add(weapon);

            RecalcMaxWeaponsRange();
            RecalcCombatStrength();
        }

        /// <summary>
        /// Removes the weapon from the Element's collection of weapons.
        /// </summary>
        /// <param name="weapon">The weapon.</param>
        public void RemoveWeapon(Weapon weapon) {
            D.Assert(Weapons.Contains(weapon));
            Weapons.Remove(weapon);
            RecalcMaxWeaponsRange();
            RecalcCombatStrength();
        }

        private void RecalcMaxWeaponsRange() {
            MaxWeaponsRange = Weapons.Max(weap => weap.Range);
        }

        private void RecalcCombatStrength() {
            var defaultValueIfEmpty = default(CombatStrength);
            Strength = Weapons.Select(weap => weap.Strength).Aggregate(defaultValueIfEmpty, (accum, wStrength) => accum + wStrength);
        }

    }
}

