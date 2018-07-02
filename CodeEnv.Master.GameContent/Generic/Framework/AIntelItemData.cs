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
    using System.Linq;
    using CodeEnv.Master.Common;
    using UnityEngine;
    using UnityEngine.Profiling;

    /// <summary>
    /// Abstract class for Data associated with an AIntelItem.
    /// </summary>
    public abstract class AIntelItemData : AItemData {

        public event EventHandler<IntelCoverageChangedEventArgs> intelCoverageChanged;

        protected abstract IntelCoverage DefaultStartingIntelCoverage { get; }

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
        // IMPROVE Utilize player. Have DefaultStartingIntelCoverage vary by player?
        private AIntel InitializeIntelState(Player player) {
            // 2.6.17 Moved decision of whether to use Comprehensive to Item.FinalInitialize which uses SetIntelCoverage().
            // SetIntelCoverage is the single place which calls PlayerAIMgr.HandleItemIntelCoverageChanged() when IntelCoverage
            // changes. This is the mechanism that manages a player's knowledge of an item. If the item's IntelCoverage > None,
            // the player has knowledge of the item, if None it doesn't.
            // WARNING: Using a DefaultStartingIntelCoverage value here besides None can result in SetIntelCoverage NOT calling 
            // PlayerAIMgr.HandleItemIntelCoverageChanged() if in fact IntelCoverage is being set to the same value as that
            // set here. This could result in the player not having knowledge of an item it should know about.
            // Currently, Stars, Systems, UCenter and Sectors start with a DefaultStartingIntelCoverage other than None -> Basic. 
            // This is all Stars, Systems, UCenter and Sectors are provided to PlayerKnowledge when instantiated and therefore don't rely
            // on PlayerAIMgr.HandleItemIntelCoverageChanged() to populate a player's knowledge.
            return InitializeIntel(DefaultStartingIntelCoverage);
        }

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

        public override void FinalInitialize() {
            base.FinalInitialize();
            if (__debugCntls.IsAllIntelCoverageComprehensive) {
                foreach (var player in _gameMgr.AllPlayers) {
                    SetIntelCoverage(player, IntelCoverage.Comprehensive);
                }
            }
            else {
                AssessAssigningOwnerAndAlliesComprehensiveCoverage();
            }
        }

        #endregion

        /// <summary>
        /// Attempts to change the IntelCoverage of this Item to newCoverage. Returns <c>true</c> if the coverage
        /// was changed, false if not. In both cases, resultingCoverage will reflect the currentCoverage, changed or not.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="newCoverage">The new coverage.</param>
        /// <param name="resultingCoverage">The resulting coverage.</param>
        /// <returns></returns>
        public bool TryChangeIntelCoverage(Player player, IntelCoverage newCoverage, out IntelCoverage resultingCoverage) {
            Profiler.BeginSample("AIntelItemData.TryChangeIntelCoverage");
            var playerIntel = GetIntel(player);
            IntelCoverage currentCoverage = playerIntel.CurrentCoverage;
            bool isCoverageChgd = false;
            if (playerIntel.IsCoverageChangeAllowed(newCoverage)) {
                if (currentCoverage != newCoverage) {
                    D.Log(ShowDebugLog, "{0} is changing {1}'s IntelCoverage from {2} to {3}.",
                        DebugName, player.DebugName, currentCoverage.GetValueName(), newCoverage.GetValueName());
                    playerIntel.CurrentCoverage = newCoverage;
                    currentCoverage = newCoverage;    // added 6.27.18 to fix returned resultingCoverage value
                    __AssessWetherToRecordComprehensiveIntelCoverageAsAchievedFor(newCoverage, player);
                    HandleIntelCoverageChangedFor(player);
                    OnIntelCoverageChanged(player);
                    isCoverageChgd = true;
                }
            }
            else {
                IntelCoverage lowestAllowedCoverage = playerIntel.LowestAllowedCoverageValue;
                // 6.9.18 If here and playerIntel is non-regressible, then newCoverage must be < lowestAllowedCoverage (currentCoverage),
                // else if regressible, then newCoverage must be < lowestAllowedCoverage (Basic for facilities/bases).
                // Ship/Fleets using RegressibleIntel can't get here as all coverage changes for them are allowed since lowest is None.
                D.Assert(newCoverage < lowestAllowedCoverage);

                if (lowestAllowedCoverage < currentCoverage) {
                    D.Assert(playerIntel is RegressibleIntel);  // Non-Regressible's lowestAllowedCoverage always currentCoverage
                                                                // 6.9.18 newCoverage was below Regressible's lowestAllowedCoverage, so regress to lowest allowed value

                    //D.Log(ShowDebugLog, "{0} is changing {1}'s IntelCoverage from {2} to {3}, the lowest allowed value.",
                    //    DebugName, player.DebugName, currentCoverage.GetValueName(), lowestAllowedCoverage.GetValueName());
                    playerIntel.CurrentCoverage = lowestAllowedCoverage;
                    currentCoverage = lowestAllowedCoverage;    // added 6.27.18 to fix returned resultingCoverage value
                    __AssessWetherToRecordComprehensiveIntelCoverageAsAchievedFor(newCoverage, player);
                    HandleIntelCoverageChangedFor(player);
                    OnIntelCoverageChanged(player);
                    isCoverageChgd = true;
                }
            }
            Profiler.EndSample();
            resultingCoverage = currentCoverage;
            return isCoverageChgd;
        }

        /// <summary>
        /// Sets the Intel coverage for this player. 
        /// <remarks>Convenience method for clients who don't care whether the value was accepted or not.</remarks>
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="newCoverage">The new coverage.</param>
        public void SetIntelCoverage(Player player, IntelCoverage newCoverage) {
            IntelCoverage unusedResultingCoverage;
            //// TrySetIntelCoverage(player, newCoverage, out unusedResultingCoverage);
            TryChangeIntelCoverage(player, newCoverage, out unusedResultingCoverage);
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

        private void OnIntelCoverageChanged(Player player) {
            if (intelCoverageChanged != null) {
                intelCoverageChanged(this, new IntelCoverageChangedEventArgs(player));
            }
            else {
                D.Warn("{0} is not firing its intelCoverageChanged event as it has no subscribers.", DebugName);
            }
        }

        #endregion

        /// <summary>
        /// Hook for derived Data classes that allows them to handle a change in this item's intel coverage.
        /// <remarks>Typically this item's data would not have anything to do when the item's IntelCoverage
        /// changes since data, by definition, is where full knowledge about the item is kept, independent
        /// of info access restrictions. Reports and interfaces play the role of 'filtering' a player's
        /// access to this knowledge stored in data by using the item's InfoAccessController.</remarks>
        /// <remarks>Not currently used.</remarks>
        /// </summary>
        /// <param name="player">The player.</param>
        protected virtual void HandleIntelCoverageChangedFor(Player player) {
            D.Log(ShowDebugLog, "{0}.IntelCoverage changed for {1} to {2}.", DebugName, player, GetIntelCoverage(player).GetValueName());
            AssessAwarenessOfItemFor(player);
        }

        protected override void HandleOwnerChangesComplete() {
            base.HandleOwnerChangesComplete();
            AssessAssigningOwnerAndAlliesComprehensiveCoverage();
            CheckForDiscoveryByUnknownPlayers();
        }

        /// <summary>
        /// Has <c>player</c>'s AIMgr assess their awareness of this item.
        /// <remarks>The item may or may not be operational. If the item is not operational,
        /// the PlayerAIMgr doing the assessment will not raise any awareChgd events.</remarks>
        /// </summary>
        /// <param name="player">The player.</param>
        private void AssessAwarenessOfItemFor(Player player) {
            var playerAIMgr = _gameMgr.GetAIManagerFor(player);
            playerAIMgr.AssessAwarenessOf(Item as IOwnerItem_Ltd);
        }

        /// <summary>
        /// Assesses whether the new owner and allies, if any, should be assigned Comprehensive IntelCoverage of this Item.
        /// <remarks>6.27.18 Comprehensive coverage will be assigned to the owner and any allies unless the new owner
        /// is NoPlayer. This may or may not change their IntelCoverage of this item as it could already be Comprehensive.</remarks>
        /// </summary>
        /// <returns></returns>
        private void AssessAssigningOwnerAndAlliesComprehensiveCoverage() {
            if (__debugCntls.IsAllIntelCoverageComprehensive) {
                return;
            }
            if (Owner != TempGameValues.NoPlayer) {
                SetIntelCoverage(Owner, IntelCoverage.Comprehensive);

                IEnumerable<Player> allies;
                if (TryGetAllies(out allies)) {
                    allies.ForAll(ally => SetIntelCoverage(ally, IntelCoverage.Comprehensive));
                }
            }
        }

        /// <summary>
        /// Checks whether the new owner of this Item has been 'discovered' by any players the owner has not yet met.
        /// <remarks>This covers the scenario where an undiscovered player already has owner info access rights to 
        /// this item but gained those rights when this item was not owned by the new owner so didn't discover them 
        /// at that time. Game Logic: A player that has is or has gotten close enough to an item to currently have 
        /// owner access rights has done some scouting of the item and will have left a probe to tell them if some 
        /// unknown race has become the owner.</remarks>
        /// </summary>
        private void CheckForDiscoveryByUnknownPlayers() {
            if (__debugCntls.IsAllIntelCoverageComprehensive) {
                // No reason to check as all players will already have discovered each other when each Item 
                // changes IntelCoverage to Comprehensive for all players from FinalInitialize. This change 
                // causes a call to each player's PlayerAiMgr.AssessAwarenessOf(item) which will find any owner
                // it has not yet discovered.
                return;
            }
            if (Owner != TempGameValues.NoPlayer) {
                var unDiscoveredPlayers = _gameMgr.AllPlayers.Except(Owner).Except(Owner.OtherKnownPlayers);
                if (unDiscoveredPlayers.Any()) {
                    foreach (var player in unDiscoveredPlayers) {
                        var undiscoveredPlayerAiMgr = _gameMgr.GetAIManagerFor(player);
                        bool isUndiscoveredPlayerFound = undiscoveredPlayerAiMgr.CheckForUndiscoveredPlayer(Item as IOwnerItem_Ltd);
                        if (isUndiscoveredPlayerFound) {
                            D.Warn("FYI. {0}: {1} discovered new Owner {2} because it already had access to Item Owner.",
                                DebugName, player.DebugName, Owner.DebugName);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns <c>true</c> if allies of itemCurrentOwner are found, <c>false</c> otherwise.
        /// </summary>
        /// <param name="alliedPlayers">The allied players.</param>
        /// <returns></returns>
        public bool TryGetAllies(out IEnumerable<Player> alliedPlayers) {
            D.AssertNotEqual(TempGameValues.NoPlayer, Owner);
            alliedPlayers = Owner.GetOtherPlayersWithRelationship(DiplomaticRelationship.Alliance);
            return alliedPlayers.Any();
        }

        #region Debug

        private HashSet<Player> __playersPreviouslyAchievingComprehensiveRelationship = new HashSet<Player>();

        private void __AssessWetherToRecordComprehensiveIntelCoverageAsAchievedFor(IntelCoverage newCoverage, Player player) {
            if (newCoverage == IntelCoverage.Comprehensive) {
                __playersPreviouslyAchievingComprehensiveRelationship.Add(player);
                // Can't assert isAdded as RegressibleIntel could have achieved Comprehensive, then lost it and regained it
            }
        }

        public bool __IsPlayerEntitledToComprehensiveRelationship(Player player) {
            if (__debugCntls.IsAllIntelCoverageComprehensive) {
                return true;
            }
            if (__IsOwnerChgUnderway) {
                // 6.20.18 This is occurring regularly with no subsequent errors appearing
                D.Log("{0} has an owner change underway. Assess whether a problem based on subsequent warnings and errors.", DebugName);
            }
            if (__playersPreviouslyAchievingComprehensiveRelationship.Contains(player) && _playerIntelLookup[player] is NonRegressibleIntel) {
                return true;
            }
            return GetIntelCoverage(player) == IntelCoverage.Comprehensive;
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

