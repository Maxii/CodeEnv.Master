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

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Text;
    using CodeEnv.Master.Common;

    /// <summary>
    /// A mutable class that holds all the current values of a specific race
    /// in a game instance.
    /// </summary>
    public class Race {

        public Species Species { get; private set; }

        public string LeaderName { get; set; }

        public string Description { get; set; }

        public GameColor Color { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Race" /> class for testing.
        /// </summary>
        /// <param name="species">Species of the race.</param>
        /// <param name="color">Optional color.</param>
        public Race(Species species, GameColor color = default(GameColor)) {
            Species = species;
            LeaderName = species.GetName() + " Leader";
            Description = species.GetDescription();
            Color = (color != default(GameColor)) ? color : RandomExtended<GameColor>.Choice(Enums<GameColor>.GetValues()
                .Except(default(GameColor), GameColor.Black, GameColor.Clear));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Race"/> class.
        /// </summary>
        /// <param name="stat">The stat.</param>
        public Race(RaceStat stat) {
            Species = stat.Species;
            LeaderName = stat.LeaderName;
            Description = stat.Description;
            Color = stat.Color;
        }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        /// <param name="race">The race to copy.</param>
        public Race(Race race) {
            Species = race.Species;
            LeaderName = race.LeaderName;
            Description = race.Description;
            Color = race.Color;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

