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

namespace CodeEnv.Master.Common.Unity {

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

