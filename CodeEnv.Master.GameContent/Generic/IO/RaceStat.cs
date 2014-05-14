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

        private string _leaderName; // can initialize as null
        public string LeaderName {
            get {
                if (_leaderName.IsNullOrEmpty()) {
                    return string.Empty;
                }
                return _leaderName;
            }
        }

        private string _description;    // can initialize as null
        public string Description {
            get {
                if (_description.IsNullOrEmpty()) {
                    return string.Empty;
                }
                return _description;
            }
        }

        public GameColor Color { get; private set; }

        public RaceStat(Species species, string leaderName, string description, GameColor color)
            : this() {
            Species = species;
            _leaderName = leaderName;
            _description = description;
            Color = color;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

