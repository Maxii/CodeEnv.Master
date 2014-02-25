// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AItemData.cs
// Abstract base class for an Item's data.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Abstract base class for an Item's data.
    /// </summary>
    public abstract class AItemData : APropertyChangeTracking {

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

        private IPlayer _owner;
        public IPlayer Owner {
            get { return _owner; }
            set { SetProperty<IPlayer>(ref _owner, value, "Owner", OnOwnerChanged); }
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
        /// Initializes a new instance of the <see cref="AItemData" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="optionalParentName">Name of the optional parent.</param>
        public AItemData(string name, string optionalParentName = "") {
            _name = name;
            OptionalParentName = optionalParentName;
        }

        protected virtual void OnOwnerChanged() {
            if (Owner != null) {
                D.Log("{0} Owner has changed to {1}.", Name, Owner.LeaderName);
            }
            else {
                D.Log("{0} no longer has an owner.", Name);
            }
        }

        protected virtual void OnTransformChanged() {
            Transform.name = Name;
        }

        protected virtual void OnNameChanged() {
            Transform.name = Name;
        }

    }
}

