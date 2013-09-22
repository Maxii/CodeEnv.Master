// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: BuildNewGameEvent.cs
// Event indicating a new game should be built using the attached settings.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    ///  Event indicating a new game should be built using the attached settings.
    /// </summary>
    public class BuildNewGameEvent : AGameEvent {

        public GameSettings Settings { get; private set; }

        public BuildNewGameEvent(object source, GameSettings settings)
            : base(source) {
            Settings = settings;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

