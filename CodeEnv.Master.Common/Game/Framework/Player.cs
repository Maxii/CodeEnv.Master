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

namespace CodeEnv.Master.Common {

    using System.Collections.Generic;

    /// <summary>
    /// Instantiable base class for a player.
    /// </summary>
    public class Player : APropertyChangeTracking, IPlayer {

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

        private IDictionary<IPlayer, DiplomaticRelations> _diplomaticRelations;

        public Player(Race race, IQ iq) {
            _race = race;
            IQ = iq;
            IsActive = true;
        }

        public DiplomaticRelations GetRelations(IPlayer player) {
            return _diplomaticRelations[player];
        }

        public void SetRelations(IPlayer player, DiplomaticRelations relation) {
            _diplomaticRelations[player] = relation;
            // TODO send DiploRelationsChange event
        }

        public bool IsRelationship(IPlayer player, DiplomaticRelations relation) {
            return _diplomaticRelations[player] == relation;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

