// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AData.cs
// Abstract base class for object data.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Abstract base class for object data.
    /// </summary>
    public abstract class AData : APropertyChangeTracking {

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

        private Transform _transform;
        public Transform Transform {
            protected get { return _transform; }
            set { SetProperty<Transform>(ref _transform, value, "Transform", OnTransformChanged); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AData" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="optionalParentName">Name of the optional parent.</param>
        public AData(string name, string optionalParentName = "") {
            _name = name;
            OptionalParentName = optionalParentName;
        }

        protected virtual void OnTransformChanged() {
            Transform.name = Name;
        }

        protected virtual void OnNameChanged() {
            Transform.name = Name;
        }

    }
}

