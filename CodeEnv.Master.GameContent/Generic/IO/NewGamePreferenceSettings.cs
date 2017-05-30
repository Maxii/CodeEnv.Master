// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: NewGamePreferenceSettings.cs
// Data container for new game preference settings.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Data container for new game preference settings.
    /// </summary>
    public class NewGamePreferenceSettings {

        public string DebugName { get { return GetType().Name; } }

        public UniverseSizeGuiSelection UniverseSizeSelection { get; set; }

        public UniverseSize UniverseSize { get; set; }  // 10.4.16 Reqd to know which Pref Property gets PlayerCount
        public int PlayerCount { get; set; }

        public SystemDensityGuiSelection SystemDensitySelection { get; set; }

        public SpeciesGuiSelection UserPlayerSpeciesSelection { get; set; }
        public SpeciesGuiSelection[] AIPlayerSpeciesSelections { get; set; }

        public GameColor UserPlayerColor { get; set; }
        public GameColor[] AIPlayerColors { get; set; }

        public IQ[] AIPlayerIQs { get; set; }

        public TeamID UserPlayerTeam { get; set; }
        public TeamID[] AIPlayersTeams { get; set; }

        public EmpireStartLevelGuiSelection UserPlayerStartLevelSelection { get; set; }
        public EmpireStartLevelGuiSelection[] AIPlayersStartLevelSelections { get; set; }

        public SystemDesirabilityGuiSelection UserPlayerHomeDesirabilitySelection { get; set; }
        public SystemDesirabilityGuiSelection[] AIPlayersHomeDesirabilitySelections { get; set; }

        public PlayerSeparationGuiSelection[] AIPlayersUserSeparationSelections { get; set; }

        public NewGamePreferenceSettings() { }

        public override string ToString() {
            return DebugName;
        }

    }
}

