// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ItemDeathEvent.cs
// Event indicating an item in the game has died. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    /// <summary>
    /// Event indicating an item in the game has died. 
    /// </summary>
    public class ItemDeathEvent : AGameEvent {

        public ItemDeathEvent(object source)
            : base(source) {
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

