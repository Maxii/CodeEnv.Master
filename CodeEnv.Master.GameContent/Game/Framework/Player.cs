// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Player.cs
// Instantiable base class for a otherPlayer.
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

    /// <summary>
    /// Instantiable base class for a otherPlayer.
    /// </summary>
    public class Player : APropertyChangeTracking {

        /// <summary>
        /// Fires when another Player's DiplomaticRelationship changes with this Player.
        /// </summary>
        public event EventHandler<RelationsChangedEventArgs> relationsChanged;

        public event EventHandler<NewPlayerMetEventArgs> newPlayerMet;

        private const string DebugNameFormat = "{0}[{1}]";

        private string _debugName;
        public string DebugName {
            get {
                _debugName = _debugName ?? DebugNameFormat.Inject(GetType().Name, LeaderName);
                return _debugName;
            }
        }

        private bool _isActive = true;
        public bool IsActive {  // accommodates an AIPlayer being eliminated in the game
            get { return _isActive; }
            set { SetProperty<bool>(ref _isActive, value, "IsActive"); }
        }

        /// <summary>
        /// Indicates whether this Player is the human user playing the game.
        /// </summary>
        public bool IsUser { get; private set; }

        public DiplomaticRelationship UserRelations { get { return GetCurrentRelations(_gameMgr.UserPlayer); } }

        public IQ IQ { get; private set; }

        public TeamID Team { get; private set; }

        public GameColor Color { get; private set; }

        public string LeaderName { get { return IsUser ? PlayerPrefsManager.Instance.Username : _leaderStat.Name; } }

        public AtlasID LeaderImageAtlasID { get { return _leaderStat.ImageAtlasID; } }

        public string LeaderImageFilename { get { return _leaderStat.ImageFilename; } }

        public Species Species { get { return _speciesStat.Species; } }

        public string SpeciesName { get { return Species.GetValueName(); } }

        public string SpeciesName_Plural { get { return _speciesStat.Name_Plural; } }

        public string SpeciesDescription { get { return _speciesStat.Description; } }

        public AtlasID SpeciesImageAtlasID { get { return _speciesStat.ImageAtlasID; } }

        public string SpeciesImageFilename { get { return _speciesStat.ImageFilename; } }

        public string HomeSystemName { get; private set; }

        [Obsolete("Use only during creation or refit")]
        public float SensorRangeMultiplier { get { return _speciesStat.SensorRangeMultiplier; } }

        [Obsolete("Use only during creation or refit")]
        public float WeaponRangeMultiplier { get { return _speciesStat.WeaponRangeMultiplier; } }

        [Obsolete("Use only during creation or refit")]
        public float CmRangeMultiplier { get { return _speciesStat.CmRangeMultiplier; } }

        public float WeaponReloadPeriodMultiplier { get { return _speciesStat.WeaponReloadPeriodMultiplier; } }

        public float CmReloadPeriodMultiplier { get { return _speciesStat.CmReloadPeriodMultiplier; } }

        public float BuyoutCostMultiplier { get { return _speciesStat.BuyoutCostMultiplier; } }

        public IEnumerable<Player> OtherKnownPlayers { get { return _currentRelationship.Keys.Where(p => IsKnown(p)).Except(this); } }

        private IDictionary<Player, DiplomaticRelationship> _initialRelationship;
        private IDictionary<Player, DiplomaticRelationship> _priorRelationship;
        private IDictionary<Player, DiplomaticRelationship> _currentRelationship;
        private LeaderStat _leaderStat;
        private SpeciesStat _speciesStat;
        private IGameManager _gameMgr;

        /// <summary>
        /// Initializes a new instance of the <see cref="Player" /> class.
        /// </summary>
        /// <param name="speciesStat">The species stat.</param>
        /// <param name="leaderStat">The leader stat.</param>
        /// <param name="iq">The IQ.</param>
        /// <param name="team">The team.</param>
        /// <param name="color">The color.</param>
        /// <param name="isUser">if set to <c>true</c> [is user].</param>
        public Player(SpeciesStat speciesStat, LeaderStat leaderStat, IQ iq, TeamID team, GameColor color, bool isUser = false) {
            _speciesStat = speciesStat;
            _leaderStat = leaderStat;
            IQ = iq;
            Team = team;
            Color = color;
            IsUser = isUser;

            InitializeValuesAndReferences();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Player"/> class as an AIPlayer for debug purposes.
        /// </summary>
        /// <param name="species">The species.</param>
        /// <param name="team">The team.</param>
        /// <param name="color">The color.</param>
        public Player(Species species, TeamID team, GameColor color) {
            _speciesStat = SpeciesFactory.Instance.MakeInstance(species);
            _leaderStat = LeaderFactory.Instance.MakeInstance(species);
            IQ = IQ.Normal;
            Team = team;
            Color = color;
            IsUser = false;
            InitializeValuesAndReferences();
        }

        private void InitializeValuesAndReferences() {
            _gameMgr = GameReferences.GameManager;
            int maxPlayers = TempGameValues.MaxPlayers;
            // Note: Each Player is instantiated by NewGameMenuLaunchButton before GameSettings is created and sent to GameManager
            _initialRelationship = new Dictionary<Player, DiplomaticRelationship>(maxPlayers);
            _priorRelationship = new Dictionary<Player, DiplomaticRelationship>(maxPlayers);
            _currentRelationship = new Dictionary<Player, DiplomaticRelationship>(maxPlayers);
            _initialRelationship[this] = DiplomaticRelationship.Self;
            _priorRelationship[this] = DiplomaticRelationship.Self;
            _currentRelationship[this] = DiplomaticRelationship.Self;
        }

        /// <summary>
        /// Sets the initial relationship between this player and unmetPlayer.
        /// <remarks>This method takes care of setting both players initial relationship.</remarks>
        /// </summary>
        /// <param name="unmetPlayer">The unmet player.</param>
        /// <param name="initialRelationship">The initial relationship.</param>
        public virtual void SetInitialRelationship(Player unmetPlayer, DiplomaticRelationship initialRelationship = DiplomaticRelationship.Neutral) {
            if (_initialRelationship.ContainsKey(unmetPlayer)) {
                D.Error("{0} already has initial relationship {1} with {2}.", DebugName, _initialRelationship[unmetPlayer].GetValueName(), unmetPlayer);
            }
            if (_priorRelationship.ContainsKey(unmetPlayer)) {
                D.Error("{0} already has prior relationship {1} with {2}.", DebugName, _priorRelationship[unmetPlayer].GetValueName(), unmetPlayer);
            }
            if (_currentRelationship.ContainsKey(unmetPlayer)) {
                D.Error("{0} already has current relationship {1} with {2}.", DebugName, _currentRelationship[unmetPlayer].GetValueName(), unmetPlayer);
            }
            _initialRelationship.Add(unmetPlayer, initialRelationship);
            _priorRelationship.Add(unmetPlayer, DiplomaticRelationship.None);
            _currentRelationship.Add(unmetPlayer, DiplomaticRelationship.None);
            unmetPlayer.SetInitialRelationship_Internal(this, initialRelationship);
        }

        internal virtual void SetInitialRelationship_Internal(Player unmetPlayer, DiplomaticRelationship initialRelationship) {
            if (_initialRelationship.ContainsKey(unmetPlayer)) {
                D.Error("{0} already has initial relationship {1} with {2}.", DebugName, _initialRelationship[unmetPlayer].GetValueName(), unmetPlayer);
            }
            if (_priorRelationship.ContainsKey(unmetPlayer)) {
                D.Error("{0} already has prior relationship {1} with {2}.", DebugName, _priorRelationship[unmetPlayer].GetValueName(), unmetPlayer);
            }
            if (_currentRelationship.ContainsKey(unmetPlayer)) {
                D.Error("{0} already has current relationship {1} with {2}.", DebugName, _currentRelationship[unmetPlayer].GetValueName(), unmetPlayer);
            }
            _initialRelationship.Add(unmetPlayer, initialRelationship);
            _priorRelationship.Add(unmetPlayer, DiplomaticRelationship.None);
            _currentRelationship.Add(unmetPlayer, DiplomaticRelationship.None);
        }

        /// <summary>
        /// Adds the newly met player to this Player's known opponents,
        /// setting their DiplomaticRelationship to the value stored for such an occasion, then synchronizes
        /// the newlyMetPlayer's DiploRelations state with this one, finally communicating
        /// the change after both states are synchronized.
        /// <remarks>Done this way to allow both newlyMetPlayer's DiploRelationship state to be
        /// synchronized BEFORE either communicates the change.</remarks>
        /// </summary>
        /// <param name="newlyMetPlayer">The newly met player.</param>
        public virtual void HandleMetNewPlayer(Player newlyMetPlayer) {
            D.AssertNotEqual(newlyMetPlayer, this, "Newly Met Player not allowed to be self.");
            D.Assert(!IsKnown(newlyMetPlayer));
            _currentRelationship[newlyMetPlayer] = _initialRelationship[newlyMetPlayer];
            D.LogBold("{0} discovered new {1}. Initial Relationship: {2}.", DebugName, newlyMetPlayer, _currentRelationship[newlyMetPlayer].GetValueName());
            newlyMetPlayer.HandleMetNewPlayer_Internal(this);
            OnPlayerMet(newlyMetPlayer);
            CommunicateChangedRelationsWith(newlyMetPlayer);
        }

        /// <summary>
        /// Adds the newly met player to this Player's known opponents,
        /// setting their DiplomaticRelationship to the value stored for such an occasion,
        /// finally communicating the change after both states are synchronized.
        /// <remarks>Done this way to allow both newlyMetPlayer's DiploRelationship state to be
        /// synchronized BEFORE either communicates the change.</remarks>
        /// </summary>
        /// <param name="newlyMetPlayer">The newly met player.</param>
        internal virtual void HandleMetNewPlayer_Internal(Player newlyMetPlayer) {
            D.AssertNotEqual(newlyMetPlayer, this, "Newly Met Player not allowed to be self.");
            D.Assert(!IsKnown(newlyMetPlayer));
            _currentRelationship[newlyMetPlayer] = _initialRelationship[newlyMetPlayer];
            //D.Log("{0} was discovered by {1}. Initial Relationship: {2}.", DebugName, newlyMetPlayer, _currentRelationship[newlyMetPlayer].GetValueName());
            OnPlayerMet(newlyMetPlayer);
            CommunicateChangedRelationsWith(newlyMetPlayer);
        }

        public virtual bool IsKnown(Player player) {
            return _currentRelationship[player] != DiplomaticRelationship.None;
        }

        public virtual DiplomaticRelationship GetCurrentRelations(Player player) {
            if (player == TempGameValues.NoPlayer) {
                return DiplomaticRelationship.None;
            }
            //D.Log("{0}.GetCurrentRelations({1}) called. Keys = {2}.", DebugName, player, _currentRelationship.Keys.Concatenate());
            return _currentRelationship[player];
        }

        public virtual DiplomaticRelationship GetPriorRelations(Player player) {
            if (player == TempGameValues.NoPlayer) {
                return DiplomaticRelationship.None;
            }
            return _priorRelationship[player];
        }

        /// <summary>
        /// Sets the DiplomaticRelationship between this player and <c>otherPlayer</c> who have already met.
        /// Then synchronizes <c>otherPlayer</c>'s DiploRelations state with this player, finally 
        /// communicating the change after both states are synchronized.
        /// <remarks>Done this way to allow both player's DiploRelationship state to be
        /// synchronized BEFORE either communicates the change.</remarks>
        /// </summary>
        /// <param name="otherPlayer">The otherPlayer.</param>
        /// <param name="newRelationship">The relationship.</param>
        public virtual void SetRelationsWith(Player otherPlayer, DiplomaticRelationship newRelationship) {
            D.AssertNotEqual(otherPlayer, TempGameValues.NoPlayer);
            D.AssertNotEqual(otherPlayer, this, "OtherPlayer not allowed to be self.");
            D.AssertNotEqual(newRelationship, DiplomaticRelationship.None);
            D.AssertNotEqual(newRelationship, DiplomaticRelationship.Self);
            if (!IsKnown(otherPlayer)) {
                D.Error("{0}: {1} not yet met.", DebugName, otherPlayer);
            }
            DiplomaticRelationship existingRelationship = _currentRelationship[otherPlayer];
            D.AssertNotDefault((int)existingRelationship);
            D.AssertNotEqual(DiplomaticRelationship.Self, newRelationship);
            if (existingRelationship == newRelationship) {
                D.Warn("{0} is attempting to set Relations to {1}, a value it already has.", DebugName, newRelationship.GetValueName());
                return;
            }

            _priorRelationship[otherPlayer] = existingRelationship;
            _currentRelationship[otherPlayer] = newRelationship;
            otherPlayer.SetRelationsWith_Internal(this, newRelationship);
            D.LogBold("{0}'s relationship with {1} is changing from {2} to {3} in Frame {4}.", DebugName, otherPlayer.DebugName,
                GetPriorRelations(otherPlayer).GetValueName(), GetCurrentRelations(otherPlayer).GetValueName(), Time.frameCount);
            CommunicateChangedRelationsWith(otherPlayer);
        }

        /// <summary>
        /// Sets the DiplomaticRelationship between this player and <c>otherPlayer</c> who have already met.
        /// Then synchronizes <c>otherPlayer</c>'s DiploRelations state with this player, finally communicating
        /// the change after both states are synchronized. Internal version intended for Player to Player coordination. 
        /// <remarks>Done this way to allow both player's DiploRelationship state to be
        /// synchronized BEFORE either communicates the change.</remarks>
        /// </summary>
        /// <param name="otherPlayer">The otherPlayer.</param>
        /// <param name="newRelationship">The relationship.</param>
        internal virtual void SetRelationsWith_Internal(Player otherPlayer, DiplomaticRelationship newRelationship) {
            D.AssertNotEqual(otherPlayer, TempGameValues.NoPlayer);
            D.AssertNotEqual(otherPlayer, this, "OtherPlayer not allowed to be self.");
            D.AssertNotEqual(newRelationship, DiplomaticRelationship.None);
            D.AssertNotEqual(newRelationship, DiplomaticRelationship.Self);
            if (!IsKnown(otherPlayer)) {
                D.Error("{0}: {1} not yet met.", DebugName, otherPlayer);
            }
            DiplomaticRelationship existingRelationship = _currentRelationship[otherPlayer];
            D.Assert(existingRelationship != DiplomaticRelationship.None && newRelationship != DiplomaticRelationship.Self);
            if (existingRelationship == newRelationship) {
                D.Warn("{0} is attempting to set Relations to {1}, a value it already has.", DebugName, newRelationship.GetValueName());
                return;
            }

            _priorRelationship[otherPlayer] = existingRelationship;
            _currentRelationship[otherPlayer] = newRelationship;
            CommunicateChangedRelationsWith(otherPlayer);
        }

        public virtual IEnumerable<Player> GetOtherPlayersWithRelationship(DiplomaticRelationship relationship) {
            D.AssertNotEqual(DiplomaticRelationship.Self, relationship);
            IList<Player> playersWithRelations = new List<Player>();
            foreach (Player knownPlayer in OtherKnownPlayers) {
                if (IsRelationshipWith(knownPlayer, relationship)) {
                    playersWithRelations.Add(knownPlayer);
                }
            }
            return playersWithRelations;
        }

        private void CommunicateChangedRelationsWith(Player player) {
            PlayerAIManager ownerAIMgr = _gameMgr.GetAIManagerFor(this);
            ownerAIMgr.HandleOwnerRelationsChangedWith(player);
            OnRelationsChanged(player);
        }

        #region IsRelationshipWith

        // 12.6.16 Removed params to avoid heap memory allocations

        public bool IsRelationshipWithUser(DiplomaticRelationship relationship) {
            D.Assert(!IsUser);
            return IsRelationshipWith(_gameMgr.UserPlayer, relationship);
        }

        public bool IsRelationshipWith(Player player, DiplomaticRelationship relationship) {
            return GetCurrentRelations(player) == relationship;
        }

        public bool IsRelationshipWith(Player player, DiplomaticRelationship relationship1, DiplomaticRelationship relationship2) {
            var currentRelations = GetCurrentRelations(player);
            return currentRelations == relationship1 || currentRelations == relationship2;
        }

        public bool IsRelationshipWith(Player player, DiplomaticRelationship relationship1, DiplomaticRelationship relationship2, DiplomaticRelationship relationship3) {
            var currentRelations = GetCurrentRelations(player);
            return currentRelations == relationship1 || currentRelations == relationship2 || currentRelations == relationship3;
        }


        public bool IsPriorRelationshipWith(Player player, DiplomaticRelationship relationship) {
            return GetPriorRelations(player) == relationship;
        }

        public bool IsPriorRelationshipWith(Player player, DiplomaticRelationship relationship1, DiplomaticRelationship relationship2) {
            var priorRelations = GetPriorRelations(player);
            return priorRelations == relationship1 || priorRelations == relationship2;
        }

        public bool IsPriorRelationshipWith(Player player, DiplomaticRelationship relationship1, DiplomaticRelationship relationship2, DiplomaticRelationship relationship3) {
            var priorRelations = GetPriorRelations(player);
            return priorRelations == relationship1 || priorRelations == relationship2 || priorRelations == relationship3;
        }

        #endregion

        public bool IsEnemyOf(Player player) {
            D.Assert(DiplomaticRelationship.War.IsEnemy());
            D.Assert(DiplomaticRelationship.ColdWar.IsEnemy());
            return IsRelationshipWith(player, DiplomaticRelationship.War, DiplomaticRelationship.ColdWar);
        }

        public bool IsPreviouslyEnemyOf(Player player) {
            D.Assert(DiplomaticRelationship.War.IsEnemy());
            D.Assert(DiplomaticRelationship.ColdWar.IsEnemy());
            return IsPriorRelationshipWith(player, DiplomaticRelationship.War, DiplomaticRelationship.ColdWar);
        }

        public bool IsAtWarWith(Player player) {
            return IsRelationshipWith(player, DiplomaticRelationship.War);
        }

        public bool IsPreviouslyAtWarWith(Player player) {
            return IsPriorRelationshipWith(player, DiplomaticRelationship.War);
        }

        public bool IsFriendlyWith(Player player) {
            D.Assert(DiplomaticRelationship.Self.IsFriendly());
            D.Assert(DiplomaticRelationship.Alliance.IsFriendly());
            D.Assert(DiplomaticRelationship.Friendly.IsFriendly());
            return IsRelationshipWith(player, DiplomaticRelationship.Alliance, DiplomaticRelationship.Friendly, DiplomaticRelationship.Self);
        }

        public bool IsPreviouslyFriendlyWith(Player player) {
            D.Assert(DiplomaticRelationship.Self.IsFriendly());
            D.Assert(DiplomaticRelationship.Alliance.IsFriendly());
            D.Assert(DiplomaticRelationship.Friendly.IsFriendly());
            return IsPriorRelationshipWith(player, DiplomaticRelationship.Alliance, DiplomaticRelationship.Friendly, DiplomaticRelationship.Self);
        }

        #region Event and Property Change Handlers

        private void OnPlayerMet(Player newlyMetPlayer) {
            if (newPlayerMet != null) {
                newPlayerMet(this, new NewPlayerMetEventArgs(newlyMetPlayer));
            }
        }

        private void OnRelationsChanged(Player otherPlayer) {
            if (GetCurrentRelations(otherPlayer) != otherPlayer.GetCurrentRelations(this)) {
                D.Error("{0} must be synchronized with {1} before RelationsChanged event fires.",
                    GetCurrentRelations(otherPlayer).GetValueName(), otherPlayer.GetCurrentRelations(this).GetValueName());
            }
            if (relationsChanged != null) {
                relationsChanged(this, new RelationsChangedEventArgs(otherPlayer));
            }
        }

        #endregion

        public override string ToString() {
            return DebugName;
        }

        #region Nested Classes

        public class NewPlayerMetEventArgs : EventArgs {

            public Player NewlyMetPlayer { get; private set; }

            public NewPlayerMetEventArgs(Player newlyMetPlayer) {
                NewlyMetPlayer = newlyMetPlayer;
            }
        }



        #endregion

        #region Debug

        public DiplomaticRelationship __InitialUserRelationship { get { return _initialRelationship[_gameMgr.UserPlayer]; } }

        #endregion

        #region Archive

        // Use of params creates heap allocations

        //public bool IsRelationshipWith(Player player, params DiplomaticRelationship[] relations) {
        //    return GetCurrentRelations(player).EqualsAnyOf(relations);
        //}

        //public bool IsPriorRelationshipWith(Player player, params DiplomaticRelationship[] relations) {
        //    return GetPriorRelations(player).EqualsAnyOf(relations);
        //}

        //public virtual IEnumerable<Player> GetOtherPlayersWithRelationship(params DiplomaticRelationship[] relations) {
        //    Utility.ValidateNotNullOrEmpty(relations);
        //    D.Assert(!relations.Contains(DiplomaticRelationship.Self));
        //    IList<Player> playersWithRelations = new List<Player>();
        //    foreach (Player knownPlayer in OtherKnownPlayers) {
        //        if (IsRelationshipWith(knownPlayer, relations)) {
        //            playersWithRelations.Add(knownPlayer);
        //        }
        //    }
        //    return playersWithRelations;
        //}

        #endregion
    }
}

