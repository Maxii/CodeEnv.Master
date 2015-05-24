// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Player.cs
// Instantiable base class for a player.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Instantiable base class for a player.
    /// TODO Need a PlayerFactory to make players so there is only one instance of each player allowing == comparisons
    /// </summary>
    public class Player : APropertyChangeTracking {

        private bool _isActive = true;
        public bool IsActive {  // accomodates an AIPlayer being eliminated in the game
            get { return _isActive; }
            set { SetProperty<bool>(ref _isActive, value, "IsActive"); }
        }

        /// <summary>
        /// Indicates whether this Player is the human user playing the game.
        /// </summary>
        public bool IsUser { get; private set; }

        public IQ IQ { get; private set; }

        public string LeaderName { get { return _race.LeaderName; } }

        public string ImageFilename { get { return _race.ImageFilename; } }

        private Race _race;
        /// <summary>
        /// A copy of this player's race.
        /// </summary>
        public Race Race {
            get { return new Race(_race); } // race instance cannot be modified as I only return a copy
            private set { _race = value; }
        }

        public GameColor Color { get { return _race.Color; } }

        private IDictionary<Player, DiplomaticRelationship> _diplomaticRelationship = new Dictionary<Player, DiplomaticRelationship>();

        /// <summary>
        /// Copy Constructor.
        /// </summary>
        /// <param name="player">The player to copy.</param>
        public Player(Player player) : this(player.Race, player.IQ, player.IsUser) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Player" /> class.
        /// </summary>
        /// <param name="race">The race.</param>
        /// <param name="iq">The iq.</param>
        /// <param name="isUser">if set to <c>true</c> this player is the human user.</param>
        public Player(Race race, IQ iq, bool isUser = false) {
            Race = race;
            IQ = iq;
            IsUser = isUser;
            _diplomaticRelationship[this] = DiplomaticRelationship.Self;    // assigning relations this way allows NoPlayer to make SetRelations illegal
        }

        public DiplomaticRelationship GetRelations(Player player) {
            if (!_diplomaticRelationship.ContainsKey(player)) {
                return DiplomaticRelationship.None;
            }
            return _diplomaticRelationship[player];
        }

        public virtual void SetRelations(Player player, DiplomaticRelationship relationship) {
            if (player == this) {
                D.Assert(relationship == DiplomaticRelationship.Self);
            }
            _diplomaticRelationship[player] = relationship;
            // TODO send DiploRelationsChange event
        }

        public bool IsRelationship(Player player, params DiplomaticRelationship[] relations) {
            return GetRelations(player).EqualsAnyOf(relations);
        }

        public bool IsEnemyOf(Player player) {
            return IsRelationship(player, DiplomaticRelationship.War, DiplomaticRelationship.ColdWar);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

