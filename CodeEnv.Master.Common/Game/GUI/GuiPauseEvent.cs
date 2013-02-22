// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiPauseEvent.cs
// Event containing a GuiPauseCommand which REQUESTS a GamePauseEvent from GameManager.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {

    /// <summary>
    /// Event containing a GuiPauseCommand which REQUESTS a GamePauseEvent from GameManager.
    /// </summary>
    public class GuiPauseEvent : GameEvent {

        public GuiPauseCommand PauseCommand { get; private set; }

        public GuiPauseEvent(GuiPauseCommand pauseCmd) {
            PauseCommand = pauseCmd;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }
}

