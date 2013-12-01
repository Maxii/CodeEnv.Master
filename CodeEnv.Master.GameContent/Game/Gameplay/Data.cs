// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Data.cs
// Base class for System, Fleet, Ship, System and all celestial objects data.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Base class for System, Fleet, Ship, System and all celestial objects data.
    /// </summary>
    public class Data : APropertyChangeTracking {

        private string _name;
        /// <summary>
        /// Gets or sets the name of the item. 
        /// </summary>
        public string Name {
            get { return _name; }
            set { SetProperty<string>(ref _name, value, "Name", OnNameChanged); }
        }

        private string _optionalParentName;
        /// <summary>
        /// Gets or sets the name of the Parent of this item. Optional.
        /// </summary>
        public string OptionalParentName {
            get { return _optionalParentName; }
            set {
                SetProperty<string>(ref _optionalParentName, value, "OptionalParentName");
            }
        }

        /// <summary>
        /// Readonly. Gets the position of the gameObject containing this data.
        /// </summary>
        public Vector3 Position {
            get {
                return Transform.position;
            }
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

        private GameDate _lastHumanPlayerIntelDate;
        /// <summary>
        /// Gets or sets the date the human player last had 
        /// intel on this location. Used only when IntelState is
        /// OutOfDate to derive the age of the last intel, 
        /// this property only needs to be updated
        /// when the intel state changes to OutOfDate.
        /// </summary>
        public GameDate LastHumanPlayerIntelDate {
            get { return _lastHumanPlayerIntelDate; }
            set {
                SetProperty<GameDate>(ref _lastHumanPlayerIntelDate, value, "LastHumanPlayerIntelDate");
            }
        }

        private Transform _transform;
        public Transform Transform {
            protected get { return _transform; }
            set { SetProperty<Transform>(ref _transform, value, "Transform", OnTransformChanged); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Data" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="maxHitPoints">The maximum hit points.</param>
        /// <param name="optionalParentName">Name of the optional parent.</param>
        public Data(string name, float maxHitPoints, string optionalParentName = "") {
            _name = name;
            _maxHitPoints = maxHitPoints;
            CurrentHitPoints = maxHitPoints;
            OptionalParentName = optionalParentName;
        }

        protected virtual void OnTransformChanged() {
            Transform.name = Name;
        }

        protected virtual void OnNameChanged() {
            Transform.name = Name;
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

    }
}

