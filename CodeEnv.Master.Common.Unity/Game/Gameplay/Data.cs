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

        /// <summary>
        /// Gets or sets the name of the item. Does not notify of changes
        /// as I can't see much reason too.
        /// </summary>
        public string ItemName { get; set; }

        private string _pieceName;
        /// <summary>
        /// Gets or sets the name of the game piece.
        /// </summary>
        public string PieceName {
            get { return _pieceName; }
            set {
                SetProperty<string>(ref _pieceName, value, "PieceName");
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

        public Data(Transform t) {
            _transform = t;
        }
    }
}

