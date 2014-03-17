// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AMortalItemData.cs
// Abstract base class for data associated with mortal items.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Abstract base class for data associated with mortal items.
    /// </summary>
    public abstract class AMortalItemData : AItemData {

        private float _maxWeaponsRange;
        public float MaxWeaponsRange {
            get { return _maxWeaponsRange; }
            set { SetProperty<float>(ref _maxWeaponsRange, value, "MaxWeaponsRange"); }
        }

        private float _maxHitPoints;
        public float MaxHitPoints {
            get { return _maxHitPoints; }
            set {
                value = Mathf.Clamp(value, Constants.ZeroF, Mathf.Infinity);
                SetProperty<float>(ref _maxHitPoints, value, "MaxHitPoints", OnMaxHitPointsChanged, OnMaxHitPointsChanging);
            }
        }

        private float _currentHitPoints;
        /// <summary>
        /// Gets or sets the current hit points of this item. 
        /// </summary>
        public virtual float CurrentHitPoints {
            get { return _currentHitPoints; }
            set {
                value = Mathf.Clamp(value, Constants.ZeroF, MaxHitPoints);
                SetProperty<float>(ref _currentHitPoints, value, "CurrentHitPoints", OnCurrentHitPointsChanged);
            }
        }

        private float _health;
        /// <summary>
        /// Readonly. Indicates the health of the item, a value between 0 and 1.
        /// </summary>
        public virtual float Health {
            get {
                //D.Log("Health {0}, CurrentHitPoints {1}, MaxHitPoints {2}.", _health, _currentHitPoints, _maxHitPoints);
                return _health;
            }
            private set {
                value = Mathf.Clamp01(value);
                SetProperty<float>(ref _health, value, "Health", OnHealthChanged);
            }
        }

        private CombatStrength _combatStrength = new CombatStrength();
        public CombatStrength Strength {
            get { return _combatStrength; }
            set {
                SetProperty<CombatStrength>(ref _combatStrength, value, "Strength");
            }
        }

        /// <summary>
        /// The mass of the Item.
        /// </summary>
        public float Mass { get; private set; }


        /// <summary>
        /// Initializes a new instance of the <see cref="AMortalItemData" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="maxHitPoints">The maximum hit points.</param>
        /// <param name="mass">The mass of the Item.</param>
        /// <param name="optionalParentName">Name of the optional parent.</param>
        public AMortalItemData(string name, float maxHitPoints, float mass, string optionalParentName = "")
            : base(name, optionalParentName) {
            _maxHitPoints = maxHitPoints;
            CurrentHitPoints = maxHitPoints;
            Mass = mass;
        }

        private void OnMaxHitPointsChanging(float newMaxHitPoints) {
            if (newMaxHitPoints < MaxHitPoints) {
                // reduction in max hit points so reduce current hit points to match
                CurrentHitPoints = Mathf.Clamp(CurrentHitPoints, Constants.ZeroF, newMaxHitPoints);
                // FIXME changing CurrentHitPoints here sends out a temporary erroneous health change event. The accurate health change event follows shortly
            }
        }

        private void OnMaxHitPointsChanged() {
            Health = MaxHitPoints > Constants.ZeroF ? CurrentHitPoints / MaxHitPoints : Constants.ZeroF;
        }

        private void OnCurrentHitPointsChanged() {
            Health = MaxHitPoints > Constants.ZeroF ? CurrentHitPoints / MaxHitPoints : Constants.ZeroF;
        }

        protected virtual void OnHealthChanged() { }

    }
}

