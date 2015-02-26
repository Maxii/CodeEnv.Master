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
    public abstract class AIntelItemData : AItemData {

        public event Action onHumanPlayerIntelCoverageChanged;

        public event Action<Player> onPlayerIntelCoverageChanged;

        private IDictionary<Player, AIntel> _playerIntelLookup;
        protected IGameManager _gameMgr;

        public AIntelItemData(Transform itemTransform, string name, Player owner)
            : base(itemTransform, name, owner) {
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
            var coverage = IsAllIntelCoverageComprehensive || Owner == player ? IntelCoverage.Comprehensive : IntelCoverage.None;
            return new Intel(coverage);
        }

        protected bool IsAllIntelCoverageComprehensive {
            get { return DebugSettings.Instance.AllIntelCoverageComprehensive; }
        }

        public override bool HasPlayerInvestigated(Player player) {
            return GetIntelCoverage(player) != IntelCoverage.None;
        }

        public bool TrySetHumanPlayerIntelCoverage(IntelCoverage newCoverage) {
            return TrySetIntelCoverage(_gameMgr.HumanPlayer, newCoverage);
        }

        public bool TrySetIntelCoverage(Player player, IntelCoverage newCoverage) {
            var playerIntel = GetPlayerIntel(player);
            if (playerIntel.IsCoverageChangeAllowed(newCoverage)) {
                playerIntel.CurrentCoverage = newCoverage;
                OnPlayerIntelCoverageChanged(player);
                return true;
            }
            //D.Log("{0} properly ignored changing {1}'s IntelCoverage from {2} to {3}.",
            //    FullName, player.LeaderName, playerIntel.CurrentCoverage.GetName(), newCoverage.GetName());
            return false;
        }

        public IntelCoverage GetHumanPlayerIntelCoverage() { return GetIntelCoverage(_gameMgr.HumanPlayer); }

        public IntelCoverage GetIntelCoverage(Player player) { return GetPlayerIntelCopy(player).CurrentCoverage; }

        /// <summary>
        /// Returns a copy of the intel instance for the HumanPlayer.
        /// </summary>
        /// <returns></returns>
        public AIntel GetHumanPlayerIntelCopy() { return GetPlayerIntelCopy(_gameMgr.HumanPlayer); }

        /// <summary>
        /// Returns a copy of the intel instance for this player.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <returns></returns>
        public AIntel GetPlayerIntelCopy(Player player) {
            AIntel intelToCopy = GetPlayerIntel(player);
            if (intelToCopy is Intel) {
                return new Intel(intelToCopy as Intel);
            }
            return new ImprovingIntel(intelToCopy as ImprovingIntel);
        }

        private AIntel GetPlayerIntel(Player player) {
            return _playerIntelLookup[player];
        }

        private void OnPlayerIntelCoverageChanged(Player player) {
            if (onPlayerIntelCoverageChanged != null) {
                onPlayerIntelCoverageChanged(player);
            }
            if (onHumanPlayerIntelCoverageChanged != null && player.IsHumanUser) {
                onHumanPlayerIntelCoverageChanged();
            }
        }

    }
}

