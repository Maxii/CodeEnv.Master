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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Abstract class for Data associated with an AItem.
    /// </summary>
    public abstract class AItemData : APropertyChangeTracking {

        private string _name;
        /// <summary>
        /// The display name of this item.
        /// </summary>
        public string Name {
            get { return _name; }
            set { SetProperty<string>(ref _name, value, "Name"); }  // 7.10.17 AItem subscribes to changes
        }

        public virtual string DebugName { get { return Name; } }

        private Player _owner;
        public Player Owner {
            get { return _owner; }
            set { SetProperty<Player>(ref _owner, value, "Owner", OwnerPropChangedHandler, OwnerPropertyChangingHandler); }
        }

        private Topography _topography;
        public Topography Topography {
            get {
                D.AssertNotDefault((int)_topography, DebugName);
                return _topography;
            }
            set { SetProperty<Topography>(ref _topography, value, "Topography", TopographyPropChangedHandler); }
        }

        public Vector3 Position { get { return Item.Position; } }

        private bool _isOperational;
        /// <summary>
        /// Indicates whether this item has commenced operations, and if
        /// it is a MortalItem, that it is not dead. Set to false to initiate death.
        /// </summary>
        public bool IsOperational {
            get { return _isOperational; }
            set { SetProperty<bool>(ref _isOperational, value, "IsOperational", IsOperationalPropChangedHandler); }
        }

        public bool IsOwnerChangeUnderway { get; set; }

        /// <summary>
        /// Used for controlling other player's access to Item Info.
        /// </summary>
        public AInfoAccessController InfoAccessCntlr { get; private set; }

        public bool ShowDebugLog { get { return Item.ShowDebugLog; } }

        public IOwnerItem Item { get; private set; }

        protected IDebugControls _debugCntls;

        #region Initialization

        /// <summary>
        /// Initializes a new instance of the <see cref="AItemData" /> class.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="owner">The owner.</param>
        public AItemData(IOwnerItem item, Player owner) {
            Item = item;
            D.AssertNotNull(owner, DebugName);  // owner can be NoPlayer but never null
            _owner = owner;
            _debugCntls = GameReferences.DebugControls;
            // 7.9.16 Initialize(); now called by AItem.InitializeOnData to move out of constructor
        }

        public virtual void Initialize() {
            InfoAccessCntlr = InitializeInfoAccessController();
        }

        protected abstract AInfoAccessController InitializeInfoAccessController();

        /// <summary>
        /// Called by Item.FinalInitialize(), this is the counterpart in data
        /// providing an opportunity to complete initialization before CommenceOperations is called.
        /// </summary>
        public virtual void FinalInitialize() {
            // 8.1.16 moved IsOperational = true here from CommenceOperations to allow all items to be IsOperational 
            // before the first Item.CommenceOperations is called.
            // 2.6.17 moved IsOperational = true to last thing Item does in FinalInitialize. Its presence here was the first thing that happened.
        }

        #endregion

        /// <summary>
        /// Called by Item.CommenceOperations(), this is its counterpart in data
        /// providing an opportunity to activate equipment that resides in data.
        /// </summary>
        public virtual void CommenceOperations() { }

        #region Event and Property Change Handlers

        private void IsOperationalPropChangedHandler() {
            HandleIsOperationalChanged();
        }

        private void OwnerPropertyChangingHandler(Player newOwner) {
            IsOwnerChangeUnderway = true;
            HandleOwnerChanging(newOwner);
        }

        private void OwnerPropChangedHandler() {
            D.AssertNotNull(Owner, DebugName);  // owner can be NoPlayer but never null
            HandleOwnerChanged();
            // IsOwnerChangeUnderway = false; Handled from AItem when all change work completed
        }

        private void TopographyPropChangedHandler() {
            HandleTopographyChanged();
        }

        #endregion

        protected virtual void HandleIsOperationalChanged() {
            D.Assert(IsOperational);    // only MortalItems should ever see a change to false
        }

        protected virtual void HandleOwnerChanging(Player newOwner) { }

        protected virtual void HandleOwnerChanged() {
            //D.Log(ShowDebugLog, "{0} Owner has changed to {1}.", DebugName, Owner);
        }

        protected virtual void HandleTopographyChanged() { }

        public sealed override string ToString() {
            return DebugName;
        }

    }
}

