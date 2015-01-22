// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AItemData.cs
// Abstract base class that holds the data for an Item.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Abstract base class that holds the data for an Item.
    /// </summary>
    public abstract class AItemData : APropertyChangeTracking {

        public AIntel PlayerIntel { get; private set; }

        private string _name;
        /// <summary>
        /// Gets or sets the name of the item. 
        /// </summary>
        public string Name {
            get { return _name; }
            set { SetProperty<string>(ref _name, value, "Name", OnNameChanged, OnNameChanging); }
        }

        private string _parentName = string.Empty;
        /// <summary>
        /// Gets or sets the name of the Parent of this item. Optional.
        /// </summary>
        public string ParentName {
            get { return _parentName; }
            set { SetProperty<string>(ref _parentName, value, "ParentName", OnParentNameChanged, OnParentNameChanging); }
        }

        public string FullName {
            get { return ParentName.IsNullOrEmpty() ? Name : ParentName + Constants.Underscore + Name; }
        }

        private Player _owner = TempGameValues.NoPlayer;
        public Player Owner {
            get { return _owner; }
            set { SetProperty<Player>(ref _owner, value, "Owner", OnOwnerChanged); }
        }

        public Topography Topography { get; protected set; }   // can't use OnPropertyChanged approach as default(SpaceTopography) = OpenSpace, aka 0 tag

        /// <summary>
        /// Readonly. Gets the position of the gameObject containing this data.
        /// </summary>
        public Vector3 Position { get { return Transform.position; } }

        private Transform _transform;
        public Transform Transform {
            protected get { return _transform; }
            set { SetProperty<Transform>(ref _transform, value, "Transform", OnTransformChanged, OnTransformChanging); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AItemData" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        public AItemData(string name) {
            Name = name;
            PlayerIntel = InitializePlayerIntel();
        }

        /// <summary>
        /// Derived classes should override this if they have a different type of AIntel than <see cref="Intel"/>.
        /// </summary>
        /// <returns></returns>
        protected virtual AIntel InitializePlayerIntel() { return new Intel(); }

        protected virtual void OnOwnerChanged() {
            D.Log("{0} Owner has changed to {1}.", FullName, Owner.LeaderName);
        }

        private void OnTransformChanging(Transform newTransform) {
            D.Assert(Transform == null);    // Transform should only change once
        }

        protected virtual void OnTransformChanged() {
            Transform.name = Name;
        }

        private void OnNameChanging(string newName) {
            //string existingName = Name.IsNullOrEmpty() ? "'nullOrEmpty'" : Name;
            //D.Log("{0}.Name changing from {1} to {2}.", GetType().Name, existingName, newName);
        }

        protected virtual void OnNameChanged() {
            if (Transform != null) {    // Transform not set when Name initially set
                Transform.name = Name;
            }
        }

        private void OnParentNameChanging(string newParentName) {
            //string existingParentName = ParentName.IsNullOrEmpty() ? "'nullOrEmpty'" : ParentName;
            //string incomingParentName = newParentName.IsNullOrEmpty() ? "'nullOrEmpty'" : newParentName;
            //D.Log("{0}.ParentName changing from {1} to {2}.", Name, existingParentName, incomingParentName);
        }

        protected virtual void OnParentNameChanged() {
            //string newParentName = ParentName.IsNullOrEmpty() ? "'nullOrEmpty'" : ParentName;
            //D.Log("{0}.ParentName changed to {1}.", Name, newParentName);
        }

    }
}

