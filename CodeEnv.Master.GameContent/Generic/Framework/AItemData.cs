// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AItemData.cs
// Abstract class for Data associated with an AItem.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Abstract class for Data associated with an AItem.
    /// </summary>
    public abstract class AItemData : APropertyChangeTracking {

        private string _name;
        public string Name {
            get { return _name; }
            set { SetProperty<string>(ref _name, value, "Name", OnNameChanged, OnNameChanging); }
        }

        public virtual string FullName { get { return Name; } }

        private Player _owner;
        public Player Owner {
            get { return _owner; }
            set { SetProperty<Player>(ref _owner, value, "Owner", OnOwnerChanged); }
        }

        public Topography Topography { get; protected set; }   // can't use OnPropertyChanged approach as default(SpaceTopography) = OpenSpace, aka 0 tag

        public Vector3 Position { get { return _itemTransform.position; } }

        protected Transform _itemTransform;

        /// <summary>
        /// Initializes a new instance of the <see cref="AItemData"/> class.
        /// </summary>
        /// <param name="itemTransform">The item transform.</param>
        /// <param name="name">The name.</param>
        /// <param name="owner">The owner.</param>
        public AItemData(Transform itemTransform, string name, Player owner) {
            _itemTransform = itemTransform; // must preceed Name change
            Name = name;
            _owner = owner;
        }

        protected virtual void OnOwnerChanged() {
            //D.Log("{0} Owner has changed to {1}.", FullName, Owner.LeaderName);
        }

        private void OnNameChanging(string newName) {
            string existingName = Name.IsNullOrEmpty() ? "'nullOrEmpty'" : Name;
            D.Log("{0}.Name changing from {1} to {2}.", GetType().Name, existingName, newName);
        }

        private void OnNameChanged() {
            _itemTransform.name = Name;
        }


    }
}

