// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AIntelData2.cs
// COMMENT - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using CodeEnv.Master.GameContent;
    using UnityEngine;

    /// <summary>
    /// 
    /// </summary>
    public abstract class AIntelData2 : AItemData2 {

        public IntelCoverage HumanPlayerIntelCoverage {
            get { return HumanPlayerIntel.CurrentCoverage; }
            set { HumanPlayerIntel.CurrentCoverage = value; }
        }

        public AIntel HumanPlayerIntel { get { return GetPlayerIntel(_gameMgr.HumanPlayer); } }

        private IDictionary<Player, AIntel> _playerIntelLookup;
        protected IGameManager _gameMgr;

        public AIntelData2(string name)
            : base(name) {
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
        /// Derived classes should override this if they have a different type of AIntel and/or starting coverage than <see cref="Intel" /> and
        /// <see cref="IntelCoverage.None"/>.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <returns></returns>
        protected virtual AIntel InitializeIntelState(Player player) {
            AIntel beginningIntel = new Intel();
            beginningIntel.CurrentCoverage = IntelCoverage.None;
            return beginningIntel;
        }

        public void SetIntelCoverage(Player player, IntelCoverage coverage) { GetPlayerIntel(player).CurrentCoverage = coverage; }

        public IntelCoverage GetIntelCoverage(Player player) { return GetPlayerIntel(player).CurrentCoverage; }

        public AIntel GetPlayerIntel(Player player) { return _playerIntelLookup[player]; }

    }
}

