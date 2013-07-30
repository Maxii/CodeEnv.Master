// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Race.cs
// A mutable class that holds all the current values of a specific race
// in a game instance.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// A mutable class that holds all the current values of a specific race
    /// in a game instance.
    /// </summary>
    public class Race {

        public Races RaceType { get; private set; }

        public string LeaderName { get; private set; }

        public StringBuilder Description { get; private set; }

        public GameColor Color { get; private set; }

        //private IList<Trait> _traits;
        //public IList<Trait> Traits { get { return _traits; } }     // return an unmodifialbe list - ArrayList.ReadOnly?

        public Race(RaceStat stats) {
            RaceType = stats.Race;
            LeaderName = stats.LeaderName;
            Description = stats.Description;
            Color = stats.Color;
            //Traits = stats.Traits;
        }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        /// <param name="race">The race to copy.</param>
        public Race(Race race) {
            RaceType = race.RaceType;
            LeaderName = race.LeaderName;
            Description = race.Description;
            Color = race.Color;
            // _traits = new List<Trait>();
            // race.Traits.ForAll<Trait>(t => _traits.Add(t));
        }

        public void AddTrait() {
            // UNDONE
            throw new NotImplementedException();
        }

        public void RemoveTrait() {
            // UNDONE
            throw new NotImplementedException();
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

