// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ElementReadyEvent.cs
// Event indicating a game elements readiness to Run. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {

    /// <summary>
    /// Event indicating a game elements readiness to progress beyond a designated game state. 
    /// IsReady = false indicates unreadiness and is a request to be recorded as such. IsReady=true
    /// indicates readiness to progress beyond the designated game state.
    /// </summary>
    [System.Obsolete]
    public class ElementReadyEvent : AGameEvent {

        /// <summary>
        /// The most advanced game state allowed until this element is ready.
        /// </summary>
        public GameState MaxGameStateUntilReady { get; private set; }

        public bool IsReady { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ElementReadyEvent"/> class.
        /// </summary>
        /// <param name="source">The source MonoBehaviour</param>
        /// <param name="maxGameStateUntilReady">The most advanced game state allowed until this element is ready.</param>
        /// <param name="isReady">if set to <c>true</c> [is ready].</param>
        public ElementReadyEvent(object source, GameState maxGameStateUntilReady, bool isReady)
            : base(source) {
            MaxGameStateUntilReady = maxGameStateUntilReady;
            IsReady = isReady;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

