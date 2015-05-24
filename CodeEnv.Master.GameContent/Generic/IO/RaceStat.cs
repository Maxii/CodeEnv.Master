// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: RaceStat.cs
// An immutable struct that holds all the externally acquired values for a Race.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Text;
    using CodeEnv.Master.Common;

    /// <summary>
    /// An immutable struct that holds all the externally acquired values for a Race.
    /// </summary>
    public struct RaceStat {

        public Species Species { get; private set; }

        public string LeaderName { get; private set; }

        public string ImageFilename { get; private set; }

        public string Description { get; private set; }

        public GameColor Color { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RaceStat"/> struct.
        /// </summary>
        /// <param name="species">The species.</param>
        /// <param name="leaderName">Name of the race leader.</param>
        /// <param name="imageFilename">The filename used to find the image texture in an atlas for this race.</param>
        /// <param name="description">The race description.</param>
        /// <param name="color">The race color.</param>
        public RaceStat(Species species, string leaderName, string imageFilename, string description, GameColor color)
            : this() {
            Species = species;
            LeaderName = leaderName;
            ImageFilename = imageFilename;
            Description = description;
            Color = color;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

