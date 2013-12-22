// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AMortalData.cs
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
    public abstract class AMortalData : AData {

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
        public float CurrentHitPoints {
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
        public float Health {
            get {
                //D.Log("Health {0}, CurrentHitPoints {1}, MaxHitPoints {2}.", _health, _currentHitPoints, _maxHitPoints);
                return _health;
            }
            private set {
                value = Mathf.Clamp01(value);
                SetProperty<float>(ref _health, value, "Health");
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AMortalData" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="maxHitPoints">The maximum hit points.</param>
        /// <param name="optionalParentName">Name of the optional parent.</param>
        public AMortalData(string name, float maxHitPoints, string optionalParentName = "")
            : base(name, optionalParentName) {
            _maxHitPoints = maxHitPoints;
            CurrentHitPoints = maxHitPoints;
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

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

