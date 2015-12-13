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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Abstract class for Data associated with an AIntelItem.
    /// </summary>
    public abstract class AIntelItemData : ADiscernibleItemData { //AItemData {

        public event EventHandler userIntelCoverageChanged;

        public event EventHandler<IntelEventArgs> intelCoverageChanged;

        protected virtual IntelCoverage DefaultStartingIntelCoverage { get { return IntelCoverage.None; } }

        protected IGameManager _gameMgr;
        private IDictionary<Player, AIntel> _playerIntelLookup;

        /// <summary>
        /// Initializes a new instance of the <see cref="AIntelItemData"/> class.
        /// </summary>
        /// <param name="itemTransform">The item transform.</param>
        /// <param name="name">The name.</param>
        /// <param name="owner">The owner.</param>
        /// <param name="cameraStat">The camera stat.</param>
        public AIntelItemData(Transform itemTransform, string name, Player owner, CameraFocusableStat cameraStat)
            : base(itemTransform, name, owner, cameraStat) {
            _gameMgr = References.GameManager;
            InitializePlayersIntel();
        }

        private void InitializePlayersIntel() {
            int playerCount = _gameMgr.AIPlayers.Count + 1;
            //D.Log("{0} initializing Players Intel settings. PlayerCount = {1}.", GetType().Name, playerCount);
            _playerIntelLookup = new Dictionary<Player, AIntel>(playerCount);
            var userPlayer = _gameMgr.UserPlayer;
            _playerIntelLookup.Add(userPlayer, InitializeIntelState(userPlayer));
            foreach (var aiPlayer in _gameMgr.AIPlayers) {
                _playerIntelLookup.Add(aiPlayer, InitializeIntelState(aiPlayer));
            }
        }

        private AIntel InitializeIntelState(Player player) {
            bool isCoverageComprehensive = DebugSettings.Instance.AllIntelCoverageComprehensive || Owner == player;
            var coverage = isCoverageComprehensive ? IntelCoverage.Comprehensive : DefaultStartingIntelCoverage;
            return MakeIntel(coverage);
        }

        /// <summary>
        /// Derived classes should override this if they have a different type of AIntel  than <see cref="Intel" />.
        /// </summary>
        /// <param name="initialcoverage">The initial coverage.</param>
        /// <returns></returns>
        protected virtual AIntel MakeIntel(IntelCoverage initialcoverage) {
            return new Intel(initialcoverage);
        }

        /// <summary>
        /// Sets the intel coverage for the User player. Returns <c>true</c> if the <c>newCoverage</c>
        /// was successfully applied, and <c>false</c> if it was rejected due to the inability of
        /// the item to regress its IntelCoverage.
        /// </summary>
        /// <param name="newCoverage">The new coverage.</param>
        /// <returns></returns>
        public bool SetUserIntelCoverage(IntelCoverage newCoverage) {
            return SetIntelCoverage(_gameMgr.UserPlayer, newCoverage);
        }

        /// <summary>
        /// Sets the intel coverage for this player. Returns <c>true</c> if the <c>newCoverage</c>
        /// was successfully applied, and <c>false</c> if it was rejected due to the inability of
        /// the item to regress its IntelCoverage.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="newCoverage">The new coverage.</param>
        /// <returns></returns>
        public bool SetIntelCoverage(Player player, IntelCoverage newCoverage) {
            var playerIntel = GetIntel(player);
            if (playerIntel.IsCoverageChangeAllowed(newCoverage)) {
                playerIntel.CurrentCoverage = newCoverage;
                OnIntelCoverageChanged(player);
                return true;
            }
            //D.Log("{0} properly ignored changing {1}'s IntelCoverage from {2} to {3}.",
            //    FullName, player.LeaderName, playerIntel.CurrentCoverage.GetName(), newCoverage.GetName());
            return false;
        }

        public IntelCoverage GetUserIntelCoverage() { return GetIntelCoverage(_gameMgr.UserPlayer); }

        public IntelCoverage GetIntelCoverage(Player player) { return GetIntelCopy(player).CurrentCoverage; }

        /// <summary>
        /// Returns a copy of the intel instance for the User.
        /// </summary>
        /// <returns></returns>
        public AIntel GetUserIntelCopy() { return GetIntelCopy(_gameMgr.UserPlayer); }

        /// <summary>
        /// Returns a copy of the intel instance for this player.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <returns></returns>
        public AIntel GetIntelCopy(Player player) {
            AIntel intelToCopy = GetIntel(player);
            if (intelToCopy is Intel) {
                return new Intel(intelToCopy as Intel);
            }
            return new ImprovingIntel(intelToCopy as ImprovingIntel);
        }

        private AIntel GetIntel(Player player) {
            return _playerIntelLookup[player];
        }

        #region Event and Property Change Handlers

        private void OnIntelCoverageChanged(Player player) {
            if (intelCoverageChanged != null) {
                intelCoverageChanged(this, new IntelEventArgs(player));
            }
            if (player.IsUser && userIntelCoverageChanged != null) {
                userIntelCoverageChanged(this, new EventArgs());
            }
        }

        #endregion

        #region Nested Classes

        public class IntelEventArgs : EventArgs {

            public Player Player { get; private set; }

            public IntelEventArgs(Player player) {
                Player = player;
            }

        }

        #endregion

    }
}

