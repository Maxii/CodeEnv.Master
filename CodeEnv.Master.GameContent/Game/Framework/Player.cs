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

    using System;
    using System.Collections.Generic;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;

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

        public bool IsPlayer { get; private set; }

        public IQ IQ { get; private set; }

        public string LeaderName { get { return _race.LeaderName; } }

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
        /// Initializes a new random instance of the <see cref="Player"/> class for testing. Excludes Humans.
        /// </summary>
        public Player()
            : this(new Race(RandomExtended<Species>.Choice(Enums<Species>.GetValues().Except(Species.None, Species.Human))),
                Enums<IQ>.GetRandom(excludeDefault: true)) { }

        public Player(Race race, IQ iq, bool isPlayer = false) {
            Race = race;
            IQ = iq;
            IsPlayer = isPlayer;
            _diplomaticRelationship[this] = DiplomaticRelationship.Self;    // assigning relations this way allows NoPlayer to make SetRelations illegal
        }

        /// <summary>
        /// Copy Constructor.
        /// </summary>
        /// <param name="player">The player to copy.</param>
        public Player(Player player) : this(player.Race, player.IQ, player.IsPlayer) { }

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

