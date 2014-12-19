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
    public class Player : APropertyChangeTracking, IPlayer {

        private IDictionary<IPlayer, DiplomaticRelationship> _diplomaticRelationship;

        /// <summary>
        /// Initializes a new random instance of the <see cref="Player"/> class for testing. Excludes Humans.
        /// </summary>
        public Player()
            : this(new Race(RandomExtended<Species>.Choice(Enums<Species>.GetValues().Except(Species.None, Species.Human))),
                Enums<IQ>.GetRandom(excludeDefault: true)) { }

        public Player(Race race, IQ iq) {
            _race = race;
            IQ = iq;
            IsActive = true;
            _diplomaticRelationship = new Dictionary<IPlayer, DiplomaticRelationship>();
            _diplomaticRelationship[this] = DiplomaticRelationship.Self;  // assigning relations this way allows NoPlayer to make SetRelations illegal
            //SetRelations(this, DiplomaticRelations.Self);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region IPlayer Members

        private bool _isActive;
        public bool IsActive {  // accomodates an AIPlayer being eliminated in the game
            get { return _isActive; }
            set { SetProperty<bool>(ref _isActive, value, "IsActive"); }
        }

        public virtual bool IsPlayer { get { return false; } }

        public IQ IQ { get; private set; }

        public virtual string LeaderName { get { return _race.LeaderName; } }

        private Race _race;
        public Race Race {
            get { return new Race(_race); } // race instance cannot be modified as I only return a copy
            private set { _race = value; }
        }

        public virtual GameColor Color { get { return _race.Color; } }

        public DiplomaticRelationship GetRelations(IPlayer player) {
            if (!_diplomaticRelationship.ContainsKey(player)) {
                return DiplomaticRelationship.None;
            }
            return _diplomaticRelationship[player];
        }

        public virtual void SetRelations(IPlayer player, DiplomaticRelationship relationship) {
            if (player == this) {
                D.Assert(relationship == DiplomaticRelationship.Self);
            }
            _diplomaticRelationship[player] = relationship;
            // TODO send DiploRelationsChange event
        }

        public bool IsRelationship(IPlayer player, params DiplomaticRelationship[] relations) {
            return GetRelations(player).EqualsAnyOf(relations);
        }

        public bool IsEnemyOf(IPlayer player) {
            return IsRelationship(player, DiplomaticRelationship.War, DiplomaticRelationship.ColdWar);
        }

        #endregion

    }
}

