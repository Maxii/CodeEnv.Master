// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiPauseRequestEvent.cs
// Event containing a PauseRequest which REQUESTS a GamePauseEvent from GameManager.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {

    /// <summary>
    /// Event containing a PauseRequest which REQUESTS a GamePauseEvent from GameManager.
    /// </summary>
    public class GuiPauseRequestEvent : AGameEvent {

        public PauseRequest PauseRequest { get; private set; }

        public GuiPauseRequestEvent(object source, PauseRequest pauseRequest)
            : base(source) {
            PauseRequest = pauseRequest;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }
}

