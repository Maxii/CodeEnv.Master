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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Data container for new game preference settings.
    /// </summary>
    public class NewGamePreferenceSettings {

        public UniverseSizeGuiSelection UniverseSizeSelection { get; set; }
        public int PlayerCount { get; set; }

        public SpeciesGuiSelection UserPlayerSpeciesSelection { get; set; }
        public SpeciesGuiSelection[] AIPlayerSpeciesSelections { get; set; }

        public GameColor UserPlayerColor { get; set; }
        public GameColor[] AIPlayerColors { get; set; }

        public IQ[] AIPlayerIQs { get; set; }

        public NewGamePreferenceSettings() { }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

