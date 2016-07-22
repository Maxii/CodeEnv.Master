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
    public abstract class AIntelItemData : ADiscernibleItemData {

        public event EventHandler<IntelCoverageChangedEventArgs> intelCoverageChanged;

        protected virtual IntelCoverage DefaultStartingIntelCoverage { get { return IntelCoverage.None; } }

        protected IGameManager _gameMgr;

        private IDictionary<Player, AIntel> _playerIntelLookup;

        /// <summary>
        /// Initializes a new instance of the <see cref="AIntelItemData" /> class.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="owner">The owner.</param>
        /// <param name="cameraStat">The camera stat.</param>
        public AIntelItemData(IIntelItem item, Player owner, ACameraItemStat cameraStat)
            : base(item, owner, cameraStat) {
            _gameMgr = References.GameManager;
        }

        public override void Initialize() {
            InitializePlayersIntel();   // must occur before InfoAccessCntlr gets initialized in base
            base.Initialize();
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
            var intel = new Intel();
            intel.InitializeCoverage(initialcoverage);
            return intel;
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
            //    FullName, player.LeaderName, playerIntel.CurrentCoverage.GetValueName(), newCoverage.GetValueName());
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
                intelCoverageChanged(this, new IntelCoverageChangedEventArgs(player));
            }
        }

        #endregion

        #region Nested Classes

        public class IntelCoverageChangedEventArgs : EventArgs {

            /// <summary>
            /// The player whos IntelCoverage of this item changed .
            /// </summary>
            public Player Player { get; private set; }

            public IntelCoverageChangedEventArgs(Player player) {
                Player = player;
            }

        }

        #endregion

    }
}

