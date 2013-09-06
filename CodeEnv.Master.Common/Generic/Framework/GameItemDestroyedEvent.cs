// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GameItemDestroyedEvent.cs
// Event indicating an item of play (source) in the game will shortly be destroyed. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    /// <summary>
    /// Event indicating an item of play (source) in the game will shortly be destroyed. This event
    /// is not raised when objects are destroyed in scene transitions or when the application is quiting.
    /// </summary>
    public class GameItemDestroyedEvent : AGameEvent {

        public GameItemDestroyedEvent(object source) : base(source) { }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

