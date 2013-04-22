// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ElementReadyEvent.cs
// COMMENT - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {

    /// <summary>
    /// Event indicating a game elements readiness to Run. IsReady = false
    /// indicates unreadiness and is a request to be recorded as such. IsReady=true
    /// indicates readiness.
    /// </summary>
    public class ElementReadyEvent : AGameEvent {

        public bool IsReady { get; private set; }

        public ElementReadyEvent(object source, bool isReady)
            : base(source) {
            IsReady = isReady;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

