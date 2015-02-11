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

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Abstract class for Data associated with an AIntelItem.
    /// </summary>
    public abstract class AIntelItemData : AItemData {

        //public IntelCoverage HumanPlayerIntelCoverage {
        //    get { return HumanPlayerIntel.CurrentCoverage; }
        //    set { HumanPlayerIntel.CurrentCoverage = value; }
        //}

        //public AIntel HumanPlayerIntel { get { return GetPlayerIntel(_gameMgr.HumanPlayer); } }

        private IDictionary<Player, AIntel> _playerIntelLookup;
        protected IGameManager _gameMgr;

        public AIntelItemData(Transform itemTransform, string name)
            : base(itemTransform, name) {
            _gameMgr = References.GameManager;
            InitializePlayersIntel();
        }

        private void InitializePlayersIntel() {
            int playerCount = _gameMgr.AIPlayers.Count + 1;
            //D.Log("{0} initializing Players Intel settings. PlayerCount = {1}.", GetType().Name, playerCount);
            _playerIntelLookup = new Dictionary<Player, AIntel>(playerCount);
            var humanPlayer = _gameMgr.HumanPlayer;
            _playerIntelLookup.Add(humanPlayer, InitializeIntelState(humanPlayer));
            foreach (var aiPlayer in _gameMgr.AIPlayers) {
                _playerIntelLookup.Add(aiPlayer, InitializeIntelState(aiPlayer));
            }
        }

        /// <summary>
        /// Derived classes should override this if they have a different type of AIntel  than <see cref="Intel" /> and/or starting coverage than
        /// <see cref="IntelCoverage.Comprehensive"/> if the owner or <see cref="IntelCoverage.None"/> if not.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <returns></returns>
        protected virtual AIntel InitializeIntelState(Player player) {
            AIntel beginningIntel = new Intel();
            beginningIntel.CurrentCoverage = Owner == player ? IntelCoverage.Comprehensive : IntelCoverage.None;
            return beginningIntel;
        }

        public void SetHumanPlayerIntelCoverage(IntelCoverage newCoverage) {
            SetIntelCoverage(_gameMgr.HumanPlayer, newCoverage);
        }

        public void SetIntelCoverage(Player player, IntelCoverage newCoverage) {
            var playerIntel = GetPlayerIntel(player);
            if (playerIntel.CurrentCoverage != newCoverage) {
                playerIntel.CurrentCoverage = newCoverage;
                OnPlayerIntelCoverageChanged(player);
            }
        }

        public IntelCoverage GetHumanPlayerIntelCoverage() { return GetIntelCoverage(_gameMgr.HumanPlayer); }

        public IntelCoverage GetIntelCoverage(Player player) { return GetPlayerIntel(player).CurrentCoverage; }

        public AIntel GetHumanPlayerIntel() { return GetPlayerIntel(_gameMgr.HumanPlayer); }

        private AIntel GetPlayerIntel(Player player) { return _playerIntelLookup[player]; }

        protected virtual void OnPlayerIntelCoverageChanged(Player player) {

        }

    }
}

