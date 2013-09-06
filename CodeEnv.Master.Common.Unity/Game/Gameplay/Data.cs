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

namespace CodeEnv.Master.Common.Unity {

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
                return _transform.position;
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

        protected Transform _transform;

        public Data(Transform t, string name, string optionalParentName = "") {
            _transform = t;
            Name = name;
            OptionalParentName = optionalParentName;
        }

        protected virtual void OnNameChanged() {
            _transform.name = Name;
        }
    }
}

