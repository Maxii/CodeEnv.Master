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

        private string _name; // default of string is null
        /// <summary>
        /// Gets or sets the name of the item. 
        /// </summary>
        public string Name {
            get { return _name; }
            set { SetProperty<string>(ref _name, value, "Name", OnNameChanged, OnNameChanging); }
        }

        private string _parentName; // default of string is null
        /// <summary>
        /// Gets or sets the name of the Parent of this item. Optional.
        /// </summary>
        public string ParentName {
            get { return _parentName; }
            set { SetProperty<string>(ref _parentName, value, "ParentName", OnParentNameChanged, OnParentNameChanging); }
        }


        public string FullName {
            get { return ParentName == string.Empty ? Name : ParentName + Constants.Underscore + Name; }
        }

        private IPlayer _owner = TempGameValues.NoPlayer;
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
            Name = name;
            ParentName = optionalParentName;
        }

        protected virtual void OnOwnerChanged() {
            if (Owner != TempGameValues.NoPlayer) {
                D.Log("{0} Owner has changed to {1}.", FullName, Owner.LeaderName);
            }
            else {
                D.Warn("{0} no longer has an owner.", FullName);
            }
        }

        protected virtual void OnTransformChanged() {
            Transform.name = Name;
        }

        private void OnNameChanging(string newName) {
            string existingName = Name.IsNullOrEmpty() ? "'nullOrEmpty'" : Name;
            D.Log("{0}.Name changing from {1} to {2}.", GetType().Name, existingName, newName);
        }

        protected virtual void OnNameChanged() {
            if (Transform != null) {    // Transform not set when Name initially set
                Transform.name = Name;
            }
        }

        private void OnParentNameChanging(string newParentName) {
            string existingParentName = ParentName.IsNullOrEmpty() ? "'nullOrEmpty'" : ParentName;
            string incomingParentName = newParentName.IsNullOrEmpty() ? "'nullOrEmpty'" : newParentName;
            D.Log("{0}.ParentName changing from {1} to {2}.", Name, existingParentName, incomingParentName);
        }

        protected virtual void OnParentNameChanged() {
            //string newParentName = ParentName.IsNullOrEmpty() ? "'nullOrEmpty'" : ParentName;
            //D.Log("{0}.ParentName changed to {1}.", Name, newParentName);
        }

    }
}

