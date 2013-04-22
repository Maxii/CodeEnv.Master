// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GamePauseStateChangedEvent.cs
// Event indicating the GamePauseState has changed.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {

    /// <summary>
    /// Event indicating the GamePauseState has changed.
    /// </summary>
    public class GamePauseStateChangedEvent : AGameEvent {

        public GamePauseState PauseState { get; private set; }

        public GamePauseStateChangedEvent(object source, GamePauseState pauseState)
            : base(source) {
            PauseState = pauseState;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

