// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GamePlayOptionsAcceptedEvent.cs
// Event indicating the user has pushed the Accept button on the GamePlayOptions Menu. The event
// contains all the current values of the options on the menu whether they were changed or not.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {

    /// <summary>
    /// Event indicating the user has pushed the Accept button on the GamePlayOptions Menu. The event
    /// contains all the current values of the options on the menu whether they were changed or not.
    /// </summary>
    public class GamePlayOptionsAcceptedEvent : AGameEvent {

        public OptionSettings Settings { get; private set; }

        public GamePlayOptionsAcceptedEvent(object source, OptionSettings settings)
            : base(source) {
            Settings = settings;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

