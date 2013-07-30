// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: RaceStat.cs
// An immutable data class that holds all the externally
// acquired values associated with a Race.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using System.Text;

    /// <summary>
    /// An immutable data class that holds all the externally
    /// acquired values associated with a Race.
    /// </summary>
    public class RaceStat {

        public Races Race { get; private set; }

        public string LeaderName { get; private set; }

        public StringBuilder Description { get; private set; }

        public GameColor Color { get; private set; }

        // TODO some holder of traits here


        /// <summary>
        /// Initializes a new instance of the <see cref="RaceStat"/> class for testing.
        /// </summary>
        public RaceStat()
            : this(Races.Human, "Maxii", new StringBuilder("... of Maxiiland"), GameColor.Blue) {
        }

        public RaceStat(Races race, string leaderName, StringBuilder description, GameColor color) {
            Race = race;
            LeaderName = leaderName;
            Description = description;
            Color = color;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

