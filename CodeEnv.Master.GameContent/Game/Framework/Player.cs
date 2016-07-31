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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Instantiable base class for a otherPlayer.
    /// </summary>
    public class Player : APropertyChangeTracking {

        /// <summary>
        /// Fires when another Player's DiplomaticRelationship changes with this Player.
        /// </summary>
        public event EventHandler<RelationsChangedEventArgs> relationsChanged;

        private const string NameFormat = "{0}[{1}]";

        public string Name { get { return NameFormat.Inject(GetType().Name, LeaderName); } }

        private bool _isActive = true;
        public bool IsActive {  // accommodates an AIPlayer being eliminated in the game
            get { return _isActive; }
            set { SetProperty<bool>(ref _isActive, value, "IsActive"); }
        }

        /// <summary>
        /// Indicates whether this Player is the human user playing the game.
        /// </summary>
        public bool IsUser { get; private set; }

        public IQ IQ { get; private set; }

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


        public float SensorRangeMultiplier { get { return _speciesStat.SensorRangeMultiplier; } }

        public float WeaponRangeMultiplier { get { return _speciesStat.WeaponRangeMultiplier; } }

        public float CountermeasureRangeMultiplier { get { return _speciesStat.ActiveCountermeasureRangeMultiplier; } }

        public float WeaponReloadPeriodMultiplier { get { return _speciesStat.WeaponReloadPeriodMultiplier; } }

        public float CountermeasureReloadPeriodMultiplier { get { return _speciesStat.CountermeasureReloadPeriodMultiplier; } }

        public IEnumerable<Player> OtherKnownPlayers { get { return _currentRelationship.Keys.Except(this); } }

        private IDictionary<Player, DiplomaticRelationship> _priorRelationship = new Dictionary<Player, DiplomaticRelationship>();
        private IDictionary<Player, DiplomaticRelationship> _currentRelationship = new Dictionary<Player, DiplomaticRelationship>();
        private LeaderStat _leaderStat;
        private SpeciesStat _speciesStat;

        /// <summary>
        /// Initializes a new instance of the <see cref="Player"/> class.
        /// </summary>
        /// <param name="speciesStat">The species stat.</param>
        /// <param name="leaderStat">The leader stat.</param>
        /// <param name="iq">The IQ.</param>
        /// <param name="color">The color.</param>
        /// <param name="isUser">if set to <c>true</c> [is user].</param>
        public Player(SpeciesStat speciesStat, LeaderStat leaderStat, IQ iq, GameColor color, bool isUser = false) {
            _speciesStat = speciesStat;
            _leaderStat = leaderStat;
            IQ = iq;
            Color = color;
            IsUser = isUser;
            _priorRelationship[this] = DiplomaticRelationship.Self;
            _currentRelationship[this] = DiplomaticRelationship.Self;    // assigning relations this way allows NoPlayer to make SetRelations illegal
        }

        public bool IsKnown(Player otherPlayer) {
            D.Assert(otherPlayer != this);
            return _currentRelationship.ContainsKey(otherPlayer);
        }

        /// <summary>
        /// Adds the newly discovered <c>otherPlayer</c> to this Player's known opponents, 
        /// setting the DiplomaticRelationship to <c>initialRelations</c>, then synchronizes
        /// <c>otherPlayer</c>'s DiploRelations state with this one, finally raising a
        /// <c>relationsChanged</c> event after both states are synchronized.
        /// <remarks>Done this way to allow both otherPlayer's DiploRelationship state to be
        /// synchronized BEFORE either raises a relationsChanged event.</remarks>
        /// </summary>
        /// <param name="otherPlayer">The other otherPlayer.</param>
        /// <param name="initialRelations">The initial relations. Default is Neutral.</param>
        public void AddNewlyDiscovered(Player otherPlayer, DiplomaticRelationship initialRelations = DiplomaticRelationship.Neutral) {
            D.Assert(initialRelations != DiplomaticRelationship.None);
            D.Assert(!IsKnown(otherPlayer));    // priorRelationship is by definition None
            _currentRelationship.Add(otherPlayer, initialRelations);
            otherPlayer.AddNewlyDiscovered_Internal(this, initialRelations);
            OnRelationsChanged(otherPlayer);
        }

        /// <summary>
        /// Adds the newly discovered <c>otherPlayer</c> to this Player's known opponents, 
        /// setting the DiplomaticRelationship to <c>initialRelations</c> and raises a
        /// <c>relationsChanged</c> event. Internal version intended for Player to Player coordination. 
        /// <remarks>Done this way to allow both otherPlayer's DiploRelationship state to be
        /// synchronized BEFORE either raises a relationsChanged event.</remarks>
        /// </summary>
        /// <param name="otherPlayer">The other otherPlayer.</param>
        /// <param name="initialRelations">The initial relations.</param>
        internal void AddNewlyDiscovered_Internal(Player otherPlayer, DiplomaticRelationship initialRelations) {
            D.Assert(initialRelations != DiplomaticRelationship.None);
            D.Assert(!IsKnown(otherPlayer));    // priorRelationship is by definition None
            _currentRelationship.Add(otherPlayer, initialRelations);
            OnRelationsChanged(otherPlayer);
        }

        public DiplomaticRelationship GetCurrentRelations(Player otherPlayer) {
            if (!_currentRelationship.ContainsKey(otherPlayer)) {
                return DiplomaticRelationship.None;
            }
            return _currentRelationship[otherPlayer];
        }

        public DiplomaticRelationship GetPriorRelations(Player otherPlayer) {
            if (!_priorRelationship.ContainsKey(otherPlayer)) {
                return DiplomaticRelationship.None;
            }
            return _priorRelationship[otherPlayer];
        }

        /// <summary>
        /// Sets the DiplomaticRelationship between this player and <c>otherPlayer</c> who have already met.
        /// Then synchronizes <c>otherPlayer</c>'s DiploRelations state with this player, finally raising a
        /// <c>relationsChanged</c> event after both states are synchronized.
        /// <remarks>Done this way to allow both player's DiploRelationship state to be
        /// synchronized BEFORE either raises a relationsChanged event.</remarks>
        /// </summary>
        /// <param name="otherPlayer">The otherPlayer.</param>
        /// <param name="newRelationship">The relationship.</param>
        public virtual void SetRelationsWith(Player otherPlayer, DiplomaticRelationship newRelationship) {
            D.Assert(otherPlayer != TempGameValues.NoPlayer);
            D.Assert(otherPlayer != this);
            D.Assert(newRelationship != DiplomaticRelationship.None);
            DiplomaticRelationship existingRelationship;
            bool isPlayerMet = _currentRelationship.TryGetValue(otherPlayer, out existingRelationship);
            D.Assert(isPlayerMet, "{0}: {1} not yet met.", Name, otherPlayer.Name);
            D.Assert(existingRelationship != DiplomaticRelationship.None);
            if (existingRelationship == newRelationship) {
                D.Warn("{0} is attempting to set Relations to {1}, a value it already has.", Name, newRelationship.GetValueName());
                return;
            }

            _priorRelationship[otherPlayer] = existingRelationship;
            _currentRelationship[otherPlayer] = newRelationship;
            otherPlayer.SetRelationsWith_Internal(this, newRelationship);
            OnRelationsChanged(otherPlayer);
        }

        /// <summary>
        /// Sets the DiplomaticRelationship between this player and <c>otherPlayer</c> who have already met.
        /// Then synchronizes <c>otherPlayer</c>'s DiploRelations state with this player, finally raising a
        /// <c>relationsChanged</c> event after both states are synchronized. Internal version intended for Player to Player coordination. 
        /// <remarks>Done this way to allow both player's DiploRelationship state to be
        /// synchronized BEFORE either raises a relationsChanged event.</remarks>
        /// </summary>
        /// <param name="otherPlayer">The otherPlayer.</param>
        /// <param name="newRelationship">The relationship.</param>
        internal virtual void SetRelationsWith_Internal(Player otherPlayer, DiplomaticRelationship newRelationship) {
            D.Assert(otherPlayer != TempGameValues.NoPlayer);
            D.Assert(otherPlayer != this);
            D.Assert(newRelationship != DiplomaticRelationship.None);
            DiplomaticRelationship existingRelationship;
            bool isPlayerMet = _currentRelationship.TryGetValue(otherPlayer, out existingRelationship);
            D.Assert(isPlayerMet, "{0}: {1} not yet met.", Name, otherPlayer.Name);
            D.Assert(existingRelationship != DiplomaticRelationship.None);
            if (existingRelationship == newRelationship) {
                D.Warn("{0} is attempting to set Relations to {1}, a value it already has.", Name, newRelationship.GetValueName());
                return;
            }

            _priorRelationship[otherPlayer] = existingRelationship;
            _currentRelationship[otherPlayer] = newRelationship;
            OnRelationsChanged(otherPlayer);
        }

        public bool IsRelationship(Player otherPlayer, params DiplomaticRelationship[] relations) {
            return GetCurrentRelations(otherPlayer).EqualsAnyOf(relations);
        }

        public bool IsPriorRelationship(Player otherPlayer, params DiplomaticRelationship[] relations) {
            return GetPriorRelations(otherPlayer).EqualsAnyOf(relations);
        }

        public bool IsEnemyOf(Player otherPlayer) {
            D.Assert(DiplomaticRelationship.War.IsEnemy());
            D.Assert(DiplomaticRelationship.ColdWar.IsEnemy());
            return IsRelationship(otherPlayer, DiplomaticRelationship.War, DiplomaticRelationship.ColdWar);
        }

        public bool IsPreviouslyEnemyOf(Player otherPlayer) {
            D.Assert(DiplomaticRelationship.War.IsEnemy());
            D.Assert(DiplomaticRelationship.ColdWar.IsEnemy());
            return IsPriorRelationship(otherPlayer, DiplomaticRelationship.War, DiplomaticRelationship.ColdWar);
        }

        public bool IsAtWarWith(Player otherPlayer) {
            return IsRelationship(otherPlayer, DiplomaticRelationship.War);
        }

        public bool IsPreviouslyAtWarWith(Player otherPlayer) {
            return IsPriorRelationship(otherPlayer, DiplomaticRelationship.War);
        }

        public bool IsFriendlyWith(Player otherPlayer) {
            D.Assert(DiplomaticRelationship.Self.IsFriendly());
            D.Assert(DiplomaticRelationship.Alliance.IsFriendly());
            D.Assert(DiplomaticRelationship.Friendly.IsFriendly());
            return IsRelationship(otherPlayer, DiplomaticRelationship.Self, DiplomaticRelationship.Alliance, DiplomaticRelationship.Friendly);
        }

        public bool IsPreviouslyFriendlyWith(Player otherPlayer) {
            D.Assert(DiplomaticRelationship.Self.IsFriendly());
            D.Assert(DiplomaticRelationship.Alliance.IsFriendly());
            D.Assert(DiplomaticRelationship.Friendly.IsFriendly());
            return IsPriorRelationship(otherPlayer, DiplomaticRelationship.Self, DiplomaticRelationship.Alliance, DiplomaticRelationship.Friendly);
        }

        #region Event and Property Change Handlers

        private void OnRelationsChanged(Player otherPlayer) {
            D.Assert(GetCurrentRelations(otherPlayer) == otherPlayer.GetCurrentRelations(this), "{0} must be synchronized with {1} before RelationsChanged event fires.",
                GetCurrentRelations(otherPlayer).GetValueName(), otherPlayer.GetCurrentRelations(this).GetValueName());

            if (relationsChanged != null) {
                relationsChanged(this, new RelationsChangedEventArgs(otherPlayer));
            }
        }

        #endregion

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

