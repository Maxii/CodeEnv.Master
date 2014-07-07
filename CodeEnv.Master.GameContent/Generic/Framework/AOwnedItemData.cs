// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AOwnedItemData.cs
// Abstract base class that holds the data for an Item that can have an owner.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Abstract base class that holds the data for an Item that can have an owner.
    /// </summary>
    public abstract class AOwnedItemData : AItemData {

        private IPlayer _owner = TempGameValues.NoPlayer;
        public IPlayer Owner {
            get { return _owner; }
            set { SetProperty<IPlayer>(ref _owner, value, "Owner", OnOwnerChanged); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AOwnedItemData"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        public AOwnedItemData(string name) : base(name) { }

        protected virtual void OnOwnerChanged() {
            D.Log("{0} Owner has changed to {1}.", FullName, Owner.LeaderName);
        }

    }
}

