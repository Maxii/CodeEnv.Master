// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GraphicsOptionsAcceptedEvent.cs
// Event indicating the user has pushed the Accept button on the GraphicsOptionsMenu.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {


    /// <summary>
    /// Event indicating the user has pushed the Accept button on the GraphicsOptionsMenu. The event
    /// contains all the current values of the options on the menu whether they were changed or not.
    /// </summary>
    public class GraphicsOptionsAcceptedEvent : AGameEvent {

        public GraphicsOptionSettings Settings { get; private set; }

        public GraphicsOptionsAcceptedEvent(object source, GraphicsOptionSettings settings)
            : base(source) {
            Settings = settings;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

