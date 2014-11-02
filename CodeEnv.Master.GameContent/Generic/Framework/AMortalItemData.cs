// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AMortalItemData.cs
// Abstract base class that holds data for Items that can take damage and die.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Abstract base class that holds data for Items that can take damage and die.
    /// </summary>
    public abstract class AMortalItemData : AItemData {

        private float _maxHitPoints;
        public float MaxHitPoints {
            get { return _maxHitPoints; }
            set {
                D.Assert(value >= Constants.ZeroF);
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

        private CombatStrength _strength;
        public CombatStrength Strength {
            get { return _strength; }
            set {
                SetProperty<CombatStrength>(ref _strength, value, "Strength");
            }
        }

        private float _maxWeaponsRange;
        /// <summary>
        /// The maximum range of this item's weapons.
        /// </summary>
        public virtual float MaxWeaponsRange {
            get { return _maxWeaponsRange; }
            set { SetProperty<float>(ref _maxWeaponsRange, value, "MaxWeaponsRange"); }
        }

        /// <summary>
        /// The mass of the Item.
        /// </summary>
        public float Mass { get; private set; }

        public AMortalItemData(string name, float mass, float maxHitPts)
            : base(name) {
            Mass = mass;
            MaxHitPoints = maxHitPts;
            CurrentHitPoints = maxHitPts;
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

