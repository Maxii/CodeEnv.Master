// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: LeaderStat.cs
//  Immutable struct containing externally acquirable values for Leaders.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable struct containing externally acquirable values for Leaders.
    /// </summary>
    public struct LeaderStat {

        public string Name { get; private set; }

        public AtlasID ImageAtlasID { get; private set; }

        public string ImageFilename { get; private set; }

        public LeaderStat(string name, AtlasID imageAtlasID, string imageFilename)
            : this() {
            Name = name;
            ImageAtlasID = imageAtlasID;
            ImageFilename = imageFilename;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

