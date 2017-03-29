// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AIntelItemData.cs
// Abstract class for Data associated with an AIntelItem.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using CodeEnv.Master.Common;
    using UnityEngine;
    using UnityEngine.Profiling;

    /// <summary>
    /// Abstract class for Data associated with an AIntelItem.
    /// </summary>
    public abstract class AIntelItemData : AItemData {

        public event EventHandler<IntelCoverageChangedEventArgs> intelCoverageChanged;

        protected virtual IntelCoverage DefaultStartingIntelCoverage { get { return IntelCoverage.None; } }

        protected IGameManager _gameMgr;

        private IDictionary<Player, AIntel> _playerIntelLookup;

        #region Initialization 

        /// <summary>
        /// Initializes a new instance of the <see cref="AIntelItemData" /> class.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="owner">The owner.</param>
        public AIntelItemData(IIntelItem item, Player owner)
            : base(item, owner) {
            _gameMgr = GameReferences.GameManager;
        }

        public override void Initialize() {
            InitializePlayersIntel();   // must occur before InfoAccessCntlr gets initialized in base
            base.Initialize();
        }

        private void InitializePlayersIntel() {
            int playerCount = _gameMgr.AIPlayers.Count + 1;
            //D.Log(ShowDebugLog, "{0} initializing Players Intel settings. PlayerCount = {1}.", GetType().Name, playerCount);
            _playerIntelLookup = new Dictionary<Player, AIntel>(playerCount);
            var userPlayer = _gameMgr.UserPlayer;
            _playerIntelLookup.Add(userPlayer, InitializeIntelState(userPlayer));
            foreach (var aiPlayer in _gameMgr.AIPlayers) {
                _playerIntelLookup.Add(aiPlayer, InitializeIntelState(aiPlayer));
            }
        }

        private AIntel InitializeIntelState(Player player) {
            // 2.6.17 Moved decision of whether to use Comprehensive to Item.FinalInitialize which uses SetIntelCoverage().
            // SetIntelCoverage is the single place which calls PlayerAIMgr.HandleItemIntelCoverageChanged() when IntelCoverage
            // changes. This is the mechanism that manages a player's knowledge of an item. If the item's IntelCoverage > None,
            // the player has knowledge of the item, if None it doesn't.
            // WARNING: Using a DefaultStartingIntelCoverage value here besides None can result in SetIntelCoverage NOT calling 
            // PlayerAIMgr.HandleItemIntelCoverageChanged() if in fact IntelCoverage is being set to the same value as that
            // set here. This could result in the player not having knowledge of an item it should know about.
            // Currently, only Stars and the UCenter start with a DefaultStartingIntelCoverage other than None -> Basic. This is not
            // a problem as all Stars and UCenter are provided to PlayerKnowledge when instantiated and therefore don't rely
            // on PlayerAIMgr.HandleItemIntelCoverageChanged() to populate a player's knowledge.
            return InitializeIntel(DefaultStartingIntelCoverage);
        }

        #endregion

        private AIntel InitializeIntel(IntelCoverage initialcoverage) {
            var intel = MakeIntelInstance();
            intel.InitializeCoverage(initialcoverage);
            return intel;
        }

        /// <summary>
        /// Derived classes should instantiate their own AIntel-derived instance.
        /// </summary>
        /// <returns></returns>
        protected abstract AIntel MakeIntelInstance();

        /// <summary>
        /// Sets the intel coverage for this player. Returns <c>true</c> if the <c>newCoverage</c>
        /// was successfully accepted, <c>false</c> if it was rejected due to the inability of
        /// the item to regress its IntelCoverage.
        /// <remarks>If newCoverage == CurrentCoverage, this method will return true as the value
        /// was 'successfully accepted', but will not initiate any related coverage changed activity
        /// since coverage properly stayed the same.</remarks>
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="newCoverage">The new coverage.</param>
        /// <returns></returns>
        public bool SetIntelCoverage(Player player, IntelCoverage newCoverage) {

            Profiler.BeginSample("AIntelItemData.SetIntelCoverage");
            var playerIntel = GetIntel(player);
            if (playerIntel.IsCoverageChangeAllowed(newCoverage)) {
                if (playerIntel.CurrentCoverage != newCoverage) {
                    //D.Log(ShowDebugLog, "{0} is changing {1}'s IntelCoverage from {2} to {3}.", 
                    //    DebugName, player.DebugName, playerIntel.CurrentCoverage.GetValueName(), newCoverage.GetValueName());
                    playerIntel.CurrentCoverage = newCoverage;
                    HandleIntelCoverageChangedFor(player);
                    OnIntelCoverageChanged(player);
                }
                else {
                    //D.Log(ShowDebugLog, "{0} has declined to change {1}'s IntelCoverage to {2} because its the same value.", 
                    //    DebugName, player.DebugName, newCoverage.GetValueName());
                }
                Profiler.EndSample();
                return true;
            }
            //D.Log(ShowDebugLog, "{0} properly ignored changing {1}'s IntelCoverage from {2} to {3}.",
            //    DebugName, player, playerIntel.CurrentCoverage.GetValueName(), newCoverage.GetValueName());
            Profiler.EndSample();

            return false;
        }

        /// <summary>
        /// Gets the IntelCoverage that the provided <c>player</c> has currently achieved for this Item.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <returns></returns>
        public IntelCoverage GetIntelCoverage(Player player) { return GetIntel(player).CurrentCoverage; }

        /// <summary>
        /// Returns a copy of the intel instance for this player.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <returns></returns>
        public AIntel GetIntelCopy(Player player) {
            AIntel intelToCopy = GetIntel(player);
            if (intelToCopy is RegressibleIntel) {
                return new RegressibleIntel(intelToCopy as RegressibleIntel);
            }
            return new NonRegressibleIntel(intelToCopy as NonRegressibleIntel);
        }

        private AIntel GetIntel(Player player) {
            return _playerIntelLookup[player];
        }

        #region Event and Property Change Handlers

        /// <summary>
        /// Hook for derived Data classes that allows them to handle a change in this item's intel coverage.
        /// <remarks>Typically this item's data would not have anything to do when the item's IntelCoverage
        /// changes since data, by definition, is where full knowledge about the item is kept, independent
        /// of info access restrictions. Reports and interfaces play the role of 'filtering' a player's
        /// access to this knowledge stored in data by using the item's InfoAccessController.</remarks>
        /// </summary>
        /// <param name="player">The player.</param>
        protected virtual void HandleIntelCoverageChangedFor(Player player) {
            D.Log(ShowDebugLog, "{0}.IntelCoverage changed for {1} to {2}.", DebugName, player, GetIntelCoverage(player).GetValueName());
        }

        private void OnIntelCoverageChanged(Player player) {
            if (intelCoverageChanged != null) {
                intelCoverageChanged(this, new IntelCoverageChangedEventArgs(player));
            }
            else {
                D.Warn("{0} is not firing its intelCoverageChanged event as it has no subscribers.", DebugName);
            }
        }

        #endregion

        #region Nested Classes

        public class IntelCoverageChangedEventArgs : EventArgs {

            /// <summary>
            /// The player whose IntelCoverage of this item changed.
            /// </summary>
            public Player Player { get; private set; }

            public IntelCoverageChangedEventArgs(Player player) {
                Player = player;
            }

        }

        #endregion

    }
}

