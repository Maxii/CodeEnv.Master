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
    public class GuiPauseRequestEvent : AEnumValueChangeEvent<PauseRequest> {

        public GuiPauseRequestEvent(object source, PauseRequest pauseRequest)
            : base(source, pauseRequest) {
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }
}

