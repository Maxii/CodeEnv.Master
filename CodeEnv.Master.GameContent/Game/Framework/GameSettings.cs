// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Settings.cs
// Data container class holding all settings for a new or loaded game.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Data container class holding all settings for a new or loaded game.
    /// </summary>
    public class GameSettings {

        //******************** Values for PlayerPrefsManager *******************
        public UniverseSizeGuiSelection UniverseSizeSelection { get; set; }
        public SpeciesGuiSelection UserPlayerSpeciesSelection { get; set; }
        public GameColor UserPlayerColor { get; set; }


        //******************** Values for GameManager *******************
        public bool __IsStartupSimulation { get; set; }
        public bool IsSavedGame { get; set; }
        public UniverseSize UniverseSize { get; set; }
        public Race UserPlayerRace { get; set; }
        public Race[] AIPlayerRaces { get; set; }

        public GameSettings() { }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }


    }
}

