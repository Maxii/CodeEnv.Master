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
        /// When <c>true</c> indicates this item has commenced operations, and if it
        /// is a MortalItem, that it is not dead.
        /// <remarks>Use IsDead to kill a MortalItem.</remarks>
        /// </summary>
        public bool IsOperational {
            get { return _isOperational; }
            set { SetProperty<bool>(ref _isOperational, value, "IsOperational", IsOperationalPropChangedHandler); }
        }

        /// <summary>
        /// One of two IsOwnerChgUnderway versions.
        /// <remarks>6.20.18 This one is being used in one case so far were it appears to be needed.
        /// 1) When FleetCmdData is responding to an IntelCoverage change from its HQElement and determines 
        /// that the HQElement's coverage is changing because its owner is changing and its about to separate from the fleet.
        /// In that case it declines to chg its own coverage to track its departing HQElement, knowing that its about to
        /// get a new HQElement whose coverage it will want.
        /// </summary>
        public bool IsOwnerChgUnderway { get; set; }

        /// <summary>
        /// Used for controlling other player's access to Item Info.
        /// </summary>
        public AInfoAccessController InfoAccessCntlr { get; private set; }

        public bool ShowDebugLog { get { return Item.ShowDebugLog; } }

        public IOwnerItem Item { get; private set; }

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
            __debugCntls = GameReferences.DebugControls;
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
            // 2.6.17 moved IsOperational = true to last thing Item does in FinalInitialize.
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
            IsOwnerChgUnderway = true;
            HandleOwnerChanging(newOwner);
        }

        private void OwnerPropChangedHandler() {
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

        protected virtual void HandleOwnerChanging(Player incomingOwner) { }

        private void HandleOwnerChanged() {
            //D.Log(ShowDebugLog, "{0} Owner has changed to {1}.", DebugName, Owner);
            PropagateOwnerChange();
            HandleOwnerChangesComplete();
        }

        /// <summary>
        /// Hook for derived classes to propagate any item owner changes that depend on this change.
        /// <remarks>Called immediately after this Item owner has changed and just before HandleOwnerChangesComplete.</remarks>
        /// <remarks>6.17.18 Makes sure all dependent owner changes have taken place before other events that
        /// are triggered by this change can fire, ala IntelCoverage and InfoAccess Chg events. Specifically, when a Settlement
        /// is founded, it changes the system owner which can change the IntelCoverage which can fire a InfoAccessChg event
        /// which is processed by the fleet that founded the settlement before the Sector's owner has been changed resulting in
        /// an error.</remarks>
        /// </summary>
        protected virtual void PropagateOwnerChange() { }

        /// <summary>
        /// Hook for derived classes called after all dependent owner changes 
        /// have been propagated that result from this Item's owner change.
        /// </summary>
        protected virtual void HandleOwnerChangesComplete() { }

        protected virtual void HandleTopographyChanged() { }

        public sealed override string ToString() {
            return DebugName;
        }

        #region Debug

        protected IDebugControls __debugCntls;

        /// <summary>
        /// One of two IsOwnerChgUnderway versions.
        /// <remarks>6.20.18 This one is used to support debug and is not needed in a release version.</remarks>
        /// </summary>
        public bool __IsOwnerChgUnderway { get { return IsOwnerChgUnderway; } }

        #endregion

    }
}

