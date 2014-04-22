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

        private IDictionary<IPlayer, DiplomaticRelations> _diplomaticRelations;

        /// <summary>
        /// Initializes a new random instance of the <see cref="Player"/> class for testing. Excludes Humans.
        /// </summary>
        public Player()
            : this(new Race(RandomExtended<Races>.Choice(Enums<Races>.GetValues().Except(Races.None, Races.Human))),
                Enums<IQ>.GetRandom(excludeDefault: true)) { }

        public Player(Race race, IQ iq) {
            _race = race;
            IQ = iq;
            IsActive = true;
            _diplomaticRelations = new Dictionary<IPlayer, DiplomaticRelations>();
            _diplomaticRelations[this] = DiplomaticRelations.Self;  // assigning relations this way allows NoPlayer to make SetRelations illegal
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

        public virtual bool IsHuman { get { return false; } }

        public IQ IQ { get; private set; }

        public virtual string LeaderName { get { return _race.LeaderName; } }

        private Race _race;
        public Race Race {
            get { return new Race(_race); } // race instance cannot be modified as I only return a copy
            private set { _race = value; }
        }

        public virtual GameColor Color { get { return _race.Color; } }

        public DiplomaticRelations GetRelations(IPlayer player) {
            if (!_diplomaticRelations.ContainsKey(player)) {
                return DiplomaticRelations.None;
            }
            return _diplomaticRelations[player];
        }

        public virtual void SetRelations(IPlayer player, DiplomaticRelations relation) {
            if (player == this) {
                D.Assert(relation == DiplomaticRelations.Self);
            }
            _diplomaticRelations[player] = relation;
            // TODO send DiploRelationsChange event
        }

        public bool IsRelationship(IPlayer player, DiplomaticRelations relation) {
            return GetRelations(player) == relation;
        }

        public bool IsEnemyOf(IPlayer player) {
            return IsRelationship(player, DiplomaticRelations.Enemy);
        }

        #endregion
    }
}

